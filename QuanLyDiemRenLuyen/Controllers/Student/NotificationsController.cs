using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;

namespace QuanLyDiemRenLuyen.Controllers.Student
{
    /// <summary>
    /// Controller xử lý Notifications của sinh viên
    /// </summary>
    public class NotificationsController : StudentBaseController
    {
        // GET: Student/Notifications
        public ActionResult Index(int page = 1, string filter = "all")
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                string mand = GetCurrentStudentId();

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

                return View("~/Views/Student/Notifications.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Student/Notifications.cshtml", new NotificationsViewModel());
            }
        }

        // POST: Student/Notifications/MarkAsRead
        [HttpPost]
        public JsonResult MarkAsRead(string notificationId)
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null)
                {
                    return Json(new { success = false, message = "Chưa đăng nhập" });
                }

                string mand = GetCurrentStudentId();

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

        // POST: Student/Notifications/MarkAllAsRead
        [HttpPost]
        public JsonResult MarkAllAsRead()
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null)
                {
                    return Json(new { success = false, message = "Chưa đăng nhập" });
                }

                string mand = GetCurrentStudentId();

                // Lấy tất cả notification IDs cho student này
                string getNotificationsQuery = @"SELECT n.ID
                                                FROM NOTIFICATIONS n
                                                WHERE (n.TO_USER_ID = :MAND)
                                                   OR (n.TO_USER_ID IS NULL AND n.TARGET_ROLE = 'STUDENT')
                                                   OR (n.TO_USER_ID IS NULL AND n.TARGET_ROLE IS NULL)";

                var getParams = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
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

        #region Private Helper Methods

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
                                  LEFT JOIN NOTIFICATION_READS nr ON n.ID = nr.NOTIFICATION_ID AND nr.STUDENT_ID = :MAND
                                  WHERE ((n.TO_USER_ID = :MAND)
                                     OR (n.TO_USER_ID IS NULL AND n.TARGET_ROLE = 'STUDENT')
                                     OR (n.TO_USER_ID IS NULL AND n.TARGET_ROLE IS NULL))
                                  {filterClause}";

            var countParams = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
            totalCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(countQuery, countParams));

            // Get paginated notifications
            int offset = (page - 1) * pageSize;
            string query = $@"SELECT * FROM (
                                SELECT n.ID, n.TITLE, n.CONTENT, n.CREATED_AT,
                                       COALESCE(nr.IS_READ, 0) AS IS_READ,
                                       nr.READ_AT,
                                       ROW_NUMBER() OVER (ORDER BY n.CREATED_AT DESC) AS RN
                                FROM NOTIFICATIONS n
                                LEFT JOIN NOTIFICATION_READS nr ON n.ID = nr.NOTIFICATION_ID AND nr.STUDENT_ID = :MAND
                                WHERE ((n.TO_USER_ID = :MAND)
                                   OR (n.TO_USER_ID IS NULL AND n.TARGET_ROLE = 'STUDENT')
                                   OR (n.TO_USER_ID IS NULL AND n.TARGET_ROLE IS NULL))
                                {filterClause}
                            )
                            WHERE RN > :Offset AND RN <= :EndRow";

            var parameters = new[]
            {
                OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand),
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
                           LEFT JOIN NOTIFICATION_READS nr ON n.ID = nr.NOTIFICATION_ID AND nr.STUDENT_ID = :MAND
                           WHERE ((n.TO_USER_ID = :MAND)
                              OR (n.TO_USER_ID IS NULL AND n.TARGET_ROLE = 'STUDENT')
                              OR (n.TO_USER_ID IS NULL AND n.TARGET_ROLE IS NULL))
                           AND COALESCE(nr.IS_READ, 0) = 0";

            var parameters = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
            return Convert.ToInt32(OracleDbHelper.ExecuteScalar(query, parameters));
        }

        #endregion
    }
}
