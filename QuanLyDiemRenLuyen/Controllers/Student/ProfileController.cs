using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;
using QuanLyDiemRenLuyen.Services;

namespace QuanLyDiemRenLuyen.Controllers.Student
{
    /// <summary>
    /// Controller xử lý Profile và thay đổi mật khẩu của sinh viên
    /// </summary>
    public class ProfileController : StudentBaseController
    {
        // GET: Student/Profile
        public ActionResult Index()
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                string mand = GetCurrentStudentId();
                var viewModel = GetStudentProfile(mand);
                return View("~/Views/Student/Profile.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Student/Profile.cshtml", new StudentProfileViewModel());
            }
        }

        // GET: Student/Profile/Edit
        public ActionResult Edit()
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                string mand = GetCurrentStudentId();
                var viewModel = GetEditProfileData(mand);
                return View("~/Views/Student/EditProfile.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Student/Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EditStudentProfileViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("~/Views/Student/EditProfile.cshtml", model);
                }

                string mand = GetCurrentStudentId();
                if (string.IsNullOrEmpty(mand) || mand != model.MAND)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Cập nhật thông tin người dùng
                string updateUserQuery = @"UPDATE USERS
                                          SET EMAIL = :Email,
                                              FULL_NAME = :FullName,
                                              AVATAR_URL = :AvatarUrl,
                                              UPDATED_AT = SYSTIMESTAMP
                                          WHERE MAND = :MAND";

                var userParams = new[]
                {
                    OracleDbHelper.CreateParameter("Email", OracleDbType.Varchar2, model.Email),
                    OracleDbHelper.CreateParameter("FullName", OracleDbType.Varchar2, model.FullName),
                    OracleDbHelper.CreateParameter("AvatarUrl", OracleDbType.Varchar2,
                        string.IsNullOrEmpty(model.AvatarUrl) ? (object)DBNull.Value : model.AvatarUrl),
                    OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand)
                };

                OracleDbHelper.ExecuteNonQuery(updateUserQuery, userParams);

                // ⭐ MÃ HÓA thông tin nhạy cảm bằng RSA
                string encryptedPhone = null;
                string encryptedAddress = null;
                string encryptedIdCard = null;

                if (!string.IsNullOrEmpty(model.Phone))
                {
                    encryptedPhone = SensitiveDataService.EncryptStudentPhone(model.Phone);
                }
                if (!string.IsNullOrEmpty(model.Address))
                {
                    encryptedAddress = SensitiveDataService.EncryptStudentAddress(model.Address);
                }
                if (!string.IsNullOrEmpty(model.IdCardNumber))
                {
                    encryptedIdCard = SensitiveDataService.EncryptIdCard(model.IdCardNumber);
                }

                // Lấy encryption key ID
                string keyId = GetCurrentEncryptionKeyId();

                // Cập nhật thông tin sinh viên (VỪA plaintext VỪA encrypted để backward compatibility)
                string updateStudentQuery = @"UPDATE STUDENTS
                                             SET PHONE = :Phone,
                                                 DATE_OF_BIRTH = :DateOfBirth,
                                                 GENDER = :Gender,
                                                 ADDRESS = :Address,
                                                 ID_CARD_NUMBER = :IdCard,
                                                 PHONE_ENCRYPTED = :PhoneEnc,
                                                 ADDRESS_ENCRYPTED = :AddressEnc,
                                                 ID_CARD_ENCRYPTED = :IdCardEnc,
                                                 ENCRYPTED_AT = SYSTIMESTAMP,
                                                 ENCRYPTION_KEY_ID = :KeyId
                                             WHERE USER_ID = :MAND";

                var studentParams = new[]
                {
                    // Plaintext (backward compatibility)
                    OracleDbHelper.CreateParameter("Phone", OracleDbType.Varchar2,
                        string.IsNullOrEmpty(model.Phone) ? (object)DBNull.Value : model.Phone),
                    OracleDbHelper.CreateParameter("DateOfBirth", OracleDbType.Date,
                        model.DateOfBirth.HasValue ? (object)model.DateOfBirth.Value : DBNull.Value),
                    OracleDbHelper.CreateParameter("Gender", OracleDbType.Varchar2,
                        string.IsNullOrEmpty(model.Gender) ? (object)DBNull.Value : model.Gender),
                    OracleDbHelper.CreateParameter("Address", OracleDbType.Varchar2,
                        string.IsNullOrEmpty(model.Address) ? (object)DBNull.Value : model.Address),
                    OracleDbHelper.CreateParameter("IdCard", OracleDbType.Varchar2,
                        string.IsNullOrEmpty(model.IdCardNumber) ? (object)DBNull.Value : model.IdCardNumber),
                    
                    // Encrypted (RSA)
                    OracleDbHelper.CreateParameter("PhoneEnc", OracleDbType.Clob,
                        encryptedPhone != null ? (object)encryptedPhone : DBNull.Value),
                    OracleDbHelper.CreateParameter("AddressEnc", OracleDbType.Clob,
                        encryptedAddress != null ? (object)encryptedAddress : DBNull.Value),
                    OracleDbHelper.CreateParameter("IdCardEnc", OracleDbType.Clob,
                        encryptedIdCard != null ? (object)encryptedIdCard : DBNull.Value),
                    OracleDbHelper.CreateParameter("KeyId", OracleDbType.Varchar2,
                        keyId != null ? (object)keyId : DBNull.Value),
                    OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand)
                };

                OracleDbHelper.ExecuteNonQuery(updateStudentQuery, studentParams);

                // Cập nhật session nếu tên thay đổi
                Session["FullName"] = model.FullName;
                Session["Email"] = model.Email;

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công! Dữ liệu nhạy cảm đã được mã hóa RSA-2048.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi khi cập nhật: " + ex.Message;
                return View("~/Views/Student/EditProfile.cshtml", model);
            }
        }

        // GET: Student/Profile/ChangePassword
        public ActionResult ChangePassword()
        {
            return View("~/Views/Student/ChangePassword.cshtml");
        }

        // POST: Student/Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Student/ChangePassword.cshtml", model);
            }

            try
            {
                string mand = GetCurrentStudentId();
                if (string.IsNullOrEmpty(mand))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Lấy thông tin mật khẩu hiện tại
                string query = "SELECT PASSWORD_HASH, PASSWORD_SALT FROM USERS WHERE MAND = :MAND";
                var parameters = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
                DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

                if (dt.Rows.Count == 0)
                {
                    ViewBag.ErrorMessage = "Không tìm thấy thông tin người dùng";
                    return View("~/Views/Student/ChangePassword.cshtml", model);
                }

                DataRow row = dt.Rows[0];
                string currentHash = row["PASSWORD_HASH"].ToString();
                string currentSalt = row["PASSWORD_SALT"].ToString();

                // Verify mật khẩu cũ
                string oldPasswordHash = PasswordHelper.HashPassword(model.CurrentPassword, currentSalt);
                if (oldPasswordHash != currentHash)
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");
                    return View("~/Views/Student/ChangePassword.cshtml", model);
                }

                // Tạo salt mới và hash mật khẩu mới
                string newSalt = PasswordHelper.GenerateSalt();
                string newHash = PasswordHelper.HashPassword(model.NewPassword, newSalt);

                // Cập nhật mật khẩu
                string updateQuery = @"UPDATE USERS 
                                      SET PASSWORD_HASH = :NewHash, 
                                          PASSWORD_SALT = :NewSalt,
                                          UPDATED_AT = SYSTIMESTAMP
                                      WHERE MAND = :MAND";

                var updateParams = new[]
                {
                    OracleDbHelper.CreateParameter("NewHash", OracleDbType.Varchar2, newHash),
                    OracleDbHelper.CreateParameter("NewSalt", OracleDbType.Varchar2, newSalt),
                    OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, updateParams);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.ErrorMessage = "Đổi mật khẩu thất bại";
                    return View("~/Views/Student/ChangePassword.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Student/ChangePassword.cshtml", model);
            }
        }

        // GET: Student/Profile/EditSensitiveInfo
        /// <summary>
        /// Hiển thị form chỉnh sửa thông tin nhạy cảm (đã mã hóa RSA)
        /// </summary>
        public ActionResult EditSensitiveInfo()
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                string mand = GetCurrentStudentId();
                var model = GetSensitiveInfoData(mand);
                
                return View("~/Views/Student/EditSensitiveInfo.cshtml", model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Student/Profile/EditSensitiveInfo
        /// <summary>
        /// Cập nhật thông tin nhạy cảm và mã hóa RSA trước khi lưu vào database
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditSensitiveInfo(EditSensitiveInfoViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("~/Views/Student/EditSensitiveInfo.cshtml", model);
                }

                string mand = GetCurrentStudentId();
                if (string.IsNullOrEmpty(mand))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Mã hóa dữ liệu nhạy cảm bằng RSA
                string encryptedPhone = SensitiveDataService.EncryptStudentPhone(model.Phone);
                string encryptedAddress = SensitiveDataService.EncryptStudentAddress(model.Address);
                string encryptedIdCard = SensitiveDataService.EncryptIdCard(model.IdCardNumber);

                // Lấy encryption key ID
                string keyId = GetCurrentEncryptionKeyId();

                // Cập nhật vào database
                string updateQuery = @"UPDATE STUDENTS
                                      SET PHONE_ENCRYPTED = :PhoneEnc,
                                          ADDRESS_ENCRYPTED = :AddressEnc,
                                          ID_CARD_ENCRYPTED = :IdCardEnc,
                                          ENCRYPTED_AT = SYSTIMESTAMP,
                                          ENCRYPTION_KEY_ID = :KeyId
                                      WHERE USER_ID = :MAND";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("PhoneEnc", OracleDbType.Clob, encryptedPhone),
                    OracleDbHelper.CreateParameter("AddressEnc", OracleDbType.Clob, encryptedAddress),
                    OracleDbHelper.CreateParameter("IdCardEnc", OracleDbType.Clob, encryptedIdCard),
                    OracleDbHelper.CreateParameter("KeyId", OracleDbType.Varchar2, keyId),
                    OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Cập nhật thông tin nhạy cảm thành công! Dữ liệu đã được mã hóa bảo mật.";
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.ErrorMessage = "Cập nhật thất bại";
                    return View("~/Views/Student/EditSensitiveInfo.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Student/EditSensitiveInfo.cshtml", model);
            }
        }

        #region Private Helper Methods

        private StudentProfileViewModel GetStudentProfile(string mand)
        {
            var profile = new StudentProfileViewModel();

            // Lấy thông tin người dùng
            string userQuery = @"SELECT MAND, EMAIL, FULL_NAME, AVATAR_URL, ROLE_NAME, IS_ACTIVE, CREATED_AT
                                FROM USERS WHERE MAND = :MAND";
            var userParams = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
            var userTable = OracleDbHelper.ExecuteQuery(userQuery, userParams);

            if (userTable.Rows.Count > 0)
            {
                var row = userTable.Rows[0];
                profile.MAND = row["MAND"].ToString();
                profile.Email = row["EMAIL"].ToString();
                profile.FullName = row["FULL_NAME"].ToString();
                profile.AvatarUrl = row["AVATAR_URL"] != DBNull.Value ? row["AVATAR_URL"].ToString() : null;
                profile.RoleName = row["ROLE_NAME"].ToString();
                profile.IsActive = Convert.ToInt32(row["IS_ACTIVE"]) == 1;
                profile.CreatedAt = Convert.ToDateTime(row["CREATED_AT"]);
            }

            // Lấy thông tin sinh viên
            string studentQuery = @"SELECT s.STUDENT_CODE, s.PHONE, s.DATE_OF_BIRTH, s.GENDER, s.ADDRESS,
                                          c.NAME as CLASS_NAME, c.CODE as CLASS_CODE,
                                          d.NAME as DEPARTMENT_NAME, d.CODE as DEPARTMENT_CODE
                                   FROM STUDENTS s
                                   LEFT JOIN CLASSES c ON s.CLASS_ID = c.ID
                                   LEFT JOIN DEPARTMENTS d ON s.DEPARTMENT_ID = d.ID
                                   WHERE s.USER_ID = :MAND";
            var studentParams = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
            var studentTable = OracleDbHelper.ExecuteQuery(studentQuery, studentParams);

            if (studentTable.Rows.Count > 0)
            {
                var row = studentTable.Rows[0];
                profile.StudentCode = row["STUDENT_CODE"].ToString();
                profile.Phone = row["PHONE"] != DBNull.Value ? row["PHONE"].ToString() : null;
                profile.DateOfBirth = row["DATE_OF_BIRTH"] != DBNull.Value ? Convert.ToDateTime(row["DATE_OF_BIRTH"]) : (DateTime?)null;
                profile.Gender = row["GENDER"] != DBNull.Value ? row["GENDER"].ToString() : null;
                profile.Address = row["ADDRESS"] != DBNull.Value ? row["ADDRESS"].ToString() : null;
                profile.ClassName = row["CLASS_NAME"] != DBNull.Value ? row["CLASS_NAME"].ToString() : null;
                profile.ClassCode = row["CLASS_CODE"] != DBNull.Value ? row["CLASS_CODE"].ToString() : null;
                profile.DepartmentName = row["DEPARTMENT_NAME"] != DBNull.Value ? row["DEPARTMENT_NAME"].ToString() : null;
                profile.DepartmentCode = row["DEPARTMENT_CODE"] != DBNull.Value ? row["DEPARTMENT_CODE"].ToString() : null;
            }

            // Lấy điểm rèn luyện theo học kỳ
            profile.TermScores = GetTermScoresForProfile(mand);

            // Lấy thống kê
            GetProfileStatistics(mand, profile);

            return profile;
        }

        private List<TermScoreInfo> GetTermScoresForProfile(string mand)
        {
            var termScores = new List<TermScoreInfo>();

            string query = @"SELECT t.ID, t.NAME, t.YEAR, t.TERM_NUMBER,
                                   s.TOTAL_SCORE, s.CLASSIFICATION, s.STATUS, s.APPROVED_AT
                            FROM SCORES s
                            INNER JOIN TERMS t ON s.TERM_ID = t.ID
                            WHERE s.STUDENT_ID = :MAND
                            ORDER BY t.YEAR DESC, t.TERM_NUMBER DESC";

            var parameters = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
            var table = OracleDbHelper.ExecuteQuery(query, parameters);

            foreach (DataRow row in table.Rows)
            {
                termScores.Add(new TermScoreInfo
                {
                    TermId = row["ID"].ToString(),
                    TermName = row["NAME"].ToString(),
                    TermYear = row["YEAR"] != DBNull.Value ? Convert.ToInt32(row["YEAR"]) : DateTime.Now.Year,
                    TermNumber = row["TERM_NUMBER"] != DBNull.Value ? Convert.ToInt32(row["TERM_NUMBER"]) : 1,
                    TotalScore = row["TOTAL_SCORE"] != DBNull.Value ? Convert.ToInt32(row["TOTAL_SCORE"]) : 70,
                    Classification = row["CLASSIFICATION"] != DBNull.Value ? row["CLASSIFICATION"].ToString() : "Chưa xếp loại",
                    ApprovedAt = row["APPROVED_AT"] != DBNull.Value ? Convert.ToDateTime(row["APPROVED_AT"]) : (DateTime?)null
                });
            }

            return termScores;
        }

        private void GetProfileStatistics(string mand, StudentProfileViewModel profile)
        {
            // Tổng số hoạt động đã đăng ký
            string query1 = "SELECT COUNT(*) FROM REGISTRATIONS WHERE STUDENT_ID = :MAND";
            var param = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
            profile.TotalActivitiesRegistered = Convert.ToInt32(OracleDbHelper.ExecuteScalar(query1, param));

            // Tổng số hoạt động đã hoàn thành (CHECKED_IN)
            string query2 = "SELECT COUNT(*) FROM REGISTRATIONS WHERE STUDENT_ID = :MAND AND STATUS = 'CHECKED_IN'";
            profile.TotalActivitiesCompleted = Convert.ToInt32(OracleDbHelper.ExecuteScalar(query2, param));

            // Điểm trung bình
            string query3 = "SELECT AVG(TOTAL_SCORE) FROM SCORES WHERE STUDENT_ID = :MAND";
            var avgScore = OracleDbHelper.ExecuteScalar(query3, param);
            profile.AverageScore = avgScore != DBNull.Value ? Convert.ToDecimal(avgScore) : 0;
        }

        private EditStudentProfileViewModel GetEditProfileData(string mand)
        {
            var model = new EditStudentProfileViewModel();

            string query = @"SELECT u.MAND, u.EMAIL, u.FULL_NAME, u.AVATAR_URL,
                                   s.STUDENT_CODE, s.PHONE, s.DATE_OF_BIRTH, s.GENDER, s.ADDRESS, s.ID_CARD_NUMBER,
                                   s.PHONE_ENCRYPTED, s.ADDRESS_ENCRYPTED, s.ID_CARD_ENCRYPTED,
                                   c.NAME as CLASS_NAME, d.NAME as DEPARTMENT_NAME
                            FROM USERS u
                            INNER JOIN STUDENTS s ON u.MAND = s.USER_ID
                            LEFT JOIN CLASSES c ON s.CLASS_ID = c.ID
                            LEFT JOIN DEPARTMENTS d ON s.DEPARTMENT_ID = d.ID
                            WHERE u.MAND = :MAND";

            var parameters = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
            var table = OracleDbHelper.ExecuteQuery(query, parameters);

            if (table.Rows.Count > 0)
            {
                var row = table.Rows[0];
                model.MAND = row["MAND"].ToString();
                model.Email = row["EMAIL"].ToString();
                model.FullName = row["FULL_NAME"].ToString();
                model.AvatarUrl = row["AVATAR_URL"] != DBNull.Value ? row["AVATAR_URL"].ToString() : null;
                model.StudentCode = row["STUDENT_CODE"].ToString();
                model.DateOfBirth = row["DATE_OF_BIRTH"] != DBNull.Value ? Convert.ToDateTime(row["DATE_OF_BIRTH"]) : (DateTime?)null;
                model.Gender = row["GENDER"] != DBNull.Value ? row["GENDER"].ToString() : null;
                model.ClassName = row["CLASS_NAME"] != DBNull.Value ? row["CLASS_NAME"].ToString() : null;
                model.DepartmentName = row["DEPARTMENT_NAME"] != DBNull.Value ? row["DEPARTMENT_NAME"].ToString() : null;

                // ⭐ GIẢI MÃ dữ liệu nhạy cảm (ưu tiên encrypted columns)
                string phoneEnc = row["PHONE_ENCRYPTED"] != DBNull.Value ? row["PHONE_ENCRYPTED"].ToString() : null;
                string addressEnc = row["ADDRESS_ENCRYPTED"] != DBNull.Value ? row["ADDRESS_ENCRYPTED"].ToString() : null;
                string idCardEnc = row["ID_CARD_ENCRYPTED"] != DBNull.Value ? row["ID_CARD_ENCRYPTED"].ToString() : null;

                // Phone: Decrypt nếu có encrypted, fallback về plaintext
                if (!string.IsNullOrEmpty(phoneEnc))
                {
                    model.Phone = SensitiveDataService.DecryptStudentPhone(phoneEnc);
                }
                else
                {
                    model.Phone = row["PHONE"] != DBNull.Value ? row["PHONE"].ToString() : null;
                }

                // Address: Decrypt nếu có encrypted, fallback về plaintext
                if (!string.IsNullOrEmpty(addressEnc))
                {
                    model.Address = SensitiveDataService.DecryptStudentAddress(addressEnc);
                }
                else
                {
                    model.Address = row["ADDRESS"] != DBNull.Value ? row["ADDRESS"].ToString() : null;
                }

                // ID Card: Decrypt nếu có encrypted, fallback về plaintext
                if (!string.IsNullOrEmpty(idCardEnc))
                {
                    model.IdCardNumber = SensitiveDataService.DecryptIdCard(idCardEnc);
                }
                else
                {
                    model.IdCardNumber = row["ID_CARD_NUMBER"] != DBNull.Value ? row["ID_CARD_NUMBER"].ToString() : null;
                }
            }

            return model;
        }

        /// <summary>
        /// Lấy thông tin nhạy cảm của sinh viên (giải mã để edit)
        /// </summary>
        private EditSensitiveInfoViewModel GetSensitiveInfoData(string mand)
        {
            var model = new EditSensitiveInfoViewModel();

            string query = @"SELECT u.FULL_NAME,
                                   s.STUDENT_CODE, 
                                   s.PHONE_ENCRYPTED, 
                                   s.ADDRESS_ENCRYPTED, 
                                   s.ID_CARD_ENCRYPTED
                            FROM USERS u
                            INNER JOIN STUDENTS s ON u.MAND = s.USER_ID
                            WHERE u.MAND = :MAND";

            var parameters = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
            var table = OracleDbHelper.ExecuteQuery(query, parameters);

            if (table.Rows.Count > 0)
            {
                var row = table.Rows[0];
                model.FullName = row["FULL_NAME"].ToString();
                model.StudentCode = row["STUDENT_CODE"].ToString();

                // Giải mã dữ liệu nhạy cảm (nếu có)
                model.CurrentEncryptedPhone = row["PHONE_ENCRYPTED"] != DBNull.Value ? row["PHONE_ENCRYPTED"].ToString() : null;
                model.CurrentEncryptedAddress = row["ADDRESS_ENCRYPTED"] != DBNull.Value ? row["ADDRESS_ENCRYPTED"].ToString() : null;
                model.CurrentEncryptedIdCard = row["ID_CARD_ENCRYPTED"] != DBNull.Value ? row["ID_CARD_ENCRYPTED"].ToString() : null;

                // Decrypt để hiển thị trong form
                if (!string.IsNullOrEmpty(model.CurrentEncryptedPhone))
                {
                    model.Phone = SensitiveDataService.DecryptStudentPhone(model.CurrentEncryptedPhone);
                }
                
                if (!string.IsNullOrEmpty(model.CurrentEncryptedAddress))
                {
                    model.Address = SensitiveDataService.DecryptStudentAddress(model.CurrentEncryptedAddress);
                }
                
                if (!string.IsNullOrEmpty(model.CurrentEncryptedIdCard))
                {
                    model.IdCardNumber = SensitiveDataService.DecryptIdCard(model.CurrentEncryptedIdCard);
                }
            }

            return model;
        }

        /// <summary>
        /// Lấy ID của encryption key hiện tại
        /// </summary>
        private string GetCurrentEncryptionKeyId()
        {
            string query = @"SELECT ID FROM ENCRYPTION_KEYS 
                            WHERE KEY_NAME = 'SYSTEM_MAIN_KEY' 
                            AND IS_ACTIVE = 1";

            var result = OracleDbHelper.ExecuteScalar(query, null);
            return result != null ? result.ToString() : null;
        }

        #endregion
    }
}
