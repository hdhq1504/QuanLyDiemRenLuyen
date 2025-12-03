using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;

namespace QuanLyDiemRenLuyen.Controllers.Student
{
    /// <summary>
    /// Controller xử lý Scores và Review Requests của sinh viên
    /// </summary>
    public class ScoresController : StudentBaseController
    {
        // GET: Student/Scores
        public ActionResult Index()
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                string mand = GetCurrentStudentId();
                var viewModel = GetScoresViewModel(mand);
                return View("~/Views/Student/Scores.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Student/Scores.cshtml", new ScoreViewModel());
            }
        }

        // GET: Student/Scores/Detail
        public ActionResult Detail(string scoreId)
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                string mand = GetCurrentStudentId();

                if (string.IsNullOrEmpty(scoreId))
                {
                    TempData["ErrorMessage"] = "Không tìm thấy điểm";
                    return RedirectToAction("Index");
                }

                var viewModel = GetScoreDetailViewModel(mand, scoreId);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy điểm";
                    return RedirectToAction("Index");
                }

                return View("~/Views/Student/ScoreDetail.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: Student/Scores/CreateReviewRequest
        public ActionResult CreateReviewRequest(string scoreId)
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                string mand = GetCurrentStudentId();

                if (string.IsNullOrEmpty(scoreId))
                {
                    TempData["ErrorMessage"] = "Không tìm thấy điểm";
                    return RedirectToAction("Index");
                }

                // Kiểm tra xem điểm có tồn tại và thuộc về sinh viên này không
                string checkQuery = @"SELECT s.ID, s.TERM_ID, t.NAME as TERM_NAME, s.TOTAL_SCORE
                                     FROM SCORES s
                                     INNER JOIN TERMS t ON s.TERM_ID = t.ID
                                     WHERE s.ID = :ScoreId AND s.STUDENT_ID = :StudentId";

                var checkParams = new[]
                {
                    OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
                };

                DataTable dt = OracleDbHelper.ExecuteQuery(checkQuery, checkParams);
                if (dt.Rows.Count == 0)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy điểm";
                    return RedirectToAction("Index");
                }

                var model = new CreateReviewRequestViewModel
                {
                    ScoreId = scoreId,
                    TermId = dt.Rows[0]["TERM_ID"].ToString(),
                    TermName = dt.Rows[0]["TERM_NAME"].ToString(),
                    CurrentScore = Convert.ToInt32(dt.Rows[0]["TOTAL_SCORE"])
                };

                return View("~/Views/Student/CreateReviewRequest.cshtml", model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Student/Scores/CreateReviewRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateReviewRequest(CreateReviewRequestViewModel model)
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                string mand = GetCurrentStudentId();

                if (!ModelState.IsValid)
                {
                    return View("~/Views/Student/CreateReviewRequest.cshtml", model);
                }

                // Kiểm tra xem đã có đơn phúc khảo chưa
                string checkQuery = @"SELECT COUNT(*) FROM FEEDBACKS
                                     WHERE STUDENT_ID = :StudentId
                                     AND TERM_ID = :TermId
                                     AND STATUS IN ('SUBMITTED', 'IN_REVIEW')";

                var checkParams = new[]
                {
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, model.TermId)
                };

                int existingCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(checkQuery, checkParams));
                if (existingCount > 0)
                {
                    TempData["ErrorMessage"] = "Bạn đã có đơn phúc khảo đang chờ xử lý cho học kỳ này";
                    return RedirectToAction("Detail", new { scoreId = model.ScoreId });
                }

                // Tạo ID mới
                string newId = "FB" + DateTime.Now.ToString("yyyyMMddHHmmss");

                // Thêm đơn phúc khảo
                string insertQuery = @"INSERT INTO FEEDBACKS
                                      (ID, STUDENT_ID, TERM_ID, TITLE, CONTENT,
                                       REQUESTED_SCORE, STATUS, CREATED_AT)
                                      VALUES
                                      (:Id, :StudentId, :TermId, :Title, :Content,
                                       :RequestedScore, 'SUBMITTED', SYSDATE)";

                var insertParams = new[]
                {
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, newId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, model.TermId),
                    OracleDbHelper.CreateParameter("Title", OracleDbType.Varchar2, model.Title),
                    OracleDbHelper.CreateParameter("Content", OracleDbType.Clob, model.Content),
                    OracleDbHelper.CreateParameter("RequestedScore", OracleDbType.Decimal, model.RequestedScore)
                };

                OracleDbHelper.ExecuteNonQuery(insertQuery, insertParams);

                TempData["SuccessMessage"] = "Gửi đơn phúc khảo thành công";
                return RedirectToAction("Detail", new { scoreId = model.ScoreId });
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Student/CreateReviewRequest.cshtml", model);
            }
        }

        #region Private Helper Methods

        private ScoreViewModel GetScoresViewModel(string mand)
        {
            var viewModel = new ScoreViewModel();

            // Lấy thông tin sinh viên
            string studentQuery = @"SELECT u.FULL_NAME, s.STUDENT_CODE, c.NAME as CLASS_NAME
                                   FROM USERS u
                                   LEFT JOIN STUDENTS s ON u.MAND = s.USER_ID
                                   LEFT JOIN CLASSES c ON s.CLASS_ID = c.ID
                                   WHERE u.MAND = :MAND";

            var studentParams = new[] { OracleDbHelper.CreateParameter("MAND", OracleDbType.Varchar2, mand) };
            DataTable studentDt = OracleDbHelper.ExecuteQuery(studentQuery, studentParams);

            if (studentDt.Rows.Count > 0)
            {
                viewModel.StudentId = mand;
                viewModel.StudentName = studentDt.Rows[0]["FULL_NAME"].ToString();
                viewModel.StudentCode = studentDt.Rows[0]["STUDENT_CODE"] != DBNull.Value ? studentDt.Rows[0]["STUDENT_CODE"].ToString() : "Chưa cập nhật";
                viewModel.ClassName = studentDt.Rows[0]["CLASS_NAME"] != DBNull.Value ? studentDt.Rows[0]["CLASS_NAME"].ToString() : "Chưa cập nhật";
            }

            // Lấy danh sách điểm theo học kỳ
            string scoresQuery = @"SELECT s.ID, s.TERM_ID, t.NAME as TERM_NAME, t.YEAR as TERM_YEAR,
                                         t.TERM_NUMBER, s.TOTAL_SCORE, s.STATUS, s.APPROVED_BY,
                                         u.FULL_NAME as APPROVED_BY_NAME, s.APPROVED_AT, s.CREATED_AT
                                  FROM SCORES s
                                  INNER JOIN TERMS t ON s.TERM_ID = t.ID
                                  LEFT JOIN USERS u ON s.APPROVED_BY = u.MAND
                                  WHERE s.STUDENT_ID = :StudentId
                                  ORDER BY t.YEAR DESC, t.TERM_NUMBER DESC";

            var scoresParams = new[] { OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand) };
            DataTable scoresDt = OracleDbHelper.ExecuteQuery(scoresQuery, scoresParams);

            viewModel.TermScores = new List<TermScoreItem>();
            foreach (DataRow row in scoresDt.Rows)
            {
                int total = Convert.ToInt32(row["TOTAL_SCORE"]);
                string classification = GetClassification(total);
                string scoreId = row["ID"].ToString();
                string status = row["STATUS"].ToString();

                // Kiểm tra xem có đơn phúc khảo đang chờ không
                string checkFeedbackQuery = @"SELECT COUNT(*) FROM FEEDBACKS
                                             WHERE STUDENT_ID = :StudentId
                                             AND TERM_ID = :TermId
                                             AND STATUS IN ('SUBMITTED', 'IN_REVIEW')";

                var feedbackParams = new[]
                {
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, row["TERM_ID"].ToString())
                };

                int pendingFeedbackCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(checkFeedbackQuery, feedbackParams));

                viewModel.TermScores.Add(new TermScoreItem
                {
                    ScoreId = scoreId,
                    TermId = row["TERM_ID"].ToString(),
                    TermName = row["TERM_NAME"].ToString(),
                    TermYear = Convert.ToInt32(row["TERM_YEAR"]),
                    TermNumber = Convert.ToInt32(row["TERM_NUMBER"]),
                    Total = total,
                    Status = status,
                    Classification = classification,
                    ApprovedBy = row["APPROVED_BY"] != DBNull.Value ? row["APPROVED_BY"].ToString() : null,
                    ApprovedByName = row["APPROVED_BY_NAME"] != DBNull.Value ? row["APPROVED_BY_NAME"].ToString() : null,
                    ApprovedAt = row["APPROVED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["APPROVED_AT"]) : null,
                    CreatedAt = Convert.ToDateTime(row["CREATED_AT"]),
                    CanRequestReview = status == "APPROVED" && pendingFeedbackCount == 0,
                    HasPendingReview = pendingFeedbackCount > 0
                });
            }

            // Tính thống kê
            viewModel.Statistics = new ScoreStatistics();
            if (viewModel.TermScores.Count > 0)
            {
                viewModel.Statistics.AverageScore = viewModel.TermScores.Average(x => x.Total);
                viewModel.Statistics.HighestScore = viewModel.TermScores.Max(x => x.Total);
                viewModel.Statistics.LowestScore = viewModel.TermScores.Min(x => x.Total);
                viewModel.Statistics.TotalTerms = viewModel.TermScores.Count;
                viewModel.Statistics.ApprovedTerms = viewModel.TermScores.Count(x => x.Status == "APPROVED");
            }

            return viewModel;
        }

        private ScoreDetailViewModel GetScoreDetailViewModel(string mand, string scoreId)
        {
            // Lấy thông tin điểm cơ bản
            string scoreQuery = @"SELECT s.ID, s.STUDENT_ID, s.TERM_ID, t.NAME as TERM_NAME,
                                        t.YEAR as TERM_YEAR, t.TERM_NUMBER, s.TOTAL_SCORE, s.STATUS,
                                        s.APPROVED_BY, u.FULL_NAME as APPROVED_BY_NAME,
                                        s.APPROVED_AT, s.CREATED_AT,
                                        st.STUDENT_CODE, us.FULL_NAME as STUDENT_NAME,
                                        c.NAME as CLASS_NAME
                                 FROM SCORES s
                                 INNER JOIN TERMS t ON s.TERM_ID = t.ID
                                 INNER JOIN STUDENTS st ON s.STUDENT_ID = st.USER_ID
                                 INNER JOIN USERS us ON st.USER_ID = us.MAND
                                 LEFT JOIN CLASSES c ON st.CLASS_ID = c.ID
                                 LEFT JOIN USERS u ON s.APPROVED_BY = u.MAND
                                 WHERE s.ID = :ScoreId AND s.STUDENT_ID = :StudentId";

            var scoreParams = new[]
            {
                OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId),
                OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
            };

            DataTable scoreDt = OracleDbHelper.ExecuteQuery(scoreQuery, scoreParams);
            if (scoreDt.Rows.Count == 0) return null;

            DataRow scoreRow = scoreDt.Rows[0];
            int total = Convert.ToInt32(scoreRow["TOTAL_SCORE"]);

            var viewModel = new ScoreDetailViewModel
            {
                ScoreId = scoreId,
                StudentId = mand,
                StudentName = scoreRow["STUDENT_NAME"].ToString(),
                StudentCode = scoreRow["STUDENT_CODE"].ToString(),
                ClassName = scoreRow["CLASS_NAME"] != DBNull.Value ? scoreRow["CLASS_NAME"].ToString() : "",
                TermId = scoreRow["TERM_ID"].ToString(),
                TermName = scoreRow["TERM_NAME"].ToString(),
                TermYear = Convert.ToInt32(scoreRow["TERM_YEAR"]),
                TermNumber = Convert.ToInt32(scoreRow["TERM_NUMBER"]),
                Total = total,
                Status = scoreRow["STATUS"].ToString(),
                Classification = GetClassification(total),
                ApprovedBy = scoreRow["APPROVED_BY"] != DBNull.Value ? scoreRow["APPROVED_BY"].ToString() : null,
                ApprovedByName = scoreRow["APPROVED_BY_NAME"] != DBNull.Value ? scoreRow["APPROVED_BY_NAME"].ToString() : null,
                ApprovedAt = scoreRow["APPROVED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(scoreRow["APPROVED_AT"]) : null,
                CreatedAt = Convert.ToDateTime(scoreRow["CREATED_AT"])
            };

            // Lấy lịch sử thay đổi điểm
            string historyQuery = @"SELECT ACTION, OLD_VALUE, NEW_VALUE, CHANGED_BY,
                                          u.FULL_NAME as CHANGED_BY_NAME, REASON, CHANGED_AT
                                   FROM SCORE_HISTORY sh
                                   LEFT JOIN USERS u ON sh.CHANGED_BY = u.MAND
                                   WHERE sh.SCORE_ID = :ScoreId
                                   ORDER BY sh.CHANGED_AT DESC";

            var historyParams = new[] { OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId) };
            DataTable historyDt = OracleDbHelper.ExecuteQuery(historyQuery, historyParams);

            viewModel.History = new List<ScoreHistoryItem>();
            foreach (DataRow row in historyDt.Rows)
            {
                viewModel.History.Add(new ScoreHistoryItem
                {
                    Action = row["ACTION"].ToString(),
                    OldValue = row["OLD_VALUE"] != DBNull.Value ? row["OLD_VALUE"].ToString() : null,
                    NewValue = row["NEW_VALUE"] != DBNull.Value ? row["NEW_VALUE"].ToString() : null,
                    ChangedBy = row["CHANGED_BY"] != DBNull.Value ? row["CHANGED_BY"].ToString() : null,
                    ChangedByName = row["CHANGED_BY_NAME"] != DBNull.Value ? row["CHANGED_BY_NAME"].ToString() : "Hệ thống",
                    Reason = row["REASON"] != DBNull.Value ? row["REASON"].ToString() : null,
                    ChangedAt = Convert.ToDateTime(row["CHANGED_AT"])
                });
            }

            // Lấy thông tin đơn phúc khảo (nếu có)
            string feedbackQuery = @"SELECT f.ID, f.TITLE, f.CONTENT, f.REQUESTED_SCORE, f.STATUS,
                                           f.RESPONSE, f.RESPONDED_BY, u.FULL_NAME as RESPONDED_BY_NAME,
                                           f.RESPONDED_AT, f.CREATED_AT
                                    FROM FEEDBACKS f
                                    LEFT JOIN USERS u ON f.RESPONDED_BY = u.MAND
                                    WHERE f.STUDENT_ID = :StudentId AND f.TERM_ID = :TermId
                                    ORDER BY f.CREATED_AT DESC
                                    FETCH FIRST 1 ROWS ONLY";

            var feedbackParams = new[]
            {
                OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand),
                OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, viewModel.TermId)
            };

            DataTable feedbackDt = OracleDbHelper.ExecuteQuery(feedbackQuery, feedbackParams);
            if (feedbackDt.Rows.Count > 0)
            {
                DataRow fbRow = feedbackDt.Rows[0];
                viewModel.ReviewRequest = new ReviewRequestInfo
                {
                    Id = fbRow["ID"].ToString(),
                    Title = fbRow["TITLE"].ToString(),
                    Content = fbRow["CONTENT"].ToString(),
                    RequestedScore = Convert.ToInt32(fbRow["REQUESTED_SCORE"]),
                    Status = fbRow["STATUS"].ToString(),
                    Response = fbRow["RESPONSE"] != DBNull.Value ? fbRow["RESPONSE"].ToString() : null,
                    RespondedBy = fbRow["RESPONDED_BY"] != DBNull.Value ? fbRow["RESPONDED_BY"].ToString() : null,
                    RespondedByName = fbRow["RESPONDED_BY_NAME"] != DBNull.Value ? fbRow["RESPONDED_BY_NAME"].ToString() : null,
                    RespondedAt = fbRow["RESPONDED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(fbRow["RESPONDED_AT"]) : null,
                    CreatedAt = Convert.ToDateTime(fbRow["CREATED_AT"])
                };
            }

            return viewModel;
        }

        private string GetClassification(decimal score)
        {
            if (score >= 90) return "Xuất sắc";
            if (score >= 80) return "Tốt";
            if (score >= 65) return "Khá";
            if (score >= 50) return "Trung bình";
            if (score >= 35) return "Yếu";
            return "Kém";
        }

        #endregion
    }
}
