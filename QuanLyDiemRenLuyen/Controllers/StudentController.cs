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

        // ==================== SCORE MANAGEMENT ====================

        // GET: Student/Scores
        public ActionResult Scores()
        {
            try
            {
                string mand = Session["MAND"]?.ToString();
                if (string.IsNullOrEmpty(mand))
                {
                    return RedirectToAction("Login", "Account");
                }

                var viewModel = GetScoresViewModel(mand);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View(new ScoreViewModel());
            }
        }

        // GET: Student/ScoreDetail
        public ActionResult ScoreDetail(string scoreId)
        {
            try
            {
                string mand = Session["MAND"]?.ToString();
                if (string.IsNullOrEmpty(mand))
                {
                    return RedirectToAction("Login", "Account");
                }

                if (string.IsNullOrEmpty(scoreId))
                {
                    TempData["ErrorMessage"] = "Không tìm thấy điểm";
                    return RedirectToAction("Scores");
                }

                var viewModel = GetScoreDetailViewModel(mand, scoreId);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy điểm";
                    return RedirectToAction("Scores");
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Scores");
            }
        }

        // GET: Student/CreateReviewRequest
        public ActionResult CreateReviewRequest(string scoreId)
        {
            try
            {
                string mand = Session["MAND"]?.ToString();
                if (string.IsNullOrEmpty(mand))
                {
                    return RedirectToAction("Login", "Account");
                }

                if (string.IsNullOrEmpty(scoreId))
                {
                    TempData["ErrorMessage"] = "Không tìm thấy điểm";
                    return RedirectToAction("Scores");
                }

                // Kiểm tra xem điểm có tồn tại và thuộc về sinh viên này không
                string checkQuery = @"SELECT s.ID, s.TERM_ID, t.NAME as TERM_NAME, s.TOTAL as TOTAL_SCORE
                                     FROM SCORES s
                                     INNER JOIN TERMS t ON s.TERM_ID = t.ID
                                     WHERE s.ID = :ScoreId AND s.STUDENT_ID = :StudentId";

                var checkParams = new[]
                {
                    OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
                };

                DataTable dt = OracleDbHelper.ExecuteQuery(checkQuery, checkParams);
                if (dt.Rows.Count == 0)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy điểm";
                    return RedirectToAction("Scores");
                }

                var model = new CreateReviewRequestViewModel
                {
                    ScoreId = scoreId,
                    TermId = dt.Rows[0]["TERM_ID"].ToString(),
                    TermName = dt.Rows[0]["TERM_NAME"].ToString(),
                    CurrentScore = Convert.ToDecimal(dt.Rows[0]["TOTAL_SCORE"])
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Scores");
            }
        }

        // POST: Student/CreateReviewRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateReviewRequest(CreateReviewRequestViewModel model)
        {
            try
            {
                string mand = Session["MAND"]?.ToString();
                if (string.IsNullOrEmpty(mand))
                {
                    return RedirectToAction("Login", "Account");
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Kiểm tra xem đã có đơn phúc khảo chưa
                string checkQuery = @"SELECT COUNT(*) FROM FEEDBACKS
                                     WHERE STUDENT_ID = :StudentId
                                     AND TERM_ID = :TermId
                                     AND STATUS IN ('SUBMITTED', 'IN_REVIEW')";

                var checkParams = new[]
                {
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, model.TermId)
                };

                int existingCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(checkQuery, checkParams));
                if (existingCount > 0)
                {
                    TempData["ErrorMessage"] = "Bạn đã có đơn phúc khảo đang chờ xử lý cho học kỳ này";
                    return RedirectToAction("ScoreDetail", new { scoreId = model.ScoreId });
                }

                // Tạo ID mới
                string newId = "FB" + DateTime.Now.ToString("yyyyMMddHHmmss");

                // Thêm đơn phúc khảo
                string insertQuery = @"INSERT INTO FEEDBACKS
                                      (ID, STUDENT_ID, TERM_ID, CRITERION_ID, TITLE, CONTENT,
                                       REQUESTED_SCORE, STATUS, CREATED_AT)
                                      VALUES
                                      (:Id, :StudentId, :TermId, :CriterionId, :Title, :Content,
                                       :RequestedScore, 'SUBMITTED', SYSDATE)";

                var insertParams = new[]
                {
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, newId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, model.TermId),
                    OracleDbHelper.CreateParameter("CriterionId", OracleDbType.Varchar2,
                        string.IsNullOrEmpty(model.CriterionId) ? (object)DBNull.Value : model.CriterionId),
                    OracleDbHelper.CreateParameter("Title", OracleDbType.Varchar2, model.Title),
                    OracleDbHelper.CreateParameter("Content", OracleDbType.Clob, model.Content),
                    OracleDbHelper.CreateParameter("RequestedScore", OracleDbType.Decimal, model.RequestedScore)
                };

                OracleDbHelper.ExecuteNonQuery(insertQuery, insertParams);

                TempData["SuccessMessage"] = "Gửi đơn phúc khảo thành công";
                return RedirectToAction("ScoreDetail", new { scoreId = model.ScoreId });
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View(model);
            }
        }

        private ScoreViewModel GetScoresViewModel(string mand)
        {
            var viewModel = new ScoreViewModel();

            // Lấy thông tin sinh viên
            string studentQuery = @"SELECT u.FULL_NAME, s.STUDENT_CODE, c.NAME as CLASS_NAME
                                   FROM USERS u
                                   INNER JOIN STUDENTS s ON u.MAND = s.USER_ID
                                   LEFT JOIN CLASSES c ON s.CLASS_ID = c.ID
                                   WHERE u.MAND = :MAND";

            var studentParams = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
            DataTable studentDt = OracleDbHelper.ExecuteQuery(studentQuery, studentParams);

            if (studentDt.Rows.Count > 0)
            {
                viewModel.StudentId = mand;
                viewModel.StudentName = studentDt.Rows[0]["FULL_NAME"].ToString();
                viewModel.StudentCode = studentDt.Rows[0]["STUDENT_CODE"].ToString();
                viewModel.ClassName = studentDt.Rows[0]["CLASS_NAME"] != DBNull.Value ? studentDt.Rows[0]["CLASS_NAME"].ToString() : "";
            }

            // Lấy danh sách điểm theo học kỳ
            string scoresQuery = @"SELECT s.ID, s.TERM_ID, t.NAME as TERM_NAME, t.YEAR as TERM_YEAR,
                                         t.TERM_NUMBER, s.TOTAL, s.STATUS, s.APPROVED_BY,
                                         u.FULL_NAME as APPROVED_BY_NAME, s.APPROVED_AT, s.CREATED_AT
                                  FROM SCORES s
                                  INNER JOIN TERMS t ON s.TERM_ID = t.ID
                                  LEFT JOIN USERS u ON s.APPROVED_BY = u.MAND
                                  WHERE s.STUDENT_ID = :StudentId
                                  ORDER BY t.YEAR DESC, t.TERM_NUMBER DESC";

            var scoresParams = new[] { OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand) };
            DataTable scoresDt = OracleDbHelper.ExecuteQuery(scoresQuery, scoresParams);

            viewModel.TermScores = new List<TermScoreItem>();
            foreach (DataRow row in scoresDt.Rows)
            {
                decimal total = Convert.ToDecimal(row["TOTAL"]);
                string classification = GetClassification(total);
                string scoreId = row["ID"].ToString();
                string status = row["STATUS"].ToString();

                // Kiểm tra xem có đơn phúc khảo đang chờ không
                string checkFeedbackQuery = @"SELECT COUNT(*) FROM FEEDBACKS
                                             WHERE STUDENT_ID = :StudentId
                                             AND TERM_ID = :TermId
                                             AND STATUS IN ('SUBMITTED', 'IN_REVIEW')";

                var feedbackParams = new[]
                {
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, row["TERM_ID"].ToString())
                };

                int pendingFeedbackCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(checkFeedbackQuery, feedbackParams));

                viewModel.TermScores.Add(new TermScoreItem
                {
                    ScoreId = scoreId,
                    TermId = row["TERM_ID"].ToString(),
                    TermName = row["TERM_NAME"].ToString(),
                    TermYear = Convert.ToInt32(row["TERM_YEAR"]),
                    TermNumber = Convert.ToInt32(row["TERM_NUMBER"]),
                    Total = total,
                    Status = status,
                    Classification = classification,
                    ApprovedBy = row["APPROVED_BY"] != DBNull.Value ? row["APPROVED_BY"].ToString() : null,
                    ApprovedByName = row["APPROVED_BY_NAME"] != DBNull.Value ? row["APPROVED_BY_NAME"].ToString() : null,
                    ApprovedAt = row["APPROVED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["APPROVED_AT"]) : null,
                    CreatedAt = Convert.ToDateTime(row["CREATED_AT"]),
                    CanRequestReview = status == "APPROVED" && pendingFeedbackCount == 0,
                    HasPendingReview = pendingFeedbackCount > 0
                });
            }

            // Tính thống kê
            viewModel.Statistics = new ScoreStatistics();
            if (viewModel.TermScores.Count > 0)
            {
                viewModel.Statistics.AverageScore = viewModel.TermScores.Average(x => x.Total);
                viewModel.Statistics.HighestScore = viewModel.TermScores.Max(x => x.Total);
                viewModel.Statistics.LowestScore = viewModel.TermScores.Min(x => x.Total);
                viewModel.Statistics.TotalTerms = viewModel.TermScores.Count;
                viewModel.Statistics.ApprovedTerms = viewModel.TermScores.Count(x => x.Status == "APPROVED");
            }

            return viewModel;
        }



        private ScoreDetailViewModel GetScoreDetailViewModel(string mand, string scoreId)
        {
            // Lấy thông tin điểm cơ bản
            string scoreQuery = @"SELECT s.ID, s.STUDENT_ID, s.TERM_ID, t.NAME as TERM_NAME,
                                        t.YEAR as TERM_YEAR, t.TERM_NUMBER, s.TOTAL, s.STATUS,
                                        s.APPROVED_BY, u.FULL_NAME as APPROVED_BY_NAME,
                                        s.APPROVED_AT, s.CREATED_AT,
                                        st.STUDENT_CODE, us.FULL_NAME as STUDENT_NAME,
                                        c.NAME as CLASS_NAME
                                 FROM SCORES s
                                 INNER JOIN TERMS t ON s.TERM_ID = t.ID
                                 INNER JOIN STUDENTS st ON s.STUDENT_ID = st.USER_ID
                                 INNER JOIN USERS us ON st.USER_ID = us.MAND
                                 LEFT JOIN CLASSES c ON st.CLASS_ID = c.ID
                                 LEFT JOIN USERS u ON s.APPROVED_BY = u.MAND
                                 WHERE s.ID = :ScoreId AND s.STUDENT_ID = :StudentId";

            var scoreParams = new[]
            {
                OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId),
                OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
            };

            DataTable scoreDt = OracleDbHelper.ExecuteQuery(scoreQuery, scoreParams);
            if (scoreDt.Rows.Count == 0) return null;

            DataRow scoreRow = scoreDt.Rows[0];
            decimal total = Convert.ToDecimal(scoreRow["TOTAL"]);

            var viewModel = new ScoreDetailViewModel
            {
                ScoreId = scoreId,
                StudentId = mand,
                StudentName = scoreRow["STUDENT_NAME"].ToString(),
                StudentCode = scoreRow["STUDENT_CODE"].ToString(),
                ClassName = scoreRow["CLASS_NAME"] != DBNull.Value ? scoreRow["CLASS_NAME"].ToString() : "",
                TermId = scoreRow["TERM_ID"].ToString(),
                TermName = scoreRow["TERM_NAME"].ToString(),
                TermYear = Convert.ToInt32(scoreRow["TERM_YEAR"]),
                TermNumber = Convert.ToInt32(scoreRow["TERM_NUMBER"]),
                Total = total,
                Status = scoreRow["STATUS"].ToString(),
                Classification = GetClassification(total),
                ApprovedBy = scoreRow["APPROVED_BY"] != DBNull.Value ? scoreRow["APPROVED_BY"].ToString() : null,
                ApprovedByName = scoreRow["APPROVED_BY_NAME"] != DBNull.Value ? scoreRow["APPROVED_BY_NAME"].ToString() : null,
                ApprovedAt = scoreRow["APPROVED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(scoreRow["APPROVED_AT"]) : null,
                CreatedAt = Convert.ToDateTime(scoreRow["CREATED_AT"])
            };

            // Lấy điểm theo tiêu chí
            string criteriaQuery = @"SELECT c.ID, c.NAME, c.MAX_POINTS,
                                           COALESCE(SUM(a.POINTS), 0) as EARNED_POINTS,
                                           COUNT(DISTINCT r.ACTIVITY_ID) as ACTIVITY_COUNT
                                    FROM CRITERIA c
                                    LEFT JOIN ACTIVITIES a ON c.ID = a.CRITERION_ID
                                    LEFT JOIN REGISTRATIONS r ON a.ID = r.ACTIVITY_ID
                                        AND r.STUDENT_ID = :StudentId
                                        AND r.STATUS = 'CHECKED_IN'
                                    WHERE c.TERM_ID = :TermId
                                    GROUP BY c.ID, c.NAME, c.MAX_POINTS
                                    ORDER BY c.ID";

            var criteriaParams = new[]
            {
                OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand),
                OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, viewModel.TermId)
            };

            DataTable criteriaDt = OracleDbHelper.ExecuteQuery(criteriaQuery, criteriaParams);
            viewModel.CriterionScores = new List<CriterionScoreItem>();

            foreach (DataRow row in criteriaDt.Rows)
            {
                string criterionId = row["ID"].ToString();
                var criterionItem = new CriterionScoreItem
                {
                    CriterionId = criterionId,
                    CriterionName = row["NAME"].ToString(),
                    MaxPoints = Convert.ToDecimal(row["MAX_POINTS"]),
                    EarnedPoints = Convert.ToDecimal(row["EARNED_POINTS"]),
                    ActivityCount = Convert.ToInt32(row["ACTIVITY_COUNT"]),
                    Activities = new List<ActivityScoreItem>()
                };

                // Lấy danh sách hoạt động của tiêu chí này
                string activitiesQuery = @"SELECT a.ID, a.TITLE, a.POINTS, a.START_AT, r.STATUS
                                          FROM ACTIVITIES a
                                          INNER JOIN REGISTRATIONS r ON a.ID = r.ACTIVITY_ID
                                          WHERE a.CRITERION_ID = :CriterionId
                                          AND r.STUDENT_ID = :StudentId
                                          AND r.STATUS = 'CHECKED_IN'
                                          ORDER BY a.START_AT DESC";

                var activityParams = new[]
                {
                    OracleDbHelper.CreateParameter("CriterionId", OracleDbType.Varchar2, criterionId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
                };

                DataTable activitiesDt = OracleDbHelper.ExecuteQuery(activitiesQuery, activityParams);
                foreach (DataRow actRow in activitiesDt.Rows)
                {
                    criterionItem.Activities.Add(new ActivityScoreItem
                    {
                        ActivityId = actRow["ID"].ToString(),
                        ActivityTitle = actRow["TITLE"].ToString(),
                        Points = actRow["POINTS"] != DBNull.Value ? Convert.ToDecimal(actRow["POINTS"]) : 0,
                        Date = Convert.ToDateTime(actRow["START_AT"]),
                        Status = actRow["STATUS"].ToString()
                    });
                }

                viewModel.CriterionScores.Add(criterionItem);
            }

            // Lấy lịch sử thay đổi điểm
            string historyQuery = @"SELECT ACTION, OLD_VALUE, NEW_VALUE, CHANGED_BY,
                                          u.FULL_NAME as CHANGED_BY_NAME, REASON, CHANGED_AT
                                   FROM SCORE_HISTORY sh
                                   LEFT JOIN USERS u ON sh.CHANGED_BY = u.MAND
                                   WHERE sh.SCORE_ID = :ScoreId
                                   ORDER BY sh.CHANGED_AT DESC";

            var historyParams = new[] { OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId) };
            DataTable historyDt = OracleDbHelper.ExecuteQuery(historyQuery, historyParams);

            viewModel.History = new List<ScoreHistoryItem>();
            foreach (DataRow row in historyDt.Rows)
            {
                viewModel.History.Add(new ScoreHistoryItem
                {
                    Action = row["ACTION"].ToString(),
                    OldValue = row["OLD_VALUE"] != DBNull.Value ? row["OLD_VALUE"].ToString() : null,
                    NewValue = row["NEW_VALUE"] != DBNull.Value ? row["NEW_VALUE"].ToString() : null,
                    ChangedBy = row["CHANGED_BY"] != DBNull.Value ? row["CHANGED_BY"].ToString() : null,
                    ChangedByName = row["CHANGED_BY_NAME"] != DBNull.Value ? row["CHANGED_BY_NAME"].ToString() : "Hệ thống",
                    Reason = row["REASON"] != DBNull.Value ? row["REASON"].ToString() : null,
                    ChangedAt = Convert.ToDateTime(row["CHANGED_AT"])
                });
            }

            // Lấy thông tin đơn phúc khảo (nếu có)
            string feedbackQuery = @"SELECT f.ID, f.TITLE, f.CONTENT, f.REQUESTED_SCORE, f.STATUS,
                                           f.RESPONSE, f.RESPONDED_BY, u.FULL_NAME as RESPONDED_BY_NAME,
                                           f.RESPONDED_AT, f.CREATED_AT
                                    FROM FEEDBACKS f
                                    LEFT JOIN USERS u ON f.RESPONDED_BY = u.MAND
                                    WHERE f.STUDENT_ID = :StudentId AND f.TERM_ID = :TermId
                                    ORDER BY f.CREATED_AT DESC
                                    FETCH FIRST 1 ROWS ONLY";

            var feedbackParams = new[]
            {
                OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand),
                OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, viewModel.TermId)
            };

            DataTable feedbackDt = OracleDbHelper.ExecuteQuery(feedbackQuery, feedbackParams);
            if (feedbackDt.Rows.Count > 0)
            {
                DataRow fbRow = feedbackDt.Rows[0];
                viewModel.ReviewRequest = new ReviewRequestInfo
                {
                    Id = fbRow["ID"].ToString(),
                    Title = fbRow["TITLE"].ToString(),
                    Content = fbRow["CONTENT"].ToString(),
                    RequestedScore = Convert.ToDecimal(fbRow["REQUESTED_SCORE"]),
                    Status = fbRow["STATUS"].ToString(),
                    Response = fbRow["RESPONSE"] != DBNull.Value ? fbRow["RESPONSE"].ToString() : null,
                    RespondedBy = fbRow["RESPONDED_BY"] != DBNull.Value ? fbRow["RESPONDED_BY"].ToString() : null,
                    RespondedByName = fbRow["RESPONDED_BY_NAME"] != DBNull.Value ? fbRow["RESPONDED_BY_NAME"].ToString() : null,
                    RespondedAt = fbRow["RESPONDED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(fbRow["RESPONDED_AT"]) : null,
                    CreatedAt = Convert.ToDateTime(fbRow["CREATED_AT"])
                };
            }

            return viewModel;
        }

        // ==================== NOTIFICATIONS ====================

        // GET: Student/Notifications
        public ActionResult Notifications(int page = 1, string filter = "all")
        {
            try
            {
                string mand = Session["MAND"]?.ToString();
                if (string.IsNullOrEmpty(mand))
                {
                    return RedirectToAction("Login", "Account");
                }

                const int pageSize = 10;
                var viewModel = new NotificationsViewModel
                {
                    CurrentPage = page,
                    CurrentFilter = filter
                };

                // Get notifications with pagination
                var notifications = GetNotificationsList(mand, filter, page, pageSize, out int totalCount);
                viewModel.Notifications = notifications;
                viewModel.TotalCount = totalCount;
                viewModel.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                viewModel.UnreadCount = GetUnreadCount(mand);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View(new NotificationsViewModel());
            }
        }

        // POST: Student/MarkAsRead
        [HttpPost]
        public JsonResult MarkAsRead(string notificationId)
        {
            try
            {
                string mand = Session["MAND"]?.ToString();
                if (string.IsNullOrEmpty(mand))
                {
                    return Json(new { success = false, message = "Chưa đăng nhập" });
                }

                // MERGE để đảm bảo chỉ có một record
                string mergeQuery = @"MERGE INTO NOTIFICATION_READS nr
                                     USING (SELECT :NotificationId AS NID, :StudentId AS SID FROM DUAL) src
                                     ON (nr.NOTIFICATION_ID = src.NID AND nr.STUDENT_ID = src.SID)
                                     WHEN MATCHED THEN
                                         UPDATE SET IS_READ = 1, READ_AT = SYSTIMESTAMP
                                     WHEN NOT MATCHED THEN
                                         INSERT (ID, NOTIFICATION_ID, STUDENT_ID, IS_READ, READ_AT)
                                         VALUES (RAWTOHEX(SYS_GUID()), src.NID, src.SID, 1, SYSTIMESTAMP)";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("NotificationId", OracleDbType.Varchar2, notificationId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
                };

                int result = OracleDbHelper.ExecuteNonQuery(mergeQuery, parameters);
                int unreadCount = GetUnreadCount(mand);

                return Json(new { success = true, unreadCount = unreadCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Student/MarkAllAsRead
        [HttpPost]
        public JsonResult MarkAllAsRead()
        {
            try
            {
                string mand = Session["MAND"]?.ToString();
                if (string.IsNullOrEmpty(mand))
                {
                    return Json(new { success = false, message = "Chưa đăng nhập" });
                }

                // Lấy tất cả notification IDs cho student này
                string getNotificationsQuery = @"SELECT n.ID
                                                FROM NOTIFICATIONS n
                                                WHERE (n.TO_USER_ID = :StudentId OR
                                                       (n.TO_USER_ID IS NULL AND n.TARGET_ROLE = 'STUDENT'))";

                var getParams = new[] { OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand) };
                DataTable notificationsDt = OracleDbHelper.ExecuteQuery(getNotificationsQuery, getParams);

                // MERGE từng notification
                foreach (DataRow row in notificationsDt.Rows)
                {
                    string notificationId = row["ID"].ToString();
                    string mergeQuery = @"MERGE INTO NOTIFICATION_READS nr
                                         USING (SELECT :NotificationId AS NID, :StudentId AS SID FROM DUAL) src
                                         ON (nr.NOTIFICATION_ID = src.NID AND nr.STUDENT_ID = src.SID)
                                         WHEN MATCHED THEN
                                             UPDATE SET IS_READ = 1, READ_AT = SYSTIMESTAMP
                                         WHEN NOT MATCHED THEN
                                             INSERT (ID, NOTIFICATION_ID, STUDENT_ID, IS_READ, READ_AT)
                                             VALUES (RAWTOHEX(SYS_GUID()), src.NID, src.SID, 1, SYSTIMESTAMP)";

                    var mergeParams = new[]
                    {
                        OracleDbHelper.CreateParameter("NotificationId", OracleDbType.Varchar2, notificationId),
                        OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
                    };

                    OracleDbHelper.ExecuteNonQuery(mergeQuery, mergeParams);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private List<NotificationItem> GetNotificationsList(string mand, string filter, int page, int pageSize, out int totalCount)
        {
            var notifications = new List<NotificationItem>();

            // Build WHERE clause based on filter
            string filterClause = "";
            if (filter == "unread")
            {
                filterClause = " AND COALESCE(nr.IS_READ, 0) = 0";
            }
            else if (filter == "read")
            {
                filterClause = " AND COALESCE(nr.IS_READ, 0) = 1";
            }

            // Get total count
            string countQuery = $@"SELECT COUNT(*)
                                  FROM NOTIFICATIONS n
                                  LEFT JOIN NOTIFICATION_READS nr ON n.ID = nr.NOTIFICATION_ID AND nr.STUDENT_ID = :StudentId
                                  WHERE (n.TO_USER_ID = :StudentId OR
                                         (n.TO_USER_ID IS NULL AND n.TARGET_ROLE = 'STUDENT'))
                                  {filterClause}";

            var countParams = new[] { OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand) };
            totalCount = Convert.ToInt32(ExecuteScalar(countQuery, countParams));

            // Get paginated notifications
            int offset = (page - 1) * pageSize;
            string query = $@"SELECT * FROM (
                                SELECT n.ID, n.TITLE, n.CONTENT, n.CREATED_AT,
                                       COALESCE(nr.IS_READ, 0) AS IS_READ,
                                       nr.READ_AT,
                                       ROW_NUMBER() OVER (ORDER BY n.CREATED_AT DESC) AS RN
                                FROM NOTIFICATIONS n
                                LEFT JOIN NOTIFICATION_READS nr ON n.ID = nr.NOTIFICATION_ID AND nr.STUDENT_ID = :StudentId
                                WHERE (n.TO_USER_ID = :StudentId OR
                                       (n.TO_USER_ID IS NULL AND n.TARGET_ROLE = 'STUDENT'))
                                {filterClause}
                            )
                            WHERE RN > :Offset AND RN <= :EndRow";

            var parameters = new[]
            {
                OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand),
                OracleDbHelper.CreateParameter("Offset", OracleDbType.Int32, offset),
                OracleDbHelper.CreateParameter("EndRow", OracleDbType.Int32, offset + pageSize)
            };

            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

            foreach (DataRow row in dt.Rows)
            {
                notifications.Add(new NotificationItem
                {
                    Id = row["ID"].ToString(),
                    Title = row["TITLE"].ToString(),
                    Content = row["CONTENT"] != DBNull.Value ? row["CONTENT"].ToString() : "",
                    CreatedAt = Convert.ToDateTime(row["CREATED_AT"]),
                    IsRead = Convert.ToInt32(row["IS_READ"]) == 1,
                    ReadAt = row["READ_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["READ_AT"]) : null
                });
            }

            return notifications;
        }

        private int GetUnreadCount(string mand)
        {
            string query = @"SELECT COUNT(*)
                           FROM NOTIFICATIONS n
                           LEFT JOIN NOTIFICATION_READS nr ON n.ID = nr.NOTIFICATION_ID AND nr.STUDENT_ID = :StudentId
                           WHERE (n.TO_USER_ID = :StudentId OR
                                  (n.TO_USER_ID IS NULL AND n.TARGET_ROLE = 'STUDENT'))
                           AND COALESCE(nr.IS_READ, 0) = 0";

            var parameters = new[] { OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand) };
            return Convert.ToInt32(OracleDbHelper.ExecuteScalar(query, parameters));
        }

        private object ExecuteScalar(string query, OracleParameter[] parameters)
        {
            return OracleDbHelper.ExecuteScalar(query, parameters);
        }

        private string GetClassification(decimal score)
        {
            if (score >= 90) return "Xuất sắc";
            if (score >= 80) return "Giỏi";
            if (score >= 65) return "Khá";
            if (score >= 50) return "Trung bình";
            return "Yếu";
        }
    }
}

