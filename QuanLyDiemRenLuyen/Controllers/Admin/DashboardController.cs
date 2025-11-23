using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;

namespace QuanLyDiemRenLuyen.Controllers.Admin
{
    public class DashboardController : AdminBaseController
    {
        // GET: Admin/Dashboard
        public ActionResult Index()
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = new AdminDashboardViewModel
                {
                    RecentActivities = new List<RecentActivityItem>(),
                    PendingApprovals = new List<PendingApprovalItem>()
                };

                // Thống kê hoạt động
                string statsQuery = @"
                    SELECT
                        (SELECT COUNT(*) FROM ACTIVITIES) as TOTAL_ACTIVITIES,
                        (SELECT COUNT(*) FROM ACTIVITIES WHERE APPROVAL_STATUS = 'PENDING') as PENDING_ACTIVITIES,
                        (SELECT COUNT(*) FROM ACTIVITIES WHERE APPROVAL_STATUS = 'APPROVED') as APPROVED_ACTIVITIES,
                        (SELECT COUNT(*) FROM ACTIVITIES WHERE APPROVAL_STATUS = 'REJECTED') as REJECTED_ACTIVITIES,
                        (SELECT COUNT(*) FROM STUDENTS) as TOTAL_STUDENTS,
                        (SELECT COUNT(*) FROM REGISTRATIONS WHERE STATUS != 'CANCELLED') as TOTAL_REGISTRATIONS,
                        (SELECT COUNT(*) FROM REGISTRATIONS WHERE STATUS = 'CHECKED_IN') as TOTAL_CHECKINS,
                        (SELECT COUNT(*) FROM PROOFS WHERE STATUS = 'SUBMITTED') as PENDING_PROOFS,
                        (SELECT COUNT(*) FROM PROOFS WHERE STATUS = 'APPROVED') as APPROVED_PROOFS
                    FROM DUAL";

                DataTable statsTable = OracleDbHelper.ExecuteQuery(statsQuery, null);
                if (statsTable.Rows.Count > 0)
                {
                    DataRow row = statsTable.Rows[0];
                    viewModel.TotalActivities = Convert.ToInt32(row["TOTAL_ACTIVITIES"]);
                    viewModel.PendingActivities = Convert.ToInt32(row["PENDING_ACTIVITIES"]);
                    viewModel.ApprovedActivities = Convert.ToInt32(row["APPROVED_ACTIVITIES"]);
                    viewModel.RejectedActivities = Convert.ToInt32(row["REJECTED_ACTIVITIES"]);
                    viewModel.TotalStudents = Convert.ToInt32(row["TOTAL_STUDENTS"]);
                    viewModel.TotalRegistrations = Convert.ToInt32(row["TOTAL_REGISTRATIONS"]);
                    viewModel.TotalCheckIns = Convert.ToInt32(row["TOTAL_CHECKINS"]);
                    viewModel.PendingProofs = Convert.ToInt32(row["PENDING_PROOFS"]);
                    viewModel.ApprovedProofs = Convert.ToInt32(row["APPROVED_PROOFS"]);
                }

                // Hoạt động gần đây
                string recentQuery = @"
                    SELECT a.ID, a.TITLE, a.CREATED_AT, a.STATUS, a.APPROVAL_STATUS,
                           (SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STATUS != 'CANCELLED') as REG_COUNT
                    FROM ACTIVITIES a
                    ORDER BY a.CREATED_AT DESC
                    FETCH FIRST 5 ROWS ONLY";

                DataTable recentTable = OracleDbHelper.ExecuteQuery(recentQuery, null);
                foreach (DataRow row in recentTable.Rows)
                {
                    viewModel.RecentActivities.Add(new RecentActivityItem
                    {
                        Id = row["ID"].ToString(),
                        Title = row["TITLE"].ToString(),
                        CreatedAt = Convert.ToDateTime(row["CREATED_AT"]),
                        Status = row["STATUS"].ToString(),
                        ApprovalStatus = row["APPROVAL_STATUS"].ToString(),
                        RegistrationCount = Convert.ToInt32(row["REG_COUNT"])
                    });
                }

                // Hoạt động chờ duyệt
                string pendingQuery = @"
                    SELECT a.ID, a.TITLE, u.FULL_NAME as ORGANIZER_NAME, a.CREATED_AT
                    FROM ACTIVITIES a
                    LEFT JOIN USERS u ON a.ORGANIZER_ID = u.MAND
                    WHERE a.APPROVAL_STATUS = 'PENDING'
                    ORDER BY a.CREATED_AT DESC
                    FETCH FIRST 5 ROWS ONLY";

                DataTable pendingTable = OracleDbHelper.ExecuteQuery(pendingQuery, null);
                foreach (DataRow row in pendingTable.Rows)
                {
                    viewModel.PendingApprovals.Add(new PendingApprovalItem
                    {
                        Id = row["ID"].ToString(),
                        Title = row["TITLE"].ToString(),
                        OrganizerName = row["ORGANIZER_NAME"] != DBNull.Value ? row["ORGANIZER_NAME"].ToString() : "",
                        CreatedAt = Convert.ToDateTime(row["CREATED_AT"]),
                        Type = "ACTIVITY"
                    });
                }

                // Phản hồi gần đây
                viewModel.RecentFeedbacks = new List<RecentFeedbackItem>();
                string feedbackQuery = @"
                    SELECT f.ID, f.TITLE, f.STATUS, f.CREATED_AT, u.FULL_NAME as STUDENT_NAME
                    FROM FEEDBACKS f
                    JOIN STUDENTS s ON f.STUDENT_ID = s.USER_ID
                    JOIN USERS u ON s.USER_ID = u.MAND
                    ORDER BY f.CREATED_AT DESC
                    FETCH FIRST 5 ROWS ONLY";

                DataTable feedbackTable = OracleDbHelper.ExecuteQuery(feedbackQuery, null);
                foreach (DataRow row in feedbackTable.Rows)
                {
                    viewModel.RecentFeedbacks.Add(new RecentFeedbackItem
                    {
                        Id = row["ID"].ToString(),
                        Title = row["TITLE"].ToString(),
                        StudentName = row["STUDENT_NAME"].ToString(),
                        Status = row["STATUS"].ToString(),
                        CreatedAt = Convert.ToDateTime(row["CREATED_AT"])
                    });
                }

                return View("~/Views/Admin/Dashboard.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/Dashboard.cshtml", new AdminDashboardViewModel
                {
                    RecentActivities = new List<RecentActivityItem>(),
                    PendingApprovals = new List<PendingApprovalItem>(),
                    RecentFeedbacks = new List<RecentFeedbackItem>()
                });
            }
        }
    }
}
