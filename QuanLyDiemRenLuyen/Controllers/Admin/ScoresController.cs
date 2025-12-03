using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;
using QuanLyDiemRenLuyen.Services;

namespace QuanLyDiemRenLuyen.Controllers.Admin
{
    public class ScoresController : AdminBaseController
    {
        // GET: Admin/Scores
        public ActionResult Index(string department, string term, string search)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetClassScoreListViewModel(department, term, search);
                return View("~/Views/Admin/ApproveScores.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/ApproveScores.cshtml", new ClassScoreListViewModel { Classes = new List<ClassItem>() });
            }
        }

        // GET: Admin/Scores/ClassScores
        public ActionResult ClassScores(string classId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                if (string.IsNullOrEmpty(classId))
                {
                    TempData["ErrorMessage"] = "Không tìm thấy lớp học";
                    return RedirectToAction("Index");
                }

                var viewModel = GetClassScoresViewModel(classId);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy lớp học";
                    return RedirectToAction("Index");
                }

                return View("~/Views/Admin/ClassScores.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Admin/Scores/UpdateScore
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateScore(string scoreId, decimal newScore, string reason)
        {
            var authCheck = CheckAuth();
            if (authCheck != null)
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                string mand = Session["MAND"].ToString();

                // Lấy điểm cũ
                string getOldScoreQuery = "SELECT TOTAL FROM SCORES WHERE ID = :ScoreId";
                var getParams = new[] { OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId) };
                object oldScoreObj = OracleDbHelper.ExecuteScalar(getOldScoreQuery, getParams);

                if (oldScoreObj == null || oldScoreObj == DBNull.Value)
                {
                    return Json(new { success = false, message = "Không tìm thấy điểm" });
                }

                decimal oldScore = Convert.ToDecimal(oldScoreObj);

                // Cập nhật điểm
                string updateQuery = @"UPDATE SCORES
                                      SET TOTAL = :NewScore,
                                          STATUS = 'PROVISIONAL'
                                      WHERE ID = :ScoreId";

                var updateParams = new[]
                {
                    OracleDbHelper.CreateParameter("NewScore", OracleDbType.Decimal, newScore),
                    OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, updateParams);

                if (result > 0)
                {
                    // Thêm vào lịch sử
                    string historyId = "SH" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    string insertHistoryQuery = @"INSERT INTO SCORE_HISTORY
                                                 (ID, SCORE_ID, ACTION, OLD_VALUE, NEW_VALUE,
                                                  CHANGED_BY, REASON, CHANGED_AT)
                                                 VALUES
                                                 (:Id, :ScoreId, 'UPDATE', :OldValue, :NewValue,
                                                  :ChangedBy, :Reason, SYSDATE)";

                    var historyParams = new[]
                    {
                        OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, historyId),
                        OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId),
                        OracleDbHelper.CreateParameter("OldValue", OracleDbType.Varchar2, oldScore.ToString()),
                        OracleDbHelper.CreateParameter("NewValue", OracleDbType.Varchar2, newScore.ToString()),
                        OracleDbHelper.CreateParameter("ChangedBy", OracleDbType.Varchar2, mand),
                        OracleDbHelper.CreateParameter("Reason", OracleDbType.Varchar2,
                            string.IsNullOrEmpty(reason) ? (object)DBNull.Value : reason)
                    };

                    OracleDbHelper.ExecuteNonQuery(insertHistoryQuery, historyParams);

                    return Json(new { success = true, message = "Cập nhật điểm thành công" });
                }

                return Json(new { success = false, message = "Không thể cập nhật điểm" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/Scores/ApproveScore
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ApproveScore(string scoreId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null)
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                string mand = Session["MAND"].ToString();

                string getScoreQuery = @"SELECT STUDENT_ID, TERM_ID, TOTAL_SCORE, CLASSIFICATION
                                        FROM SCORES WHERE ID = :ScoreId";
                var getParams = new[] { OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId) };
                DataTable scoreTable = OracleDbHelper.ExecuteQuery(getScoreQuery, getParams);

                if (scoreTable.Rows.Count == 0)
                {
                    return Json(new { success = false, message = "Không tìm thấy điểm" });
                }

                DataRow scoreRow = scoreTable.Rows[0];
                string studentId = scoreRow["STUDENT_ID"].ToString();
                string termId = scoreRow["TERM_ID"].ToString();
                int totalScore = Convert.ToInt32(scoreRow["TOTAL_SCORE"]);
                string classification = scoreRow["CLASSIFICATION"] != DBNull.Value ? scoreRow["CLASSIFICATION"].ToString() : "";

                string dataToSign = DigitalSignatureService.CreateScoreDataString(
                    studentId,
                    termId,
                    totalScore,
                    classification,
                    "APPROVED"
                );

                string signature = DigitalSignatureService.SignScoreData(dataToSign);
                string dataHash = DigitalSignatureService.CreateScoreDataHash(dataToSign);
                string keyId = RsaKeyManager.GetCurrentEncryptionKeyId();

                string updateQuery = @"UPDATE SCORES
                                      SET STATUS = 'APPROVED',
                                          APPROVED_BY = :ApprovedBy,
                                          APPROVED_AT = SYSTIMESTAMP,
                                          DIGITAL_SIGNATURE = :Signature,
                                          SIGNED_DATA_HASH = :DataHash,
                                          SIGNATURE_KEY_ID = :KeyId,
                                          SIGNATURE_ALGORITHM = 'RSA-SHA256',
                                          SIGNATURE_VERIFIED = 1,
                                          SIGNED_BY = :SignedBy,
                                          SIGNED_AT = SYSTIMESTAMP
                                      WHERE ID = :ScoreId";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("ApprovedBy", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("Signature", OracleDbType.Clob, signature),
                    OracleDbHelper.CreateParameter("DataHash", OracleDbType.Varchar2, dataHash),
                    OracleDbHelper.CreateParameter("KeyId", OracleDbType.Varchar2, keyId),
                    OracleDbHelper.CreateParameter("SignedBy", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);

                if (result > 0)
                {
                    // ⭐ Step 5: Log to SCORE_AUDIT_SIGNATURES table
                    string insertAuditQuery = @"INSERT INTO SCORE_AUDIT_SIGNATURES
                                               (ID, SCORE_ID, ACTION_TYPE, PERFORMED_BY, 
                                                SIGNATURE_VALUE, VERIFICATION_RESULT, 
                                                DATA_HASH_AFTER, NOTES)
                                               VALUES
                                               (RAWTOHEX(SYS_GUID()), :ScoreId, 'SIGN', :PerformedBy,
                                                :Signature, 'SUCCESS',
                                                :DataHash, 'Auto-signed on approval')";

                    var auditParams = new[]
                    {
                        OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Int32, int.Parse(scoreId)),
                        OracleDbHelper.CreateParameter("PerformedBy", OracleDbType.Varchar2, mand),
                        OracleDbHelper.CreateParameter("Signature", OracleDbType.Clob, signature),
                        OracleDbHelper.CreateParameter("DataHash", OracleDbType.Varchar2, dataHash)
                    };

                    OracleDbHelper.ExecuteNonQuery(insertAuditQuery, auditParams);

                    // Step 6: Traditional history log
                    string historyId = "SH" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    string insertHistoryQuery = @"INSERT INTO SCORE_HISTORY
                                                 (ID, SCORE_ID, ACTION, CHANGED_BY, CHANGED_AT)
                                                 VALUES
                                                 (:Id, :ScoreId, 'APPROVE_WITH_SIGNATURE', :ChangedBy, SYSTIMESTAMP)";

                    var historyParams = new[]
                    {
                        OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, historyId),
                        OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId),
                        OracleDbHelper.CreateParameter("ChangedBy", OracleDbType.Varchar2, mand)
                    };

                    OracleDbHelper.ExecuteNonQuery(insertHistoryQuery, historyParams);

                    return Json(new { 
                        success = true, 
                        message = "✅ Phê duyệt và ký điện tử thành công!" 
                    });
                }

                return Json(new { success = false, message = "Không thể phê duyệt điểm" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/Scores/ApproveSelectedScores
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ApproveSelectedScores(string[] scoreIds)
        {
            var authCheck = CheckAuth();
            if (authCheck != null)
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                if (scoreIds == null || scoreIds.Length == 0)
                {
                    return Json(new { success = false, message = "Chưa chọn điểm nào" });
                }

                string mand = Session["MAND"].ToString();
                int successCount = 0;

                foreach (string scoreId in scoreIds)
                {
                    string updateQuery = @"UPDATE SCORES
                                          SET STATUS = 'APPROVED',
                                              APPROVED_BY = :ApprovedBy,
                                              APPROVED_AT = SYSDATE
                                          WHERE ID = :ScoreId";

                    var parameters = new[]
                    {
                        OracleDbHelper.CreateParameter("ApprovedBy", OracleDbType.Varchar2, mand),
                        OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId)
                    };

                    int result = OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);

                    if (result > 0)
                    {
                        successCount++;

                        // Thêm vào lịch sử
                        string historyId = "SH" + DateTime.Now.ToString("yyyyMMddHHmmss") + successCount;
                        string insertHistoryQuery = @"INSERT INTO SCORE_HISTORY
                                                     (ID, SCORE_ID, ACTION, CHANGED_BY, CHANGED_AT)
                                                     VALUES
                                                     (:Id, :ScoreId, 'APPROVE', :ChangedBy, SYSDATE)";

                        var historyParams = new[]
                        {
                            OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, historyId),
                            OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId),
                            OracleDbHelper.CreateParameter("ChangedBy", OracleDbType.Varchar2, mand)
                        };

                        OracleDbHelper.ExecuteNonQuery(insertHistoryQuery, historyParams);
                    }
                }

                return Json(new { success = true, message = $"Đã phê duyệt {successCount}/{scoreIds.Length} điểm" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        #region Private Helper Methods

        private ClassScoreViewModel GetClassScoresViewModel(string classId)
        {
            // Lấy thông tin lớp học
            string classQuery = @"SELECT c.ID, c.NAME, c.CODE, d.NAME as DEPT_NAME,
                                        t.ID as TERM_ID, t.NAME as TERM_NAME,
                                        u.FULL_NAME as ADVISOR_NAME
                                 FROM CLASSES c
                                 LEFT JOIN DEPARTMENTS d ON c.DEPARTMENT_ID = d.ID
                                 LEFT JOIN TERMS t ON t.IS_CURRENT = 1
                                 LEFT JOIN USERS u ON c.ADVISOR_ID = u.MAND
                                 WHERE c.ID = :ClassId";

            var classParams = new[] { OracleDbHelper.CreateParameter("ClassId", OracleDbType.Varchar2, classId) };
            DataTable classDt = OracleDbHelper.ExecuteQuery(classQuery, classParams);

            if (classDt.Rows.Count == 0) return null;

            DataRow classRow = classDt.Rows[0];
            string termId = classRow["TERM_ID"] != DBNull.Value ? classRow["TERM_ID"].ToString() : null;

            var viewModel = new ClassScoreViewModel
            {
                ClassId = classId,
                ClassName = classRow["NAME"].ToString(),
                ClassCode = classRow["CODE"].ToString(),
                DepartmentName = classRow["DEPT_NAME"] != DBNull.Value ? classRow["DEPT_NAME"].ToString() : "",
                TermId = termId,
                TermName = classRow["TERM_NAME"] != DBNull.Value ? classRow["TERM_NAME"].ToString() : "",
                AdvisorName = classRow["ADVISOR_NAME"] != DBNull.Value ? classRow["ADVISOR_NAME"].ToString() : "",
                Students = new List<StudentScoreItem>()
            };

            if (string.IsNullOrEmpty(termId))
            {
                return viewModel;
            }

            // Lấy danh sách sinh viên và điểm
            string studentsQuery = @"SELECT s.USER_ID, st.STUDENT_CODE, u.FULL_NAME,
                                           sc.ID as SCORE_ID, sc.TOTAL, sc.STATUS,
                                           (SELECT COUNT(*) FROM REGISTRATIONS r
                                            INNER JOIN ACTIVITIES a ON r.ACTIVITY_ID = a.ID
                                            WHERE r.STUDENT_ID = s.USER_ID
                                            AND a.TERM_ID = :TermId
                                            AND r.STATUS = 'CHECKED_IN') as ACTIVITY_COUNT
                                    FROM STUDENTS s
                                    INNER JOIN USERS u ON s.USER_ID = u.MAND
                                    LEFT JOIN SCORES sc ON s.USER_ID = sc.STUDENT_ID AND sc.TERM_ID = :TermId
                                    WHERE s.CLASS_ID = :ClassId
                                    ORDER BY st.STUDENT_CODE";

            var studentsParams = new[]
            {
                OracleDbHelper.CreateParameter("ClassId", OracleDbType.Varchar2, classId),
                OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId)
            };

            DataTable studentsDt = OracleDbHelper.ExecuteQuery(studentsQuery, studentsParams);

            foreach (DataRow row in studentsDt.Rows)
            {
                int total = row["TOTAL"] != DBNull.Value ? Convert.ToInt32(row["TOTAL"]) : 0;
                string status = row["STATUS"] != DBNull.Value ? row["STATUS"].ToString() : "PROVISIONAL";

                viewModel.Students.Add(new StudentScoreItem
                {
                    ScoreId = row["SCORE_ID"] != DBNull.Value ? row["SCORE_ID"].ToString() : null,
                    StudentId = row["USER_ID"].ToString(),
                    StudentCode = row["STUDENT_CODE"].ToString(),
                    StudentName = row["FULL_NAME"].ToString(),
                    Total = total,
                    Status = status,
                    Classification = GetClassification(total),
                    ActivityCount = Convert.ToInt32(row["ACTIVITY_COUNT"]),
                    CanEdit = true,
                    CanApprove = status == "PROVISIONAL"
                });
            }

            // Tính thống kê
            viewModel.Statistics = new ClassScoreStatistics
            {
                TotalStudents = viewModel.Students.Count,
                ApprovedStudents = viewModel.Students.Count(x => x.Status == "APPROVED"),
                AverageScore = viewModel.Students.Count > 0 ? (int)viewModel.Students.Average(x => x.Total) : 0,
                HighestScore = viewModel.Students.Count > 0 ? viewModel.Students.Max(x => x.Total) : 0
            };

            return viewModel;
        }

        private ClassScoreListViewModel GetClassScoreListViewModel(string department, string term, string search)
        {
            var viewModel = new ClassScoreListViewModel
            {
                FilterDepartment = department,
                FilterTerm = term,
                SearchKeyword = search,
                Classes = new List<ClassItem>()
            };

            // Lấy học kỳ hiện tại nếu không có filter
            string termId = term;
            if (string.IsNullOrEmpty(termId))
            {
                string currentTermQuery = "SELECT ID FROM TERMS WHERE IS_CURRENT = 1 FETCH FIRST 1 ROWS ONLY";
                object termObj = OracleDbHelper.ExecuteScalar(currentTermQuery, null);
                termId = termObj != null ? termObj.ToString() : null;
            }

            if (string.IsNullOrEmpty(termId))
            {
                return viewModel;
            }

            // Lấy danh sách lớp
            string classesQuery = @"SELECT c.ID, c.NAME, c.CODE, d.NAME as DEPT_NAME,
                                          t.ID as TERM_ID, t.NAME as TERM_NAME,
                                          (SELECT COUNT(*) FROM STUDENTS WHERE CLASS_ID = c.ID) as TOTAL_STUDENTS,
                                          (SELECT COUNT(*) FROM SCORES sc
                                           INNER JOIN STUDENTS s ON sc.STUDENT_ID = s.USER_ID
                                           WHERE s.CLASS_ID = c.ID AND sc.TERM_ID = :TermId
                                           AND sc.STATUS = 'PROVISIONAL') as PENDING_APPROVAL,
                                          (SELECT COUNT(*) FROM SCORES sc
                                           INNER JOIN STUDENTS s ON sc.STUDENT_ID = s.USER_ID
                                           WHERE s.CLASS_ID = c.ID AND sc.TERM_ID = :TermId
                                           AND sc.STATUS = 'APPROVED') as APPROVED_STUDENTS,
                                          (SELECT AVG(sc.TOTAL) FROM SCORES sc
                                           INNER JOIN STUDENTS s ON sc.STUDENT_ID = s.USER_ID
                                           WHERE s.CLASS_ID = c.ID AND sc.TERM_ID = :TermId) as AVG_SCORE
                                   FROM CLASSES c
                                   LEFT JOIN DEPARTMENTS d ON c.DEPARTMENT_ID = d.ID
                                   LEFT JOIN TERMS t ON t.ID = :TermId
                                   WHERE 1=1";

            var parameters = new List<OracleParameter>
            {
                OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId)
            };

            if (!string.IsNullOrEmpty(department))
            {
                classesQuery += " AND c.DEPARTMENT_ID = :DepartmentId";
                parameters.Add(OracleDbHelper.CreateParameter("DepartmentId", OracleDbType.Varchar2, department));
            }

            if (!string.IsNullOrEmpty(search))
            {
                classesQuery += " AND (c.NAME LIKE :Search OR c.CODE LIKE :Search)";
                parameters.Add(OracleDbHelper.CreateParameter("Search", OracleDbType.Varchar2, "%" + search + "%"));
            }

            classesQuery += " ORDER BY c.CODE";

            DataTable classesDt = OracleDbHelper.ExecuteQuery(classesQuery, parameters.ToArray());

            foreach (DataRow row in classesDt.Rows)
            {
                viewModel.Classes.Add(new ClassItem
                {
                    ClassId = row["ID"].ToString(),
                    ClassName = row["NAME"].ToString(),
                    ClassCode = row["CODE"].ToString(),
                    DepartmentName = row["DEPT_NAME"] != DBNull.Value ? row["DEPT_NAME"].ToString() : "",
                    TermId = row["TERM_ID"] != DBNull.Value ? row["TERM_ID"].ToString() : "",
                    TermName = row["TERM_NAME"] != DBNull.Value ? row["TERM_NAME"].ToString() : "",
                    TotalStudents = Convert.ToInt32(row["TOTAL_STUDENTS"]),
                    PendingApproval = Convert.ToInt32(row["PENDING_APPROVAL"]),
                    ApprovedStudents = Convert.ToInt32(row["APPROVED_STUDENTS"]),
                    AverageScore = row["AVG_SCORE"] != DBNull.Value ? Convert.ToInt32(row["AVG_SCORE"]) : 0
                });
            }

            return viewModel;
        }

        private string GetClassification(decimal score)
        {
            if (score >= 90) return "Xuất sắc";
            if (score >= 80) return "Giỏi";
            if (score >= 65) return "Khá";
            if (score >= 50) return "Trung bình";
            return "Yếu";
        }

        #endregion
    }
}
