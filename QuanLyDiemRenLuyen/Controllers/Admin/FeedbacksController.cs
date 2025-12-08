using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;

namespace QuanLyDiemRenLuyen.Controllers.Admin
{
    public class FeedbacksController : AdminBaseController
    {
        // GET: Admin/Feedbacks
        public ActionResult Index(string status, string search, int page = 1)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = new AdminFeedbackListViewModel
                {
                    FilterStatus = status ?? "ALL",
                    SearchKeyword = search,
                    CurrentPage = page,
                    PageSize = 20
                };

                viewModel.Feedbacks = GetFeedbacksList(status, search, page, viewModel.PageSize, out int totalCount);
                viewModel.TotalPages = (int)Math.Ceiling((double)totalCount / viewModel.PageSize);

                return View("~/Views/Admin/Feedbacks/Index.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/Feedbacks/Index.cshtml", new AdminFeedbackListViewModel { Feedbacks = new List<AdminFeedbackItem>() });
            }
        }

        // GET: Admin/Feedbacks/Detail/id
        public ActionResult Detail(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetFeedbackDetail(id);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy phản hồi";
                    return RedirectToAction("Index");
                }

                return View("~/Views/Admin/Feedbacks/Detail.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Admin/Feedbacks/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = Session["MAND"].ToString();
                
                // Get feedback details
                var feedback = GetFeedbackDetail(id);
                if (feedback == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy phản hồi";
                    return Redirect(Url.RouteUrl("AdminFeedbacks", new { action = "Index" }));
                }

                if (string.IsNullOrEmpty(feedback.ActivityId))
                {
                    TempData["ErrorMessage"] = "Phản hồi không liên kết với hoạt động";
                    return Redirect(Url.RouteUrl("AdminFeedbacks", new { action = "Detail", id = id }));
                }

                // Get student USER_ID from STUDENT_CODE
                string getStudentQuery = @"SELECT USER_ID FROM STUDENTS WHERE STUDENT_CODE = :StudentCode";
                var studentParams = new[] { OracleDbHelper.CreateParameter("StudentCode", OracleDbType.Varchar2, feedback.StudentId) };
                DataTable studentDt = OracleDbHelper.ExecuteQuery(getStudentQuery, studentParams);
                
                if (studentDt.Rows.Count == 0)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sinh viên";
                    return Redirect(Url.RouteUrl("AdminFeedbacks", new { action = "Detail", id = id }));
                }
                
                string studentUserId = studentDt.Rows[0]["USER_ID"].ToString();

                // Get current term from feedback
                string getTermQuery = @"SELECT TERM_ID FROM FEEDBACKS WHERE ID = :Id";
                var termParams = new[] { OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id) };
                DataTable termDt = OracleDbHelper.ExecuteQuery(getTermQuery, termParams);
                string termId = termDt.Rows[0]["TERM_ID"].ToString();

                // Update or create SCORES record
                string checkScoreQuery = @"SELECT ID, TOTAL FROM SCORES WHERE STUDENT_ID = :StudentId AND TERM_ID = :TermId";
                var checkParams = new[]
                {
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, studentUserId),
                    OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId)
                };
                DataTable scoreDt = OracleDbHelper.ExecuteQuery(checkScoreQuery, checkParams);

                decimal pointsToAdd = feedback.ActivityPoints ?? 0;

                if (scoreDt.Rows.Count > 0)
                {
                    // Update existing score
                    string scoreId = scoreDt.Rows[0]["ID"].ToString();
                    decimal currentTotal = Convert.ToDecimal(scoreDt.Rows[0]["TOTAL"]);
                    decimal newTotal = currentTotal + pointsToAdd;

                    string updateScoreQuery = @"UPDATE SCORES SET TOTAL = :NewTotal WHERE ID = :ScoreId";
                    var updateParams = new[]
                    {
                        OracleDbHelper.CreateParameter("NewTotal", OracleDbType.Decimal, newTotal),
                        OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId)
                    };
                    OracleDbHelper.ExecuteNonQuery(updateScoreQuery, updateParams);
                }
                else
                {
                    // Create new score record
                    string newScoreId = "SC" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    string insertScoreQuery = @"INSERT INTO SCORES (ID, STUDENT_ID, TERM_ID, TOTAL, STATUS, CREATED_AT)
                                               VALUES (:Id, :StudentId, :TermId, :Total, 'PENDING', SYSDATE)";
                    var insertParams = new[]
                    {
                        OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, newScoreId),
                        OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, studentUserId),
                        OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId),
                        OracleDbHelper.CreateParameter("Total", OracleDbType.Decimal, pointsToAdd)
                    };
                    OracleDbHelper.ExecuteNonQuery(insertScoreQuery, insertParams);
                }

                // Update feedback status
                string updateFeedbackQuery = @"UPDATE FEEDBACKS
                                              SET STATUS = 'RESPONDED',
                                                  RESPONSE = :Response,
                                                  RESPONDED_AT = SYSDATE
                                              WHERE ID = :Id";
                var feedbackParams = new[]
                {
                    OracleDbHelper.CreateParameter("Response", OracleDbType.Clob, $"Đã duyệt và cộng {pointsToAdd} điểm từ hoạt động {feedback.ActivityTitle}"),
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id)
                };
                OracleDbHelper.ExecuteNonQuery(updateFeedbackQuery, feedbackParams);

                TempData["SuccessMessage"] = $"Đã duyệt và cộng {pointsToAdd} điểm cho sinh viên";
                return Redirect(Url.RouteUrl("AdminFeedbacks", new { action = "Detail", id = id }));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return Redirect(Url.RouteUrl("AdminFeedbacks", new { action = "Detail", id = id }));
            }
        }

        // POST: Admin/Feedbacks/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(string id, string reason)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = Session["MAND"].ToString();

                if (string.IsNullOrWhiteSpace(reason))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối";
                    return Redirect(Url.RouteUrl("AdminFeedbacks", new { action = "Detail", id = id }));
                }

                string updateQuery = @"UPDATE FEEDBACKS
                                      SET STATUS = 'RESPONDED',
                                          RESPONSE = :Response,
                                          RESPONDED_AT = SYSDATE
                                      WHERE ID = :Id";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("Response", OracleDbType.Clob, reason),
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Đã từ chối phản hồi";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể cập nhật phản hồi";
                }

                return Redirect(Url.RouteUrl("AdminFeedbacks", new { action = "Detail", id = id }));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return Redirect(Url.RouteUrl("AdminFeedbacks", new { action = "Detail", id = id }));
            }
        }

        #region Private Helper Methods

        private List<AdminFeedbackItem> GetFeedbacksList(string status, string search, int page, int pageSize, out int totalCount)
        {
            var feedbacks = new List<AdminFeedbackItem>();
            totalCount = 0;

            string countQuery = @"SELECT COUNT(*) FROM FEEDBACKS f
                                 INNER JOIN STUDENTS s ON f.STUDENT_ID = s.USER_ID
                                 INNER JOIN USERS u ON s.USER_ID = u.MAND
                                 WHERE 1=1";

            string dataQuery = @"SELECT f.ID, f.TITLE, f.CONTENT, f.STATUS, f.CREATED_AT, f.RESPONDED_AT,
                                       s.STUDENT_CODE, u.FULL_NAME as STUDENT_NAME,
                                       t.NAME as TERM_NAME
                                FROM FEEDBACKS f
                                INNER JOIN STUDENTS s ON f.STUDENT_ID = s.USER_ID
                                INNER JOIN USERS u ON s.USER_ID = u.MAND
                                INNER JOIN TERMS t ON f.TERM_ID = t.ID
                                WHERE 1=1";

            var parameters = new List<OracleParameter>();

            if (!string.IsNullOrEmpty(status) && status != "ALL")
            {
                countQuery += " AND f.STATUS = :Status";
                dataQuery += " AND f.STATUS = :Status";
                parameters.Add(OracleDbHelper.CreateParameter("Status", OracleDbType.Varchar2, status));
            }

            if (!string.IsNullOrEmpty(search))
            {
                countQuery += " AND (UPPER(f.TITLE) LIKE :Search OR UPPER(u.FULL_NAME) LIKE :Search OR UPPER(s.STUDENT_CODE) LIKE :Search)";
                dataQuery += " AND (UPPER(f.TITLE) LIKE :Search OR UPPER(u.FULL_NAME) LIKE :Search OR UPPER(s.STUDENT_CODE) LIKE :Search)";
                parameters.Add(OracleDbHelper.CreateParameter("Search", OracleDbType.Varchar2, "%" + search.ToUpper() + "%"));
            }

            totalCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(countQuery, parameters.ToArray()));

            dataQuery += " ORDER BY f.CREATED_AT DESC";
            int offset = (page - 1) * pageSize;
            dataQuery = $@"SELECT * FROM (
                            SELECT a.*, ROWNUM rnum FROM ({dataQuery}) a
                            WHERE ROWNUM <= {offset + pageSize}
                          ) WHERE rnum > {offset}";

            DataTable dt = OracleDbHelper.ExecuteQuery(dataQuery, parameters.ToArray());

            foreach (DataRow row in dt.Rows)
            {
                string content = row["CONTENT"].ToString();
                try
                {
                    content = EncryptionHelper.Decrypt(content);
                }
                catch { } // Ignore decryption errors for list view

                feedbacks.Add(new AdminFeedbackItem
                {
                    Id = row["ID"].ToString(),
                    Title = row["TITLE"].ToString(),
                    Content = content.Length > 100 ? content.Substring(0, 97) + "..." : content,
                    StudentId = row["STUDENT_CODE"].ToString(),
                    StudentName = row["STUDENT_NAME"].ToString(),
                    TermName = row["TERM_NAME"].ToString(),
                    Status = row["STATUS"].ToString(),
                    CreatedAt = Convert.ToDateTime(row["CREATED_AT"]),
                    RespondedAt = row["RESPONDED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["RESPONDED_AT"]) : null
                });
            }

            return feedbacks;
        }

        private AdminFeedbackItem GetFeedbackDetail(string id)
        {
            string query = @"SELECT f.ID, f.TITLE, f.CONTENT, f.STATUS, f.CREATED_AT, f.RESPONDED_AT, f.RESPONSE,
                                   f.STUDENT_ID, f.ACTIVITY_ID,
                                   s.STUDENT_CODE, u.FULL_NAME as STUDENT_NAME,
                                   t.NAME as TERM_NAME,
                                   a.TITLE as ACTIVITY_TITLE, a.POINTS as ACTIVITY_POINTS,
                                   reg.STATUS as REG_STATUS, reg.CHECKED_IN_AT
                            FROM FEEDBACKS f
                            INNER JOIN STUDENTS s ON f.STUDENT_ID = s.USER_ID
                            INNER JOIN USERS u ON s.USER_ID = u.MAND
                            INNER JOIN TERMS t ON f.TERM_ID = t.ID
                            LEFT JOIN ACTIVITIES a ON f.ACTIVITY_ID = a.ID
                            LEFT JOIN REGISTRATIONS reg ON f.ACTIVITY_ID = reg.ACTIVITY_ID AND f.STUDENT_ID = reg.STUDENT_ID
                            WHERE f.ID = :Id";

            var parameters = new[] { OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id) };
            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new AdminFeedbackItem
            {
                Id = row["ID"].ToString(),
                Title = row["TITLE"].ToString(),
                Content = EncryptionHelper.Decrypt(row["CONTENT"].ToString()),
                StudentId = row["STUDENT_CODE"].ToString(),
                StudentName = row["STUDENT_NAME"].ToString(),
                TermName = row["TERM_NAME"].ToString(),
                Status = row["STATUS"].ToString(),
                CreatedAt = Convert.ToDateTime(row["CREATED_AT"]),
                RespondedAt = row["RESPONDED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["RESPONDED_AT"]) : null,
                Response = row["RESPONSE"] != DBNull.Value ? row["RESPONSE"].ToString() : null,
                // Activity info
                ActivityId = row["ACTIVITY_ID"] != DBNull.Value ? row["ACTIVITY_ID"].ToString() : null,
                ActivityTitle = row["ACTIVITY_TITLE"] != DBNull.Value ? row["ACTIVITY_TITLE"].ToString() : null,
                ActivityPoints = row["ACTIVITY_POINTS"] != DBNull.Value ? Convert.ToDecimal(row["ACTIVITY_POINTS"]) : (decimal?)null,
                HasCheckedIn = row["REG_STATUS"] != DBNull.Value && row["REG_STATUS"].ToString() == "CHECKED_IN",
                CheckedInAt = row["CHECKED_IN_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["CHECKED_IN_AT"]) : null
            };
        }

        #endregion
    }
}
