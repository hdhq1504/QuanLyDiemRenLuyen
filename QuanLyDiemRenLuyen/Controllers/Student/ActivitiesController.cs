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

namespace QuanLyDiemRenLuyen.Controllers.Student
{
    /// <summary>
    /// Controller xử lý Activities của sinh viên
    /// </summary>
    public class ActivitiesController : StudentBaseController
    {
        // GET: Student/Activities
        public ActionResult Index(string search, string status, int page = 1)
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                string mand = GetCurrentStudentId();

                var viewModel = new ActivityListViewModel
                {
                    SearchKeyword = search,
                    FilterStatus = status ?? "ALL",
                    CurrentPage = page,
                    PageSize = 10
                };

                viewModel.Activities = GetActivitiesList(mand, search, status, page, viewModel.PageSize, out int totalCount);
                viewModel.TotalPages = (int)Math.Ceiling((double)totalCount / viewModel.PageSize);

                return View("~/Views/Student/Activities.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Student/Activities.cshtml", new ActivityListViewModel());
            }
        }

        // GET: Student/Activities/Detail/id
        public ActionResult Detail(string id)
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                string mand = GetCurrentStudentId();
                var viewModel = GetActivityDetail(id, mand);
                
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hoạt động";
                    return RedirectToAction("Index");
                }

                return View("~/Views/Student/ActivityDetail.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Student/Activities/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string activityId)
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null)
                {
                    TempData["ErrorMessage"] = "Vui lòng đăng nhập";
                    return RedirectToAction("Login", "Account");
                }

                string mand = GetCurrentStudentId();

                // Kiểm tra hoạt động có tồn tại không
                string checkQuery = @"SELECT COUNT(*) FROM ACTIVITIES
                                     WHERE ID = :ActivityId AND STATUS = 'ACTIVE'";
                var checkParams = new[] { OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId) };
                int activityExists = Convert.ToInt32(OracleDbHelper.ExecuteScalar(checkQuery, checkParams));

                if (activityExists == 0)
                {
                    TempData["ErrorMessage"] = "Hoạt động không tồn tại hoặc đã bị hủy";
                    return RedirectToAction("Detail", new { id = activityId });
                }

                // Kiểm tra đã đăng ký chưa
                string checkRegQuery = @"SELECT COUNT(*) FROM REGISTRATIONS
                                        WHERE ACTIVITY_ID = :ActivityId AND STUDENT_ID = :StudentId";
                var checkRegParams = new[]
                {
                    OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
                };
                int alreadyRegistered = Convert.ToInt32(OracleDbHelper.ExecuteScalar(checkRegQuery, checkRegParams));

                if (alreadyRegistered > 0)
                {
                    TempData["ErrorMessage"] = "Bạn đã đăng ký hoạt động này rồi";
                    return RedirectToAction("Detail", new { id = activityId });
                }

                // Đăng ký hoạt động
                string insertQuery = @"INSERT INTO REGISTRATIONS (ID, ACTIVITY_ID, STUDENT_ID, STATUS, REGISTERED_AT)
                                      VALUES (RAWTOHEX(SYS_GUID()), :ActivityId, :StudentId, 'REGISTERED', SYSTIMESTAMP)";
                var insertParams = new[]
                {
                    OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
                };

                int result = OracleDbHelper.ExecuteNonQuery(insertQuery, insertParams);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Đăng ký hoạt động thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Đăng ký thất bại, vui lòng thử lại";
                }

                return RedirectToAction("Detail", new { id = activityId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Detail", new { id = activityId });
            }
        }

        // POST: Student/Activities/CancelRegistration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelRegistration(string activityId)
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null)
                {
                    TempData["ErrorMessage"] = "Vui lòng đăng nhập";
                    return RedirectToAction("Login", "Account");
                }

                string mand = GetCurrentStudentId();

                // Hủy đăng ký (cập nhật status thành CANCELLED thay vì xóa)
                string deleteQuery = @"UPDATE REGISTRATIONS
                                      SET STATUS = 'CANCELLED'
                                      WHERE ACTIVITY_ID = :ActivityId
                                      AND STUDENT_ID = :StudentId
                                      AND STATUS = 'REGISTERED'";
                var deleteParams = new[]
                {
                    OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
                };

                int result = OracleDbHelper.ExecuteNonQuery(deleteQuery, deleteParams);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Hủy đăng ký thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể hủy đăng ký. Có thể hoạt động đã được duyệt.";
                }

                return RedirectToAction("Detail", new { id = activityId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Detail", new { id = activityId });
            }
        }

        // GET: Student/Activities/UploadProof/id
        public ActionResult UploadProof(string id)
        {
            try
            {
                var authCheck = CheckAuth();
                if (authCheck != null) return authCheck;

                string mand = GetCurrentStudentId();
                var viewModel = GetUploadProofViewModel(id, mand);
                
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hoạt động hoặc bạn chưa đăng ký hoạt động này";
                    return RedirectToAction("Index");
                }

                return View("~/Views/Student/UploadProof.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Student/Activities/UploadProof
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadProof(string activityId, HttpPostedFileBase proofFile, string note)
        {
            var authCheck = CheckAuth();
            if (authCheck != null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập";
                return RedirectToAction("Login", "Account");
            }

            string mand = GetCurrentStudentId();

            try
            {
                if (proofFile == null || proofFile.ContentLength == 0)
                {
                    ViewBag.ErrorMessage = "Vui lòng chọn file minh chứng";
                    return View(GetUploadProofViewModel(activityId, mand));
                }

                // Kiểm tra định dạng file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                var fileExtension = Path.GetExtension(proofFile.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ViewBag.ErrorMessage = "Chỉ chấp nhận file ảnh (jpg, png) hoặc PDF";
                    return View(GetUploadProofViewModel(activityId, mand));
                }

                // Kiểm tra kích thước file (max 5MB)
                if (proofFile.ContentLength > 5 * 1024 * 1024)
                {
                    ViewBag.ErrorMessage = "File không được vượt quá 5MB";
                    return View(GetUploadProofViewModel(activityId, mand));
                }

                // Tạo thư mục lưu file
                string uploadFolder = Server.MapPath("~/Uploads/Proofs");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                // Tạo tên file unique
                string fileName = $"{mand}_{activityId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                string filePath = Path.Combine(uploadFolder, fileName);
                string relativePath = $"/Uploads/Proofs/{fileName}";

                // Lưu file
                proofFile.SaveAs(filePath);

                // Lấy REGISTRATION_ID
                string getRegIdQuery = @"SELECT ID FROM REGISTRATIONS
                                        WHERE ACTIVITY_ID = :ActivityId AND STUDENT_ID = :StudentId";
                var getRegIdParams = new[]
                {
                    OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
                };
                string registrationId = OracleDbHelper.ExecuteScalar(getRegIdQuery, getRegIdParams)?.ToString();

                if (string.IsNullOrEmpty(registrationId))
                {
                    ViewBag.ErrorMessage = "Không tìm thấy thông tin đăng ký";
                    return View(GetUploadProofViewModel(activityId, mand));
                }

                // Insert vào bảng PROOFS
                string insertQuery = @"INSERT INTO PROOFS
                                      (ID, REGISTRATION_ID, STUDENT_ID, ACTIVITY_ID, FILE_NAME, STORED_PATH,
                                       CONTENT_TYPE, FILE_SIZE, NOTE, STATUS, CREATED_AT_UTC)
                                      VALUES
                                      (RAWTOHEX(SYS_GUID()), :RegistrationId, :StudentId, :ActivityId, :FileName, :StoredPath,
                                       :ContentType, :FileSize, :Note, 'SUBMITTED', SYS_EXTRACT_UTC(SYSTIMESTAMP))";
                var insertParams = new[]
                {
                    OracleDbHelper.CreateParameter("RegistrationId", OracleDbType.Varchar2, registrationId),
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId),
                    OracleDbHelper.CreateParameter("FileName", OracleDbType.Varchar2, proofFile.FileName),
                    OracleDbHelper.CreateParameter("StoredPath", OracleDbType.Varchar2, relativePath),
                    OracleDbHelper.CreateParameter("ContentType", OracleDbType.Varchar2, proofFile.ContentType),
                    OracleDbHelper.CreateParameter("FileSize", OracleDbType.Int32, proofFile.ContentLength),
                    OracleDbHelper.CreateParameter("Note", OracleDbType.Varchar2, note ?? "")
                };

                int result = OracleDbHelper.ExecuteNonQuery(insertQuery, insertParams);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Upload minh chứng thành công!";
                    return RedirectToAction("Detail", new { id = activityId });
                }
                else
                {
                    ViewBag.ErrorMessage = "Upload thất bại, vui lòng thử lại";
                    return View(GetUploadProofViewModel(activityId, mand));
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi: " + ex.Message;
                return View(GetUploadProofViewModel(activityId, mand));
            }
        }

        #region Private Helper Methods

        private List<ActivityItem> GetActivitiesList(string mand, string search, string status, int page, int pageSize, out int totalCount)
        {
            var activities = new List<ActivityItem>();
            totalCount = 0;

            try
            {
                // Build query với điều kiện tìm kiếm và lọc
                string countQuery = @"SELECT COUNT(*) FROM ACTIVITIES a WHERE a.APPROVAL_STATUS = 'APPROVED' AND a.STATUS = 'OPEN'";
                string dataQuery = @"SELECT a.ID, a.TITLE, a.DESCRIPTION, a.START_AT, a.END_AT, a.LOCATION,
                                       a.POINTS, a.MAX_SEATS, a.STATUS,
                                       cr.NAME as CRITERION_NAME, u.FULL_NAME as ORGANIZER_NAME,
                                       (SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STATUS != 'CANCELLED') as CURRENT_PARTICIPANTS,
                                       (SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STUDENT_ID = :StudentId AND STATUS != 'CANCELLED') as IS_REGISTERED,
                                       (SELECT STATUS FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STUDENT_ID = :StudentId AND ROWNUM = 1) as REG_STATUS
                                FROM ACTIVITIES a
                                LEFT JOIN CRITERIA cr ON a.CRITERION_ID = cr.ID
                                LEFT JOIN USERS u ON a.ORGANIZER_ID = u.MAND
                                WHERE a.APPROVAL_STATUS = 'APPROVED' AND a.STATUS = 'OPEN'";

                var parameters = new List<OracleParameter>
                {
                    OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
                };

                if (!string.IsNullOrEmpty(search))
                {
                    countQuery += " AND (UPPER(a.TITLE) LIKE :Search OR UPPER(a.DESCRIPTION) LIKE :Search)";
                    dataQuery += " AND (UPPER(a.TITLE) LIKE :Search OR UPPER(a.DESCRIPTION) LIKE :Search)";
                    parameters.Add(OracleDbHelper.CreateParameter("Search", OracleDbType.Varchar2, "%" + search.ToUpper() + "%"));
                }

                if (!string.IsNullOrEmpty(status) && status != "ALL")
                {
                    if (status == "UPCOMING")
                    {
                        dataQuery += " AND a.START_AT > SYSDATE";
                        countQuery += " AND a.START_AT > SYSDATE";
                    }
                    else if (status == "ONGOING")
                    {
                        dataQuery += " AND SYSDATE BETWEEN a.START_AT AND a.END_AT";
                        countQuery += " AND SYSDATE BETWEEN a.START_AT AND a.END_AT";
                    }
                    else if (status == "COMPLETED")
                    {
                        dataQuery += " AND a.END_AT < SYSDATE";
                        countQuery += " AND a.END_AT < SYSDATE";
                    }
                }

                // Đếm tổng số
                totalCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(countQuery, parameters.ToArray()));

                // Thêm phân trang
                dataQuery += " ORDER BY a.START_AT DESC";
                int offset = (page - 1) * pageSize;
                dataQuery = $@"SELECT * FROM (
                            SELECT a.*, ROWNUM rnum FROM ({dataQuery}) a
                            WHERE ROWNUM <= {offset + pageSize}
                          ) WHERE rnum > {offset}";

                DataTable dt = OracleDbHelper.ExecuteQuery(dataQuery, parameters.ToArray());

                foreach (DataRow row in dt.Rows)
                {
                    activities.Add(new ActivityItem
                    {
                        Id = row["ID"].ToString(),
                        Title = row["TITLE"].ToString(),
                        Description = row["DESCRIPTION"] != DBNull.Value ? row["DESCRIPTION"].ToString() : "",
                        StartAt = Convert.ToDateTime(row["START_AT"]),
                        EndAt = Convert.ToDateTime(row["END_AT"]),
                        Location = row["LOCATION"] != DBNull.Value ? row["LOCATION"].ToString() : "",
                        Points = row["POINTS"] != DBNull.Value ? (decimal?)Convert.ToDecimal(row["POINTS"]) : null,
                        MaxParticipants = row["MAX_SEATS"] != DBNull.Value ? Convert.ToInt32(row["MAX_SEATS"]) : 0,
                        CurrentParticipants = row["CURRENT_PARTICIPANTS"] != DBNull.Value ? Convert.ToInt32(row["CURRENT_PARTICIPANTS"]) : 0,
                        Status = row["STATUS"].ToString(),
                        RegistrationDeadline = null,
                        CategoryName = row["CRITERION_NAME"] != DBNull.Value ? row["CRITERION_NAME"].ToString() : "",
                        OrganizerName = row["ORGANIZER_NAME"] != DBNull.Value ? row["ORGANIZER_NAME"].ToString() : "",
                        IsRegistered = row["IS_REGISTERED"] != DBNull.Value && Convert.ToInt32(row["IS_REGISTERED"]) > 0,
                        RegistrationStatus = row["REG_STATUS"] != DBNull.Value ? row["REG_STATUS"].ToString() : ""
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

        private ActivityDetailViewModel GetActivityDetail(string activityId, string mand)
        {
            string query = @"SELECT a.ID, a.TITLE, a.DESCRIPTION, a.START_AT, a.END_AT, a.LOCATION,
                                   a.POINTS, a.MAX_SEATS, a.STATUS, a.CREATED_AT,
                                   cr.NAME as CRITERION_NAME, u.FULL_NAME as ORGANIZER_NAME,
                                   (SELECT COUNT(*) FROM REGISTRATIONS WHERE ACTIVITY_ID = a.ID AND STATUS != 'CANCELLED') as CURRENT_PARTICIPANTS,
                                   r.STATUS as REG_STATUS, r.REGISTERED_AT,
                                   p.STATUS as PROOF_STATUS, p.STORED_PATH as PROOF_FILE_PATH,
                                   p.NOTE as PROOF_NOTE, p.CREATED_AT_UTC as PROOF_UPLOADED_AT
                            FROM ACTIVITIES a
                            LEFT JOIN CRITERIA cr ON a.CRITERION_ID = cr.ID
                            LEFT JOIN USERS u ON a.ORGANIZER_ID = u.MAND
                            LEFT JOIN REGISTRATIONS r ON a.ID = r.ACTIVITY_ID AND r.STUDENT_ID = :StudentId
                            LEFT JOIN (
                                SELECT * FROM (
                                    SELECT REGISTRATION_ID, STATUS, STORED_PATH, NOTE, CREATED_AT_UTC,
                                           ROW_NUMBER() OVER (PARTITION BY REGISTRATION_ID ORDER BY CREATED_AT_UTC DESC) as rn
                                    FROM PROOFS
                                ) WHERE rn = 1
                            ) p ON r.ID = p.REGISTRATION_ID
                            WHERE a.ID = :ActivityId";

            var parameters = new[]
            {
                OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId),
                OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
            };

            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            var viewModel = new ActivityDetailViewModel
            {
                Id = row["ID"].ToString(),
                Title = row["TITLE"].ToString(),
                Description = row["DESCRIPTION"] != DBNull.Value ? row["DESCRIPTION"].ToString() : "",
                StartAt = Convert.ToDateTime(row["START_AT"]),
                EndAt = Convert.ToDateTime(row["END_AT"]),
                Location = row["LOCATION"] != DBNull.Value ? row["LOCATION"].ToString() : "",
                Points = row["POINTS"] != DBNull.Value ? (decimal?)Convert.ToDecimal(row["POINTS"]) : null,
                MaxParticipants = row["MAX_SEATS"] != DBNull.Value ? Convert.ToInt32(row["MAX_SEATS"]) : 0,
                CurrentParticipants = row["CURRENT_PARTICIPANTS"] != DBNull.Value ? Convert.ToInt32(row["CURRENT_PARTICIPANTS"]) : 0,
                Status = row["STATUS"].ToString(),
                RegistrationDeadline = null,
                CategoryName = row["CRITERION_NAME"] != DBNull.Value ? row["CRITERION_NAME"].ToString() : "",
                OrganizerName = row["ORGANIZER_NAME"] != DBNull.Value ? row["ORGANIZER_NAME"].ToString() : "",
                Requirements = "",
                Benefits = "",
                CreatedAt = Convert.ToDateTime(row["CREATED_AT"]),
                IsRegistered = row["REG_STATUS"] != DBNull.Value && row["REG_STATUS"].ToString() != "CANCELLED",
                RegistrationStatus = row["REG_STATUS"] != DBNull.Value ? row["REG_STATUS"].ToString() : "",
                RegisteredAt = row["REGISTERED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["REGISTERED_AT"]) : null,
                ProofStatus = row["PROOF_STATUS"] != DBNull.Value ? row["PROOF_STATUS"].ToString() : "NOT_UPLOADED",
                ProofFilePath = row["PROOF_FILE_PATH"] != DBNull.Value ? row["PROOF_FILE_PATH"].ToString() : "",
                ProofNote = row["PROOF_NOTE"] != DBNull.Value ? row["PROOF_NOTE"].ToString() : "",
                ProofUploadedAt = row["PROOF_UPLOADED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["PROOF_UPLOADED_AT"]) : null,
                ApprovedPoints = null
            };

            // Xác định các quyền
            DateTime now = DateTime.Now;
            viewModel.CanRegister = !viewModel.IsRegistered &&
                                   viewModel.Status == "ACTIVE" &&
                                   (viewModel.RegistrationDeadline == null || viewModel.RegistrationDeadline > now) &&
                                   viewModel.CurrentParticipants < viewModel.MaxParticipants;

            viewModel.CanCancelRegistration = viewModel.IsRegistered &&
                                             viewModel.RegistrationStatus == "REGISTERED" &&
                                             viewModel.StartAt > now;

            viewModel.CanUploadProof = viewModel.IsRegistered &&
                                      viewModel.RegistrationStatus == "CHECKED_IN" &&
                                      viewModel.ProofStatus != "APPROVED";

            return viewModel;
        }

        private UploadProofViewModel GetUploadProofViewModel(string activityId, string mand)
        {
            string query = @"SELECT a.TITLE, p.STORED_PATH, p.STATUS, p.CREATED_AT_UTC
                            FROM ACTIVITIES a
                            INNER JOIN REGISTRATIONS r ON a.ID = r.ACTIVITY_ID
                            LEFT JOIN PROOFS p ON r.ID = p.REGISTRATION_ID
                            WHERE a.ID = :ActivityId AND r.STUDENT_ID = :StudentId
                            ORDER BY p.CREATED_AT_UTC DESC
                            FETCH FIRST 1 ROWS ONLY";

            var parameters = new[]
            {
                OracleDbHelper.CreateParameter("ActivityId", OracleDbType.Varchar2, activityId),
                OracleDbHelper.CreateParameter("StudentId", OracleDbType.Varchar2, mand)
            };

            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new UploadProofViewModel
            {
                ActivityId = activityId,
                ActivityTitle = row["TITLE"].ToString(),
                CurrentProofFilePath = row["STORED_PATH"] != DBNull.Value ? row["STORED_PATH"].ToString() : "",
                CurrentProofStatus = row["STATUS"] != DBNull.Value ? row["STATUS"].ToString() : "NOT_UPLOADED",
                CurrentProofUploadedAt = row["CREATED_AT_UTC"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["CREATED_AT_UTC"]) : null
            };
        }

        #endregion
    }
}
