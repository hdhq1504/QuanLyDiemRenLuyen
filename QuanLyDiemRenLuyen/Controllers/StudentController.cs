using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;

namespace QuanLyDiemRenLuyen.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        // GET: Student/Dashboard
        public ActionResult Dashboard()
        {
            try
            {
                string mand = Session["MAND"]?.ToString();
                if (string.IsNullOrEmpty(mand))
                {
                    return RedirectToAction("Login", "Account");
                }

                var viewModel = new StudentDashboardViewModel
                {
                    User = GetUserInfo(mand),
                    StudentInfo = GetStudentInfo(mand),
                    TermScores = GetTermScores(mand),
                    RecentActivities = GetRecentActivities(mand),
                    UnreadNotifications = GetUnreadNotifications(mand),
                    Statistics = GetStatistics(mand)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View(new StudentDashboardViewModel());
            }
        }

        private User GetUserInfo(string mand)
        {
            string query = @"SELECT MAND, EMAIL, FULL_NAME, AVATAR_URL, ROLE_NAME, IS_ACTIVE, CREATED_AT 
                           FROM USERS WHERE MAND = :MAND";

            var parameters = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new User
            {
                MAND = row["MAND"].ToString(),
                Email = row["EMAIL"].ToString(),
                FullName = row["FULL_NAME"].ToString(),
                AvatarUrl = row["AVATAR_URL"] != DBNull.Value ? row["AVATAR_URL"].ToString() : null,
                RoleName = row["ROLE_NAME"].ToString(),
                IsActive = Convert.ToInt32(row["IS_ACTIVE"]) == 1,
                CreatedAt = Convert.ToDateTime(row["CREATED_AT"])
            };
        }

        private StudentInfo GetStudentInfo(string mand)
        {
            string query = @"SELECT s.STUDENT_CODE, c.NAME as CLASS_NAME, d.NAME as DEPT_NAME,
                           s.DATE_OF_BIRTH, s.GENDER, s.PHONE, s.ADDRESS
                           FROM STUDENTS s
                           LEFT JOIN CLASSES c ON s.CLASS_ID = c.ID
                           LEFT JOIN DEPARTMENTS d ON s.DEPARTMENT_ID = d.ID
                           WHERE s.USER_ID = :MAND";

            var parameters = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

            if (dt.Rows.Count == 0) return new StudentInfo();

            DataRow row = dt.Rows[0];
            return new StudentInfo
            {
                StudentCode = row["STUDENT_CODE"] != DBNull.Value ? row["STUDENT_CODE"].ToString() : "",
                ClassName = row["CLASS_NAME"] != DBNull.Value ? row["CLASS_NAME"].ToString() : "",
                DepartmentName = row["DEPT_NAME"] != DBNull.Value ? row["DEPT_NAME"].ToString() : "",
                DOB = row["DATE_OF_BIRTH"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["DATE_OF_BIRTH"]) : null,
                Gender = row["GENDER"] != DBNull.Value ? row["GENDER"].ToString() : "",
                Phone = row["PHONE"] != DBNull.Value ? row["PHONE"].ToString() : "",
                Address = row["ADDRESS"] != DBNull.Value ? row["ADDRESS"].ToString() : ""
            };
        }

        private List<TermScore> GetTermScores(string mand)
        {
            string query = @"SELECT s.TERM_ID, t.NAME as TERM_NAME, s.TOTAL_SCORE, s.STATUS, s.APPROVED_AT
                           FROM SCORES s
                           INNER JOIN TERMS t ON s.TERM_ID = t.ID
                           WHERE s.STUDENT_ID = :MAND
                           ORDER BY t.START_DATE DESC";

            var parameters = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

            var scores = new List<TermScore>();
            foreach (DataRow row in dt.Rows)
            {
                scores.Add(new TermScore
                {
                    TermId = row["TERM_ID"].ToString(),
                    TermName = row["TERM_NAME"].ToString(),
                    Total = Convert.ToDecimal(row["TOTAL_SCORE"]),
                    Status = row["STATUS"].ToString(),
                    ApprovedAt = row["APPROVED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["APPROVED_AT"]) : null
                });
            }
            return scores;
        }

        private List<ActivityRegistration> GetRecentActivities(string mand)
        {
            string query = @"SELECT a.ID as ACTIVITY_ID, a.TITLE, a.DESCRIPTION, a.START_AT, a.END_AT, 
                           a.LOCATION, a.POINTS, r.STATUS as REG_STATUS, r.REGISTERED_AT
                           FROM REGISTRATIONS r
                           INNER JOIN ACTIVITIES a ON r.ACTIVITY_ID = a.ID
                           WHERE r.STUDENT_ID = :MAND
                           ORDER BY r.REGISTERED_AT DESC
                           FETCH FIRST 10 ROWS ONLY";

            var parameters = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

            var activities = new List<ActivityRegistration>();
            foreach (DataRow row in dt.Rows)
            {
                activities.Add(new ActivityRegistration
                {
                    ActivityId = row["ACTIVITY_ID"].ToString(),
                    Title = row["TITLE"].ToString(),
                    Description = row["DESCRIPTION"] != DBNull.Value ? row["DESCRIPTION"].ToString() : "",
                    StartAt = Convert.ToDateTime(row["START_AT"]),
                    EndAt = Convert.ToDateTime(row["END_AT"]),
                    Location = row["LOCATION"] != DBNull.Value ? row["LOCATION"].ToString() : "",
                    Points = row["POINTS"] != DBNull.Value ? (decimal?)Convert.ToDecimal(row["POINTS"]) : null,
                    RegistrationStatus = row["REG_STATUS"].ToString(),
                    RegisteredAt = Convert.ToDateTime(row["REGISTERED_AT"])
                });
            }
            return activities;
        }

        private List<Notification> GetUnreadNotifications(string mand)
        {
            string query = @"SELECT n.ID, n.TITLE, n.CONTENT, n.CREATED_AT,
                           COALESCE(nr.IS_READ, 0) as IS_READ
                           FROM NOTIFICATIONS n
                           LEFT JOIN NOTIFICATION_READS nr ON n.ID = nr.NOTIFICATION_ID AND nr.STUDENT_ID = :MAND
                           WHERE (n.TO_USER_ID = :MAND OR n.TARGET_ROLE = 'STUDENT' OR n.TARGET_ROLE IS NULL)
                           AND COALESCE(nr.IS_READ, 0) = 0
                           ORDER BY n.CREATED_AT DESC
                           FETCH FIRST 5 ROWS ONLY";

            var parameters = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

            var notifications = new List<Notification>();
            foreach (DataRow row in dt.Rows)
            {
                notifications.Add(new Notification
                {
                    Id = row["ID"].ToString(),
                    Title = row["TITLE"].ToString(),
                    Content = row["CONTENT"] != DBNull.Value ? row["CONTENT"].ToString() : "",
                    CreatedAt = Convert.ToDateTime(row["CREATED_AT"]),
                    IsRead = Convert.ToInt32(row["IS_READ"]) == 1
                });
            }
            return notifications;
        }

        private DashboardStatistics GetStatistics(string mand)
        {
            var stats = new DashboardStatistics();

            // Tổng số hoạt động đã đăng ký
            string query1 = "SELECT COUNT(*) FROM REGISTRATIONS WHERE STUDENT_ID = :MAND";
            var param = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
            stats.TotalActivitiesRegistered = Convert.ToInt32(OracleDbHelper.ExecuteScalar(query1, param));

            // Tổng số hoạt động đã hoàn thành (checked in)
            string query2 = "SELECT COUNT(*) FROM REGISTRATIONS WHERE STUDENT_ID = :MAND AND STATUS = 'CHECKED_IN'";
            stats.TotalActivitiesCompleted = Convert.ToInt32(OracleDbHelper.ExecuteScalar(query2, param));

            // Điểm học kỳ hiện tại
            string query3 = @"SELECT COALESCE(s.TOTAL_SCORE, 70) as TOTAL_SCORE
                            FROM SCORES s
                            INNER JOIN TERMS t ON s.TERM_ID = t.ID
                            WHERE s.STUDENT_ID = :MAND
                            AND SYSDATE BETWEEN t.START_DATE AND t.END_DATE
                            FETCH FIRST 1 ROWS ONLY";

            object result = OracleDbHelper.ExecuteScalar(query3, param);
            stats.CurrentTermScore = result != null ? Convert.ToDecimal(result) : 70;

            // Số thông báo chưa đọc
            string query4 = @"SELECT COUNT(*)
                            FROM NOTIFICATIONS n
                            LEFT JOIN NOTIFICATION_READS nr ON n.ID = nr.NOTIFICATION_ID AND nr.STUDENT_ID = :MAND
                            WHERE (n.TO_USER_ID = :MAND OR n.TARGET_ROLE = 'STUDENT' OR n.TARGET_ROLE IS NULL)
                            AND COALESCE(nr.IS_READ, 0) = 0";
            stats.UnreadNotificationCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(query4, param));

            return stats;
        }

        // GET: Student/Profile
        public ActionResult Profile()
        {
            try
            {
                string mand = Session["MAND"] != null ? Session["MAND"].ToString() : null;
                if (string.IsNullOrEmpty(mand))
                {
                    return RedirectToAction("Login", "Account");
                }

                var viewModel = GetStudentProfile(mand);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View(new StudentProfileViewModel());
            }
        }

        // GET: Student/EditProfile
        public ActionResult EditProfile()
        {
            try
            {
                string mand = Session["MAND"] != null ? Session["MAND"].ToString() : null;
                if (string.IsNullOrEmpty(mand))
                {
                    return RedirectToAction("Login", "Account");
                }

                var viewModel = GetEditProfileData(mand);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Profile");
            }
        }

        // POST: Student/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(EditStudentProfileViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                string mand = Session["MAND"] != null ? Session["MAND"].ToString() : null;
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

                // Cập nhật thông tin sinh viên
                string updateStudentQuery = @"UPDATE STUDENTS
                                             SET PHONE = :Phone,
                                                 DATE_OF_BIRTH = :DateOfBirth,
                                                 GENDER = :Gender,
                                                 ADDRESS = :Address
                                             WHERE USER_ID = :MAND";

                var studentParams = new[]
                {
                    OracleDbHelper.CreateParameter("Phone", OracleDbType.Varchar2,
                        string.IsNullOrEmpty(model.Phone) ? (object)DBNull.Value : model.Phone),
                    OracleDbHelper.CreateParameter("DateOfBirth", OracleDbType.Date,
                        model.DateOfBirth.HasValue ? (object)model.DateOfBirth.Value : DBNull.Value),
                    OracleDbHelper.CreateParameter("Gender", OracleDbType.Varchar2,
                        string.IsNullOrEmpty(model.Gender) ? (object)DBNull.Value : model.Gender),
                    OracleDbHelper.CreateParameter("Address", OracleDbType.Varchar2,
                        string.IsNullOrEmpty(model.Address) ? (object)DBNull.Value : model.Address),
                    OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand)
                };

                OracleDbHelper.ExecuteNonQuery(updateStudentQuery, studentParams);

                // Cập nhật session nếu tên thay đổi
                Session["FullName"] = model.FullName;
                Session["Email"] = model.Email;

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi khi cập nhật: " + ex.Message;
                return View(model);
            }
        }

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
                                   s.STUDENT_CODE, s.PHONE, s.DATE_OF_BIRTH, s.GENDER, s.ADDRESS,
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
                model.Phone = row["PHONE"] != DBNull.Value ? row["PHONE"].ToString() : null;
                model.DateOfBirth = row["DATE_OF_BIRTH"] != DBNull.Value ? Convert.ToDateTime(row["DATE_OF_BIRTH"]) : (DateTime?)null;
                model.Gender = row["GENDER"] != DBNull.Value ? row["GENDER"].ToString() : null;
                model.Address = row["ADDRESS"] != DBNull.Value ? row["ADDRESS"].ToString() : null;
                model.ClassName = row["CLASS_NAME"] != DBNull.Value ? row["CLASS_NAME"].ToString() : null;
                model.DepartmentName = row["DEPARTMENT_NAME"] != DBNull.Value ? row["DEPARTMENT_NAME"].ToString() : null;
            }

            return model;
        }

        // ==================== ACTIVITIES MANAGEMENT ====================

        // GET: Student/Activities
        public ActionResult Activities(string search, string status, int page = 1)
        {
            try
            {
                string mand = Session["MAND"]?.ToString();
                if (string.IsNullOrEmpty(mand))
                {
                    return RedirectToAction("Login", "Account");
                }

                var viewModel = new ActivityListViewModel
                {
                    SearchKeyword = search,
                    FilterStatus = status ?? "ALL",
                    CurrentPage = page,
                    PageSize = 10
                };

                viewModel.Activities = GetActivitiesList(mand, search, status, page, viewModel.PageSize, out int totalCount);
                viewModel.TotalPages = (int)Math.Ceiling((double)totalCount / viewModel.PageSize);

                // Debug
                ViewBag.DebugMessage = $"Total: {totalCount}, Activities: {viewModel.Activities.Count}, MAND: {mand}";

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message + " | StackTrace: " + ex.StackTrace;
                return View(new ActivityListViewModel());
            }
        }

        // GET: Student/ActivityDetail/id
        public ActionResult ActivityDetail(string id)
        {
            try
            {
                string mand = Session["MAND"]?.ToString();
                if (string.IsNullOrEmpty(mand))
                {
                    return RedirectToAction("Login", "Account");
                }

                var viewModel = GetActivityDetail(id, mand);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hoạt động";
                    return RedirectToAction("Activities");
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Activities");
            }
        }

        // POST: Student/RegisterActivity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterActivity(string activityId)
        {
            try
            {
                string mand = Session["MAND"]?.ToString();
                if (string.IsNullOrEmpty(mand))
                {
                    TempData["ErrorMessage"] = "Vui lòng đăng nhập";
                    return RedirectToAction("Login", "Account");
                }

                // Kiểm tra hoạt động có tồn tại không
                string checkQuery = @"SELECT COUNT(*) FROM ACTIVITIES
                                     WHERE ID = :ActivityId AND STATUS = 'ACTIVE'";
                var checkParams = new[] { OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId) };
                int activityExists = Convert.ToInt32(OracleDbHelper.ExecuteScalar(checkQuery, checkParams));

                if (activityExists == 0)
                {
                    TempData["ErrorMessage"] = "Hoạt động không tồn tại hoặc đã bị hủy";
                    return RedirectToAction("ActivityDetail", new { id = activityId });
                }

                // Kiểm tra đã đăng ký chưa
                string checkRegQuery = @"SELECT COUNT(*) FROM REGISTRATIONS
                                        WHERE ACTIVITY_ID = :ActivityId AND STUDENT_ID = :StudentId";
                var checkRegParams = new[]
                {
                    OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
                };
                int alreadyRegistered = Convert.ToInt32(OracleDbHelper.ExecuteScalar(checkRegQuery, checkRegParams));

                if (alreadyRegistered > 0)
                {
                    TempData["ErrorMessage"] = "Bạn đã đăng ký hoạt động này rồi";
                    return RedirectToAction("ActivityDetail", new { id = activityId });
                }

                // Đăng ký hoạt động
                string insertQuery = @"INSERT INTO REGISTRATIONS (ID, ACTIVITY_ID, STUDENT_ID, STATUS, REGISTERED_AT)
                                      VALUES (RAWTOHEX(SYS_GUID()), :ActivityId, :StudentId, 'REGISTERED', SYSTIMESTAMP)";
                var insertParams = new[]
                {
                    OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
                };

                int result = OracleDbHelper.ExecuteNonQuery(insertQuery, insertParams);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Đăng ký hoạt động thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Đăng ký thất bại, vui lòng thử lại";
                }

                return RedirectToAction("ActivityDetail", new { id = activityId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("ActivityDetail", new { id = activityId });
            }
        }

        // POST: Student/CancelRegistration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelRegistration(string activityId)
        {
            try
            {
                string mand = Session["MAND"]?.ToString();
                if (string.IsNullOrEmpty(mand))
                {
                    TempData["ErrorMessage"] = "Vui lòng đăng nhập";
                    return RedirectToAction("Login", "Account");
                }

                // Hủy đăng ký (cập nhật status thành CANCELLED thay vì xóa)
                string deleteQuery = @"UPDATE REGISTRATIONS
                                      SET STATUS = 'CANCELLED'
                                      WHERE ACTIVITY_ID = :ActivityId
                                      AND STUDENT_ID = :StudentId
                                      AND STATUS = 'REGISTERED'";
                var deleteParams = new[]
                {
                    OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
                };

                int result = OracleDbHelper.ExecuteNonQuery(deleteQuery, deleteParams);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Hủy đăng ký thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể hủy đăng ký. Có thể hoạt động đã được duyệt.";
                }

                return RedirectToAction("ActivityDetail", new { id = activityId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("ActivityDetail", new { id = activityId });
            }
        }

        // GET: Student/UploadProof/id
        public ActionResult UploadProof(string id)
        {
            try
            {
                string mand = Session["MAND"]?.ToString();
                if (string.IsNullOrEmpty(mand))
                {
                    return RedirectToAction("Login", "Account");
                }

                var viewModel = GetUploadProofViewModel(id, mand);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hoạt động hoặc bạn chưa đăng ký hoạt động này";
                    return RedirectToAction("Activities");
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Activities");
            }
        }

        // POST: Student/UploadProof
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadProof(string activityId, HttpPostedFileBase proofFile, string note)
        {
            string mand = Session["MAND"]?.ToString();
            if (string.IsNullOrEmpty(mand))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập";
                return RedirectToAction("Login", "Account");
            }

            try
            {

                if (proofFile == null || proofFile.ContentLength == 0)
                {
                    ViewBag.ErrorMessage = "Vui lòng chọn file minh chứng";
                    return View(GetUploadProofViewModel(activityId, mand));
                }

                // Kiểm tra định dạng file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                var fileExtension = Path.GetExtension(proofFile.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ViewBag.ErrorMessage = "Chỉ chấp nhận file ảnh (jpg, png) hoặc PDF";
                    return View(GetUploadProofViewModel(activityId, mand));
                }

                // Kiểm tra kích thước file (max 5MB)
                if (proofFile.ContentLength > 5 * 1024 * 1024)
                {
                    ViewBag.ErrorMessage = "File không được vượt quá 5MB";
                    return View(GetUploadProofViewModel(activityId, mand));
                }

                // Tạo thư mục lưu file
                string uploadFolder = Server.MapPath("~/Uploads/Proofs");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                // Tạo tên file unique
                string fileName = $"{mand}_{activityId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                string filePath = Path.Combine(uploadFolder, fileName);
                string relativePath = $"/Uploads/Proofs/{fileName}";

                // Lưu file
                proofFile.SaveAs(filePath);

                // Lấy REGISTRATION_ID
                string getRegIdQuery = @"SELECT ID FROM REGISTRATIONS
                                        WHERE ACTIVITY_ID = :ActivityId AND STUDENT_ID = :StudentId";
                var getRegIdParams = new[]
                {
                    OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
                };
                string registrationId = OracleDbHelper.ExecuteScalar(getRegIdQuery, getRegIdParams)?.ToString();

                if (string.IsNullOrEmpty(registrationId))
                {
                    ViewBag.ErrorMessage = "Không tìm thấy thông tin đăng ký";
                    return View(GetUploadProofViewModel(activityId, mand));
                }

                // Insert vào bảng PROOFS
                string insertQuery = @"INSERT INTO PROOFS
                                      (ID, REGISTRATION_ID, STUDENT_ID, ACTIVITY_ID, FILE_NAME, STORED_PATH,
                                       CONTENT_TYPE, FILE_SIZE, NOTE, STATUS, CREATED_AT_UTC)
                                      VALUES
                                      (RAWTOHEX(SYS_GUID()), :RegistrationId, :StudentId, :ActivityId, :FileName, :StoredPath,
                                       :ContentType, :FileSize, :Note, 'SUBMITTED', SYS_EXTRACT_UTC(SYSTIMESTAMP))";
                var insertParams = new[]
                {
                    OracleDbHelper.CreateParameter("RegistrationId", OracleDbType.Varchar2, registrationId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId),
                    OracleDbHelper.CreateParameter("FileName", OracleDbType.Varchar2, proofFile.FileName),
                    OracleDbHelper.CreateParameter("StoredPath", OracleDbType.Varchar2, relativePath),
                    OracleDbHelper.CreateParameter("ContentType", OracleDbType.Varchar2, proofFile.ContentType),
                    OracleDbHelper.CreateParameter("FileSize", OracleDbType.Int32, proofFile.ContentLength),
                    OracleDbHelper.CreateParameter("Note", OracleDbType.Varchar2, note ?? "")
                };

                int result = OracleDbHelper.ExecuteNonQuery(insertQuery, insertParams);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Upload minh chứng thành công!";
                    return RedirectToAction("ActivityDetail", new { id = activityId });
                }
                else
                {
                    ViewBag.ErrorMessage = "Upload thất bại, vui lòng thử lại";
                    return View(GetUploadProofViewModel(activityId, mand));
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi: " + ex.Message;
                return View(GetUploadProofViewModel(activityId, mand));
            }
        }

        // ==================== CHANGE PASSWORD ====================

        // GET: Student/ChangePassword
        public ActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: Student/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                string mand = Session["MAND"]?.ToString();
                if (string.IsNullOrEmpty(mand))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Kiểm tra mật khẩu hiện tại
                string checkQuery = "SELECT PASSWORD_HASH, PASSWORD_SALT FROM USERS WHERE MAND = :MAND";
                var checkParams = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
                DataTable dt = OracleDbHelper.ExecuteQuery(checkQuery, checkParams);

                if (dt.Rows.Count == 0)
                {
                    ModelState.AddModelError("", "Không tìm thấy thông tin người dùng");
                    return View(model);
                }

                string currentHash = dt.Rows[0]["PASSWORD_HASH"].ToString();
                string currentSalt = dt.Rows[0]["PASSWORD_SALT"].ToString();

                if (!PasswordHelper.VerifyPassword(model.CurrentPassword, currentSalt, currentHash))
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");
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
                                      WHERE MAND = :MAND";
                var updateParams = new[]
                {
                    OracleDbHelper.CreateParameter("PasswordHash", OracleDbType.Varchar2, newPasswordHash),
                    OracleDbHelper.CreateParameter("PasswordSalt", OracleDbType.Varchar2, newSalt),
                    OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, updateParams);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                    return RedirectToAction("Profile");
                }
                else
                {
                    ViewBag.ErrorMessage = "Đổi mật khẩu thất bại, vui lòng thử lại";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View(model);
            }
        }

        // ==================== HELPER METHODS ====================

        private List<ActivityItem> GetActivitiesList(string mand, string search, string status, int page, int pageSize, out int totalCount)
        {
            var activities = new List<ActivityItem>();
            totalCount = 0;

            try
            {
                // Build query với điều kiện tìm kiếm và lọc
                // Chỉ hiển thị hoạt động đã được duyệt (APPROVAL_STATUS = 'APPROVED') và đang mở (STATUS = 'OPEN')
                string countQuery = @"SELECT COUNT(*) FROM ACTIVITIES a WHERE a.APPROVAL_STATUS = 'APPROVED' AND a.STATUS = 'OPEN'";
                string dataQuery = @"SELECT a.ID, a.TITLE, a.DESCRIPTION, a.START_AT, a.END_AT, a.LOCATION,
                                       a.POINTS, a.MAX_SEATS, a.STATUS,
                                       cr.NAME as CRITERION_NAME, u.FULL_NAME as ORGANIZER_NAME,
                                       (SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STATUS != 'CANCELLED') as CURRENT_PARTICIPANTS,
                                       (SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STUDENT_ID = :StudentId AND STATUS != 'CANCELLED') as IS_REGISTERED,
                                       (SELECT STATUS FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STUDENT_ID = :StudentId AND ROWNUM = 1) as REG_STATUS
                                FROM ACTIVITIES a
                                LEFT JOIN CRITERIA cr ON a.CRITERION_ID = cr.ID
                                LEFT JOIN USERS u ON a.ORGANIZER_ID = u.MAND
                                WHERE a.APPROVAL_STATUS = 'APPROVED' AND a.STATUS = 'OPEN'";

                var parameters = new List<OracleParameter>
            {
                OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
            };

                if (!string.IsNullOrEmpty(search))
                {
                    countQuery += " AND (UPPER(a.TITLE) LIKE :Search OR UPPER(a.DESCRIPTION) LIKE :Search)";
                    dataQuery += " AND (UPPER(a.TITLE) LIKE :Search OR UPPER(a.DESCRIPTION) LIKE :Search)";
                    parameters.Add(OracleDbHelper.CreateParameter("Search", OracleDbType.Varchar2, "%" + search.ToUpper() + "%"));
                }

                if (!string.IsNullOrEmpty(status) && status != "ALL")
                {
                    if (status == "UPCOMING")
                    {
                        dataQuery += " AND a.START_AT > SYSDATE";
                        countQuery += " AND a.START_AT > SYSDATE";
                    }
                    else if (status == "ONGOING")
                    {
                        dataQuery += " AND SYSDATE BETWEEN a.START_AT AND a.END_AT";
                        countQuery += " AND SYSDATE BETWEEN a.START_AT AND a.END_AT";
                    }
                    else if (status == "COMPLETED")
                    {
                        dataQuery += " AND a.END_AT < SYSDATE";
                        countQuery += " AND a.END_AT < SYSDATE";
                    }
                }

                // Đếm tổng số
                totalCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(countQuery, parameters.ToArray()));

                // Debug: Log total count
                System.Diagnostics.Debug.WriteLine($"Total activities count: {totalCount}");

                // Thêm phân trang
                dataQuery += " ORDER BY a.START_AT DESC";
                int offset = (page - 1) * pageSize;
                dataQuery = $@"SELECT * FROM (
                            SELECT a.*, ROWNUM rnum FROM ({dataQuery}) a
                            WHERE ROWNUM <= {offset + pageSize}
                          ) WHERE rnum > {offset}";

                // Debug: Log query
                System.Diagnostics.Debug.WriteLine($"Query: {dataQuery}");

                DataTable dt = OracleDbHelper.ExecuteQuery(dataQuery, parameters.ToArray());

                // Debug: Log result count
                System.Diagnostics.Debug.WriteLine($"DataTable rows: {dt.Rows.Count}");

                foreach (DataRow row in dt.Rows)
                {
                    activities.Add(new ActivityItem
                    {
                        Id = row["ID"].ToString(),
                        Title = row["TITLE"].ToString(),
                        Description = row["DESCRIPTION"] != DBNull.Value ? row["DESCRIPTION"].ToString() : "",
                        StartAt = Convert.ToDateTime(row["START_AT"]),
                        EndAt = Convert.ToDateTime(row["END_AT"]),
                        Location = row["LOCATION"] != DBNull.Value ? row["LOCATION"].ToString() : "",
                        Points = row["POINTS"] != DBNull.Value ? (decimal?)Convert.ToDecimal(row["POINTS"]) : null,
                        MaxParticipants = row["MAX_SEATS"] != DBNull.Value ? Convert.ToInt32(row["MAX_SEATS"]) : 0,
                        CurrentParticipants = row["CURRENT_PARTICIPANTS"] != DBNull.Value ? Convert.ToInt32(row["CURRENT_PARTICIPANTS"]) : 0,
                        Status = row["STATUS"].ToString(),
                        RegistrationDeadline = null, // Không có trong schema
                        CategoryName = row["CRITERION_NAME"] != DBNull.Value ? row["CRITERION_NAME"].ToString() : "",
                        OrganizerName = row["ORGANIZER_NAME"] != DBNull.Value ? row["ORGANIZER_NAME"].ToString() : "",
                        IsRegistered = row["IS_REGISTERED"] != DBNull.Value && Convert.ToInt32(row["IS_REGISTERED"]) > 0,
                        RegistrationStatus = row["REG_STATUS"] != DBNull.Value ? row["REG_STATUS"].ToString() : ""
                    });
                }

                return activities;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetActivitiesList: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                throw; // Re-throw để controller bắt được
            }
        }

        private ActivityDetailViewModel GetActivityDetail(string activityId, string mand)
        {
            string query = @"SELECT a.ID, a.TITLE, a.DESCRIPTION, a.START_AT, a.END_AT, a.LOCATION,
                                   a.POINTS, a.MAX_SEATS, a.STATUS, a.CREATED_AT,
                                   cr.NAME as CRITERION_NAME, u.FULL_NAME as ORGANIZER_NAME,
                                   (SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STATUS != 'CANCELLED') as CURRENT_PARTICIPANTS,
                                   r.STATUS as REG_STATUS, r.REGISTERED_AT,
                                   p.STATUS as PROOF_STATUS, p.STORED_PATH as PROOF_FILE_PATH,
                                   p.NOTE as PROOF_NOTE, p.CREATED_AT_UTC as PROOF_UPLOADED_AT
                            FROM ACTIVITIES a
                            LEFT JOIN CRITERIA cr ON a.CRITERION_ID = cr.ID
                            LEFT JOIN USERS u ON a.ORGANIZER_ID = u.MAND
                            LEFT JOIN REGISTRATIONS r ON a.ID = r.ACTIVITY_ID AND r.STUDENT_ID = :StudentId
                            LEFT JOIN (
                                SELECT * FROM (
                                    SELECT REGISTRATION_ID, STATUS, STORED_PATH, NOTE, CREATED_AT_UTC,
                                           ROW_NUMBER() OVER (PARTITION BY REGISTRATION_ID ORDER BY CREATED_AT_UTC DESC) as rn
                                    FROM PROOFS
                                ) WHERE rn = 1
                            ) p ON r.ID = p.REGISTRATION_ID
                            WHERE a.ID = :ActivityId";

            var parameters = new[]
            {
                OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId),
                OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
            };

            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            var viewModel = new ActivityDetailViewModel
            {
                Id = row["ID"].ToString(),
                Title = row["TITLE"].ToString(),
                Description = row["DESCRIPTION"] != DBNull.Value ? row["DESCRIPTION"].ToString() : "",
                StartAt = Convert.ToDateTime(row["START_AT"]),
                EndAt = Convert.ToDateTime(row["END_AT"]),
                Location = row["LOCATION"] != DBNull.Value ? row["LOCATION"].ToString() : "",
                Points = row["POINTS"] != DBNull.Value ? (decimal?)Convert.ToDecimal(row["POINTS"]) : null,
                MaxParticipants = row["MAX_SEATS"] != DBNull.Value ? Convert.ToInt32(row["MAX_SEATS"]) : 0,
                CurrentParticipants = row["CURRENT_PARTICIPANTS"] != DBNull.Value ? Convert.ToInt32(row["CURRENT_PARTICIPANTS"]) : 0,
                Status = row["STATUS"].ToString(),
                RegistrationDeadline = null, // Không có trong schema
                CategoryName = row["CRITERION_NAME"] != DBNull.Value ? row["CRITERION_NAME"].ToString() : "",
                OrganizerName = row["ORGANIZER_NAME"] != DBNull.Value ? row["ORGANIZER_NAME"].ToString() : "",
                Requirements = "", // Không có trong schema
                Benefits = "", // Không có trong schema
                CreatedAt = Convert.ToDateTime(row["CREATED_AT"]),
                IsRegistered = row["REG_STATUS"] != DBNull.Value && row["REG_STATUS"].ToString() != "CANCELLED",
                RegistrationStatus = row["REG_STATUS"] != DBNull.Value ? row["REG_STATUS"].ToString() : "",
                RegisteredAt = row["REGISTERED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["REGISTERED_AT"]) : null,
                ProofStatus = row["PROOF_STATUS"] != DBNull.Value ? row["PROOF_STATUS"].ToString() : "NOT_UPLOADED",
                ProofFilePath = row["PROOF_FILE_PATH"] != DBNull.Value ? row["PROOF_FILE_PATH"].ToString() : "",
                ProofNote = row["PROOF_NOTE"] != DBNull.Value ? row["PROOF_NOTE"].ToString() : "",
                ProofUploadedAt = row["PROOF_UPLOADED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["PROOF_UPLOADED_AT"]) : null,
                ApprovedPoints = null // Không có trong schema
            };

            // Xác định các quyền
            DateTime now = DateTime.Now;
            viewModel.CanRegister = !viewModel.IsRegistered &&
                                   viewModel.Status == "ACTIVE" &&
                                   (viewModel.RegistrationDeadline == null || viewModel.RegistrationDeadline > now) &&
                                   viewModel.CurrentParticipants < viewModel.MaxParticipants;

            viewModel.CanCancelRegistration = viewModel.IsRegistered &&
                                             viewModel.RegistrationStatus == "REGISTERED" &&
                                             viewModel.StartAt > now;

            viewModel.CanUploadProof = viewModel.IsRegistered &&
                                      viewModel.RegistrationStatus == "CHECKED_IN" &&
                                      viewModel.ProofStatus != "APPROVED";

            return viewModel;
        }

        private UploadProofViewModel GetUploadProofViewModel(string activityId, string mand)
        {
            string query = @"SELECT a.TITLE, p.STORED_PATH, p.STATUS, p.CREATED_AT_UTC
                            FROM ACTIVITIES a
                            INNER JOIN REGISTRATIONS r ON a.ID = r.ACTIVITY_ID
                            LEFT JOIN PROOFS p ON r.ID = p.REGISTRATION_ID
                            WHERE a.ID = :ActivityId AND r.STUDENT_ID = :StudentId
                            ORDER BY p.CREATED_AT_UTC DESC
                            FETCH FIRST 1 ROWS ONLY";

            var parameters = new[]
            {
                OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId),
                OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
            };

            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new UploadProofViewModel
            {
                ActivityId = activityId,
                ActivityTitle = row["TITLE"].ToString(),
                CurrentProofFilePath = row["STORED_PATH"] != DBNull.Value ? row["STORED_PATH"].ToString() : "",
                CurrentProofStatus = row["STATUS"] != DBNull.Value ? row["STATUS"].ToString() : "NOT_UPLOADED",
                CurrentProofUploadedAt = row["CREATED_AT_UTC"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["CREATED_AT_UTC"]) : null
            };
        }
    }
}

