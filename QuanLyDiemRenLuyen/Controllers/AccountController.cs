using System;
using System.Data;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;
using QuanLyDiemRenLuyen.Services;

namespace QuanLyDiemRenLuyen.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Kiểm tra user trong database
                string query = @"SELECT MAND, EMAIL, FULL_NAME, AVATAR_URL, ROLE_NAME, 
                                PASSWORD_HASH, PASSWORD_SALT, IS_ACTIVE, FAILED_LOGIN_COUNT, 
                                LOCKOUT_END_UTC 
                                FROM USERS 
                                WHERE EMAIL = :Email";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("Email", OracleDbType.Varchar2, model.Email)
                };

                DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

                if (dt.Rows.Count == 0)
                {
                    ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                    return View(model);
                }

                DataRow row = dt.Rows[0];

                // Kiểm tra tài khoản có bị khóa không
                bool isActive = Convert.ToInt32(row["IS_ACTIVE"]) == 1;
                if (!isActive)
                {
                    ModelState.AddModelError("", "Tài khoản đã bị vô hiệu hóa");
                    return View(model);
                }

                // Kiểm tra lockout
                if (row["LOCKOUT_END_UTC"] != DBNull.Value)
                {
                    DateTime lockoutEnd = Convert.ToDateTime(row["LOCKOUT_END_UTC"]);
                    if (lockoutEnd > DateTime.UtcNow)
                    {
                        ModelState.AddModelError("", "Tài khoản đang bị khóa. Vui lòng thử lại sau");
                        return View(model);
                    }
                }

                // Xác thực mật khẩu
                string passwordHash = row["PASSWORD_HASH"].ToString();
                string passwordSalt = row["PASSWORD_SALT"].ToString();

                if (!PasswordHelper.VerifyPassword(model.Password, passwordSalt, passwordHash))
                {
                    // Tăng failed login count
                    int failedCount = Convert.ToInt32(row["FAILED_LOGIN_COUNT"]) + 1;
                    string updateQuery = @"UPDATE USERS 
                                         SET FAILED_LOGIN_COUNT = :FailedCount,
                                             LOCKOUT_END_UTC = :LockoutEnd
                                         WHERE EMAIL = :Email";

                    DateTime? lockoutEnd = null;
                    if (failedCount >= 5)
                    {
                        lockoutEnd = DateTime.UtcNow.AddMinutes(15);
                    }

                    var updateParams = new[]
                    {
                        OracleDbHelper.CreateParameter("FailedCount", OracleDbType.Int32, failedCount),
                        OracleDbHelper.CreateParameter("LockoutEnd", OracleDbType.TimeStamp, (object)lockoutEnd ?? DBNull.Value),
                        OracleDbHelper.CreateParameter("Email", OracleDbType.Varchar2, model.Email)
                    };

                    OracleDbHelper.ExecuteNonQuery(updateQuery, updateParams);

                    ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                    return View(model);
                }

                // Đăng nhập thành công - Reset failed login count
                string resetQuery = @"UPDATE USERS 
                                    SET FAILED_LOGIN_COUNT = 0, LOCKOUT_END_UTC = NULL 
                                    WHERE EMAIL = :Email";
                OracleDbHelper.ExecuteNonQuery(resetQuery, new[]
                {
                    OracleDbHelper.CreateParameter("Email", OracleDbType.Varchar2, model.Email)
                });

                // Tạo authentication ticket
                string mand = row["MAND"].ToString();
                string fullName = row["FULL_NAME"].ToString();
                string roleName = row["ROLE_NAME"].ToString();

                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                    1,
                    mand,
                    DateTime.Now,
                    DateTime.Now.AddMinutes(model.RememberMe ? 43200 : 30), // 30 days or 30 minutes
                    model.RememberMe,
                    roleName,
                    FormsAuthentication.FormsCookiePath
                );

                string encryptedTicket = FormsAuthentication.Encrypt(ticket);
                HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                Response.Cookies.Add(authCookie);

                // Lưu thông tin vào Session
                Session["MAND"] = mand;
                Session["FullName"] = fullName;
                Session["RoleName"] = roleName;
                Session["Email"] = model.Email;

                // Log login thành công với encrypted details
                try
                {
                    var auditService = new AuditCryptoService();
                    string clientIp = Request.UserHostAddress;
                    string userAgent = Request.UserAgent;
                    auditService.LogLogin(mand, clientIp, userAgent, success: true);
                }
                catch { /* Không fail nếu audit lỗi */ }

                // ========== SESSION TOKEN (lưu vào database) ==========
                try
                {
                    var sessionTokenService = new SessionTokenService();
                    var sessionInfo = sessionTokenService.CreateSession(mand);
                    if (sessionInfo != null)
                    {
                        Session["SessionToken"] = sessionInfo.Token;
                    }
                }
                catch { /* Không fail nếu session token lỗi */ }

                // ========== SECURITY CONTEXT INTEGRATION ==========
                // Set VPD Context, OLS Session Label, and Audit Context
                try
                {
                    string clientIpForContext = Request.UserHostAddress;
                    string sessionId = Session.SessionID;
                    
                    // Set all security contexts (VPD, OLS, Audit)
                    SecurityContextService.SetAllSecurityContexts(
                        userId: mand,
                        roleName: roleName,
                        sessionId: sessionId,
                        clientIp: clientIpForContext
                    );
                }
                catch { /* Không fail nếu security context lỗi - VPD/OLS có thể chưa cài đặt */ }

                // Redirect theo role
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                else if (roleName == "STUDENT")
                {
                    return RedirectToAction("Dashboard", "Student");
                }
                else if (roleName == "ADMIN")
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else if (roleName == "LECTURER")
                {
                    return Redirect("/Lecturer/Dashboard");
                }
                else
                {
                    return RedirectToAction("Home", "Home");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi: " + ex.Message);
                return View(model);
            }
        }

        // GET: Account/Register (Đã vô hiệu hóa - chỉ Admin được tạo tài khoản)
        [AllowAnonymous]
        public ActionResult Register()
        {
            TempData["InfoMessage"] = "Chức năng đăng ký đã bị vô hiệu hóa. Vui lòng liên hệ Quản trị viên để được cấp tài khoản.";
            return RedirectToAction("Login");
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            // ========== CLEAR SESSION TOKEN ==========
            // Clear session token from database before logout
            try
            {
                var mand = Session["MAND"]?.ToString();
                if (!string.IsNullOrEmpty(mand))
                {
                    var sessionTokenService = new SessionTokenService();
                    sessionTokenService.ClearSessionToken(mand);
                }
            }
            catch { /* Không fail nếu clear session token lỗi */ }

            // ========== CLEAR SECURITY CONTEXTS ==========
            // Clear VPD Context, OLS, and Audit Context before logout
            try
            {
                SecurityContextService.ClearAllSecurityContexts();
            }
            catch { /* Không fail nếu clear context lỗi */ }

            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Account");
        }

        // ==================== FORGOT PASSWORD ====================

        // GET: Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Kiểm tra email có tồn tại không
                string checkQuery = "SELECT MAND, FULL_NAME FROM USERS WHERE EMAIL = :Email AND IS_ACTIVE = 1";
                var checkParams = new[] { OracleDbHelper.CreateParameter("Email", OracleDbType.Varchar2, model.Email) };
                DataTable dt = OracleDbHelper.ExecuteQuery(checkQuery, checkParams);

                if (dt.Rows.Count == 0)
                {
                    ModelState.AddModelError("Email", "Email không tồn tại trong hệ thống");
                    return View(model);
                }

                string mand = dt.Rows[0]["MAND"].ToString();
                string fullName = dt.Rows[0]["FULL_NAME"].ToString();

                // Tạo mã reset password (6 ký tự)
                string resetCode = GenerateResetCode();

                // Lưu mã reset vào database PASSWORD_RESET_TOKENS
                string insertQuery = @"INSERT INTO PASSWORD_RESET_TOKENS (ID, EMAIL, TOKEN, CREATED_AT_UTC, EXPIRES_AT_UTC, IS_USED)
                                      VALUES (RAWTOHEX(SYS_GUID()), :Email, :Token, SYS_EXTRACT_UTC(SYSTIMESTAMP), SYS_EXTRACT_UTC(SYSTIMESTAMP) + INTERVAL '30' MINUTE, 0)";
                var insertParams = new[]
                {
                    OracleDbHelper.CreateParameter("Email", OracleDbType.Varchar2, model.Email),
                    OracleDbHelper.CreateParameter("Token", OracleDbType.Varchar2, resetCode)
                };

                OracleDbHelper.ExecuteNonQuery(insertQuery, insertParams);

                // Trong thực tế, bạn nên gửi email chứa mã reset
                // Ở đây tôi sẽ hiển thị mã trực tiếp (chỉ để demo)
                TempData["ResetCode"] = resetCode;
                TempData["Email"] = model.Email;
                TempData["SuccessMessage"] = $"Mã xác nhận đã được tạo: {resetCode}. Vui lòng sử dụng mã này để đặt lại mật khẩu.";

                return RedirectToAction("ResetPassword");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View(model);
            }
        }

        // GET: Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword()
        {
            var model = new ResetPasswordViewModel();
            if (TempData["Email"] != null)
            {
                model.Email = TempData["Email"].ToString();
                ViewBag.ResetCode = TempData["ResetCode"];
            }
            return View(model);
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Kiểm tra mã reset
                string checkQuery = @"SELECT ID, EMAIL, EXPIRES_AT_UTC, IS_USED,
                                      CASE WHEN EXPIRES_AT_UTC > SYS_EXTRACT_UTC(SYSTIMESTAMP) THEN 1 ELSE 0 END AS IS_VALID
                                     FROM PASSWORD_RESET_TOKENS
                                     WHERE EMAIL = :Email
                                     AND TOKEN = :Token
                                     AND IS_USED = 0
                                     ORDER BY CREATED_AT_UTC DESC
                                     FETCH FIRST 1 ROWS ONLY";

                var checkParams = new[]
                {
                    OracleDbHelper.CreateParameter("Email", OracleDbType.Varchar2, model.Email),
                    OracleDbHelper.CreateParameter("Token", OracleDbType.Varchar2, model.ResetCode)
                };

                DataTable dt = OracleDbHelper.ExecuteQuery(checkQuery, checkParams);

                if (dt.Rows.Count == 0)
                {
                    ModelState.AddModelError("ResetCode", "Mã xác nhận không hợp lệ");
                    return View(model);
                }

                DataRow row = dt.Rows[0];
                string resetId = row["ID"].ToString();
                string email = row["EMAIL"].ToString();
                int isValid = Convert.ToInt32(row["IS_VALID"]);

                // Kiểm tra mã đã hết hạn chưa (dựa trên kết quả từ Oracle)
                if (isValid == 0)
                {
                    ModelState.AddModelError("ResetCode", "Mã xác nhận đã hết hạn");
                    return View(model);
                }

                // Tạo salt mới và hash mật khẩu mới
                string newSalt = PasswordHelper.GenerateSalt();
                string newPasswordHash = PasswordHelper.HashPassword(model.NewPassword, newSalt);

                // Cập nhật mật khẩu
                string updateQuery = @"UPDATE USERS
                                      SET PASSWORD_HASH = :PasswordHash,
                                          PASSWORD_SALT = :PasswordSalt,
                                          UPDATED_AT = SYSTIMESTAMP
                                      WHERE EMAIL = :Email";
                var updateParams = new[]
                {
                    OracleDbHelper.CreateParameter("PasswordHash", OracleDbType.Varchar2, newPasswordHash),
                    OracleDbHelper.CreateParameter("PasswordSalt", OracleDbType.Varchar2, newSalt),
                    OracleDbHelper.CreateParameter("Email", OracleDbType.Varchar2, email)
                };

                OracleDbHelper.ExecuteNonQuery(updateQuery, updateParams);

                // Đánh dấu mã reset đã được sử dụng
                string markUsedQuery = "UPDATE PASSWORD_RESET_TOKENS SET IS_USED = 1 WHERE ID = :ResetId";
                var markUsedParams = new[] { OracleDbHelper.CreateParameter("ResetId", OracleDbType.Varchar2, resetId) };
                OracleDbHelper.ExecuteNonQuery(markUsedQuery, markUsedParams);

                TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View(model);
            }
        }

        // Helper method để tạo mã reset
        private string GenerateResetCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}

