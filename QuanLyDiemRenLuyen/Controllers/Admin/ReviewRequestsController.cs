using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;

namespace QuanLyDiemRenLuyen.Controllers.Admin
{
    public class ReviewRequestsController : AdminBaseController
    {
        // GET: Admin/ReviewRequests
        public ActionResult Index(string status, string term)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetReviewRequestListViewModel(status, term);
                return View("~/Views/Admin/ReviewRequests.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/ReviewRequests.cshtml", new ReviewRequestListViewModel { Requests = new List<ReviewRequestItem>() });
            }
        }

        // POST: Admin/ReviewRequests/Respond
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Respond(string requestId, string action, string response, decimal? approvedScore)
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

        #region Private Helper Methods

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

        #endregion
    }
}
