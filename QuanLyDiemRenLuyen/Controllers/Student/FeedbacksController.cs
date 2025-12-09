using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;
using QuanLyDiemRenLuyen.Services;

namespace QuanLyDiemRenLuyen.Controllers.Student
{
    /// <summary>
    /// Controller xử lý Feedbacks của sinh viên
    /// </summary>
    public class FeedbacksController : StudentBaseController
    {
        // GET: Student/Feedbacks
        public ActionResult Index(string status, string termId, int page = 1)
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                string mand = GetCurrentStudentId();

                var viewModel = new StudentFeedbackListViewModel
                {
                    FilterStatus = status ?? "ALL",
                    FilterTermId = termId,
                    CurrentPage = page,
                    PageSize = 10
                };

                // Get available terms
                viewModel.AvailableTerms = GetAvailableTerms();

                // Get feedbacks
                viewModel.Feedbacks = GetFeedbacksList(mand, status, termId, page, viewModel.PageSize, out int totalCount);
                viewModel.TotalPages = (int)Math.Ceiling((double)totalCount / viewModel.PageSize);

                return View("~/Views/Student/Feedbacks.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Student/Feedbacks.cshtml", new StudentFeedbackListViewModel());
            }
        }

        // GET: Student/Feedbacks/Detail
        public ActionResult Detail(string id)
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                string mand = GetCurrentStudentId();
                var viewModel = GetFeedbackDetail(id, mand);

                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy phản hồi";
                    return RedirectToAction("Index");
                }

                return View("~/Views/Student/FeedbackDetail.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: Student/Feedbacks/Create
        public ActionResult Create()
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                string mand = GetCurrentStudentId();

                var model = new CreateFeedbackViewModel
                {
                    AvailableTerms = GetAvailableTerms(),
                    AvailableActivities = GetParticipatedActivities(mand)
                };

                return View("~/Views/Student/CreateFeedback.cshtml", model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Student/Feedbacks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateFeedbackViewModel model, HttpPostedFileBase[] attachments)
        {
            string mand = null;
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                mand = GetCurrentStudentId();

                if (!ModelState.IsValid)
                {
                    model.AvailableTerms = GetAvailableTerms();
                    model.AvailableActivities = GetParticipatedActivities(mand);
                    return View("~/Views/Student/CreateFeedback.cshtml", model);
                }

                // Tạo ID mới
                string newId = "FB" + DateTime.Now.ToString("yyyyMMddHHmmss");

                // Insert feedback
                string insertQuery = @"INSERT INTO FEEDBACKS
                                      (ID, STUDENT_ID, TERM_ID, ACTIVITY_ID, TITLE, CONTENT, STATUS, CREATED_AT)
                                      VALUES
                                      (:Id, :StudentId, :TermId, :ActivityId, :Title, :Content, 'SUBMITTED', SYSDATE)";

                var insertParams = new[]
                {
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, newId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, model.TermId),
                    OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, string.IsNullOrEmpty(model.ActivityId) ? (object)DBNull.Value : model.ActivityId),
                    OracleDbHelper.CreateParameter("Title", OracleDbType.Varchar2, model.Title),
                    OracleDbHelper.CreateParameter("Content", OracleDbType.Clob, EncryptionHelper.Encrypt(model.Content)) // Encrypt Content
                };

                OracleDbHelper.ExecuteNonQuery(insertQuery, insertParams);

                // Handle file attachments
                if (attachments != null && attachments.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
                    string uploadFolder = Server.MapPath("~/Uploads/Feedbacks");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    foreach (var file in attachments)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            var fileExtension = Path.GetExtension(file.FileName).ToLower();
                            if (!allowedExtensions.Contains(fileExtension))
                            {
                                continue; // Skip invalid file types
                            }

                            if (file.ContentLength > 10 * 1024 * 1024) // 10MB limit
                            {
                                continue; // Skip files too large
                            }

                            // Generate unique filename
                            string fileName = $"{mand}_{newId}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}{fileExtension}";
                            string filePath = Path.Combine(uploadFolder, fileName);
                            string relativePath = $"/Uploads/Feedbacks/{fileName}";

                            // Save file
                            file.SaveAs(filePath);

                            // Insert into database
                            string attachInsertQuery = @"INSERT INTO FEEDBACK_ATTACHMENTS
                                                        (ID, FEEDBACK_ID, FILE_NAME, STORED_PATH, CONTENT_TYPE, FILE_SIZE, UPLOADED_AT)
                                                        VALUES
                                                        (RAWTOHEX(SYS_GUID()), :FeedbackId, :FileName, :StoredPath, :ContentType, :FileSize, SYSDATE)";

                            var attachParams = new[]
                            {
                                OracleDbHelper.CreateParameter("FeedbackId", OracleDbType.Varchar2, newId),
                                OracleDbHelper.CreateParameter("FileName", OracleDbType.Varchar2, EncryptionHelper.Encrypt(file.FileName)), // Encrypt FileName
                                OracleDbHelper.CreateParameter("StoredPath", OracleDbType.Varchar2, relativePath),
                                OracleDbHelper.CreateParameter("ContentType", OracleDbType.Varchar2, file.ContentType),
                                OracleDbHelper.CreateParameter("FileSize", OracleDbType.Int32, file.ContentLength)
                            };

                            OracleDbHelper.ExecuteNonQuery(attachInsertQuery, attachParams);
                        }
                    }
                }

                TempData["SuccessMessage"] = "Gửi phản hồi thành công!";
                return RedirectToAction("Detail", new { id = newId });
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                model.AvailableTerms = GetAvailableTerms();
                model.AvailableActivities = GetParticipatedActivities(mand);
                return View("~/Views/Student/CreateFeedback.cshtml", model);
            }
        }

        // POST: Student/Feedbacks/UploadAttachment
        [HttpPost]
        public ActionResult UploadAttachment(string feedbackId, HttpPostedFileBase file)
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null)
                {
                    if (Request.IsAjaxRequest()) return Json(new { success = false, message = "Chưa đăng nhập" });
                    return authCheck;
                }

                string mand = GetCurrentStudentId();

                if (file == null || file.ContentLength == 0)
                {
                    if (Request.IsAjaxRequest()) return Json(new { success = false, message = "Vui lòng chọn file" });
                    TempData["ErrorMessage"] = "Vui lòng chọn file";
                    return RedirectToAction("Detail", new { id = feedbackId });
                }

                // Validate file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    if (Request.IsAjaxRequest()) return Json(new { success = false, message = "Chỉ chấp nhận file ảnh, PDF hoặc Word" });
                    TempData["ErrorMessage"] = "Chỉ chấp nhận file ảnh, PDF hoặc Word";
                    return RedirectToAction("Detail", new { id = feedbackId });
                }

                if (file.ContentLength > 10 * 1024 * 1024) // 10MB
                {
                    if (Request.IsAjaxRequest()) return Json(new { success = false, message = "File không được vượt quá 10MB" });
                    TempData["ErrorMessage"] = "File không được vượt quá 10MB";
                    return RedirectToAction("Detail", new { id = feedbackId });
                }

                // Create upload folder
                string uploadFolder = Server.MapPath("~/Uploads/Feedbacks");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                // Generate unique filename
                string fileName = $"{mand}_{feedbackId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                string filePath = Path.Combine(uploadFolder, fileName);
                string relativePath = $"/Uploads/Feedbacks/{fileName}";

                // Save file
                file.SaveAs(filePath);

                // Insert into database
                string insertQuery = @"INSERT INTO FEEDBACK_ATTACHMENTS
                                      (ID, FEEDBACK_ID, FILE_NAME, STORED_PATH, CONTENT_TYPE, FILE_SIZE, UPLOADED_AT)
                                      VALUES
                                      (RAWTOHEX(SYS_GUID()), :FeedbackId, :FileName, :StoredPath, :ContentType, :FileSize, SYSDATE)";

                var insertParams = new[]
                {
                    OracleDbHelper.CreateParameter("FeedbackId", OracleDbType.Varchar2, feedbackId),
                    OracleDbHelper.CreateParameter("FileName", OracleDbType.Varchar2, EncryptionHelper.Encrypt(file.FileName)), // Encrypt FileName
                    OracleDbHelper.CreateParameter("StoredPath", OracleDbType.Varchar2, relativePath),
                    OracleDbHelper.CreateParameter("ContentType", OracleDbType.Varchar2, file.ContentType),
                    OracleDbHelper.CreateParameter("FileSize", OracleDbType.Int32, file.ContentLength)
                };

                OracleDbHelper.ExecuteNonQuery(insertQuery, insertParams);

                // Encrypt file path using FilePathCryptoService
                // Get the new attachment ID để encrypt path
                try
                {
                    string getIdQuery = @"SELECT ID FROM FEEDBACK_ATTACHMENTS 
                                         WHERE FEEDBACK_ID = :FeedbackId 
                                         ORDER BY UPLOADED_AT DESC FETCH FIRST 1 ROW ONLY";
                    var idResult = OracleDbHelper.ExecuteScalar(getIdQuery, new[] {
                        OracleDbHelper.CreateParameter("FeedbackId", OracleDbType.Varchar2, feedbackId)
                    });
                    if (idResult != null)
                    {
                        var pathService = new FilePathCryptoService();
                        pathService.EncryptAttachmentPath(idResult.ToString(), relativePath);
                    }
                }
                catch { /* Continue nếu encryption lỗi - path vẫn lưu plain */ }

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, fileName = file.FileName, filePath = relativePath });
                }
                
                TempData["SuccessMessage"] = "Upload file thành công";
                return RedirectToAction("Detail", new { id = feedbackId });
            }
            catch (Exception ex)
            {
                if (Request.IsAjaxRequest()) return Json(new { success = false, message = ex.Message });
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Detail", new { id = feedbackId });
            }
        }

        #region Private Helper Methods

        private List<TermOption> GetAvailableTerms()
        {
            var terms = new List<TermOption>();
            string query = @"SELECT ID, NAME, YEAR
                            FROM TERMS
                            ORDER BY YEAR DESC";

            DataTable dt = OracleDbHelper.ExecuteQuery(query, null);
            foreach (DataRow row in dt.Rows)
            {
                terms.Add(new TermOption
                {
                    Id = row["ID"].ToString(),
                    Name = row["NAME"].ToString(),
                    Year = Convert.ToInt32(row["YEAR"])
                });
            }

            return terms;
        }

        private List<ActivityOption> GetParticipatedActivities(string studentId)
        {
            var activities = new List<ActivityOption>();
            
            // Get activities where student has CHECKED_IN (participated)
            string query = @"SELECT a.ID, a.TITLE, a.START_AT, a.TERM_ID, a.POINTS
                            FROM ACTIVITIES a
                            INNER JOIN REGISTRATIONS r ON a.ID = r.ACTIVITY_ID
                            WHERE r.STUDENT_ID = :StudentId
                              AND r.STATUS = 'CHECKED_IN'
                            ORDER BY a.START_AT DESC";

            var parameters = new[] { OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, studentId) };
            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

            foreach (DataRow row in dt.Rows)
            {
                activities.Add(new ActivityOption
                {
                    Id = row["ID"].ToString(),
                    Title = row["TITLE"].ToString(),
                    StartAt = Convert.ToDateTime(row["START_AT"]),
                    TermId = row["TERM_ID"].ToString(),
                    Points = row["POINTS"] != DBNull.Value ? Convert.ToDecimal(row["POINTS"]) : (decimal?)null
                });
            }

            return activities;
        }


        private List<StudentFeedbackItem> GetFeedbacksList(string mand, string status, string termId, int page, int pageSize, out int totalCount)
        {
            var feedbacks = new List<StudentFeedbackItem>();

            // Build WHERE clause
            string whereClause = "WHERE f.STUDENT_ID = :StudentId";
            var parameters = new List<OracleParameter>
            {
                OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
            };

            if (!string.IsNullOrEmpty(status) && status != "ALL")
            {
                whereClause += " AND f.STATUS = :Status";
                parameters.Add(OracleDbHelper.CreateParameter("Status", OracleDbType.Varchar2, status));
            }

            if (!string.IsNullOrEmpty(termId))
            {
                whereClause += " AND f.TERM_ID = :TermId";
                parameters.Add(OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId));
            }

            // Get total count
            string countQuery = $"SELECT COUNT(*) FROM FEEDBACKS f {whereClause}";
            totalCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(countQuery, parameters.ToArray()));

            // Get paginated data
            int offset = (page - 1) * pageSize;
            string query = $@"SELECT * FROM (
                                SELECT f.ID, f.TERM_ID, t.NAME as TERM_NAME,
                                       f.TITLE, f.STATUS, f.CREATED_AT, f.RESPONDED_AT,
                                       CASE WHEN f.RESPONSE IS NOT NULL THEN 1 ELSE 0 END as HAS_RESPONSE,
                                       ROW_NUMBER() OVER (ORDER BY f.CREATED_AT DESC) AS RN
                                FROM FEEDBACKS f
                                INNER JOIN TERMS t ON f.TERM_ID = t.ID
                                {whereClause}
                            )
                            WHERE RN > :Offset AND RN <= :EndRow";

            parameters.Add(OracleDbHelper.CreateParameter("Offset", OracleDbType.Int32, offset));
            parameters.Add(OracleDbHelper.CreateParameter("EndRow", OracleDbType.Int32, offset + pageSize));

            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters.ToArray());

            foreach (DataRow row in dt.Rows)
            {
                feedbacks.Add(new StudentFeedbackItem
                {
                    Id = row["ID"].ToString(),
                    TermId = row["TERM_ID"].ToString(),
                    TermName = row["TERM_NAME"].ToString(),
                    Title = row["TITLE"].ToString(),
                    Status = row["STATUS"].ToString(),
                    CreatedAt = Convert.ToDateTime(row["CREATED_AT"]),
                    RespondedAt = row["RESPONDED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["RESPONDED_AT"]) : null,
                    HasResponse = Convert.ToInt32(row["HAS_RESPONSE"]) == 1
                });
            }

            return feedbacks;
        }

        private StudentFeedbackDetailViewModel GetFeedbackDetail(string feedbackId, string mand)
        {
            string query = @"SELECT f.ID, f.STUDENT_ID, f.TERM_ID, t.NAME as TERM_NAME,
                                   f.TITLE, f.CONTENT, f.STATUS, f.RESPONSE,
                                   f.CREATED_AT, f.UPDATED_AT, f.RESPONDED_AT
                            FROM FEEDBACKS f
                            INNER JOIN TERMS t ON f.TERM_ID = t.ID
                            WHERE f.ID = :FeedbackId AND f.STUDENT_ID = :StudentId";

            var parameters = new[]
            {
                OracleDbHelper.CreateParameter("FeedbackId", OracleDbType.Varchar2, feedbackId),
                OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
            };

            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);
            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            var viewModel = new StudentFeedbackDetailViewModel
            {
                Id = row["ID"].ToString(),
                TermId = row["TERM_ID"].ToString(),
                TermName = row["TERM_NAME"].ToString(),
                Title = row["TITLE"].ToString(),
                Content = EncryptionHelper.Decrypt(row["CONTENT"].ToString()), // Decrypt Content
                Status = row["STATUS"].ToString(),
                Response = row["RESPONSE"] != DBNull.Value ? row["RESPONSE"].ToString() : null,
                CreatedAt = Convert.ToDateTime(row["CREATED_AT"]),
                UpdatedAt = row["UPDATED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["UPDATED_AT"]) : null,
                RespondedAt = row["RESPONDED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["RESPONDED_AT"]) : null,
                Attachments = new List<FeedbackAttachmentItem>()
            };

            // Get attachments
            string attachmentQuery = @"SELECT ID, FILE_NAME, STORED_PATH, CONTENT_TYPE, FILE_SIZE, UPLOADED_AT
                                      FROM FEEDBACK_ATTACHMENTS
                                      WHERE FEEDBACK_ID = :FeedbackId
                                      ORDER BY UPLOADED_AT DESC";

            var attachmentParams = new[] { OracleDbHelper.CreateParameter("FeedbackId", OracleDbType.Varchar2, feedbackId) };
            DataTable attachmentDt = OracleDbHelper.ExecuteQuery(attachmentQuery, attachmentParams);

            var pathService = new FilePathCryptoService();
            foreach (DataRow attRow in attachmentDt.Rows)
            {
                string attachmentId = attRow["ID"].ToString();
                // Try to get decrypted path, fallback to plain stored path
                string storedPath = pathService.GetAttachmentPath(attachmentId) 
                                    ?? attRow["STORED_PATH"].ToString();
                
                viewModel.Attachments.Add(new FeedbackAttachmentItem
                {
                    Id = attachmentId,
                    FileName = TryDecrypt(attRow["FILE_NAME"].ToString()),
                    StoredPath = storedPath,
                    ContentType = attRow["CONTENT_TYPE"].ToString(),
                    FileSize = Convert.ToInt32(attRow["FILE_SIZE"]),
                    UploadedAt = Convert.ToDateTime(attRow["UPLOADED_AT"])
                });
            }

            return viewModel;
        }

        private string TryDecrypt(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            try
            {
                return EncryptionHelper.Decrypt(value);
            }
            catch
            {
                // If decryption fails, return the original value (might be unencrypted)
                return value;
            }
        }

        #endregion
    }
}
