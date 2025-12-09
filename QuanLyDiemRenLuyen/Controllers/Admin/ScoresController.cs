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
        public ActionResult Index(string department, string term, string classification, string search)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetSchoolWideScoresViewModel(department, term, classification, search);
                return View("~/Views/Admin/ApproveScores.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/ApproveScores.cshtml", new SchoolWideScoresViewModel { Students = new List<StudentScorePublicationItem>() });
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
                    return RedirectToRoute("AdminScores", new { action = "Index" });
                }

                var viewModel = GetClassScoresViewModel(classId);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy lớp học";
                    return RedirectToRoute("AdminScores", new { action = "Index" });
                }

                return View("~/Views/Admin/ClassScores.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToRoute("AdminScores", new { action = "Index" });
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
                string getOldScoreQuery = "SELECT TOTAL_SCORE FROM SCORES WHERE ID = :ScoreId";
                var getParams = new[] { OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Varchar2, scoreId) };
                object oldScoreObj = OracleDbHelper.ExecuteScalar(getOldScoreQuery, getParams);

                if (oldScoreObj == null || oldScoreObj == DBNull.Value)
                {
                    return Json(new { success = false, message = "Không tìm thấy điểm" });
                }

                decimal oldScore = Convert.ToDecimal(oldScoreObj);

                // Cập nhật điểm
                string updateQuery = @"UPDATE SCORES
                                      SET TOTAL_SCORE = :NewScore,
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
                                           sc.ID as SCORE_ID, sc.TOTAL_SCORE, sc.STATUS,
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
                int total = row["TOTAL_SCORE"] != DBNull.Value ? Convert.ToInt32(row["TOTAL_SCORE"]) : 0;
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
                                          (SELECT AVG(sc.TOTAL_SCORE) FROM SCORES sc
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

        private SchoolWideScoresViewModel GetSchoolWideScoresViewModel(string department, string term, string classification, string search)
        {
            var viewModel = new SchoolWideScoresViewModel
            {
                FilterDepartment = department,
                FilterClassification = classification,
                SearchKeyword = search,
                Students = new List<StudentScorePublicationItem>(),
                Terms = new List<TermSelectItem>(),
                Departments = new List<DepartmentSelectItem>()
            };

            // Get terms for dropdown
            string termsQuery = "SELECT ID, NAME, IS_CURRENT, SCORE_STATUS, DRAFT_PUBLISHED_AT, FEEDBACK_DEADLINE, OFFICIAL_PUBLISHED_AT, PUBLISHED_BY FROM TERMS ORDER BY START_DATE DESC";
            DataTable termsDt = OracleDbHelper.ExecuteQuery(termsQuery, null);
            foreach (DataRow row in termsDt.Rows)
            {
                viewModel.Terms.Add(new TermSelectItem
                {
                    Id = row["ID"].ToString(),
                    Name = row["NAME"].ToString(),
                    IsCurrent = row["IS_CURRENT"] != DBNull.Value && Convert.ToInt32(row["IS_CURRENT"]) == 1
                });
            }

            // Get selected term or current term
            string termId = term;
            DataRow selectedTermRow = null;
            if (string.IsNullOrEmpty(termId))
            {
                selectedTermRow = termsDt.AsEnumerable().FirstOrDefault(r => r["IS_CURRENT"] != DBNull.Value && Convert.ToInt32(r["IS_CURRENT"]) == 1);
                if (selectedTermRow != null) termId = selectedTermRow["ID"].ToString();
            }
            else
            {
                selectedTermRow = termsDt.AsEnumerable().FirstOrDefault(r => r["ID"].ToString() == termId);
            }

            if (selectedTermRow != null)
            {
                viewModel.TermId = termId;
                viewModel.TermName = selectedTermRow["NAME"].ToString();
                viewModel.ScoreStatus = selectedTermRow["SCORE_STATUS"] != DBNull.Value ? selectedTermRow["SCORE_STATUS"].ToString() : "PROVISIONAL";
                viewModel.DraftPublishedAt = selectedTermRow["DRAFT_PUBLISHED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(selectedTermRow["DRAFT_PUBLISHED_AT"]) : null;
                viewModel.FeedbackDeadline = selectedTermRow["FEEDBACK_DEADLINE"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(selectedTermRow["FEEDBACK_DEADLINE"]) : null;
                viewModel.OfficialPublishedAt = selectedTermRow["OFFICIAL_PUBLISHED_AT"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(selectedTermRow["OFFICIAL_PUBLISHED_AT"]) : null;
            }

            if (string.IsNullOrEmpty(termId)) return viewModel;

            // Get departments for dropdown
            string deptsQuery = "SELECT ID, NAME FROM DEPARTMENTS ORDER BY NAME";
            DataTable deptsDt = OracleDbHelper.ExecuteQuery(deptsQuery, null);
            foreach (DataRow row in deptsDt.Rows)
            {
                viewModel.Departments.Add(new DepartmentSelectItem
                {
                    Id = row["ID"].ToString(),
                    Name = row["NAME"].ToString()
                });
            }

            // Get class submission progress
            string classProgressQuery = @"SELECT 
                (SELECT COUNT(DISTINCT c.ID) FROM CLASSES c 
                 INNER JOIN STUDENTS s ON s.CLASS_ID = c.ID) as TOTAL_CLASSES,
                (SELECT COUNT(DISTINCT c.ID) FROM CLASSES c 
                 INNER JOIN STUDENTS s ON s.CLASS_ID = c.ID
                 WHERE NOT EXISTS (
                     SELECT 1 FROM SCORES sc 
                     WHERE sc.STUDENT_ID = s.USER_ID 
                     AND sc.TERM_ID = :TermId 
                     AND sc.STATUS = 'PROVISIONAL'
                 ) AND EXISTS (
                     SELECT 1 FROM SCORES sc 
                     WHERE sc.STUDENT_ID = s.USER_ID 
                     AND sc.TERM_ID = :TermId 
                     AND sc.STATUS = 'DRAFT_PUBLISHED'
                 )) as SUBMITTED_CLASSES
                FROM DUAL";
            var progressParams = new[] { OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId) };
            DataTable progressDt = OracleDbHelper.ExecuteQuery(classProgressQuery, progressParams);
            if (progressDt.Rows.Count > 0)
            {
                viewModel.TotalClasses = progressDt.Rows[0]["TOTAL_CLASSES"] != DBNull.Value ? Convert.ToInt32(progressDt.Rows[0]["TOTAL_CLASSES"]) : 0;
                viewModel.SubmittedClasses = progressDt.Rows[0]["SUBMITTED_CLASSES"] != DBNull.Value ? Convert.ToInt32(progressDt.Rows[0]["SUBMITTED_CLASSES"]) : 0;
            }

            // Get all scores for the term (with ClassCode for sorting) - ONLY SUBMITTED scores
            string scoresQuery = @"SELECT sc.ID as SCORE_ID, sc.STUDENT_ID, st.STUDENT_CODE, u.FULL_NAME,
                                          c.CODE as CLASS_CODE, c.NAME as CLASS_NAME, d.NAME as DEPT_NAME,
                                          sc.TOTAL_SCORE, sc.CLASSIFICATION, sc.STATUS
                                   FROM SCORES sc
                                   INNER JOIN STUDENTS st ON sc.STUDENT_ID = st.USER_ID
                                   INNER JOIN USERS u ON st.USER_ID = u.MAND
                                   LEFT JOIN CLASSES c ON st.CLASS_ID = c.ID
                                   LEFT JOIN DEPARTMENTS d ON st.DEPARTMENT_ID = d.ID
                                   WHERE sc.TERM_ID = :TermId
                                   AND sc.STATUS IN ('SUBMITTED', 'APPROVED', 'DRAFT_PUBLISHED', 'OFFICIAL')";

            var parameters = new List<OracleParameter> { OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId) };

            if (!string.IsNullOrEmpty(department))
            {
                scoresQuery += " AND st.DEPARTMENT_ID = :DeptId";
                parameters.Add(OracleDbHelper.CreateParameter("DeptId", OracleDbType.Varchar2, department));
            }

            if (!string.IsNullOrEmpty(classification))
            {
                scoresQuery += " AND sc.CLASSIFICATION = :Classification";
                parameters.Add(OracleDbHelper.CreateParameter("Classification", OracleDbType.Varchar2, classification));
            }

            if (!string.IsNullOrEmpty(search))
            {
                scoresQuery += " AND (st.STUDENT_CODE LIKE :Search OR u.FULL_NAME LIKE :Search)";
                parameters.Add(OracleDbHelper.CreateParameter("Search", OracleDbType.Varchar2, "%" + search + "%"));
            }

            // Basic sort by class code in SQL
            scoresQuery += " ORDER BY c.CODE, st.STUDENT_CODE";

            DataTable scoresDt = OracleDbHelper.ExecuteQuery(scoresQuery, parameters.ToArray());

            var tempStudents = new List<StudentScorePublicationItem>();
            foreach (DataRow row in scoresDt.Rows)
            {
                int totalScore = Convert.ToInt32(row["TOTAL_SCORE"]);
                string classificationValue = row["CLASSIFICATION"] != DBNull.Value ? row["CLASSIFICATION"].ToString() : GetClassification(totalScore);

                tempStudents.Add(new StudentScorePublicationItem
                {
                    ScoreId = row["SCORE_ID"].ToString(),
                    StudentId = row["STUDENT_ID"].ToString(),
                    StudentCode = row["STUDENT_CODE"].ToString(),
                    StudentName = row["FULL_NAME"].ToString(),
                    ClassCode = row["CLASS_CODE"] != DBNull.Value ? row["CLASS_CODE"].ToString() : "",
                    ClassName = row["CLASS_NAME"] != DBNull.Value ? row["CLASS_NAME"].ToString() : "",
                    DepartmentName = row["DEPT_NAME"] != DBNull.Value ? row["DEPT_NAME"].ToString() : "",
                    TotalScore = totalScore,
                    Classification = classificationValue,
                    Status = row["STATUS"].ToString()
                });

                // Count by classification
                if (totalScore >= 90) viewModel.ExcellentCount++;
                else if (totalScore >= 80) viewModel.GoodCount++;
                else if (totalScore >= 65) viewModel.FairCount++;
                else if (totalScore >= 50) viewModel.AverageCount++;
                else viewModel.WeakCount++;
            }

            // Sort by ClassCode, then by Vietnamese last name (last word), then full name, then student code
            viewModel.Students = tempStudents
                .OrderBy(s => s.ClassCode)
                .ThenBy(s => GetVietnameseLastName(s.StudentName))
                .ThenBy(s => s.StudentName)
                .ThenBy(s => s.StudentCode)
                .ToList();

            viewModel.TotalStudents = viewModel.Students.Count;

            // Count pending feedbacks
            string feedbacksQuery = @"SELECT COUNT(*) FROM FEEDBACKS WHERE TERM_ID = :TermId AND STATUS = 'SUBMITTED'";
            var fbParams = new[] { OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId) };
            object fbCount = OracleDbHelper.ExecuteScalar(feedbacksQuery, fbParams);
            viewModel.PendingFeedbacks = fbCount != null ? Convert.ToInt32(fbCount) : 0;

            return viewModel;
        }

        /// <summary>
        /// Lấy tên riêng (từ cuối cùng) trong tên tiếng Việt để sắp xếp
        /// Ví dụ: "Nguyễn Văn An" -> "An"
        /// </summary>
        private string GetVietnameseLastName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return "";
            var parts = fullName.Trim().Split(' ');
            return parts.Length > 0 ? parts[parts.Length - 1] : fullName;
        }

        #endregion

        #region Publish Actions

        // POST: Admin/Scores/PublishDraft
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PublishDraft(string termId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = Session["MAND"].ToString();
                DateTime now = DateTime.Now;
                DateTime deadline = now.AddDays(3);

                // Update TERMS
                string updateTermQuery = @"UPDATE TERMS 
                                          SET SCORE_STATUS = 'DRAFT',
                                              DRAFT_PUBLISHED_AT = :PublishedAt,
                                              FEEDBACK_DEADLINE = :Deadline,
                                              PUBLISHED_BY = :PublishedBy
                                          WHERE ID = :TermId";
                var termParams = new[]
                {
                    OracleDbHelper.CreateParameter("PublishedAt", OracleDbType.TimeStamp, now),
                    OracleDbHelper.CreateParameter("Deadline", OracleDbType.TimeStamp, deadline),
                    OracleDbHelper.CreateParameter("PublishedBy", OracleDbType.Varchar2, mand),
                    OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId)
                };
                OracleDbHelper.ExecuteNonQuery(updateTermQuery, termParams);

                // Update all SCORES status
                string updateScoresQuery = @"UPDATE SCORES SET STATUS = 'DRAFT_PUBLISHED' WHERE TERM_ID = :TermId AND STATUS = 'PROVISIONAL'";
                var scoreParams = new[] { OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId) };
                OracleDbHelper.ExecuteNonQuery(updateScoresQuery, scoreParams);

                // Get term name
                string termNameQuery = "SELECT NAME FROM TERMS WHERE ID = :TermId";
                object termNameObj = OracleDbHelper.ExecuteScalar(termNameQuery, scoreParams);
                string termName = termNameObj?.ToString() ?? "";

                // Create notification for all students
                string notificationId = "N" + now.ToString("yyyyMMddHHmmss");
                string notificationTitle = $"Điểm rèn luyện {termName} - Công bố dự kiến";
                string notificationContent = $"Điểm rèn luyện {termName} đã được công bố dự kiến. Bạn có thể xem điểm và gửi phản hồi nếu có sai sót. Hạn chót phản hồi: {deadline:dd/MM/yyyy HH:mm}.";

                string insertNotificationQuery = @"INSERT INTO NOTIFICATIONS (ID, TITLE, CONTENT, TARGET_ROLE, CREATED_AT)
                                                  VALUES (:Id, :Title, :Content, 'STUDENT', SYSDATE)";
                var notiParams = new[]
                {
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, notificationId),
                    OracleDbHelper.CreateParameter("Title", OracleDbType.Varchar2, notificationTitle),
                    OracleDbHelper.CreateParameter("Content", OracleDbType.Clob, notificationContent)
                };
                OracleDbHelper.ExecuteNonQuery(insertNotificationQuery, notiParams);

                TempData["SuccessMessage"] = $"Đã công bố điểm dự kiến. Sinh viên có thể phản hồi đến {deadline:dd/MM/yyyy HH:mm}";
                return RedirectToRoute("AdminScores", new { action = "Index", term = termId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToRoute("AdminScores", new { action = "Index", term = termId });
            }
        }

        // POST: Admin/Scores/PublishOfficial
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PublishOfficial(string termId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = Session["MAND"].ToString();
                DateTime now = DateTime.Now;

                // Update TERMS
                string updateTermQuery = @"UPDATE TERMS 
                                          SET SCORE_STATUS = 'OFFICIAL',
                                              OFFICIAL_PUBLISHED_AT = :PublishedAt
                                          WHERE ID = :TermId";
                var termParams = new[]
                {
                    OracleDbHelper.CreateParameter("PublishedAt", OracleDbType.TimeStamp, now),
                    OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId)
                };
                OracleDbHelper.ExecuteNonQuery(updateTermQuery, termParams);

                // Update all SCORES status to OFFICIAL
                string updateScoresQuery = @"UPDATE SCORES SET STATUS = 'OFFICIAL' WHERE TERM_ID = :TermId AND STATUS = 'DRAFT_PUBLISHED'";
                var scoreParams = new[] { OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId) };
                OracleDbHelper.ExecuteNonQuery(updateScoresQuery, scoreParams);

                // Get term name
                string termNameQuery = "SELECT NAME FROM TERMS WHERE ID = :TermId";
                object termNameObj = OracleDbHelper.ExecuteScalar(termNameQuery, scoreParams);
                string termName = termNameObj?.ToString() ?? "";

                // Create notification
                string notificationId = "N" + now.ToString("yyyyMMddHHmmss") + "O";
                string notificationTitle = $"Điểm rèn luyện {termName} - Chính thức";
                string notificationContent = $"Điểm rèn luyện {termName} đã được công bố chính thức. Đây là kết quả cuối cùng sau khi xử lý các phản hồi.";

                string insertNotificationQuery = @"INSERT INTO NOTIFICATIONS (ID, TITLE, CONTENT, TARGET_ROLE, CREATED_AT)
                                                  VALUES (:Id, :Title, :Content, 'STUDENT', SYSDATE)";
                var notiParams = new[]
                {
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, notificationId),
                    OracleDbHelper.CreateParameter("Title", OracleDbType.Varchar2, notificationTitle),
                    OracleDbHelper.CreateParameter("Content", OracleDbType.Clob, notificationContent)
                };
                OracleDbHelper.ExecuteNonQuery(insertNotificationQuery, notiParams);

                TempData["SuccessMessage"] = "Đã công bố điểm chính thức!";
                return RedirectToRoute("AdminScores", new { action = "Index", term = termId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToRoute("AdminScores", new { action = "Index", term = termId });
            }
        }

        // GET: Admin/Scores/ExportScores
        public ActionResult ExportScores(string termId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetSchoolWideScoresViewModel(null, termId, null, null);
                
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("STT,MSSV,Họ tên,Lớp,Khoa,Điểm,Xếp loại");

                int stt = 1;
                foreach (var student in viewModel.Students)
                {
                    csv.AppendLine($"{stt},{student.StudentCode},{student.StudentName},{student.ClassName},{student.DepartmentName},{student.TotalScore},{student.Classification}");
                    stt++;
                }

                byte[] buffer = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
                return File(buffer, "text/csv", $"DiemRenLuyen_{viewModel.TermName.Replace(" ", "_")}.csv");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xuất file: " + ex.Message;
                return RedirectToRoute("AdminScores", new { action = "Index", term = termId });
            }
        }

        // POST: Admin/Scores/InitializeScores
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult InitializeScores(string termId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                // Insert 70 points for all students who don't have a score in this term
                string insertQuery = @"INSERT INTO SCORES (STUDENT_ID, TERM_ID, TOTAL_SCORE, CLASSIFICATION, STATUS, CREATED_AT)
                                      SELECT s.USER_ID, :TermId, 70, 'Khá', 'PROVISIONAL', SYSTIMESTAMP
                                      FROM STUDENTS s
                                      WHERE NOT EXISTS (
                                          SELECT 1 FROM SCORES sc 
                                          WHERE sc.STUDENT_ID = s.USER_ID AND sc.TERM_ID = :TermId
                                      )";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId)
                };

                int insertedCount = OracleDbHelper.ExecuteNonQuery(insertQuery, parameters);

                if (insertedCount > 0)
                {
                    TempData["SuccessMessage"] = $"Đã khởi tạo điểm 70 cho {insertedCount} sinh viên";
                }
                else
                {
                    TempData["SuccessMessage"] = "Tất cả sinh viên đã có điểm trong học kỳ này";
                }

                return RedirectToRoute("AdminScores", new { action = "Index", term = termId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToRoute("AdminScores", new { action = "Index", term = termId });
            }
        }

        // POST: Admin/Scores/SetCurrentTerm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetCurrentTerm(string termId, bool initializeScores = true)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                // 1. Reset all terms IS_CURRENT to 0
                string resetQuery = "UPDATE TERMS SET IS_CURRENT = 0";
                OracleDbHelper.ExecuteNonQuery(resetQuery, null);

                // 2. Set selected term as current
                string setCurrentQuery = @"UPDATE TERMS 
                                          SET IS_CURRENT = 1,
                                              SCORE_STATUS = NVL(SCORE_STATUS, 'PROVISIONAL')
                                          WHERE ID = :TermId";
                var setParams = new[] { OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId) };
                OracleDbHelper.ExecuteNonQuery(setCurrentQuery, setParams);

                // 3. Get term name
                string termNameQuery = "SELECT NAME FROM TERMS WHERE ID = :TermId";
                object termNameObj = OracleDbHelper.ExecuteScalar(termNameQuery, setParams);
                string termName = termNameObj?.ToString() ?? "";

                // 4. Initialize scores if requested
                int insertedCount = 0;
                if (initializeScores)
                {
                    string insertQuery = @"INSERT INTO SCORES (STUDENT_ID, TERM_ID, TOTAL_SCORE, CLASSIFICATION, STATUS, CREATED_AT)
                                          SELECT s.USER_ID, :TermId, 70, 'Khá', 'PROVISIONAL', SYSTIMESTAMP
                                          FROM STUDENTS s
                                          WHERE NOT EXISTS (
                                              SELECT 1 FROM SCORES sc 
                                              WHERE sc.STUDENT_ID = s.USER_ID AND sc.TERM_ID = :TermId
                                          )";
                    insertedCount = OracleDbHelper.ExecuteNonQuery(insertQuery, setParams);
                }

                string message = $"Đã chuyển sang {termName}";
                if (insertedCount > 0)
                {
                    message += $" và khởi tạo điểm 70 cho {insertedCount} sinh viên";
                }
                TempData["SuccessMessage"] = message;

                return RedirectToRoute("AdminScores", new { action = "Index", term = termId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToRoute("AdminScores", new { action = "Index" });
            }
        }

        // POST: Admin/Scores/CreateNextTerm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateNextTerm()
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                // Determine next term based on current date
                DateTime now = DateTime.Now;
                int currentYear = now.Year;
                int currentMonth = now.Month;

                // Academic year: Sep-Aug of next year
                // HK1: Sep-Jan, HK2: Feb-May, HK3: Jun-Aug
                int termNumber;
                int academicYear;
                DateTime startDate, endDate;

                if (currentMonth >= 9) // Sep-Dec: HK1
                {
                    termNumber = 1;
                    academicYear = currentYear;
                    startDate = new DateTime(currentYear, 9, 1);
                    endDate = new DateTime(currentYear + 1, 1, 31);
                }
                else if (currentMonth == 1) // Jan: still HK1
                {
                    termNumber = 1;
                    academicYear = currentYear - 1;
                    startDate = new DateTime(currentYear - 1, 9, 1);
                    endDate = new DateTime(currentYear, 1, 31);
                }
                else if (currentMonth >= 2 && currentMonth <= 5) // Feb-May: HK2
                {
                    termNumber = 2;
                    academicYear = currentYear - 1;
                    startDate = new DateTime(currentYear, 2, 1);
                    endDate = new DateTime(currentYear, 5, 31);
                }
                else // Jun-Aug: HK3 (Summer)
                {
                    termNumber = 3;
                    academicYear = currentYear - 1;
                    startDate = new DateTime(currentYear, 6, 1);
                    endDate = new DateTime(currentYear, 8, 31);
                }

                string termName = $"Học kỳ {termNumber} - {academicYear}-{academicYear + 1}";
                string termId = $"HK{termNumber}_{academicYear}";

                // Check if term already exists
                string checkQuery = "SELECT COUNT(*) FROM TERMS WHERE ID = :TermId OR NAME = :TermName";
                var checkParams = new[]
                {
                    OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId),
                    OracleDbHelper.CreateParameter("TermName", OracleDbType.Varchar2, termName)
                };
                object existsObj = OracleDbHelper.ExecuteScalar(checkQuery, checkParams);
                if (existsObj != null && Convert.ToInt32(existsObj) > 0)
                {
                    TempData["ErrorMessage"] = $"Học kỳ '{termName}' đã tồn tại!";
                    return RedirectToRoute("AdminScores", new { action = "Index" });
                }

                // Create new term
                string insertQuery = @"INSERT INTO TERMS (ID, NAME, YEAR, TERM_NUMBER, START_DATE, END_DATE, IS_CURRENT, SCORE_STATUS)
                                      VALUES (:Id, :Name, :Year, :TermNumber, :StartDate, :EndDate, 0, 'PROVISIONAL')";
                var insertParams = new[]
                {
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, termId),
                    OracleDbHelper.CreateParameter("Name", OracleDbType.Varchar2, termName),
                    OracleDbHelper.CreateParameter("Year", OracleDbType.Int32, academicYear),
                    OracleDbHelper.CreateParameter("TermNumber", OracleDbType.Int32, termNumber),
                    OracleDbHelper.CreateParameter("StartDate", OracleDbType.Date, startDate),
                    OracleDbHelper.CreateParameter("EndDate", OracleDbType.Date, endDate)
                };
                OracleDbHelper.ExecuteNonQuery(insertQuery, insertParams);

                TempData["SuccessMessage"] = $"Đã tạo '{termName}'. Chọn học kỳ và click 'Đặt làm HK hiện tại' để kích hoạt.";
                return RedirectToRoute("AdminScores", new { action = "Index", term = termId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToRoute("AdminScores", new { action = "Index" });
            }
        }

        #endregion
    }
}
