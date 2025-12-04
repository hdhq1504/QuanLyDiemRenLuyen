using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;
using System.Web.Mvc;

namespace QuanLyDiemRenLuyen.Controllers.Lecturer
{
    /// <summary>
    /// Controller xử lý Dashboard của giảng viên
    /// </summary>
    public class DashboardController : LecturerBaseController
    {
        // GET: Lecturer/Dashboard
        public ActionResult Index()
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                string mand = GetCurrentUserId();

                var viewModel = new LecturerDashboardViewModel
                {
                    FullName = Session["FullName"]?.ToString() ?? "Giảng viên",
                    Email = Session["Email"]?.ToString() ?? "",
                    Statistics = GetStatistics(mand),
                    RecentActivities = GetRecentActivities(mand),
                    PendingApprovals = GetPendingApprovals(mand)
                };

                return View("~/Views/Lecturer/Dashboard.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Lecturer/Dashboard.cshtml", new LecturerDashboardViewModel());
            }
        }

        #region Private Helper Methods

        private LecturerStatistics GetStatistics(string mand)
        {
            var stats = new LecturerStatistics();

            try
            {
                // Tổng số hoạt động đã tạo
                string query1 = "SELECT COUNT(*) FROM ACTIVITIES WHERE ORGANIZER_ID = :MAND";
                var param = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
                stats.TotalActivities = Convert.ToInt32(OracleDbHelper.ExecuteScalar(query1, param));

                // Số hoạt động chờ duyệt
                string query2 = "SELECT COUNT(*) FROM ACTIVITIES WHERE ORGANIZER_ID = :MAND AND APPROVAL_STATUS = 'PENDING'";
                stats.PendingActivities = Convert.ToInt32(OracleDbHelper.ExecuteScalar(query2, param));

                // Số hoạt động đã duyệt
                string query3 = "SELECT COUNT(*) FROM ACTIVITIES WHERE ORGANIZER_ID = :MAND AND APPROVAL_STATUS = 'APPROVED'";
                stats.ApprovedActivities = Convert.ToInt32(OracleDbHelper.ExecuteScalar(query3, param));

                // Tổng số sinh viên đã đăng ký các hoạt động
                string query4 = @"SELECT COUNT(DISTINCT r.STUDENT_ID) 
                                  FROM ACTIVITIES a
                                  INNER JOIN REGISTRATIONS r ON a.ID = r.ACTIVITY_ID
                                  WHERE a.ORGANIZER_ID = :MAND";
                stats.TotalStudentsRegistered = Convert.ToInt32(OracleDbHelper.ExecuteScalar(query4, param));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetStatistics: {ex.Message}");
            }

            return stats;
        }

        private List<LecturerActivityItem> GetRecentActivities(string mand)
        {
            var activities = new List<LecturerActivityItem>();

            try
            {
                string query = @"SELECT a.ID, a.TITLE, a.START_AT, a.END_AT, a.STATUS, a.APPROVAL_STATUS,
                                       (SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STATUS != 'CANCELLED') as TOTAL_REGISTERED,
                                       (SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STATUS = 'CHECKED_IN') as TOTAL_CHECKED_IN
                                 FROM ACTIVITIES a
                                 WHERE a.ORGANIZER_ID = :MAND
                                 ORDER BY a.START_AT DESC
                                 FETCH FIRST 10 ROWS ONLY";

                var parameters = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
                DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

                foreach (DataRow row in dt.Rows)
                {
                    activities.Add(new LecturerActivityItem
                    {
                        Id = row["ID"].ToString(),
                        Title = row["TITLE"].ToString(),
                        StartAt = Convert.ToDateTime(row["START_AT"]),
                        EndAt = Convert.ToDateTime(row["END_AT"]),
                        Status = row["STATUS"].ToString(),
                        ApprovalStatus = row["APPROVAL_STATUS"].ToString(),
                        TotalRegistered = row["TOTAL_REGISTERED"] != DBNull.Value ? Convert.ToInt32(row["TOTAL_REGISTERED"]) : 0,
                        TotalCheckedIn = row["TOTAL_CHECKED_IN"] != DBNull.Value ? Convert.ToInt32(row["TOTAL_CHECKED_IN"]) : 0
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetRecentActivities: {ex.Message}");
            }

            return activities;
        }

        private List<PendingApprovalItem> GetPendingApprovals(string mand)
        {
            var approvals = new List<PendingApprovalItem>();

            try
            {
                // Lấy các minh chứng chờ duyệt từ các hoạt động của giảng viên (nếu giảng viên có quyền duyệt)
                string query = @"SELECT p.ID, p.FILE_NAME, p.CREATED_AT_UTC, s.FULL_NAME as STUDENT_NAME, a.TITLE as ACTIVITY_TITLE
                                 FROM PROOFS p
                                 INNER JOIN REGISTRATIONS r ON p.REGISTRATION_ID = r.ID
                                 INNER JOIN ACTIVITIES a ON r.ACTIVITY_ID = a.ID
                                 INNER JOIN USERS s ON r.STUDENT_ID = s.MAND
                                 WHERE a.ORGANIZER_ID = :MAND AND p.STATUS = 'SUBMITTED'
                                 ORDER BY p.CREATED_AT_UTC DESC
                                 FETCH FIRST 10 ROWS ONLY";

                var parameters = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
                DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

                foreach (DataRow row in dt.Rows)
                {
                    string encryptedFileName = row["FILE_NAME"].ToString();
                    string decryptedFileName = string.Empty;
                    try
                    {
                        decryptedFileName = EncryptionHelper.Decrypt(encryptedFileName);
                    }
                    catch
                    {
                        decryptedFileName = "File không xác định";
                    }

                    approvals.Add(new PendingApprovalItem
                    {
                        Id = row["ID"].ToString(),
                        Title = $"{row["STUDENT_NAME"]} - {row["ACTIVITY_TITLE"]}",
                        OrganizerName = decryptedFileName,
                        CreatedAt = Convert.ToDateTime(row["CREATED_AT_UTC"]),
                        Type = "PROOF"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetPendingApprovals: {ex.Message}");
            }

            return approvals;
        }

        #endregion
    }
}
