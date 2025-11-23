using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;
using System.Web.Mvc;

namespace QuanLyDiemRenLuyen.Controllers.Student
{
    /// <summary>
    /// Controller xử lý Dashboard của sinh viên
    /// </summary>
    public class DashboardController : StudentBaseController
    {
        // GET: Student/Dashboard
        public ActionResult Index()
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                string mand = GetCurrentStudentId();

                var viewModel = new StudentDashboardViewModel
                {
                    User = GetUserInfo(mand),
                    StudentInfo = GetStudentInfo(mand),
                    TermScores = GetTermScores(mand),
                    RecentActivities = GetRecentActivities(mand),
                    UnreadNotifications = GetUnreadNotifications(mand),
                    Statistics = GetStatistics(mand)
                };

                return View("~/Views/Student/Dashboard.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Student/Dashboard.cshtml", new StudentDashboardViewModel());
            }
        }

        #region Private Helper Methods

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

        #endregion
    }
}
