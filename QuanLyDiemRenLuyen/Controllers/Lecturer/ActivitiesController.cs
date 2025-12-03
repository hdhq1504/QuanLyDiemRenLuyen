using System;
using System.Collections.Generic;
using System.Data;
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
                                           APPROVAL_STATUS, ORGANIZER_ID, CREATED_AT)
                                          VALUES 
                                          (RAWTOHEX(SYS_GUID()), :Title, :Description, :Requirements, :Benefits,
                                           :TermId, :StartAt, :EndAt,
                                           'OPEN', :MaxSeats, :Location, :Points,
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
                Points = row["POINTS"] != DBNull.Value ? Convert.ToDecimal(row["POINTS"]) : 0
            };
        }

        #endregion
    }
}
