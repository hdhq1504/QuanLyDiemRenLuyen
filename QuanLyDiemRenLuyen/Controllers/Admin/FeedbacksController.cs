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

        // POST: Admin/Feedbacks/Respond
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Respond(string id, string response)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = Session["MAND"].ToString();

                string updateQuery = @"UPDATE FEEDBACKS
                                      SET STATUS = 'RESPONDED',
                                          RESPONSE = :Response,
                                          RESPONDED_BY = :RespondedBy,
                                          RESPONDED_AT = SYSDATE
                                      WHERE ID = :Id";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("Response", OracleDbType.Clob, response),
                    OracleDbHelper.CreateParameter("RespondedBy", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Đã gửi phản hồi thành công";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể cập nhật phản hồi";
                }

                return RedirectToAction("Detail", new { id = id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Detail", new { id = id });
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
                                   s.STUDENT_CODE, u.FULL_NAME as STUDENT_NAME,
                                   t.NAME as TERM_NAME,
                                   r.FULL_NAME as RESPONDER_NAME
                            FROM FEEDBACKS f
                            INNER JOIN STUDENTS s ON f.STUDENT_ID = s.USER_ID
                            INNER JOIN USERS u ON s.USER_ID = u.MAND
                            INNER JOIN TERMS t ON f.TERM_ID = t.ID
                            LEFT JOIN USERS r ON f.RESPONDED_BY = r.MAND
                            WHERE f.ID = :Id";

            var parameters = new[] { OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id) };
            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new AdminFeedbackItem
            {
                Id = row["ID"].ToString(),
                Title = row["TITLE"].ToString(),
                Content = EncryptionHelper.Decrypt(row["CONTENT"].ToString()), // Decrypt Content
                StudentId = row["STUDENT_CODE"].ToString(),
                StudentName = row["STUDENT_NAME"].ToString(),
                TermName = row["TERM_NAME"].ToString(),
                Status = row["STATUS"].ToString(),
                CreatedAt = Convert.ToDateTime(row["CREATED_AT"]),
                RespondedAt = row["RESPONDED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["RESPONDED_AT"]) : null,
                Response = row["RESPONSE"] != DBNull.Value ? row["RESPONSE"].ToString() : null,
                RespondedBy = row["RESPONDER_NAME"] != DBNull.Value ? row["RESPONDER_NAME"].ToString() : null
            };
        }

        #endregion
    }
}
