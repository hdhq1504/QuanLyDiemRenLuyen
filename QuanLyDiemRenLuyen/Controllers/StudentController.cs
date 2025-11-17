using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
    }
}

