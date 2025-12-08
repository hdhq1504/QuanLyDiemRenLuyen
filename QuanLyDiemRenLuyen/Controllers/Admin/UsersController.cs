using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;

namespace QuanLyDiemRenLuyen.Controllers.Admin
{
    public class UsersController : AdminBaseController
    {
        // GET: Admin/Users
        public ActionResult Index(string search, string role, int page = 1)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = new UserIndexViewModel
                {
                    SearchKeyword = search,
                    FilterRole = role,
                    CurrentPage = page,
                    PageSize = 20
                };

                viewModel.Users = GetUsersList(search, role, page, viewModel.PageSize, out int totalCount);
                viewModel.TotalPages = (int)Math.Ceiling((double)totalCount / viewModel.PageSize);

                return View("~/Views/Admin/Users/Index.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/Users/Index.cshtml", new UserIndexViewModel { Users = new List<UserViewModel>() });
            }
        }

        // GET: Admin/Users/Create
        public ActionResult Create()
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            return View("~/Views/Admin/Users/Create.cshtml", new UserCreateViewModel());
        }

        // POST: Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UserCreateViewModel model)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/Users/Create.cshtml", model);
            }

            try
            {
                // Check duplicate MAND or Email
                string checkQuery = "SELECT COUNT(*) FROM USERS WHERE MAND = :Id OR EMAIL = :Email";
                var checkParams = new[] {
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, model.Id),
                    OracleDbHelper.CreateParameter("Email", OracleDbType.Varchar2, model.Email)
                };
                int count = Convert.ToInt32(OracleDbHelper.ExecuteScalar(checkQuery, checkParams));

                if (count > 0)
                {
                    ModelState.AddModelError("", "Mã người dùng hoặc Email đã tồn tại");
                    return View("~/Views/Admin/Users/Create.cshtml", model);
                }

                // Hash password
                string salt = PasswordHelper.GenerateSalt();
                string hash = PasswordHelper.HashPassword(model.Password, salt);

                string insertQuery = @"INSERT INTO USERS (MAND, FULL_NAME, EMAIL, PASSWORD_HASH, PASSWORD_SALT, ROLE_NAME, IS_ACTIVE)
                                       VALUES (:Id, :FullName, :Email, :Hash, :Salt, :Role, :IsActive)";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, model.Id),
                    OracleDbHelper.CreateParameter("FullName", OracleDbType.Varchar2, model.FullName),
                    OracleDbHelper.CreateParameter("Email", OracleDbType.Varchar2, model.Email),
                    OracleDbHelper.CreateParameter("Hash", OracleDbType.Varchar2, hash),
                    OracleDbHelper.CreateParameter("Salt", OracleDbType.Varchar2, salt),
                    OracleDbHelper.CreateParameter("Role", OracleDbType.Varchar2, model.Role),
                    OracleDbHelper.CreateParameter("IsActive", OracleDbType.Int32, model.IsActive ? 1 : 0)
                };

                OracleDbHelper.ExecuteNonQuery(insertQuery, parameters);

                // If Role is STUDENT, create record in STUDENTS table
                if (model.Role == "STUDENT")
                {
                    string insertStudentQuery = @"INSERT INTO STUDENTS (USER_ID, STUDENT_CODE) VALUES (:UserId, :StudentCode)";
                    var studentParams = new[]
                    {
                        OracleDbHelper.CreateParameter("UserId", OracleDbType.Varchar2, model.Id),
                        OracleDbHelper.CreateParameter("StudentCode", OracleDbType.Varchar2, model.Id) // Default StudentCode = MAND
                    };
                    OracleDbHelper.ExecuteNonQuery(insertStudentQuery, studentParams);
                }

                TempData["SuccessMessage"] = "Thêm người dùng thành công";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi: " + ex.Message;
                return View("~/Views/Admin/Users/Create.cshtml", model);
            }
        }

        // GET: Admin/Users/Edit/id
        public ActionResult Edit(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string query = "SELECT * FROM USERS WHERE MAND = :Id";
                var parameters = new[] { OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id) };
                DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

                if (dt.Rows.Count == 0)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng";
                    return RedirectToAction("Index");
                }

                DataRow row = dt.Rows[0];
                var model = new UserEditViewModel
                {
                    Id = row["MAND"].ToString(),
                    FullName = row["FULL_NAME"].ToString(),
                    Email = row["EMAIL"].ToString(),
                    Role = row["ROLE_NAME"].ToString(),
                    IsActive = Convert.ToInt32(row["IS_ACTIVE"]) == 1
                };

                // Load classes list
                model.Classes = GetClassesList();

                // If STUDENT, get current class
                if (model.Role == "STUDENT")
                {
                    string studentQuery = "SELECT CLASS_ID FROM STUDENTS WHERE USER_ID = :UserId";
                    var studentParams = new[] { OracleDbHelper.CreateParameter("UserId", OracleDbType.Varchar2, id) };
                    DataTable studentDt = OracleDbHelper.ExecuteQuery(studentQuery, studentParams);
                    
                    if (studentDt.Rows.Count > 0 && studentDt.Rows[0]["CLASS_ID"] != DBNull.Value)
                    {
                        model.ClassId = studentDt.Rows[0]["CLASS_ID"].ToString();
                    }
                }

                return View("~/Views/Admin/Users/Edit.cshtml", model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Admin/Users/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UserEditViewModel model)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            if (!ModelState.IsValid)
            {
                model.Classes = GetClassesList();
                return View("~/Views/Admin/Users/Edit.cshtml", model);
            }

            try
            {
                // Check duplicate Email (excluding current user)
                string checkQuery = "SELECT COUNT(*) FROM USERS WHERE EMAIL = :Email AND MAND != :Id";
                var checkParams = new[] {
                    OracleDbHelper.CreateParameter("Email", OracleDbType.Varchar2, model.Email),
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, model.Id)
                };
                int count = Convert.ToInt32(OracleDbHelper.ExecuteScalar(checkQuery, checkParams));

                if (count > 0)
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại");
                    model.Classes = GetClassesList();
                    return View("~/Views/Admin/Users/Edit.cshtml", model);
                }

                string updateQuery = @"UPDATE USERS
                                       SET FULL_NAME = :FullName,
                                           EMAIL = :Email,
                                           ROLE_NAME = :Role,
                                           IS_ACTIVE = :IsActive,
                                           UPDATED_AT = SYSTIMESTAMP
                                       WHERE MAND = :Id";

                var parameters = new List<OracleParameter>
                {
                    OracleDbHelper.CreateParameter("FullName", OracleDbType.Varchar2, model.FullName),
                    OracleDbHelper.CreateParameter("Email", OracleDbType.Varchar2, model.Email),
                    OracleDbHelper.CreateParameter("Role", OracleDbType.Varchar2, model.Role),
                    OracleDbHelper.CreateParameter("IsActive", OracleDbType.Int32, model.IsActive ? 1 : 0),
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, model.Id)
                };

                OracleDbHelper.ExecuteNonQuery(updateQuery, parameters.ToArray());

                // Update password if provided
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    string salt = PasswordHelper.GenerateSalt();
                    string hash = PasswordHelper.HashPassword(model.NewPassword, salt);

                    string passQuery = "UPDATE USERS SET PASSWORD_HASH = :Hash, PASSWORD_SALT = :Salt WHERE MAND = :Id";
                    var passParams = new[]
                    {
                        OracleDbHelper.CreateParameter("Hash", OracleDbType.Varchar2, hash),
                        OracleDbHelper.CreateParameter("Salt", OracleDbType.Varchar2, salt),
                        OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, model.Id)
                    };
                    OracleDbHelper.ExecuteNonQuery(passQuery, passParams);
                }

                // Update class for STUDENT role
                if (model.Role == "STUDENT" && !string.IsNullOrEmpty(model.ClassId))
                {
                    // Check if student record exists
                    string checkStudentQuery = "SELECT COUNT(*) FROM STUDENTS WHERE USER_ID = :UserId";
                    var checkStudentParams = new[] { OracleDbHelper.CreateParameter("UserId", OracleDbType.Varchar2, model.Id) };
                    int studentCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(checkStudentQuery, checkStudentParams));

                    if (studentCount > 0)
                    {
                        // Update existing student
                        string updateStudentQuery = "UPDATE STUDENTS SET CLASS_ID = :ClassId WHERE USER_ID = :UserId";
                        var updateStudentParams = new[]
                        {
                            OracleDbHelper.CreateParameter("ClassId", OracleDbType.Varchar2, model.ClassId),
                            OracleDbHelper.CreateParameter("UserId", OracleDbType.Varchar2, model.Id)
                        };
                        OracleDbHelper.ExecuteNonQuery(updateStudentQuery, updateStudentParams);
                    }
                    else
                    {
                        // Insert new student record
                        string insertStudentQuery = "INSERT INTO STUDENTS (USER_ID, STUDENT_CODE, CLASS_ID) VALUES (:UserId, :StudentCode, :ClassId)";
                        var insertStudentParams = new[]
                        {
                            OracleDbHelper.CreateParameter("UserId", OracleDbType.Varchar2, model.Id),
                            OracleDbHelper.CreateParameter("StudentCode", OracleDbType.Varchar2, model.Id),
                            OracleDbHelper.CreateParameter("ClassId", OracleDbType.Varchar2, model.ClassId)
                        };
                        OracleDbHelper.ExecuteNonQuery(insertStudentQuery, insertStudentParams);
                    }
                }

                TempData["SuccessMessage"] = "Cập nhật người dùng thành công";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi: " + ex.Message;
                model.Classes = GetClassesList();
                return View("~/Views/Admin/Users/Edit.cshtml", model);
            }
        }

        // GET: Admin/Users/Details/id (AJAX)
        public JsonResult Details(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            try
            {
                string query = "SELECT * FROM USERS WHERE MAND = :Id";
                var parameters = new[] { OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id) };
                DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

                if (dt.Rows.Count == 0)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" }, JsonRequestBehavior.AllowGet);
                }

                DataRow row = dt.Rows[0];
                var userDetails = new
                {
                    success = true,
                    data = new
                    {
                        Id = row["MAND"].ToString(),
                        FullName = row["FULL_NAME"].ToString(),
                        Email = row["EMAIL"].ToString(),
                        Role = row["ROLE_NAME"].ToString(),
                        IsActive = Convert.ToInt32(row["IS_ACTIVE"]) == 1,
                        CreatedAt = Convert.ToDateTime(row["CREATED_AT"]).ToString("dd/MM/yyyy HH:mm:ss"),
                        UpdatedAt = row["UPDATED_AT"] != DBNull.Value 
                            ? Convert.ToDateTime(row["UPDATED_AT"]).ToString("dd/MM/yyyy HH:mm:ss") 
                            : "Chưa cập nhật"
                    }
                };

                return Json(userDetails, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Admin/Users/Delete/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            // Prevent deleting self
            if (id == GetCurrentUserId())
            {
                TempData["ErrorMessage"] = "Không thể xóa chính mình";
                return RedirectToAction("Index");
            }

            try
            {
                string deleteQuery = "DELETE FROM USERS WHERE MAND = :Id";
                var parameters = new[] { OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id) };
                OracleDbHelper.ExecuteNonQuery(deleteQuery, parameters);

                TempData["SuccessMessage"] = "Xóa người dùng thành công";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        #region Helper Methods

        private List<UserViewModel> GetUsersList(string search, string role, int page, int pageSize, out int totalCount)
        {
            var users = new List<UserViewModel>();
            totalCount = 0;

            try
            {
                string countQuery = "SELECT COUNT(*) FROM USERS WHERE 1=1";
                string dataQuery = "SELECT MAND, FULL_NAME, EMAIL, ROLE_NAME, IS_ACTIVE, CREATED_AT FROM USERS WHERE 1=1";

                var parameters = new List<OracleParameter>();

                if (!string.IsNullOrEmpty(search))
                {
                    countQuery += " AND (UPPER(MAND) LIKE :Search OR UPPER(FULL_NAME) LIKE :Search OR UPPER(EMAIL) LIKE :Search)";
                    dataQuery += " AND (UPPER(MAND) LIKE :Search OR UPPER(FULL_NAME) LIKE :Search OR UPPER(EMAIL) LIKE :Search)";
                    parameters.Add(OracleDbHelper.CreateParameter("Search", OracleDbType.Varchar2, "%" + search.ToUpper() + "%"));
                }

                if (!string.IsNullOrEmpty(role) && role != "ALL")
                {
                    countQuery += " AND ROLE_NAME = :Role";
                    dataQuery += " AND ROLE_NAME = :Role";
                    parameters.Add(OracleDbHelper.CreateParameter("Role", OracleDbType.Varchar2, role));
                }

                totalCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(countQuery, parameters.ToArray()));

                dataQuery += " ORDER BY CREATED_AT DESC";
                int offset = (page - 1) * pageSize;
                dataQuery = $@"SELECT * FROM (
                                SELECT a.*, ROWNUM rnum FROM ({dataQuery}) a
                                WHERE ROWNUM <= {offset + pageSize}
                              ) WHERE rnum > {offset}";

                DataTable dt = OracleDbHelper.ExecuteQuery(dataQuery, parameters.ToArray());

                foreach (DataRow row in dt.Rows)
                {
                    users.Add(new UserViewModel
                    {
                        Id = row["MAND"].ToString(),
                        FullName = row["FULL_NAME"].ToString(),
                        Email = row["EMAIL"].ToString(),
                        Role = row["ROLE_NAME"].ToString(),
                        IsActive = Convert.ToInt32(row["IS_ACTIVE"]) == 1,
                        CreatedAt = Convert.ToDateTime(row["CREATED_AT"])
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return users;
        }

        private List<SelectListItem> GetClassesList()
        {
            var classes = new List<SelectListItem>();
            try
            {
                string query = "SELECT ID, NAME FROM CLASSES ORDER BY NAME";
                DataTable dt = OracleDbHelper.ExecuteQuery(query);
                
                foreach (DataRow row in dt.Rows)
                {
                    classes.Add(new SelectListItem
                    {
                        Value = row["ID"].ToString(),
                        Text = row["NAME"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading classes: " + ex.Message);
            }
            return classes;
        }

        #endregion
    }
}
