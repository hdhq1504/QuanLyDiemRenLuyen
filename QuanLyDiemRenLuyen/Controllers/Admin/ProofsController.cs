using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;
using QuanLyDiemRenLuyen.Services;

namespace QuanLyDiemRenLuyen.Controllers.Admin
{
    public class ProofsController : AdminBaseController
    {
        // GET: Admin/Proofs
        public ActionResult Index(string status, int page = 1)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                int pageSize = 10;
                var viewModel = new ProofListViewModel
                {
                    FilterStatus = status ?? "ALL",
                    CurrentPage = page,
                    Proofs = GetProofsList(status, page, pageSize, out int totalCount)
                };

                viewModel.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return View("~/Views/Admin/Proofs/Index.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/Proofs/Index.cshtml", new ProofListViewModel { Proofs = new List<ProofItem>() });
            }
        }

        // GET: Admin/Proofs/Detail/id
        public ActionResult Detail(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetProofDetail(id);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy minh chứng";
                    return RedirectToAction("Index");
                }

                return View("~/Views/Admin/Proofs/Detail.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Admin/Proofs/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(string id)
        {
            return ProcessProof(id, "APPROVED");
        }

        // POST: Admin/Proofs/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(string id)
        {
            return ProcessProof(id, "REJECTED");
        }

        private ActionResult ProcessProof(string id, string status)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return RedirectToAction("Login", "Account");

            try
            {
                string mand = Session["MAND"].ToString();

                // 1. Update Proof Status
                string updateProofQuery = @"UPDATE PROOFS 
                                           SET STATUS = :Status, 
                                               REVIEWED_AT_UTC = SYS_EXTRACT_UTC(SYSTIMESTAMP)
                                           WHERE ID = :Id";
                
                var proofParams = new[]
                {
                    OracleDbHelper.CreateParameter("Status", OracleDbType.Varchar2, status),
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateProofQuery, proofParams);

                if (result > 0)
                {
                    // 2. If Approved, update Registration to CHECKED_IN if not already
                    if (status == "APPROVED")
                    {
                        string updateRegQuery = @"UPDATE REGISTRATIONS r
                                                 SET STATUS = 'CHECKED_IN',
                                                     CHECKED_IN_AT = SYSTIMESTAMP
                                                 WHERE ID = (SELECT REGISTRATION_ID FROM PROOFS WHERE ID = :ProofId)
                                                 AND STATUS = 'REGISTERED'";
                        
                        var regParams = new[] { OracleDbHelper.CreateParameter("ProofId", OracleDbType.Varchar2, id) };
                        OracleDbHelper.ExecuteNonQuery(updateRegQuery, regParams);
                    }

                    TempData["SuccessMessage"] = status == "APPROVED" ? "Đã duyệt minh chứng" : "Đã từ chối minh chứng";
                }
                else
                {
                    TempData["ErrorMessage"] = "Cập nhật thất bại";
                }

                return RedirectToAction("Detail", new { id = id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Detail", new { id = id });
            }
        }

        #region Private Helper Methods

        private List<ProofItem> GetProofsList(string status, int page, int pageSize, out int totalCount)
        {
            var list = new List<ProofItem>();
            totalCount = 0;

            string baseQuery = @"FROM PROOFS p
                                JOIN USERS u ON p.STUDENT_ID = u.MAND
                                JOIN ACTIVITIES a ON p.ACTIVITY_ID = a.ID
                                WHERE 1=1";
            
            var parameters = new List<OracleParameter>();

            if (!string.IsNullOrEmpty(status) && status != "ALL")
            {
                baseQuery += " AND p.STATUS = :Status";
                parameters.Add(OracleDbHelper.CreateParameter("Status", OracleDbType.Varchar2, status));
            }

            // Count
            string countQuery = "SELECT COUNT(*) " + baseQuery;
            totalCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(countQuery, parameters.ToArray()));

            // Data
            string dataQuery = @"SELECT p.ID, p.STUDENT_ID, u.FULL_NAME, p.ACTIVITY_ID, a.TITLE, 
                                       p.FILE_NAME, p.STORED_PATH, p.STATUS, p.CREATED_AT_UTC " + baseQuery + " ORDER BY p.CREATED_AT_UTC DESC";

            int offset = (page - 1) * pageSize;
            string pagingQuery = $@"SELECT * FROM (
                                    SELECT x.*, ROWNUM rnum FROM ({dataQuery}) x
                                    WHERE ROWNUM <= {offset + pageSize}
                                  ) WHERE rnum > {offset}";

            DataTable dt = OracleDbHelper.ExecuteQuery(pagingQuery, parameters.ToArray());

            var pathService = new FilePathCryptoService();
            foreach (DataRow row in dt.Rows)
            {
                string proofId = row["ID"].ToString();
                // Try to get decrypted path, fallback to plain stored path
                string storedPath = pathService.GetProofPath(proofId) 
                                    ?? row["STORED_PATH"].ToString();
                
                list.Add(new ProofItem
                {
                    Id = proofId,
                    StudentId = row["STUDENT_ID"].ToString(),
                    StudentName = row["FULL_NAME"].ToString(),
                    ActivityId = row["ACTIVITY_ID"].ToString(),
                    ActivityTitle = row["TITLE"].ToString(),
                    FileName = EncryptionHelper.Decrypt(row["FILE_NAME"].ToString()), // Decrypt
                    StoredPath = storedPath,
                    Status = row["STATUS"].ToString(),
                    CreatedAt = ((DateTimeOffset)row["CREATED_AT_UTC"]).DateTime
                });
            }

            return list;
        }

        private ProofDetailViewModel GetProofDetail(string id)
        {
            string query = @"SELECT p.*, u.FULL_NAME, c.NAME as CLASS_NAME, a.TITLE as ACTIVITY_TITLE, a.START_AT
                            FROM PROOFS p
                            JOIN USERS u ON p.STUDENT_ID = u.MAND
                            LEFT JOIN STUDENTS s ON u.MAND = s.USER_ID
                            LEFT JOIN CLASSES c ON s.CLASS_ID = c.ID
                            JOIN ACTIVITIES a ON p.ACTIVITY_ID = a.ID
                            WHERE p.ID = :Id";

            var parameters = new[] { OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id) };
            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            string proofId = row["ID"].ToString();
            
            // Try to get decrypted path, fallback to plain stored path
            var pathService = new FilePathCryptoService();
            string storedPath = pathService.GetProofPath(proofId) 
                                ?? row["STORED_PATH"].ToString();
            
            return new ProofDetailViewModel
            {
                Id = proofId,
                RegistrationId = row["REGISTRATION_ID"].ToString(),
                StudentId = row["STUDENT_ID"].ToString(),
                StudentName = row["FULL_NAME"].ToString(),
                StudentClass = row["CLASS_NAME"] != DBNull.Value ? row["CLASS_NAME"].ToString() : "N/A",
                ActivityId = row["ACTIVITY_ID"].ToString(),
                ActivityTitle = row["ACTIVITY_TITLE"].ToString(),
                ActivityDate = Convert.ToDateTime(row["START_AT"]),
                FileName = EncryptionHelper.Decrypt(row["FILE_NAME"].ToString()),
                StoredPath = storedPath,
                ContentType = row["CONTENT_TYPE"].ToString(),
                FileSize = Convert.ToInt64(row["FILE_SIZE"]),
                Note = row["NOTE"] != DBNull.Value ? row["NOTE"].ToString() : "",
                Status = row["STATUS"].ToString(),
                CreatedAt = ((DateTimeOffset)row["CREATED_AT_UTC"]).DateTime,
                ReviewedAt = row["REVIEWED_AT_UTC"] != DBNull.Value ? (DateTime?)((DateTimeOffset)row["REVIEWED_AT_UTC"]).DateTime : null
            };
        }

        #endregion
    }
}
