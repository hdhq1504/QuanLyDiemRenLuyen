using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;

namespace QuanLyDiemRenLuyen.Controllers
{
    public class AdminController : Controller
    {
        // ==================== AUTHENTICATION CHECK ====================
        private bool IsAdmin()
        {
            string role = Session["RoleName"]?.ToString();
            return role == "ADMIN" || role == "LECTURER";
        }

        private ActionResult CheckAuth()
        {
            if (Session["MAND"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này";
                return RedirectToAction("Index", "Home");
            }
            return null;
        }

        // ==================== DASHBOARD ====================

        // GET: Admin/Dashboard
        public ActionResult Dashboard()
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

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View(new AdminDashboardViewModel
                {
                    RecentActivities = new List<RecentActivityItem>(),
                    PendingApprovals = new List<PendingApprovalItem>()
                });
            }
        }

        // ==================== ACTIVITY MANAGEMENT ====================

        // GET: Admin/Activities
        public ActionResult Activities(string search, string status, string approvalStatus, int page = 1)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = new AdminActivityListViewModel
                {
                    SearchKeyword = search,
                    FilterStatus = status ?? "ALL",
                    FilterApprovalStatus = approvalStatus ?? "ALL",
                    CurrentPage = page,
                    PageSize = 20
                };

                viewModel.Activities = GetActivitiesList(search, status, approvalStatus, page, viewModel.PageSize, out int totalCount);
                viewModel.TotalPages = (int)Math.Ceiling((double)totalCount / viewModel.PageSize);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View(new AdminActivityListViewModel { Activities = new List<AdminActivityItem>() });
            }
        }

        // GET: Admin/ApproveActivity/id
        public ActionResult ApproveActivity(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetActivityForApproval(id);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hoạt động";
                    return RedirectToAction("Activities");
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Activities");
            }
        }

        // POST: Admin/ApproveActivity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveActivity(ApprovalActionViewModel model)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ";
                return RedirectToAction("ApproveActivity", new { id = model.ActivityId });
            }

            try
            {
                string mand = Session["MAND"].ToString();
                string newStatus = model.Action == "APPROVE" ? "APPROVED" : "REJECTED";

                string updateQuery = @"UPDATE ACTIVITIES
                                      SET APPROVAL_STATUS = :ApprovalStatus,
                                          APPROVED_BY = :ApprovedBy,
                                          APPROVED_AT = SYSTIMESTAMP
                                      WHERE ID = :ActivityId";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("ApprovalStatus", OracleDbType.Varchar2, newStatus),
                    OracleDbHelper.CreateParameter("ApprovedBy", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, model.ActivityId)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = model.Action == "APPROVE"
                        ? "Đã phê duyệt hoạt động thành công"
                        : "Đã từ chối hoạt động";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể cập nhật trạng thái";
                }

                return RedirectToAction("Activities");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("ApproveActivity", new { id = model.ActivityId });
            }
        }

        // GET: Admin/ViewRegistrations/activityId
        public ActionResult ViewRegistrations(string id, string filterStatus, string search, int page = 1)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetRegistrations(id, filterStatus, search, page, 20, out int totalCount);
                viewModel.CurrentPage = page;
                viewModel.TotalPages = (int)Math.Ceiling((double)totalCount / viewModel.PageSize);
                viewModel.FilterStatus = filterStatus ?? "ALL";
                viewModel.SearchKeyword = search;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View(new RegistrationManagementViewModel
                {
                    Registrations = new List<RegistrationItem>()
                });
            }
        }

        // ==================== HELPER METHODS ====================

        private List<AdminActivityItem> GetActivitiesList(string search, string status, string approvalStatus, int page, int pageSize, out int totalCount)
        {
            var activities = new List<AdminActivityItem>();
            totalCount = 0;

            try
            {
                string countQuery = @"SELECT COUNT(*) FROM ACTIVITIES a WHERE 1=1";
                string dataQuery = @"SELECT a.ID, a.TITLE, a.DESCRIPTION, a.START_AT, a.END_AT, a.LOCATION,
                                           a.POINTS, a.MAX_SEATS, a.STATUS, a.APPROVAL_STATUS, a.CREATED_AT,
                                           cr.NAME as CRITERION_NAME, u.FULL_NAME as ORGANIZER_NAME,
                                           (SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STATUS != 'CANCELLED') as CURRENT_PARTICIPANTS
                                    FROM ACTIVITIES a
                                    LEFT JOIN CRITERIA cr ON a.CRITERION_ID = cr.ID
                                    LEFT JOIN USERS u ON a.ORGANIZER_ID = u.MAND
                                    WHERE 1=1";

                var parameters = new List<OracleParameter>();

                if (!string.IsNullOrEmpty(search))
                {
                    countQuery += " AND (UPPER(a.TITLE) LIKE :Search OR UPPER(a.DESCRIPTION) LIKE :Search)";
                    dataQuery += " AND (UPPER(a.TITLE) LIKE :Search OR UPPER(a.DESCRIPTION) LIKE :Search)";
                    parameters.Add(OracleDbHelper.CreateParameter("Search", OracleDbType.Varchar2, "%" + search.ToUpper() + "%"));
                }

                if (!string.IsNullOrEmpty(status) && status != "ALL")
                {
                    countQuery += " AND a.STATUS = :Status";
                    dataQuery += " AND a.STATUS = :Status";
                    parameters.Add(OracleDbHelper.CreateParameter("Status", OracleDbType.Varchar2, status));
                }

                if (!string.IsNullOrEmpty(approvalStatus) && approvalStatus != "ALL")
                {
                    countQuery += " AND a.APPROVAL_STATUS = :ApprovalStatus";
                    dataQuery += " AND a.APPROVAL_STATUS = :ApprovalStatus";
                    parameters.Add(OracleDbHelper.CreateParameter("ApprovalStatus", OracleDbType.Varchar2, approvalStatus));
                }

                totalCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(countQuery, parameters.ToArray()));

                dataQuery += " ORDER BY a.CREATED_AT DESC";
                int offset = (page - 1) * pageSize;
                dataQuery = $@"SELECT * FROM (
                                SELECT a.*, ROWNUM rnum FROM ({dataQuery}) a
                                WHERE ROWNUM <= {offset + pageSize}
                              ) WHERE rnum > {offset}";

                DataTable dt = OracleDbHelper.ExecuteQuery(dataQuery, parameters.ToArray());

                foreach (DataRow row in dt.Rows)
                {
                    activities.Add(new AdminActivityItem
                    {
                        Id = row["ID"].ToString(),
                        Title = row["TITLE"].ToString(),
                        Description = row["DESCRIPTION"] != DBNull.Value ? row["DESCRIPTION"].ToString() : "",
                        StartAt = Convert.ToDateTime(row["START_AT"]),
                        EndAt = Convert.ToDateTime(row["END_AT"]),
                        Location = row["LOCATION"] != DBNull.Value ? row["LOCATION"].ToString() : "",
                        Points = row["POINTS"] != DBNull.Value ? (decimal?)Convert.ToDecimal(row["POINTS"]) : null,
                        MaxSeats = row["MAX_SEATS"] != DBNull.Value ? Convert.ToInt32(row["MAX_SEATS"]) : 0,
                        CurrentParticipants = row["CURRENT_PARTICIPANTS"] != DBNull.Value ? Convert.ToInt32(row["CURRENT_PARTICIPANTS"]) : 0,
                        Status = row["STATUS"].ToString(),
                        ApprovalStatus = row["APPROVAL_STATUS"].ToString(),
                        CriterionName = row["CRITERION_NAME"] != DBNull.Value ? row["CRITERION_NAME"].ToString() : "",
                        OrganizerName = row["ORGANIZER_NAME"] != DBNull.Value ? row["ORGANIZER_NAME"].ToString() : "",
                        CreatedAt = Convert.ToDateTime(row["CREATED_AT"])
                    });
                }

                return activities;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetActivitiesList: {ex.Message}");
                throw;
            }
        }

        private ActivityApprovalViewModel GetActivityForApproval(string activityId)
        {
            string query = @"SELECT a.ID, a.TITLE, a.DESCRIPTION, a.START_AT, a.END_AT, a.LOCATION,
                                   a.POINTS, a.MAX_SEATS, a.STATUS, a.APPROVAL_STATUS, a.CREATED_AT,
                                   a.APPROVED_AT, a.APPROVED_BY,
                                   cr.NAME as CRITERION_NAME,
                                   u.FULL_NAME as ORGANIZER_NAME, u.EMAIL as ORGANIZER_EMAIL,
                                   approver.FULL_NAME as APPROVER_NAME,
                                   (SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STATUS != 'CANCELLED') as CURRENT_REGISTRATIONS,
                                   (SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STATUS = 'CHECKED_IN') as CURRENT_CHECKINS
                            FROM ACTIVITIES a
                            LEFT JOIN CRITERIA cr ON a.CRITERION_ID = cr.ID
                            LEFT JOIN USERS u ON a.ORGANIZER_ID = u.MAND
                            LEFT JOIN USERS approver ON a.APPROVED_BY = approver.MAND
                            WHERE a.ID = :ActivityId";

            var parameters = new[]
            {
                OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId)
            };

            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new ActivityApprovalViewModel
            {
                Id = row["ID"].ToString(),
                Title = row["TITLE"].ToString(),
                Description = row["DESCRIPTION"] != DBNull.Value ? row["DESCRIPTION"].ToString() : "",
                StartAt = Convert.ToDateTime(row["START_AT"]),
                EndAt = Convert.ToDateTime(row["END_AT"]),
                Location = row["LOCATION"] != DBNull.Value ? row["LOCATION"].ToString() : "",
                Points = row["POINTS"] != DBNull.Value ? (decimal?)Convert.ToDecimal(row["POINTS"]) : null,
                MaxSeats = row["MAX_SEATS"] != DBNull.Value ? Convert.ToInt32(row["MAX_SEATS"]) : 0,
                Status = row["STATUS"].ToString(),
                ApprovalStatus = row["APPROVAL_STATUS"].ToString(),
                CriterionName = row["CRITERION_NAME"] != DBNull.Value ? row["CRITERION_NAME"].ToString() : "",
                OrganizerName = row["ORGANIZER_NAME"] != DBNull.Value ? row["ORGANIZER_NAME"].ToString() : "",
                OrganizerEmail = row["ORGANIZER_EMAIL"] != DBNull.Value ? row["ORGANIZER_EMAIL"].ToString() : "",
                CreatedAt = Convert.ToDateTime(row["CREATED_AT"]),
                ApprovedAt = row["APPROVED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["APPROVED_AT"]) : null,
                ApprovedBy = row["APPROVER_NAME"] != DBNull.Value ? row["APPROVER_NAME"].ToString() : "",
                CurrentRegistrations = row["CURRENT_REGISTRATIONS"] != DBNull.Value ? Convert.ToInt32(row["CURRENT_REGISTRATIONS"]) : 0,
                CurrentCheckIns = row["CURRENT_CHECKINS"] != DBNull.Value ? Convert.ToInt32(row["CURRENT_CHECKINS"]) : 0
            };
        }

        private RegistrationManagementViewModel GetRegistrations(string activityId, string filterStatus, string search, int page, int pageSize, out int totalCount)
        {
            totalCount = 0;

            // Lấy thông tin hoạt động
            string activityQuery = "SELECT TITLE FROM ACTIVITIES WHERE ID = :ActivityId";
            var activityParams = new[] { OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId) };
            DataTable activityTable = OracleDbHelper.ExecuteQuery(activityQuery, activityParams);

            string activityTitle = activityTable.Rows.Count > 0 ? activityTable.Rows[0]["TITLE"].ToString() : "";

            var viewModel = new RegistrationManagementViewModel
            {
                ActivityId = activityId,
                ActivityTitle = activityTitle,
                Registrations = new List<RegistrationItem>(),
                PageSize = pageSize
            };

            // Build query
            string countQuery = @"SELECT COUNT(*) FROM REGISTRATIONS r
                                 INNER JOIN STUDENTS s ON r.STUDENT_ID = s.MAND
                                 WHERE r.ACTIVITY_ID = :ActivityId";

            string dataQuery = @"SELECT r.ID as REG_ID, r.STUDENT_ID, r.STATUS, r.REGISTERED_AT, r.CHECKED_IN_AT,
                                       s.FULL_NAME as STUDENT_NAME, s.EMAIL as STUDENT_EMAIL, s.CLASS_ID,
                                       p.ID as PROOF_ID, p.STATUS as PROOF_STATUS, p.STORED_PATH as PROOF_PATH, p.CREATED_AT_UTC as PROOF_UPLOADED_AT
                                FROM REGISTRATIONS r
                                INNER JOIN STUDENTS s ON r.STUDENT_ID = s.MAND
                                LEFT JOIN (
                                    SELECT * FROM (
                                        SELECT REGISTRATION_ID, ID, STATUS, STORED_PATH, CREATED_AT_UTC,
                                               ROW_NUMBER() OVER (PARTITION BY REGISTRATION_ID ORDER BY CREATED_AT_UTC DESC) as rn
                                        FROM PROOFS
                                    ) WHERE rn = 1
                                ) p ON r.ID = p.REGISTRATION_ID
                                WHERE r.ACTIVITY_ID = :ActivityId";

            var parameters = new List<OracleParameter>
            {
                OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId)
            };

            if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "ALL")
            {
                countQuery += " AND r.STATUS = :Status";
                dataQuery += " AND r.STATUS = :Status";
                parameters.Add(OracleDbHelper.CreateParameter("Status", OracleDbType.Varchar2, filterStatus));
            }

            if (!string.IsNullOrEmpty(search))
            {
                countQuery += " AND (UPPER(s.FULL_NAME) LIKE :Search OR UPPER(s.EMAIL) LIKE :Search)";
                dataQuery += " AND (UPPER(s.FULL_NAME) LIKE :Search OR UPPER(s.EMAIL) LIKE :Search)";
                parameters.Add(OracleDbHelper.CreateParameter("Search", OracleDbType.Varchar2, "%" + search.ToUpper() + "%"));
            }

            totalCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(countQuery, parameters.ToArray()));

            dataQuery += " ORDER BY r.REGISTERED_AT DESC";
            int offset = (page - 1) * pageSize;
            dataQuery = $@"SELECT * FROM (
                            SELECT a.*, ROWNUM rnum FROM ({dataQuery}) a
                            WHERE ROWNUM <= {offset + pageSize}
                          ) WHERE rnum > {offset}";

            DataTable dt = OracleDbHelper.ExecuteQuery(dataQuery, parameters.ToArray());

            foreach (DataRow row in dt.Rows)
            {
                viewModel.Registrations.Add(new RegistrationItem
                {
                    RegistrationId = row["REG_ID"].ToString(),
                    StudentId = row["STUDENT_ID"].ToString(),
                    StudentName = row["STUDENT_NAME"].ToString(),
                    StudentEmail = row["STUDENT_EMAIL"].ToString(),
                    ClassName = row["CLASS_ID"] != DBNull.Value ? row["CLASS_ID"].ToString() : "",
                    Status = row["STATUS"].ToString(),
                    RegisteredAt = Convert.ToDateTime(row["REGISTERED_AT"]),
                    CheckedInAt = row["CHECKED_IN_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["CHECKED_IN_AT"]) : null,
                    HasProof = row["PROOF_ID"] != DBNull.Value,
                    ProofStatus = row["PROOF_STATUS"] != DBNull.Value ? row["PROOF_STATUS"].ToString() : "",
                    ProofFilePath = row["PROOF_PATH"] != DBNull.Value ? row["PROOF_PATH"].ToString() : "",
                    ProofUploadedAt = row["PROOF_UPLOADED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["PROOF_UPLOADED_AT"]) : null
                });
            }

            return viewModel;
        }

        // ==================== SCORE MANAGEMENT ====================

        // GET: Admin/ClassScores
        public ActionResult ClassScores(string classId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                if (string.IsNullOrEmpty(classId))
                {
                    TempData["ErrorMessage"] = "Không tìm thấy lớp học";
                    return RedirectToAction("ApproveScores");
                }

                var viewModel = GetClassScoresViewModel(classId);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy lớp học";
                    return RedirectToAction("ApproveScores");
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("ApproveScores");
            }
        }

        // POST: Admin/UpdateScore
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateScore(string scoreId, decimal newScore, string reason)
        {
            var authCheck = CheckAuth();
            if (authCheck != null)
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                string mand = Session["MAND"].ToString();

                // Lấy điểm cũ
                string getOldScoreQuery = "SELECT TOTAL FROM SCORES WHERE ID = :ScoreId";
                var getParams = new[] { OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId) };
                object oldScoreObj = OracleDbHelper.ExecuteScalar(getOldScoreQuery, getParams);

                if (oldScoreObj == null || oldScoreObj == DBNull.Value)
                {
                    return Json(new { success = false, message = "Không tìm thấy điểm" });
                }

                decimal oldScore = Convert.ToDecimal(oldScoreObj);

                // Cập nhật điểm
                string updateQuery = @"UPDATE SCORES
                                      SET TOTAL = :NewScore,
                                          STATUS = 'PROVISIONAL'
                                      WHERE ID = :ScoreId";

                var updateParams = new[]
                {
                    OracleDbHelper.CreateParameter("NewScore", OracleDbType.Decimal, newScore),
                    OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, updateParams);

                if (result > 0)
                {
                    // Thêm vào lịch sử
                    string historyId = "SH" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    string insertHistoryQuery = @"INSERT INTO SCORE_HISTORY
                                                 (ID, SCORE_ID, ACTION, OLD_VALUE, NEW_VALUE,
                                                  CHANGED_BY, REASON, CHANGED_AT)
                                                 VALUES
                                                 (:Id, :ScoreId, 'UPDATE', :OldValue, :NewValue,
                                                  :ChangedBy, :Reason, SYSDATE)";

                    var historyParams = new[]
                    {
                        OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, historyId),
                        OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId),
                        OracleDbHelper.CreateParameter("OldValue", OracleDbType.Varchar2, oldScore.ToString()),
                        OracleDbHelper.CreateParameter("NewValue", OracleDbType.Varchar2, newScore.ToString()),
                        OracleDbHelper.CreateParameter("ChangedBy", OracleDbType.Varchar2, mand),
                        OracleDbHelper.CreateParameter("Reason", OracleDbType.Varchar2,
                            string.IsNullOrEmpty(reason) ? (object)DBNull.Value : reason)
                    };

                    OracleDbHelper.ExecuteNonQuery(insertHistoryQuery, historyParams);

                    return Json(new { success = true, message = "Cập nhật điểm thành công" });
                }

                return Json(new { success = false, message = "Không thể cập nhật điểm" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/ApproveScore
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ApproveScore(string scoreId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null)
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                string mand = Session["MAND"].ToString();

                string updateQuery = @"UPDATE SCORES
                                      SET STATUS = 'APPROVED',
                                          APPROVED_BY = :ApprovedBy,
                                          APPROVED_AT = SYSDATE
                                      WHERE ID = :ScoreId";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("ApprovedBy", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);

                if (result > 0)
                {
                    // Thêm vào lịch sử
                    string historyId = "SH" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    string insertHistoryQuery = @"INSERT INTO SCORE_HISTORY
                                                 (ID, SCORE_ID, ACTION, CHANGED_BY, CHANGED_AT)
                                                 VALUES
                                                 (:Id, :ScoreId, 'APPROVE', :ChangedBy, SYSDATE)";

                    var historyParams = new[]
                    {
                        OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, historyId),
                        OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId),
                        OracleDbHelper.CreateParameter("ChangedBy", OracleDbType.Varchar2, mand)
                    };

                    OracleDbHelper.ExecuteNonQuery(insertHistoryQuery, historyParams);

                    return Json(new { success = true, message = "Phê duyệt điểm thành công" });
                }

                return Json(new { success = false, message = "Không thể phê duyệt điểm" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }



        // POST: Admin/ApproveSelectedScores
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ApproveSelectedScores(string[] scoreIds)
        {
            var authCheck = CheckAuth();
            if (authCheck != null)
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                if (scoreIds == null || scoreIds.Length == 0)
                {
                    return Json(new { success = false, message = "Chưa chọn điểm nào" });
                }

                string mand = Session["MAND"].ToString();
                int successCount = 0;

                foreach (string scoreId in scoreIds)
                {
                    string updateQuery = @"UPDATE SCORES
                                          SET STATUS = 'APPROVED',
                                              APPROVED_BY = :ApprovedBy,
                                              APPROVED_AT = SYSDATE
                                          WHERE ID = :ScoreId";

                    var parameters = new[]
                    {
                        OracleDbHelper.CreateParameter("ApprovedBy", OracleDbType.Varchar2, mand),
                        OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId)
                    };

                    int result = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);

                    if (result > 0)
                    {
                        successCount++;

                        // Thêm vào lịch sử
                        string historyId = "SH" + DateTime.Now.ToString("yyyyMMddHHmmss") + successCount;
                        string insertHistoryQuery = @"INSERT INTO SCORE_HISTORY
                                                     (ID, SCORE_ID, ACTION, CHANGED_BY, CHANGED_AT)
                                                     VALUES
                                                     (:Id, :ScoreId, 'APPROVE', :ChangedBy, SYSDATE)";

                        var historyParams = new[]
                        {
                            OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, historyId),
                            OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId),
                            OracleDbHelper.CreateParameter("ChangedBy", OracleDbType.Varchar2, mand)
                        };

                        OracleDbHelper.ExecuteNonQuery(insertHistoryQuery, historyParams);
                    }
                }

                return Json(new { success = true, message = $"Đã phê duyệt {successCount}/{scoreIds.Length} điểm" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // GET: Admin/ApproveScores
        public ActionResult ApproveScores(string department, string term, string search)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetClassScoreListViewModel(department, term, search);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View(new ClassScoreListViewModel { Classes = new List<ClassItem>() });
            }
        }

        // GET: Admin/ReviewRequests
        public ActionResult ReviewRequests(string status, string term)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetReviewRequestListViewModel(status, term);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View(new ReviewRequestListViewModel { Requests = new List<ReviewRequestItem>() });
            }
        }

        // POST: Admin/RespondReviewRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult RespondReviewRequest(string requestId, string action, string response, decimal? approvedScore)
        {
            var authCheck = CheckAuth();
            if (authCheck != null)
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                string mand = Session["MAND"].ToString();
                string newStatus = action == "APPROVE" ? "APPROVED" : "REJECTED";

                // Cập nhật đơn phúc khảo
                string updateFeedbackQuery = @"UPDATE FEEDBACKS
                                              SET STATUS = :Status,
                                                  RESPONSE = :Response,
                                                  RESPONDED_BY = :RespondedBy,
                                                  RESPONDED_AT = SYSDATE
                                              WHERE ID = :RequestId";

                var feedbackParams = new[]
                {
                    OracleDbHelper.CreateParameter("Status", OracleDbType.Varchar2, newStatus),
                    OracleDbHelper.CreateParameter("Response", OracleDbType.Clob,
                        string.IsNullOrEmpty(response) ? (object)DBNull.Value : response),
                    OracleDbHelper.CreateParameter("RespondedBy", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("RequestId", OracleDbType.Varchar2, requestId)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateFeedbackQuery, feedbackParams);

                if (result > 0 && action == "APPROVE" && approvedScore.HasValue)
                {
                    // Lấy thông tin feedback để cập nhật điểm
                    string getFeedbackQuery = @"SELECT STUDENT_ID, TERM_ID FROM FEEDBACKS WHERE ID = :RequestId";
                    var getParams = new[] { OracleDbHelper.CreateParameter("RequestId", OracleDbType.Varchar2, requestId) };
                    DataTable feedbackDt = OracleDbHelper.ExecuteQuery(getFeedbackQuery, getParams);

                    if (feedbackDt.Rows.Count > 0)
                    {
                        string studentId = feedbackDt.Rows[0]["STUDENT_ID"].ToString();
                        string termId = feedbackDt.Rows[0]["TERM_ID"].ToString();

                        // Lấy điểm cũ
                        string getScoreQuery = @"SELECT ID, TOTAL FROM SCORES
                                                WHERE STUDENT_ID = :StudentId AND TERM_ID = :TermId";
                        var scoreParams = new[]
                        {
                            OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, studentId),
                            OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId)
                        };
                        DataTable scoreDt = OracleDbHelper.ExecuteQuery(getScoreQuery, scoreParams);

                        if (scoreDt.Rows.Count > 0)
                        {
                            string scoreId = scoreDt.Rows[0]["ID"].ToString();
                            decimal oldScore = Convert.ToDecimal(scoreDt.Rows[0]["TOTAL"]);

                            // Cập nhật điểm
                            string updateScoreQuery = @"UPDATE SCORES
                                                       SET TOTAL = :NewScore,
                                                           STATUS = 'APPROVED',
                                                           APPROVED_BY = :ApprovedBy,
                                                           APPROVED_AT = SYSDATE
                                                       WHERE ID = :ScoreId";

                            var updateScoreParams = new[]
                            {
                                OracleDbHelper.CreateParameter("NewScore", OracleDbType.Decimal, approvedScore.Value),
                                OracleDbHelper.CreateParameter("ApprovedBy", OracleDbType.Varchar2, mand),
                                OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId)
                            };

                            OracleDbHelper.ExecuteNonQuery(updateScoreQuery, updateScoreParams);

                            // Thêm vào lịch sử
                            string historyId = "SH" + DateTime.Now.ToString("yyyyMMddHHmmss");
                            string insertHistoryQuery = @"INSERT INTO SCORE_HISTORY
                                                         (ID, SCORE_ID, ACTION, OLD_VALUE, NEW_VALUE,
                                                          CHANGED_BY, REASON, CHANGED_AT)
                                                         VALUES
                                                         (:Id, :ScoreId, 'REVIEW_APPROVED', :OldValue, :NewValue,
                                                          :ChangedBy, :Reason, SYSDATE)";

                            var historyParams = new[]
                            {
                                OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, historyId),
                                OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId),
                                OracleDbHelper.CreateParameter("OldValue", OracleDbType.Varchar2, oldScore.ToString()),
                                OracleDbHelper.CreateParameter("NewValue", OracleDbType.Varchar2, approvedScore.Value.ToString()),
                                OracleDbHelper.CreateParameter("ChangedBy", OracleDbType.Varchar2, mand),
                                OracleDbHelper.CreateParameter("Reason", OracleDbType.Varchar2, "Phúc khảo được chấp nhận")
                            };

                            OracleDbHelper.ExecuteNonQuery(insertHistoryQuery, historyParams);
                        }
                    }
                }

                string message = action == "APPROVE" ? "Đã chấp nhận đơn phúc khảo" : "Đã từ chối đơn phúc khảo";
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }


        // ==================== SCORE HELPER METHODS ====================

        private ClassScoreViewModel GetClassScoresViewModel(string classId)
        {
            // Lấy thông tin lớp học
            string classQuery = @"SELECT c.ID, c.NAME, c.CODE, d.NAME as DEPT_NAME,
                                        t.ID as TERM_ID, t.NAME as TERM_NAME,
                                        u.FULL_NAME as ADVISOR_NAME
                                 FROM CLASSES c
                                 LEFT JOIN DEPARTMENTS d ON c.DEPARTMENT_ID = d.ID
                                 LEFT JOIN TERMS t ON t.IS_CURRENT = 1
                                 LEFT JOIN USERS u ON c.ADVISOR_ID = u.MAND
                                 WHERE c.ID = :ClassId";

            var classParams = new[] { OracleDbHelper.CreateParameter("ClassId", OracleDbType.Varchar2, classId) };
            DataTable classDt = OracleDbHelper.ExecuteQuery(classQuery, classParams);

            if (classDt.Rows.Count == 0) return null;

            DataRow classRow = classDt.Rows[0];
            string termId = classRow["TERM_ID"] != DBNull.Value ? classRow["TERM_ID"].ToString() : null;

            var viewModel = new ClassScoreViewModel
            {
                ClassId = classId,
                ClassName = classRow["NAME"].ToString(),
                ClassCode = classRow["CODE"].ToString(),
                DepartmentName = classRow["DEPT_NAME"] != DBNull.Value ? classRow["DEPT_NAME"].ToString() : "",
                TermId = termId,
                TermName = classRow["TERM_NAME"] != DBNull.Value ? classRow["TERM_NAME"].ToString() : "",
                AdvisorName = classRow["ADVISOR_NAME"] != DBNull.Value ? classRow["ADVISOR_NAME"].ToString() : "",
                Students = new List<StudentScoreItem>()
            };

            if (string.IsNullOrEmpty(termId))
            {
                return viewModel;
            }

            // Lấy danh sách sinh viên và điểm
            string studentsQuery = @"SELECT s.USER_ID, st.STUDENT_CODE, u.FULL_NAME,
                                           sc.ID as SCORE_ID, sc.TOTAL, sc.STATUS,
                                           (SELECT COUNT(*) FROM REGISTRATIONS r
                                            INNER JOIN ACTIVITIES a ON r.ACTIVITY_ID = a.ID
                                            WHERE r.STUDENT_ID = s.USER_ID
                                            AND a.TERM_ID = :TermId
                                            AND r.STATUS = 'CHECKED_IN') as ACTIVITY_COUNT
                                    FROM STUDENTS s
                                    INNER JOIN USERS u ON s.USER_ID = u.MAND
                                    LEFT JOIN SCORES sc ON s.USER_ID = sc.STUDENT_ID AND sc.TERM_ID = :TermId
                                    WHERE s.CLASS_ID = :ClassId
                                    ORDER BY st.STUDENT_CODE";

            var studentsParams = new[]
            {
                OracleDbHelper.CreateParameter("ClassId", OracleDbType.Varchar2, classId),
                OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId)
            };

            DataTable studentsDt = OracleDbHelper.ExecuteQuery(studentsQuery, studentsParams);

            foreach (DataRow row in studentsDt.Rows)
            {
                decimal total = row["TOTAL"] != DBNull.Value ? Convert.ToDecimal(row["TOTAL"]) : 0;
                string status = row["STATUS"] != DBNull.Value ? row["STATUS"].ToString() : "PROVISIONAL";

                viewModel.Students.Add(new StudentScoreItem
                {
                    ScoreId = row["SCORE_ID"] != DBNull.Value ? row["SCORE_ID"].ToString() : null,
                    StudentId = row["USER_ID"].ToString(),
                    StudentCode = row["STUDENT_CODE"].ToString(),
                    StudentName = row["FULL_NAME"].ToString(),
                    Total = total,
                    Status = status,
                    Classification = GetClassification(total),
                    ActivityCount = Convert.ToInt32(row["ACTIVITY_COUNT"]),
                    CanEdit = true,
                    CanApprove = status == "PROVISIONAL"
                });
            }

            // Tính thống kê
            viewModel.Statistics = new ClassScoreStatistics
            {
                TotalStudents = viewModel.Students.Count,
                ApprovedStudents = viewModel.Students.Count(x => x.Status == "APPROVED"),
                AverageScore = viewModel.Students.Count > 0 ? viewModel.Students.Average(x => x.Total) : 0,
                HighestScore = viewModel.Students.Count > 0 ? viewModel.Students.Max(x => x.Total) : 0
            };

            return viewModel;
        }

        private ClassScoreListViewModel GetClassScoreListViewModel(string department, string term, string search)
        {
            var viewModel = new ClassScoreListViewModel
            {
                FilterDepartment = department,
                FilterTerm = term,
                SearchKeyword = search,
                Classes = new List<ClassItem>()
            };

            // Lấy học kỳ hiện tại nếu không có filter
            string termId = term;
            if (string.IsNullOrEmpty(termId))
            {
                string currentTermQuery = "SELECT ID FROM TERMS WHERE IS_CURRENT = 1 FETCH FIRST 1 ROWS ONLY";
                object termObj = OracleDbHelper.ExecuteScalar(currentTermQuery, null);
                termId = termObj != null ? termObj.ToString() : null;
            }

            if (string.IsNullOrEmpty(termId))
            {
                return viewModel;
            }

            // Lấy danh sách lớp
            string classesQuery = @"SELECT c.ID, c.NAME, c.CODE, d.NAME as DEPT_NAME,
                                          t.ID as TERM_ID, t.NAME as TERM_NAME,
                                          (SELECT COUNT(*) FROM STUDENTS WHERE CLASS_ID = c.ID) as TOTAL_STUDENTS,
                                          (SELECT COUNT(*) FROM SCORES sc
                                           INNER JOIN STUDENTS s ON sc.STUDENT_ID = s.USER_ID
                                           WHERE s.CLASS_ID = c.ID AND sc.TERM_ID = :TermId
                                           AND sc.STATUS = 'PROVISIONAL') as PENDING_APPROVAL,
                                          (SELECT COUNT(*) FROM SCORES sc
                                           INNER JOIN STUDENTS s ON sc.STUDENT_ID = s.USER_ID
                                           WHERE s.CLASS_ID = c.ID AND sc.TERM_ID = :TermId
                                           AND sc.STATUS = 'APPROVED') as APPROVED_STUDENTS,
                                          (SELECT AVG(sc.TOTAL) FROM SCORES sc
                                           INNER JOIN STUDENTS s ON sc.STUDENT_ID = s.USER_ID
                                           WHERE s.CLASS_ID = c.ID AND sc.TERM_ID = :TermId) as AVG_SCORE
                                   FROM CLASSES c
                                   LEFT JOIN DEPARTMENTS d ON c.DEPARTMENT_ID = d.ID
                                   LEFT JOIN TERMS t ON t.ID = :TermId
                                   WHERE 1=1";

            var parameters = new List<OracleParameter>
            {
                OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId)
            };

            if (!string.IsNullOrEmpty(department))
            {
                classesQuery += " AND c.DEPARTMENT_ID = :DepartmentId";
                parameters.Add(OracleDbHelper.CreateParameter("DepartmentId", OracleDbType.Varchar2, department));
            }

            if (!string.IsNullOrEmpty(search))
            {
                classesQuery += " AND (c.NAME LIKE :Search OR c.CODE LIKE :Search)";
                parameters.Add(OracleDbHelper.CreateParameter("Search", OracleDbType.Varchar2, "%" + search + "%"));
            }

            classesQuery += " ORDER BY c.CODE";

            DataTable classesDt = OracleDbHelper.ExecuteQuery(classesQuery, parameters.ToArray());

            foreach (DataRow row in classesDt.Rows)
            {
                viewModel.Classes.Add(new ClassItem
                {
                    ClassId = row["ID"].ToString(),
                    ClassName = row["NAME"].ToString(),
                    ClassCode = row["CODE"].ToString(),
                    DepartmentName = row["DEPT_NAME"] != DBNull.Value ? row["DEPT_NAME"].ToString() : "",
                    TermId = row["TERM_ID"] != DBNull.Value ? row["TERM_ID"].ToString() : "",
                    TermName = row["TERM_NAME"] != DBNull.Value ? row["TERM_NAME"].ToString() : "",
                    TotalStudents = Convert.ToInt32(row["TOTAL_STUDENTS"]),
                    PendingApproval = Convert.ToInt32(row["PENDING_APPROVAL"]),
                    ApprovedStudents = Convert.ToInt32(row["APPROVED_STUDENTS"]),
                    AverageScore = row["AVG_SCORE"] != DBNull.Value ? Convert.ToDecimal(row["AVG_SCORE"]) : 0
                });
            }

            return viewModel;
        }

        private ReviewRequestListViewModel GetReviewRequestListViewModel(string status, string term)
        {
            var viewModel = new ReviewRequestListViewModel
            {
                FilterStatus = status ?? "ALL",
                FilterTerm = term,
                Requests = new List<ReviewRequestItem>()
            };

            // Lấy danh sách đơn phúc khảo
            string requestsQuery = @"SELECT f.ID, f.TITLE, f.CONTENT, f.REQUESTED_SCORE, f.STATUS,
                                           f.CREATED_AT, f.RESPONDED_AT,
                                           s.STUDENT_CODE, u.FULL_NAME as STUDENT_NAME,
                                           t.NAME as TERM_NAME, t.YEAR as TERM_YEAR,
                                           sc.TOTAL as CURRENT_SCORE
                                    FROM FEEDBACKS f
                                    INNER JOIN STUDENTS s ON f.STUDENT_ID = s.USER_ID
                                    INNER JOIN USERS u ON s.USER_ID = u.MAND
                                    INNER JOIN TERMS t ON f.TERM_ID = t.ID
                                    LEFT JOIN SCORES sc ON f.STUDENT_ID = sc.STUDENT_ID AND f.TERM_ID = sc.TERM_ID
                                    WHERE 1=1";

            var parameters = new List<OracleParameter>();

            if (!string.IsNullOrEmpty(status) && status != "ALL")
            {
                requestsQuery += " AND f.STATUS = :Status";
                parameters.Add(OracleDbHelper.CreateParameter("Status", OracleDbType.Varchar2, status));
            }

            if (!string.IsNullOrEmpty(term))
            {
                requestsQuery += " AND f.TERM_ID = :TermId";
                parameters.Add(OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, term));
            }

            requestsQuery += " ORDER BY f.CREATED_AT DESC";

            DataTable requestsDt = OracleDbHelper.ExecuteQuery(requestsQuery, parameters.ToArray());

            foreach (DataRow row in requestsDt.Rows)
            {
                viewModel.Requests.Add(new ReviewRequestItem
                {
                    RequestId = row["ID"].ToString(),
                    Title = row["TITLE"].ToString(),
                    Content = row["CONTENT"].ToString(),
                    StudentCode = row["STUDENT_CODE"].ToString(),
                    StudentName = row["STUDENT_NAME"].ToString(),
                    TermName = row["TERM_NAME"].ToString(),
                    TermYear = Convert.ToInt32(row["TERM_YEAR"]),
                    CurrentScore = row["CURRENT_SCORE"] != DBNull.Value ? Convert.ToDecimal(row["CURRENT_SCORE"]) : 0,
                    RequestedScore = Convert.ToDecimal(row["REQUESTED_SCORE"]),
                    Status = row["STATUS"].ToString(),
                    CreatedAt = Convert.ToDateTime(row["CREATED_AT"]),
                    RespondedAt = row["RESPONDED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["RESPONDED_AT"]) : null
                });
            }

            return viewModel;
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



