using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;

namespace QuanLyDiemRenLuyen.Controllers.Lecturer
{
    public class ActivitiesController : LecturerBaseController
    {
        // GET: Lecturer/Activities
        public ActionResult Index(string search, string status, int page = 1)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = GetCurrentUserId();
                var viewModel = new LecturerActivityListViewModel
                {
                    SearchKeyword = search,
                    FilterStatus = status ?? "ALL",
                    CurrentPage = page,
                    PageSize = 10
                };

                viewModel.Activities = GetMyActivities(mand, search, status, page, viewModel.PageSize, out int totalCount);
                viewModel.TotalPages = (int)Math.Ceiling((double)totalCount / viewModel.PageSize);

                return View("~/Views/Lecturer/Activities/Index.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Lecturer/Activities/Index.cshtml", new LecturerActivityListViewModel());
            }
        }

        // GET: Lecturer/Activities/Create
        public ActionResult Create()
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            var viewModel = new ActivityFormViewModel
            {
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(1).AddHours(2)
            };
            
            LoadViewBagData();
            return View("~/Views/Lecturer/Activities/Form.cshtml", viewModel);
        }

        // POST: Lecturer/Activities/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)] // Allow HTML in description/requirements/benefits
        public ActionResult Save(ActivityFormViewModel model)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            if (!ModelState.IsValid)
            {
                LoadViewBagData();
                return View("~/Views/Lecturer/Activities/Form.cshtml", model);
            }

            try
            {
                string mand = GetCurrentUserId();
                
                if (string.IsNullOrEmpty(model.Id))
                {
                    // CREATE
                    string insertQuery = @"INSERT INTO ACTIVITIES 
                                          (ID, TITLE, DESCRIPTION, REQUIREMENTS, BENEFITS, 
                                           TERM_ID, START_AT, END_AT, 
                                           STATUS, MAX_SEATS, LOCATION, POINTS, 
                                           REGISTRATION_START, REGISTRATION_DEADLINE,
                                           APPROVAL_STATUS, ORGANIZER_ID, CREATED_AT)
                                          VALUES 
                                          (RAWTOHEX(SYS_GUID()), :Title, :Description, :Requirements, :Benefits,
                                           :TermId, :StartAt, :EndAt,
                                           'OPEN', :MaxSeats, :Location, :Points,
                                           :RegistrationStart, :RegistrationDeadline,
                                           'PENDING', :OrganizerId, SYSTIMESTAMP)";

                    var parameters = new[]
                    {
                        OracleDbHelper.CreateParameter("Title", OracleDbType.Varchar2, model.Title),
                        OracleDbHelper.CreateParameter("Description", OracleDbType.Clob, model.Description ?? ""),
                        OracleDbHelper.CreateParameter("Requirements", OracleDbType.Clob, model.Requirements ?? ""),
                        OracleDbHelper.CreateParameter("Benefits", OracleDbType.Clob, model.Benefits ?? ""),
                        OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, model.TermId),
                        OracleDbHelper.CreateParameter("StartAt", OracleDbType.TimeStamp, model.StartAt),
                        OracleDbHelper.CreateParameter("EndAt", OracleDbType.TimeStamp, model.EndAt),
                        OracleDbHelper.CreateParameter("MaxSeats", OracleDbType.Int32, model.MaxSeats),
                        OracleDbHelper.CreateParameter("Location", OracleDbType.Varchar2, model.Location),
                        OracleDbHelper.CreateParameter("Points", OracleDbType.Decimal, model.Points),
                        OracleDbHelper.CreateParameter("RegistrationStart", OracleDbType.TimeStamp, model.RegistrationStart.HasValue ? (object)model.RegistrationStart.Value : DBNull.Value),
                        OracleDbHelper.CreateParameter("RegistrationDeadline", OracleDbType.TimeStamp, model.RegistrationDeadline.HasValue ? (object)model.RegistrationDeadline.Value : DBNull.Value),
                        OracleDbHelper.CreateParameter("OrganizerId", OracleDbType.Varchar2, mand)
                    };

                    OracleDbHelper.ExecuteNonQuery(insertQuery, parameters);
                    TempData["SuccessMessage"] = "Tạo hoạt động thành công! Vui lòng chờ duyệt.";
                }
                else
                {
                    // UPDATE
                    // Check ownership
                    if (!IsMyActivity(model.Id, mand))
                    {
                        TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa hoạt động này";
                        return RedirectToRoute("LecturerActivities", new { action = "Index" });
                    }

                    string updateQuery = @"UPDATE ACTIVITIES 
                                          SET TITLE = :Title, 
                                              DESCRIPTION = :Description,
                                              REQUIREMENTS = :Requirements,
                                              BENEFITS = :Benefits,
                                              TERM_ID = :TermId,
                                              START_AT = :StartAt,
                                              END_AT = :EndAt,
                                              MAX_SEATS = :MaxSeats,
                                              LOCATION = :Location,
                                              POINTS = :Points,
                                              REGISTRATION_START = :RegistrationStart,
                                              REGISTRATION_DEADLINE = :RegistrationDeadline,
                                              APPROVAL_STATUS = 'PENDING'
                                          WHERE ID = :Id AND ORGANIZER_ID = :OrganizerId";

                    var parameters = new[]
                    {
                        OracleDbHelper.CreateParameter("Title", OracleDbType.Varchar2, model.Title),
                        OracleDbHelper.CreateParameter("Description", OracleDbType.Clob, model.Description ?? ""),
                        OracleDbHelper.CreateParameter("Requirements", OracleDbType.Clob, model.Requirements ?? ""),
                        OracleDbHelper.CreateParameter("Benefits", OracleDbType.Clob, model.Benefits ?? ""),
                        OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, model.TermId),
                        OracleDbHelper.CreateParameter("StartAt", OracleDbType.TimeStamp, model.StartAt),
                        OracleDbHelper.CreateParameter("EndAt", OracleDbType.TimeStamp, model.EndAt),
                        OracleDbHelper.CreateParameter("MaxSeats", OracleDbType.Int32, model.MaxSeats),
                        OracleDbHelper.CreateParameter("Location", OracleDbType.Varchar2, model.Location),
                        OracleDbHelper.CreateParameter("Points", OracleDbType.Decimal, model.Points),
                        OracleDbHelper.CreateParameter("RegistrationStart", OracleDbType.TimeStamp, model.RegistrationStart.HasValue ? (object)model.RegistrationStart.Value : DBNull.Value),
                        OracleDbHelper.CreateParameter("RegistrationDeadline", OracleDbType.TimeStamp, model.RegistrationDeadline.HasValue ? (object)model.RegistrationDeadline.Value : DBNull.Value),
                        OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, model.Id),
                        OracleDbHelper.CreateParameter("OrganizerId", OracleDbType.Varchar2, mand)
                    };

                    OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);
                    TempData["SuccessMessage"] = "Cập nhật hoạt động thành công! Vui lòng chờ duyệt lại.";
                }

                return RedirectToRoute("LecturerActivities", new { action = "Index" });
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi: " + ex.Message;
                LoadViewBagData();
                return View("~/Views/Lecturer/Activities/Form.cshtml", model);
            }
        }

        // GET: Lecturer/Activities/Edit/id
        public ActionResult Edit(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            string mand = GetCurrentUserId();
            var model = GetActivityForEdit(id, mand);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hoạt động hoặc bạn không có quyền chỉnh sửa";
                return RedirectToRoute("LecturerActivities", new { action = "Index" });
            }

            LoadViewBagData();
            return View("~/Views/Lecturer/Activities/Form.cshtml", model);
        }

        // POST: Lecturer/Activities/Delete/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = GetCurrentUserId();
                
                // Check ownership
                if (!IsMyActivity(id, mand))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xóa hoạt động này";
                    return RedirectToRoute("LecturerActivities", new { action = "Index" });
                }

                // Check registrations
                string checkRegQuery = "SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = :Id";
                var checkParams = new[] { OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id) };
                int regCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(checkRegQuery, checkParams));

                if (regCount > 0)
                {
                    // Has registrations -> Cancel
                    string cancelQuery = "UPDATE ACTIVITIES SET STATUS = 'CANCELLED' WHERE ID = :Id";
                    OracleDbHelper.ExecuteNonQuery(cancelQuery, checkParams);
                    TempData["SuccessMessage"] = "Đã hủy hoạt động (vì đã có sinh viên đăng ký)";
                }
                else
                {
                    // No registrations -> Delete
                    string deleteQuery = "DELETE FROM ACTIVITIES WHERE ID = :Id";
                    OracleDbHelper.ExecuteNonQuery(deleteQuery, checkParams);
                    TempData["SuccessMessage"] = "Đã xóa hoạt động";
                }

                return RedirectToRoute("LecturerActivities", new { action = "Index" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToRoute("LecturerActivities", new { action = "Index" });
            }
        }

        // GET: Lecturer/Activities/Participants/activityId
        public ActionResult Participants(string id, string filterStatus, string search, int page = 1)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = GetCurrentUserId();
                
                // Check ownership - only organizer can check-in participants
                if (!IsMyActivity(id, mand))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền điểm danh hoạt động này";
                    return RedirectToRoute("LecturerActivities", new { action = "Index" });
                }

                var viewModel = GetParticipants(id, filterStatus, search, page, 20, out int totalCount);
                viewModel.CurrentPage = page;
                viewModel.TotalPages = (int)Math.Ceiling((double)totalCount / viewModel.PageSize);
                viewModel.FilterStatus = filterStatus ?? "ALL";
                viewModel.SearchKeyword = search;

                return View("~/Views/Lecturer/Activities/Participants.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Lecturer/Activities/Participants.cshtml", new ParticipantsViewModel
                {
                    Participants = new List<ParticipantItem>()
                });
            }
        }

        // POST: Lecturer/Activities/CheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CheckIn(string registrationId, string activityId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = GetCurrentUserId();
                
                // Check ownership
                if (!IsMyActivity(activityId, mand))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền điểm danh hoạt động này";
                    return RedirectToRoute("LecturerActivities", new { action = "Index" });
                }

                // Check activity time for warning
                string timeQuery = @"SELECT START_AT, END_AT FROM ACTIVITIES WHERE ID = :ActivityId";
                var timeParams = new[] { OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId) };
                DataTable activityTime = OracleDbHelper.ExecuteQuery(timeQuery, timeParams);
                
                bool isOutsideTime = false;
                if (activityTime.Rows.Count > 0)
                {
                    DateTime startAt = Convert.ToDateTime(activityTime.Rows[0]["START_AT"]);
                    DateTime endAt = Convert.ToDateTime(activityTime.Rows[0]["END_AT"]);
                    DateTime now = DateTime.Now;
                    isOutsideTime = now < startAt || now > endAt;
                }

                string updateQuery = @"UPDATE REGISTRATIONS 
                                       SET STATUS = 'CHECKED_IN', 
                                           CHECKED_IN_AT = SYSTIMESTAMP,
                                           CHECKED_IN_BY = :CheckedInBy
                                       WHERE ID = :RegId AND STATUS = 'REGISTERED'";

                var parameters = new[] {
                    OracleDbHelper.CreateParameter("CheckedInBy", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("RegId", OracleDbType.Varchar2, registrationId)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Điểm danh thành công!";
                    if (isOutsideTime)
                    {
                        TempData["WarningMessage"] = "Lưu ý: Điểm danh ngoài thời gian hoạt động.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể điểm danh. Sinh viên có thể đã được điểm danh hoặc đã hủy đăng ký.";
                }

                return RedirectToRoute("LecturerActivities", new { action = "Participants", id = activityId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToRoute("LecturerActivities", new { action = "Participants", id = activityId });
            }
        }

        // POST: Lecturer/Activities/BulkCheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkCheckIn(string activityId, string registrationIds)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            string mand = GetCurrentUserId();
            
            // Check ownership
            if (!IsMyActivity(activityId, mand))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền điểm danh hoạt động này";
                return RedirectToRoute("LecturerActivities", new { action = "Index" });
            }

            if (string.IsNullOrEmpty(registrationIds))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một sinh viên để điểm danh.";
                return RedirectToRoute("LecturerActivities", new { action = "Participants", id = activityId });
            }

            try
            {
                // Check activity time for warning
                string timeQuery = @"SELECT START_AT, END_AT FROM ACTIVITIES WHERE ID = :ActivityId";
                var timeParams = new[] { OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId) };
                DataTable activityTime = OracleDbHelper.ExecuteQuery(timeQuery, timeParams);
                
                bool isOutsideTime = false;
                if (activityTime.Rows.Count > 0)
                {
                    DateTime startAt = Convert.ToDateTime(activityTime.Rows[0]["START_AT"]);
                    DateTime endAt = Convert.ToDateTime(activityTime.Rows[0]["END_AT"]);
                    DateTime now = DateTime.Now;
                    isOutsideTime = now < startAt || now > endAt;
                }

                var regIdList = registrationIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                               .Select(id => id.Trim())
                                               .Where(id => !string.IsNullOrEmpty(id))
                                               .ToArray();

                if (regIdList.Length == 0)
                {
                    TempData["ErrorMessage"] = "Không có sinh viên hợp lệ để điểm danh.";
                    return RedirectToRoute("LecturerActivities", new { action = "Participants", id = activityId });
                }

                // Build batch update with IN clause
                var parameterNames = regIdList.Select((id, index) => $":Id{index}").ToArray();
                string inClause = string.Join(",", parameterNames);
                
                string updateQuery = $@"UPDATE REGISTRATIONS 
                                        SET STATUS = 'CHECKED_IN', 
                                            CHECKED_IN_AT = SYSTIMESTAMP,
                                            CHECKED_IN_BY = :CheckedInBy
                                        WHERE ID IN ({inClause}) AND STATUS = 'REGISTERED'";

                var parameters = new List<OracleParameter>
                {
                    OracleDbHelper.CreateParameter("CheckedInBy", OracleDbType.Varchar2, mand)
                };

                for (int i = 0; i < regIdList.Length; i++)
                {
                    parameters.Add(OracleDbHelper.CreateParameter($"Id{i}", OracleDbType.Varchar2, regIdList[i]));
                }

                int successCount = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters.ToArray());

                TempData["SuccessMessage"] = $"Đã điểm danh thành công {successCount}/{regIdList.Length} sinh viên.";
                if (isOutsideTime)
                {
                    TempData["WarningMessage"] = "Lưu ý: Điểm danh ngoài thời gian hoạt động.";
                }
                return RedirectToRoute("LecturerActivities", new { action = "Participants", id = activityId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToRoute("LecturerActivities", new { action = "Participants", id = activityId });
            }
        }

        // POST: Lecturer/Activities/UnCheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UnCheckIn(string registrationId, string activityId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = GetCurrentUserId();
                
                // Check ownership
                if (!IsMyActivity(activityId, mand))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền hủy điểm danh hoạt động này";
                    return RedirectToRoute("LecturerActivities", new { action = "Index" });
                }

                string updateQuery = @"UPDATE REGISTRATIONS 
                                       SET STATUS = 'REGISTERED', 
                                           CHECKED_IN_AT = NULL,
                                           CHECKED_IN_BY = NULL
                                       WHERE ID = :RegId AND STATUS = 'CHECKED_IN'";

                var parameters = new[] {
                    OracleDbHelper.CreateParameter("RegId", OracleDbType.Varchar2, registrationId)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Hủy điểm danh thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể hủy điểm danh. Sinh viên có thể chưa được điểm danh.";
                }

                return RedirectToRoute("LecturerActivities", new { action = "Participants", id = activityId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToRoute("LecturerActivities", new { action = "Participants", id = activityId });
            }
        }

        // GET: Lecturer/Activities/ExportParticipants/activityId
        public ActionResult ExportParticipants(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = GetCurrentUserId();
                
                // Check ownership
                if (!IsMyActivity(id, mand))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xuất báo cáo hoạt động này";
                    return RedirectToRoute("LecturerActivities", new { action = "Index" });
                }

                // Get all participants (no pagination)
                var viewModel = GetParticipants(id, null, null, 1, 10000, out _);

                var sb = new System.Text.StringBuilder();
                
                // BOM for UTF-8 Excel compatibility
                sb.Append('\uFEFF');
                
                // Title info
                sb.AppendLine($"DANH SÁCH ĐIỂM DANH - {viewModel.ActivityTitle}");
                sb.AppendLine($"Thời gian: {viewModel.ActivityStartAt:dd/MM/yyyy HH:mm} - {viewModel.ActivityEndAt:dd/MM/yyyy HH:mm}");
                sb.AppendLine($"Tổng đăng ký: {viewModel.TotalRegistered} | Đã điểm danh: {viewModel.TotalCheckedIn} | Tỷ lệ: {viewModel.AttendanceRate:0.0}%");
                sb.AppendLine();
                
                // Headers
                sb.AppendLine("STT,MSSV,Họ và tên,Lớp,Trạng thái,Thời gian điểm danh,Người điểm danh");
                
                // Data
                int stt = 1;
                foreach (var p in viewModel.Participants)
                {
                    string status = p.Status == "CHECKED_IN" ? "Đã điểm danh" : "Chưa điểm danh";
                    string checkedInAt = p.CheckedInAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
                    string checkedInBy = p.CheckedInByName ?? "";
                    
                    // Escape commas in fields
                    sb.AppendLine($"{stt++},\"{p.StudentCode}\",\"{p.StudentName}\",\"{p.ClassName}\",\"{status}\",\"{checkedInAt}\",\"{checkedInBy}\"");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
                string fileName = $"DiemDanh_{viewModel.ActivityTitle.Replace(" ", "_").Replace(",", "")}_{DateTime.Now:yyyyMMdd}.csv";
                return File(bytes, "text/csv; charset=utf-8", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xuất CSV: " + ex.Message;
                return RedirectToRoute("LecturerActivities", new { action = "Participants", id = id });
            }
        }

        // ==================== ATTENDANCE IMPROVEMENT ====================

        // GET: Lecturer/Activities/Attendance/activityId
        public ActionResult Attendance(string id, string filterStatus, string search)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = GetCurrentUserId();
                
                // Check ownership
                if (!IsMyActivity(id, mand))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền điểm danh hoạt động này";
                    return RedirectToRoute("LecturerActivities", new { action = "Index" });
                }

                var viewModel = GetAttendanceViewModel(id, filterStatus, search);
                return View("~/Views/Lecturer/Activities/Attendance.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToRoute("LecturerActivities", new { action = "Index" });
            }
        }

        // POST: Lecturer/Activities/StartAttendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StartAttendance(string activityId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = GetCurrentUserId();
                
                if (!IsMyActivity(activityId, mand))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền điểm danh hoạt động này";
                    return RedirectToRoute("LecturerActivities", new { action = "Index" });
                }

                string updateQuery = @"UPDATE ACTIVITIES 
                                       SET ATTENDANCE_STATUS = 'IN_PROGRESS' 
                                       WHERE ID = :Id AND ATTENDANCE_STATUS = 'PENDING'";
                var parameters = new[] { OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, activityId) };
                
                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);
                
                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Đã bắt đầu điểm danh!";
                }
                
                return RedirectToRoute("LecturerActivities", new { action = "Attendance", id = activityId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToRoute("LecturerActivities", new { action = "Attendance", id = activityId });
            }
        }

        // POST: Lecturer/Activities/MarkAllPresent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAllPresent(string activityId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = GetCurrentUserId();
                
                if (!IsMyActivity(activityId, mand))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền điểm danh hoạt động này";
                    return RedirectToRoute("LecturerActivities", new { action = "Index" });
                }

                // Update all PENDING registrations to PRESENT
                string updateQuery = @"UPDATE REGISTRATIONS 
                                       SET ATTENDANCE_STATUS = 'PRESENT',
                                           STATUS = 'CHECKED_IN',
                                           CHECKED_IN_AT = SYSTIMESTAMP,
                                           CHECKED_IN_BY = :CheckedInBy
                                       WHERE ACTIVITY_ID = :ActivityId 
                                       AND ATTENDANCE_STATUS = 'PENDING'
                                       AND STATUS != 'CANCELLED'";
                
                var parameters = new[] {
                    OracleDbHelper.CreateParameter("CheckedInBy", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);
                
                // Update activity status to IN_PROGRESS if still PENDING
                string actUpdateQuery = @"UPDATE ACTIVITIES SET ATTENDANCE_STATUS = 'IN_PROGRESS' 
                                          WHERE ID = :Id AND ATTENDANCE_STATUS = 'PENDING'";
                OracleDbHelper.ExecuteNonQuery(actUpdateQuery, new[] { OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, activityId) });

                TempData["SuccessMessage"] = $"Đã đánh dấu {result} sinh viên CÓ MẶT!";
                return RedirectToRoute("LecturerActivities", new { action = "Attendance", id = activityId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToRoute("LecturerActivities", new { action = "Attendance", id = activityId });
            }
        }

        // POST: Lecturer/Activities/MarkAllAbsent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAllAbsent(string activityId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = GetCurrentUserId();
                
                if (!IsMyActivity(activityId, mand))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền điểm danh hoạt động này";
                    return RedirectToRoute("LecturerActivities", new { action = "Index" });
                }

                // Update all PENDING registrations to ABSENT
                string updateQuery = @"UPDATE REGISTRATIONS 
                                       SET ATTENDANCE_STATUS = 'ABSENT'
                                       WHERE ACTIVITY_ID = :ActivityId 
                                       AND ATTENDANCE_STATUS = 'PENDING'
                                       AND STATUS != 'CANCELLED'";
                
                var parameters = new[] {
                    OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);
                
                // Update activity status to IN_PROGRESS if still PENDING
                string actUpdateQuery = @"UPDATE ACTIVITIES SET ATTENDANCE_STATUS = 'IN_PROGRESS' 
                                          WHERE ID = :Id AND ATTENDANCE_STATUS = 'PENDING'";
                OracleDbHelper.ExecuteNonQuery(actUpdateQuery, new[] { OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, activityId) });

                TempData["SuccessMessage"] = $"Đã đánh dấu {result} sinh viên VẮNG MẶT!";
                return RedirectToRoute("LecturerActivities", new { action = "Attendance", id = activityId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToRoute("LecturerActivities", new { action = "Attendance", id = activityId });
            }
        }

        // POST: Lecturer/Activities/UpdateAttendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateAttendance(string registrationId, string status, string activityId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = GetCurrentUserId();
                
                if (!IsMyActivity(activityId, mand))
                {
                    return Json(new { success = false, message = "Không có quyền" });
                }

                if (status != "PRESENT" && status != "ABSENT")
                {
                    return Json(new { success = false, message = "Trạng thái không hợp lệ" });
                }

                string updateQuery;
                OracleParameter[] parameters;

                if (status == "PRESENT")
                {
                    updateQuery = @"UPDATE REGISTRATIONS 
                                   SET ATTENDANCE_STATUS = 'PRESENT',
                                       STATUS = 'CHECKED_IN',
                                       CHECKED_IN_AT = SYSTIMESTAMP,
                                       CHECKED_IN_BY = :CheckedInBy
                                   WHERE ID = :RegId AND SCORE_APPLIED = 0";
                    parameters = new[] {
                        OracleDbHelper.CreateParameter("CheckedInBy", OracleDbType.Varchar2, mand),
                        OracleDbHelper.CreateParameter("RegId", OracleDbType.Varchar2, registrationId)
                    };
                }
                else
                {
                    updateQuery = @"UPDATE REGISTRATIONS 
                                   SET ATTENDANCE_STATUS = 'ABSENT',
                                       STATUS = 'REGISTERED',
                                       CHECKED_IN_AT = NULL,
                                       CHECKED_IN_BY = NULL
                                   WHERE ID = :RegId AND SCORE_APPLIED = 0";
                    parameters = new[] {
                        OracleDbHelper.CreateParameter("RegId", OracleDbType.Varchar2, registrationId)
                    };
                }

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);

                if (result > 0)
                {
                    return Json(new { success = true, message = "Đã cập nhật" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể cập nhật (có thể đã tính điểm)" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Lecturer/Activities/ConfirmAttendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmAttendance(string activityId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = GetCurrentUserId();
                
                if (!IsMyActivity(activityId, mand))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xác nhận hoạt động này";
                    return RedirectToRoute("LecturerActivities", new { action = "Index" });
                }

                // Get activity info
                string actQuery = @"SELECT POINTS, ABSENCE_PENALTY, TERM_ID FROM ACTIVITIES WHERE ID = :Id";
                var actParams = new[] { OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, activityId) };
                DataTable actDt = OracleDbHelper.ExecuteQuery(actQuery, actParams);
                
                if (actDt.Rows.Count == 0)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hoạt động";
                    return RedirectToRoute("LecturerActivities", new { action = "Index" });
                }

                int activityPoints = actDt.Rows[0]["POINTS"] != DBNull.Value ? Convert.ToInt32(actDt.Rows[0]["POINTS"]) : 0;
                int absencePenalty = actDt.Rows[0]["ABSENCE_PENALTY"] != DBNull.Value ? Convert.ToInt32(actDt.Rows[0]["ABSENCE_PENALTY"]) : 5;
                string termId = actDt.Rows[0]["TERM_ID"].ToString();

                // Get all registrations that haven't been scored yet
                string regQuery = @"SELECT ID, STUDENT_ID, ATTENDANCE_STATUS 
                                   FROM REGISTRATIONS 
                                   WHERE ACTIVITY_ID = :ActivityId 
                                   AND SCORE_APPLIED = 0 
                                   AND STATUS != 'CANCELLED'
                                   AND ATTENDANCE_STATUS IN ('PRESENT', 'ABSENT')";
                DataTable regDt = OracleDbHelper.ExecuteQuery(regQuery, actParams);

                int presentCount = 0;
                int absentCount = 0;

                foreach (DataRow row in regDt.Rows)
                {
                    string studentId = row["STUDENT_ID"].ToString();
                    string attendanceStatus = row["ATTENDANCE_STATUS"].ToString();
                    string registrationId = row["ID"].ToString();
                    
                    int pointsToAdd = attendanceStatus == "PRESENT" ? activityPoints : -absencePenalty;
                    
                    // Update or insert SCORES
                    string scoreQuery = @"
                        MERGE INTO SCORES s
                        USING (SELECT :StudentId as STUDENT_ID, :TermId as TERM_ID FROM DUAL) src
                        ON (s.STUDENT_ID = src.STUDENT_ID AND s.TERM_ID = src.TERM_ID)
                        WHEN MATCHED THEN
                            UPDATE SET TOTAL_SCORE = GREATEST(0, LEAST(100, TOTAL_SCORE + :Points))
                        WHEN NOT MATCHED THEN
                            INSERT (STUDENT_ID, TERM_ID, TOTAL_SCORE, STATUS)
                            VALUES (:StudentId, :TermId, GREATEST(0, LEAST(100, 70 + :Points)), 'PROVISIONAL')";
                    
                    var scoreParams = new[] {
                        OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, studentId),
                        OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId),
                        OracleDbHelper.CreateParameter("Points", OracleDbType.Int32, pointsToAdd)
                    };
                    
                    OracleDbHelper.ExecuteNonQuery(scoreQuery, scoreParams);
                    
                    // Mark registration as scored
                    string updateRegQuery = @"UPDATE REGISTRATIONS 
                                              SET SCORE_APPLIED = 1 
                                              WHERE ID = :RegId";
                    OracleDbHelper.ExecuteNonQuery(updateRegQuery, new[] { 
                        OracleDbHelper.CreateParameter("RegId", OracleDbType.Varchar2, registrationId) 
                    });

                    if (attendanceStatus == "PRESENT") presentCount++;
                    else absentCount++;
                }

                // Update activity status to CONFIRMED
                string confirmQuery = @"UPDATE ACTIVITIES 
                                       SET ATTENDANCE_STATUS = 'CONFIRMED',
                                           ATTENDANCE_CONFIRMED_BY = :ConfirmedBy,
                                           ATTENDANCE_CONFIRMED_AT = SYSTIMESTAMP
                                       WHERE ID = :Id";
                var confirmParams = new[] {
                    OracleDbHelper.CreateParameter("ConfirmedBy", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, activityId)
                };
                OracleDbHelper.ExecuteNonQuery(confirmQuery, confirmParams);

                TempData["SuccessMessage"] = $"Đã xác nhận điểm danh và tính điểm! Có mặt: {presentCount} (+{activityPoints}đ), Vắng: {absentCount} (-{absencePenalty}đ)";
                return RedirectToRoute("LecturerActivities", new { action = "Attendance", id = activityId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToRoute("LecturerActivities", new { action = "Attendance", id = activityId });
            }
        }

        #region Helper Methods

        private void LoadViewBagData()
        {
            // Load Terms
            string termQuery = "SELECT ID, NAME FROM TERMS ORDER BY START_DATE DESC";
            DataTable dtTerms = OracleDbHelper.ExecuteQuery(termQuery);
            ViewBag.Terms = new SelectList(dtTerms.DefaultView, "ID", "NAME");
        }

        private bool IsMyActivity(string activityId, string organizerId)
        {
            string query = "SELECT COUNT(*) FROM ACTIVITIES WHERE ID = :Id AND ORGANIZER_ID = :OrganizerId";
            var parameters = new[]
            {
                OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, activityId),
                OracleDbHelper.CreateParameter("OrganizerId", OracleDbType.Varchar2, organizerId)
            };
            return Convert.ToInt32(OracleDbHelper.ExecuteScalar(query, parameters)) > 0;
        }

        private List<LecturerActivityItem> GetMyActivities(string mand, string search, string status, int page, int pageSize, out int totalCount)
        {
            var activities = new List<LecturerActivityItem>();
            totalCount = 0;

            string countQuery = "SELECT COUNT(*) FROM ACTIVITIES WHERE ORGANIZER_ID = :OrganizerId";
            string dataQuery = @"SELECT a.ID, a.TITLE, a.START_AT, a.END_AT, a.LOCATION, 
                                       a.POINTS, a.MAX_SEATS, a.STATUS, a.APPROVAL_STATUS, a.CREATED_AT,
                                       (SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STATUS != 'CANCELLED') as CURRENT_PARTICIPANTS
                                FROM ACTIVITIES a
                                WHERE a.ORGANIZER_ID = :OrganizerId";

            var parameters = new List<OracleParameter>
            {
                OracleDbHelper.CreateParameter("OrganizerId", OracleDbType.Varchar2, mand)
            };

            if (!string.IsNullOrEmpty(search))
            {
                countQuery += " AND UPPER(TITLE) LIKE :Search";
                dataQuery += " AND UPPER(TITLE) LIKE :Search";
                parameters.Add(OracleDbHelper.CreateParameter("Search", OracleDbType.Varchar2, "%" + search.ToUpper() + "%"));
            }

            if (!string.IsNullOrEmpty(status) && status != "ALL")
            {
                countQuery += " AND STATUS = :Status";
                dataQuery += " AND STATUS = :Status";
                parameters.Add(OracleDbHelper.CreateParameter("Status", OracleDbType.Varchar2, status));
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
                activities.Add(new LecturerActivityItem
                {
                    Id = row["ID"].ToString(),
                    Title = row["TITLE"].ToString(),
                    StartAt = Convert.ToDateTime(row["START_AT"]),
                    EndAt = Convert.ToDateTime(row["END_AT"]),
                    Location = row["LOCATION"] != DBNull.Value ? row["LOCATION"].ToString() : "",
                    Points = row["POINTS"] != DBNull.Value ? (decimal?)Convert.ToDecimal(row["POINTS"]) : null,
                    MaxSeats = row["MAX_SEATS"] != DBNull.Value ? Convert.ToInt32(row["MAX_SEATS"]) : 0,
                    CurrentParticipants = row["CURRENT_PARTICIPANTS"] != DBNull.Value ? Convert.ToInt32(row["CURRENT_PARTICIPANTS"]) : 0,
                    Status = row["STATUS"].ToString(),
                    ApprovalStatus = row["APPROVAL_STATUS"].ToString(),
                    CreatedAt = Convert.ToDateTime(row["CREATED_AT"])
                });
            }

            return activities;
        }

        private ActivityFormViewModel GetActivityForEdit(string id, string organizerId)
        {
            string query = @"SELECT * FROM ACTIVITIES WHERE ID = :Id AND ORGANIZER_ID = :OrganizerId";
            var parameters = new[]
            {
                OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id),
                OracleDbHelper.CreateParameter("OrganizerId", OracleDbType.Varchar2, organizerId)
            };

            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);
            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new ActivityFormViewModel
            {
                Id = row["ID"].ToString(),
                Title = row["TITLE"].ToString(),
                Description = row["DESCRIPTION"] != DBNull.Value ? row["DESCRIPTION"].ToString() : "",
                Requirements = row["REQUIREMENTS"] != DBNull.Value ? row["REQUIREMENTS"].ToString() : "",
                Benefits = row["BENEFITS"] != DBNull.Value ? row["BENEFITS"].ToString() : "",
                TermId = row["TERM_ID"].ToString(),
                StartAt = Convert.ToDateTime(row["START_AT"]),
                EndAt = Convert.ToDateTime(row["END_AT"]),
                MaxSeats = row["MAX_SEATS"] != DBNull.Value ? Convert.ToInt32(row["MAX_SEATS"]) : 0,
                Location = row["LOCATION"] != DBNull.Value ? row["LOCATION"].ToString() : "",
                Points = row["POINTS"] != DBNull.Value ? Convert.ToDecimal(row["POINTS"]) : 0,
                RegistrationStart = row["REGISTRATION_START"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["REGISTRATION_START"]) : null,
                RegistrationDeadline = row["REGISTRATION_DEADLINE"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["REGISTRATION_DEADLINE"]) : null
            };
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
                                        r.REGISTERED_AT, r.CHECKED_IN_AT, r.CHECKED_IN_BY,
                                        u.FULL_NAME as STUDENT_NAME, u.EMAIL as STUDENT_EMAIL,
                                        s.STUDENT_CODE, c.NAME as CLASS_NAME,
                                        p.ID as PROOF_ID, p.STATUS as PROOF_STATUS,
                                        checker.FULL_NAME as CHECKED_IN_BY_NAME
                                 FROM REGISTRATIONS r
                                 INNER JOIN USERS u ON r.STUDENT_ID = u.MAND
                                 INNER JOIN STUDENTS s ON u.MAND = s.USER_ID
                                 LEFT JOIN CLASSES c ON s.CLASS_ID = c.ID
                                 LEFT JOIN USERS checker ON r.CHECKED_IN_BY = checker.MAND
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
                    ProofStatus = row["PROOF_STATUS"] != DBNull.Value ? row["PROOF_STATUS"].ToString() : "",
                    CheckedInBy = row["CHECKED_IN_BY"] != DBNull.Value ? row["CHECKED_IN_BY"].ToString() : "",
                    CheckedInByName = row["CHECKED_IN_BY_NAME"] != DBNull.Value ? row["CHECKED_IN_BY_NAME"].ToString() : ""
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

        private AttendanceViewModel GetAttendanceViewModel(string activityId, string filterStatus, string search)
        {
            // Get activity info
            string activityQuery = @"SELECT a.ID, a.TITLE, a.START_AT, a.END_AT, a.LOCATION, 
                                           a.POINTS, a.ABSENCE_PENALTY, a.ATTENDANCE_STATUS,
                                           a.ATTENDANCE_CONFIRMED_BY, a.ATTENDANCE_CONFIRMED_AT,
                                           u.FULL_NAME as CONFIRMED_BY_NAME
                                    FROM ACTIVITIES a
                                    LEFT JOIN USERS u ON a.ATTENDANCE_CONFIRMED_BY = u.MAND
                                    WHERE a.ID = :ActivityId";
            var activityParams = new[] { OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId) };
            DataTable activityTable = OracleDbHelper.ExecuteQuery(activityQuery, activityParams);

            if (activityTable.Rows.Count == 0)
            {
                return new AttendanceViewModel
                {
                    ActivityId = activityId,
                    Records = new List<AttendanceRecordItem>()
                };
            }

            var row = activityTable.Rows[0];
            var viewModel = new AttendanceViewModel
            {
                ActivityId = activityId,
                ActivityTitle = row["TITLE"].ToString(),
                ActivityStartAt = Convert.ToDateTime(row["START_AT"]),
                ActivityEndAt = Convert.ToDateTime(row["END_AT"]),
                Location = row["LOCATION"] != DBNull.Value ? row["LOCATION"].ToString() : "",
                ActivityPoints = row["POINTS"] != DBNull.Value ? Convert.ToInt32(row["POINTS"]) : 0,
                AbsencePenalty = row["ABSENCE_PENALTY"] != DBNull.Value ? Convert.ToInt32(row["ABSENCE_PENALTY"]) : 5,
                AttendanceStatus = row["ATTENDANCE_STATUS"] != DBNull.Value ? row["ATTENDANCE_STATUS"].ToString() : "PENDING",
                ConfirmedByName = row["CONFIRMED_BY_NAME"] != DBNull.Value ? row["CONFIRMED_BY_NAME"].ToString() : "",
                ConfirmedAt = row["ATTENDANCE_CONFIRMED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["ATTENDANCE_CONFIRMED_AT"]) : null,
                Records = new List<AttendanceRecordItem>(),
                FilterStatus = filterStatus ?? "ALL",
                SearchKeyword = search
            };

            // Get registrations
            string dataQuery = @"SELECT r.ID as REG_ID, r.STUDENT_ID, r.ATTENDANCE_STATUS, r.SCORE_APPLIED,
                                       r.REGISTERED_AT, r.CHECKED_IN_AT,
                                       u.FULL_NAME as STUDENT_NAME,
                                       s.STUDENT_CODE, c.NAME as CLASS_NAME
                                FROM REGISTRATIONS r
                                INNER JOIN USERS u ON r.STUDENT_ID = u.MAND
                                INNER JOIN STUDENTS s ON u.MAND = s.USER_ID
                                LEFT JOIN CLASSES c ON s.CLASS_ID = c.ID
                                WHERE r.ACTIVITY_ID = :ActivityId AND r.STATUS != 'CANCELLED'";

            var parameters = new List<OracleParameter> { activityParams[0] };

            // Apply filters
            if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "ALL")
            {
                dataQuery += " AND r.ATTENDANCE_STATUS = :Status";
                parameters.Add(OracleDbHelper.CreateParameter("Status", OracleDbType.Varchar2, filterStatus));
            }

            if (!string.IsNullOrEmpty(search))
            {
                dataQuery += " AND (UPPER(u.FULL_NAME) LIKE :Search OR UPPER(s.STUDENT_CODE) LIKE :Search)";
                parameters.Add(OracleDbHelper.CreateParameter("Search", OracleDbType.Varchar2, "%" + search.ToUpper() + "%"));
            }

            dataQuery += " ORDER BY c.NAME, u.FULL_NAME";

            DataTable dt = OracleDbHelper.ExecuteQuery(dataQuery, parameters.ToArray());

            foreach (DataRow r in dt.Rows)
            {
                viewModel.Records.Add(new AttendanceRecordItem
                {
                    RegistrationId = r["REG_ID"].ToString(),
                    StudentId = r["STUDENT_ID"].ToString(),
                    StudentCode = r["STUDENT_CODE"].ToString(),
                    StudentName = r["STUDENT_NAME"].ToString(),
                    ClassName = r["CLASS_NAME"] != DBNull.Value ? r["CLASS_NAME"].ToString() : "",
                    AttendanceStatus = r["ATTENDANCE_STATUS"] != DBNull.Value ? r["ATTENDANCE_STATUS"].ToString() : "PENDING",
                    ScoreApplied = r["SCORE_APPLIED"] != DBNull.Value && Convert.ToInt32(r["SCORE_APPLIED"]) == 1,
                    RegisteredAt = Convert.ToDateTime(r["REGISTERED_AT"]),
                    CheckedInAt = r["CHECKED_IN_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(r["CHECKED_IN_AT"]) : null
                });
            }

            // Calculate statistics
            viewModel.TotalStudents = viewModel.Records.Count;
            viewModel.PresentCount = viewModel.Records.Count(x => x.AttendanceStatus == "PRESENT");
            viewModel.AbsentCount = viewModel.Records.Count(x => x.AttendanceStatus == "ABSENT");
            viewModel.PendingCount = viewModel.Records.Count(x => x.AttendanceStatus == "PENDING");

            return viewModel;
        }

        #endregion
    }
}
