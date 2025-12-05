using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;

namespace QuanLyDiemRenLuyen.Controllers.Admin
{
    public class ActivitiesController : AdminBaseController
    {
        // GET: Admin/Activities
        public ActionResult Index(string search, string status, string approvalStatus, int page = 1)
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

                return View("~/Views/Admin/Activities.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/Activities.cshtml", new AdminActivityListViewModel { Activities = new List<AdminActivityItem>() });
            }
        }

        // GET: Admin/Activities/Approve/id
        public ActionResult Approve(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetActivityForApproval(id);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hoạt động";
                    return RedirectToRoute("AdminActivities", new { action = "Index" });
                }

                return View("~/Views/Admin/ApproveActivity.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToRoute("AdminActivities", new { action = "Index" });
            }
        }

        // POST: Admin/Activities/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(ApprovalActionViewModel model)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ";
                return RedirectToRoute("AdminActivities", new { action = "Approve", id = model.ActivityId });
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

                return RedirectToRoute("AdminActivities", new { action = "Index" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToRoute("AdminActivities", new { action = "Approve", id = model.ActivityId });
            }
        }

        // GET: Admin/Activities/Registrations/activityId
        public ActionResult Registrations(string id, string filterStatus, string search, int page = 1)
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

                return View("~/Views/Admin/ViewRegistrations.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/ViewRegistrations.cshtml", new RegistrationManagementViewModel
                {
                    Registrations = new List<RegistrationItem>()
                });
            }
        }

        #region Private Helper Methods

        private List<AdminActivityItem> GetActivitiesList(string search, string status, string approvalStatus, int page, int pageSize, out int totalCount)
        {
            var activities = new List<AdminActivityItem>();
            totalCount = 0;

            try
            {
                string countQuery = @"SELECT COUNT(*) FROM ACTIVITIES a WHERE 1=1";
                string dataQuery = @"SELECT a.ID, a.TITLE, a.DESCRIPTION, a.START_AT, a.END_AT, a.LOCATION,
                                           a.POINTS, a.MAX_SEATS, a.STATUS, a.APPROVAL_STATUS, a.CREATED_AT,
                                           u.FULL_NAME as ORGANIZER_NAME,
                                           (SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STATUS != 'CANCELLED') as CURRENT_PARTICIPANTS
                                    FROM ACTIVITIES a
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
                                   u.FULL_NAME as ORGANIZER_NAME, u.EMAIL as ORGANIZER_EMAIL,
                                   approver.FULL_NAME as APPROVER_NAME,
                                   (SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STATUS != 'CANCELLED') as CURRENT_REGISTRATIONS,
                                   (SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STATUS = 'CHECKED_IN') as CURRENT_CHECKINS
                            FROM ACTIVITIES a
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
                                 INNER JOIN USERS u ON r.STUDENT_ID = u.MAND
                                 WHERE r.ACTIVITY_ID = :ActivityId";

            string dataQuery = @"SELECT r.ID as REG_ID, r.STUDENT_ID, r.STATUS, r.REGISTERED_AT, r.CHECKED_IN_AT,
                                       u.FULL_NAME as STUDENT_NAME, u.EMAIL as STUDENT_EMAIL,
                                       s.CLASS_ID,
                                       p.ID as PROOF_ID, p.STATUS as PROOF_STATUS, p.STORED_PATH as PROOF_PATH, 
                                       p.FILE_NAME as PROOF_FILE_NAME, p.CREATED_AT_UTC as PROOF_UPLOADED_AT
                                FROM REGISTRATIONS r
                                INNER JOIN USERS u ON r.STUDENT_ID = u.MAND
                                LEFT JOIN STUDENTS s ON u.MAND = s.USER_ID
                                LEFT JOIN (
                                    SELECT * FROM (
                                        SELECT REGISTRATION_ID, ID, STATUS, STORED_PATH, FILE_NAME, CREATED_AT_UTC,
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
                countQuery += " AND (UPPER(u.FULL_NAME) LIKE :Search OR UPPER(u.EMAIL) LIKE :Search)";
                dataQuery += " AND (UPPER(u.FULL_NAME) LIKE :Search OR UPPER(u.EMAIL) LIKE :Search)";
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
                    ProofFileName = row["PROOF_FILE_NAME"] != DBNull.Value ? EncryptionHelper.Decrypt(row["PROOF_FILE_NAME"].ToString()) : "", // Decrypt FileName
                    ProofUploadedAt = row["PROOF_UPLOADED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["PROOF_UPLOADED_AT"]) : null
                });
            }

            return viewModel;
        }

        // GET: Admin/Activities/Participants/activityId
        public ActionResult Participants(string id, string filterStatus, string search, int page = 1)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetParticipants(id, filterStatus, search, page, 20, out int totalCount);
                viewModel.CurrentPage = page;
                viewModel.TotalPages = (int)Math.Ceiling((double)totalCount / viewModel.PageSize);
                viewModel.FilterStatus = filterStatus ?? "ALL";
                viewModel.SearchKeyword = search;

                return View("~/Views/Admin/Participants.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/Participants.cshtml", new ParticipantsViewModel
                {
                    Participants = new List<ParticipantItem>()
                });
            }
        }

        // POST: Admin/Activities/CheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CheckIn(string registrationId, string activityId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string updateQuery = @"UPDATE REGISTRATIONS 
                                       SET STATUS = 'CHECKED_IN', 
                                           CHECKED_IN_AT = SYSTIMESTAMP
                                       WHERE ID = :RegId AND STATUS = 'REGISTERED'";

                var parameters = new[] {
                    OracleDbHelper.CreateParameter("RegId", OracleDbType.Varchar2, registrationId)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Điểm danh thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể điểm danh. Sinh viên có thể đã được điểm danh hoặc đã hủy đăng ký.";
                }

                return RedirectToAction("Participants", new { id = activityId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Participants", new { id = activityId });
            }
        }

        // POST: Admin/Activities/BulkCheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkCheckIn(string activityId, string registrationIds)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            if (string.IsNullOrEmpty(registrationIds))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một sinh viên để điểm danh.";
                return RedirectToAction("Participants", new { id = activityId });
            }

            try
            {
                var regIdList = registrationIds.Split(',');
                int successCount = 0;

                foreach (var regId in regIdList)
                {
                    if (string.IsNullOrWhiteSpace(regId)) continue;

                    string updateQuery = @"UPDATE REGISTRATIONS 
                                           SET STATUS = 'CHECKED_IN', 
                                               CHECKED_IN_AT = SYSTIMESTAMP
                                           WHERE ID = :RegId AND STATUS = 'REGISTERED'";

                    var parameters = new[] {
                        OracleDbHelper.CreateParameter("RegId", OracleDbType.Varchar2, regId.Trim())
                    };

                    int result = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);
                    if (result > 0) successCount++;
                }

                TempData["SuccessMessage"] = $"Đã điểm danh thành công {successCount}/{regIdList.Length} sinh viên.";
                return RedirectToAction("Participants", new { id = activityId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Participants", new { id = activityId });
            }
        }

        private ParticipantsViewModel GetParticipants(string activityId, string filterStatus,
                                                      string search, int page, int pageSize,
                                                      out int totalCount)
        {
            totalCount = 0;

            // Get activity info
            string activityQuery = @"SELECT TITLE, START_AT, END_AT FROM ACTIVITIES WHERE ID = :ActivityId";
            var activityParams = new[] { OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId) };
            DataTable activityTable = OracleDbHelper.ExecuteQuery(activityQuery, activityParams);

            if (activityTable.Rows.Count == 0)
            {
                return new ParticipantsViewModel
                {
                    ActivityId = activityId,
                    Participants = new List<ParticipantItem>(),
                    PageSize = pageSize
                };
            }

            var viewModel = new ParticipantsViewModel
            {
                ActivityId = activityId,
                ActivityTitle = activityTable.Rows[0]["TITLE"].ToString(),
                ActivityStartAt = Convert.ToDateTime(activityTable.Rows[0]["START_AT"]),
                ActivityEndAt = Convert.ToDateTime(activityTable.Rows[0]["END_AT"]),
                Participants = new List<ParticipantItem>(),
                PageSize = pageSize
            };

            // Build query for participants
            string countQuery = @"SELECT COUNT(*) FROM REGISTRATIONS r
                                  INNER JOIN USERS u ON r.STUDENT_ID = u.MAND
                                  INNER JOIN STUDENTS s ON u.MAND = s.USER_ID
                                  WHERE r.ACTIVITY_ID = :ActivityId AND r.STATUS != 'CANCELLED'";

            string dataQuery = @"SELECT r.ID as REG_ID, r.STUDENT_ID, r.STATUS, 
                                        r.REGISTERED_AT, r.CHECKED_IN_AT,
                                        u.FULL_NAME as STUDENT_NAME, u.EMAIL as STUDENT_EMAIL,
                                        s.STUDENT_CODE, c.NAME as CLASS_NAME,
                                        p.ID as PROOF_ID, p.STATUS as PROOF_STATUS
                                 FROM REGISTRATIONS r
                                 INNER JOIN USERS u ON r.STUDENT_ID = u.MAND
                                 INNER JOIN STUDENTS s ON u.MAND = s.USER_ID
                                 LEFT JOIN CLASSES c ON s.CLASS_ID = c.ID
                                 LEFT JOIN (
                                     SELECT REGISTRATION_ID, ID, STATUS,
                                            ROW_NUMBER() OVER (PARTITION BY REGISTRATION_ID ORDER BY CREATED_AT_UTC DESC) as rn
                                     FROM PROOFS
                                 ) p ON r.ID = p.REGISTRATION_ID AND p.rn = 1
                                 WHERE r.ACTIVITY_ID = :ActivityId AND r.STATUS != 'CANCELLED'";

            var parameters = new List<OracleParameter>
            {
                OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId)
            };

            // Apply filters
            if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "ALL")
            {
                countQuery += " AND r.STATUS = :Status";
                dataQuery += " AND r.STATUS = :Status";
                parameters.Add(OracleDbHelper.CreateParameter("Status", OracleDbType.Varchar2, filterStatus));
            }

            if (!string.IsNullOrEmpty(search))
            {
                countQuery += " AND (UPPER(u.FULL_NAME) LIKE :Search OR UPPER(s.STUDENT_CODE) LIKE :Search)";
                dataQuery += " AND (UPPER(u.FULL_NAME) LIKE :Search OR UPPER(s.STUDENT_CODE) LIKE :Search)";
                parameters.Add(OracleDbHelper.CreateParameter("Search", OracleDbType.Varchar2, "%" + search.ToUpper() + "%"));
            }

            totalCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(countQuery, parameters.ToArray()));

            // Pagination
            dataQuery += " ORDER BY r.REGISTERED_AT DESC";
            int offset = (page - 1) * pageSize;
            dataQuery = $@"SELECT * FROM (
                            SELECT a.*, ROWNUM rnum FROM ({dataQuery}) a
                            WHERE ROWNUM <= {offset + pageSize}
                           ) WHERE rnum > {offset}";

            DataTable dt = OracleDbHelper.ExecuteQuery(dataQuery, parameters.ToArray());

            foreach (DataRow row in dt.Rows)
            {
                viewModel.Participants.Add(new ParticipantItem
                {
                    RegistrationId = row["REG_ID"].ToString(),
                    StudentId = row["STUDENT_ID"].ToString(),
                    StudentCode = row["STUDENT_CODE"].ToString(),
                    StudentName = row["STUDENT_NAME"].ToString(),
                    StudentEmail = row["STUDENT_EMAIL"].ToString(),
                    ClassName = row["CLASS_NAME"] != DBNull.Value ? row["CLASS_NAME"].ToString() : "",
                    Status = row["STATUS"].ToString(),
                    RegisteredAt = Convert.ToDateTime(row["REGISTERED_AT"]),
                    CheckedInAt = row["CHECKED_IN_AT"] != DBNull.Value
                        ? (DateTime?)Convert.ToDateTime(row["CHECKED_IN_AT"])
                        : null,
                    HasProof = row["PROOF_ID"] != DBNull.Value,
                    ProofStatus = row["PROOF_STATUS"] != DBNull.Value ? row["PROOF_STATUS"].ToString() : ""
                });
            }

            // Calculate statistics
            string statsQuery = @"SELECT 
                                    COUNT(*) as TOTAL_REGISTERED,
                                    SUM(CASE WHEN STATUS = 'CHECKED_IN' THEN 1 ELSE 0 END) as TOTAL_CHECKED_IN
                                  FROM REGISTRATIONS
                                  WHERE ACTIVITY_ID = :ActivityId AND STATUS != 'CANCELLED'";

            DataTable statsTable = OracleDbHelper.ExecuteQuery(statsQuery, new[] { activityParams[0] });

            if (statsTable.Rows.Count > 0)
            {
                viewModel.TotalRegistered = Convert.ToInt32(statsTable.Rows[0]["TOTAL_REGISTERED"]);
                viewModel.TotalCheckedIn = Convert.ToInt32(statsTable.Rows[0]["TOTAL_CHECKED_IN"]);
                viewModel.AttendanceRate = viewModel.TotalRegistered > 0
                    ? (decimal)viewModel.TotalCheckedIn / viewModel.TotalRegistered * 100
                    : 0;
            }

            return viewModel;
        }

        #endregion
    }
}
