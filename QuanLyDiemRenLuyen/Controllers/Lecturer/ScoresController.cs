using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;

namespace QuanLyDiemRenLuyen.Controllers.Lecturer
{
    /// <summary>
    /// Controller quản lý điểm rèn luyện cho Giảng viên
    /// Chỉ cho phép xem điểm các lớp được phân công
    /// </summary>
    public class ScoresController : LecturerBaseController
    {
        // GET: Lecturer/Scores/MyClasses
        public ActionResult MyClasses()
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = GetCurrentUserId();
                var viewModel = GetMyClassScoresViewModel(mand);
                return View("~/Views/Lecturer/Scores/MyClasses.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Lecturer/Scores/MyClasses.cshtml", new MyClassScoresViewModel());
            }
        }

        // GET: Lecturer/Scores/ClassDetail/{id}
        public ActionResult ClassDetail(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = GetCurrentUserId();
                bool isShared;
                string sharedByName, permissionType;

                if (!HasClassAccess(mand, id, out isShared, out sharedByName, out permissionType))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xem điểm lớp này";
                    return RedirectToRoute("LecturerScores", new { action = "MyClasses" });
                }

                var viewModel = GetClassScoreDetailViewModel(id);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy lớp";
                    return RedirectToRoute("LecturerScores", new { action = "MyClasses" });
                }

                // Set shared class info
                viewModel.IsSharedClass = isShared;
                viewModel.SharedByName = sharedByName;
                viewModel.PermissionType = permissionType;

                // Set permission flags for edit/approve
                // CVHT (not shared) always has full permission
                // For shared: EDIT/APPROVE can edit, only APPROVE can approve
                if (!isShared)
                {
                    viewModel.CanEdit = true;
                    viewModel.CanApprove = true;
                }
                else
                {
                    viewModel.CanEdit = permissionType == "EDIT" || permissionType == "APPROVE";
                    viewModel.CanApprove = permissionType == "APPROVE";
                }

                return View("~/Views/Lecturer/Scores/ClassDetail.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToRoute("LecturerScores", new { action = "MyClasses" });
            }
        }

        // GET: Lecturer/Scores/Export/{id}
        public ActionResult Export(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string mand = GetCurrentUserId();
                bool isShared;
                string sharedByName, permissionType;
                if (!HasClassAccess(mand, id, out isShared, out sharedByName, out permissionType))
                {
                    TempData["ErrorMessage"] = "Không có quyền";
                    return RedirectToRoute("LecturerScores", new { action = "MyClasses" });
                }

                var viewModel = GetClassScoreDetailViewModel(id);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy lớp";
                    return RedirectToRoute("LecturerScores", new { action = "MyClasses" });
                }

                var csv = new System.Text.StringBuilder();
                csv.AppendLine("STT,MSSV,Họ tên,Điểm,Xếp loại,Trạng thái,Số hoạt động");

                int stt = 1;
                foreach (var student in viewModel.Students)
                {
                    csv.AppendLine($"{stt},{student.StudentCode},{student.StudentName},{student.TotalScore},{student.Classification},{student.Status},{student.ActivityCount}");
                    stt++;
                }

                byte[] buffer = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
                return File(buffer, "text/csv", $"DiemLop_{viewModel.ClassCode}_{viewModel.TermName.Replace(" ", "_")}.csv");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xuất CSV: " + ex.Message;
                return RedirectToRoute("LecturerScores", new { action = "MyClasses" });
            }
        }

        // POST: Lecturer/Scores/UpdateScore
        // Cho phép CVHT hoặc người có quyền EDIT/APPROVE sửa điểm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateScore(int scoreId, int newScore, string reason)
        {
            var authCheck = CheckAuth();
            if (authCheck != null)
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                string mand = GetCurrentUserId();

                // Lấy class_id từ score để kiểm tra quyền
                string getClassQuery = @"SELECT s.CLASS_ID FROM SCORES sc 
                                        INNER JOIN STUDENTS s ON sc.STUDENT_ID = s.USER_ID 
                                        WHERE sc.ID = :ScoreId";
                var getClassParams = new[] { OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Int32, scoreId) };
                object classIdObj = OracleDbHelper.ExecuteScalar(getClassQuery, getClassParams);

                if (classIdObj == null || classIdObj == DBNull.Value)
                {
                    return Json(new { success = false, message = "Không tìm thấy điểm" });
                }

                string classId = classIdObj.ToString();
                bool isShared;
                string sharedByName, permissionType;

                if (!HasClassAccess(mand, classId, out isShared, out sharedByName, out permissionType))
                {
                    return Json(new { success = false, message = "Không có quyền truy cập lớp này" });
                }

                // Kiểm tra quyền EDIT (CVHT hoặc có quyền EDIT/APPROVE)
                bool canEdit = !isShared || permissionType == "EDIT" || permissionType == "APPROVE";
                if (!canEdit)
                {
                    return Json(new { success = false, message = "Bạn chỉ có quyền xem, không được chỉnh sửa" });
                }

                // Lấy điểm cũ
                string getOldScoreQuery = "SELECT TOTAL_SCORE FROM SCORES WHERE ID = :ScoreId";
                object oldScoreObj = OracleDbHelper.ExecuteScalar(getOldScoreQuery, getClassParams);
                int oldScore = oldScoreObj != null && oldScoreObj != DBNull.Value ? Convert.ToInt32(oldScoreObj) : 0;

                // Cập nhật điểm và classification
                string classification = GetClassification(newScore);
                string updateQuery = @"UPDATE SCORES
                                      SET TOTAL_SCORE = :NewScore,
                                          CLASSIFICATION = :Classification,
                                          STATUS = 'PROVISIONAL'
                                      WHERE ID = :ScoreId";

                var updateParams = new[]
                {
                    OracleDbHelper.CreateParameter("NewScore", OracleDbType.Int32, newScore),
                    OracleDbHelper.CreateParameter("Classification", OracleDbType.Varchar2, classification),
                    OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Int32, scoreId)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, updateParams);

                if (result > 0)
                {
                    // Thêm vào lịch sử
                    string historyId = "SH" + DateTime.Now.ToString("yyyyMMddHHmmss") + scoreId;
                    string insertHistoryQuery = @"INSERT INTO SCORE_HISTORY
                                                 (ID, SCORE_ID, ACTION, OLD_VALUE, NEW_VALUE,
                                                  CHANGED_BY, REASON, CHANGED_AT)
                                                 VALUES
                                                 (:Id, :ScoreId, 'UPDATE', :OldValue, :NewValue,
                                                  :ChangedBy, :Reason, SYSDATE)";

                    var historyParams = new[]
                    {
                        OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, historyId),
                        OracleDbHelper.CreateParameter("ScoreId", OracleDbType.Int32, scoreId),
                        OracleDbHelper.CreateParameter("OldValue", OracleDbType.Varchar2, oldScore.ToString()),
                        OracleDbHelper.CreateParameter("NewValue", OracleDbType.Varchar2, newScore.ToString()),
                        OracleDbHelper.CreateParameter("ChangedBy", OracleDbType.Varchar2, mand),
                        OracleDbHelper.CreateParameter("Reason", OracleDbType.Varchar2,
                            string.IsNullOrEmpty(reason) ? (object)DBNull.Value : reason)
                    };

                    OracleDbHelper.ExecuteNonQuery(insertHistoryQuery, historyParams);

                    return Json(new { success = true, message = "Cập nhật điểm thành công", newClassification = classification });
                }

                return Json(new { success = false, message = "Không thể cập nhật điểm" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Lecturer/Scores/SubmitClassScores
        // Chốt danh sách điểm lớp để gửi cho Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SubmitClassScores(string classId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null)
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                string mand = GetCurrentUserId();
                bool isShared;
                string sharedByName, permissionType;

                if (!HasClassAccess(mand, classId, out isShared, out sharedByName, out permissionType))
                {
                    return Json(new { success = false, message = "Không có quyền truy cập lớp này" });
                }

                // Kiểm tra quyền APPROVE (CVHT hoặc có quyền APPROVE)
                bool canApprove = !isShared || permissionType == "APPROVE";
                if (!canApprove)
                {
                    return Json(new { success = false, message = "Bạn không có quyền chốt danh sách điểm" });
                }

                // Lấy term hiện tại
                string termQuery = "SELECT ID FROM TERMS WHERE IS_CURRENT = 1 FETCH FIRST 1 ROWS ONLY";
                object termIdObj = OracleDbHelper.ExecuteScalar(termQuery, null);
                if (termIdObj == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy học kỳ hiện tại" });
                }
                string termId = termIdObj.ToString();

                // Cập nhật status tất cả điểm trong lớp thành DRAFT_PUBLISHED
                string updateQuery = @"UPDATE SCORES sc
                                      SET sc.STATUS = 'DRAFT_PUBLISHED'
                                      WHERE sc.TERM_ID = :TermId
                                      AND sc.STATUS = 'PROVISIONAL'
                                      AND sc.STUDENT_ID IN (SELECT USER_ID FROM STUDENTS WHERE CLASS_ID = :ClassId)";

                var updateParams = new[]
                {
                    OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId),
                    OracleDbHelper.CreateParameter("ClassId", OracleDbType.Varchar2, classId)
                };

                int result = OracleDbHelper.ExecuteNonQuery(updateQuery, updateParams);

                return Json(new { success = true, message = $"Đã chốt {result} điểm và gửi cho Admin xem xét" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        #region Helper Methods

        /// <summary>
        /// Check if lecturer has access to view scores for a class.
        /// Checks both direct assignment (CVHT) and shared permissions.
        /// </summary>
        private bool HasClassAccess(string lecturerId, string classId, out bool isShared, out string sharedByName, out string permissionType)
        {
            isShared = false;
            sharedByName = null;
            permissionType = null;

            // Kiểm tra CVHT
            string assignQuery = @"SELECT COUNT(*) FROM CLASS_LECTURER_ASSIGNMENTS 
                                  WHERE LECTURER_ID = :LecturerId AND CLASS_ID = :ClassId AND IS_ACTIVE = 1";
            var assignParams = new[]
            {
                OracleDbHelper.CreateParameter("LecturerId", OracleDbType.Varchar2, lecturerId),
                OracleDbHelper.CreateParameter("ClassId", OracleDbType.Varchar2, classId)
            };
            if (Convert.ToInt32(OracleDbHelper.ExecuteScalar(assignQuery, assignParams)) > 0)
            {
                return true;
            }

            // Kiểm tra quyền chia sẻ
            string sharedQuery = @"SELECT csp.PERMISSION_TYPE, u.FULL_NAME as GRANTED_BY_NAME
                                  FROM CLASS_SCORE_PERMISSIONS csp
                                  INNER JOIN USERS u ON csp.GRANTED_BY = u.MAND
                                  WHERE csp.GRANTED_TO = :LecturerId 
                                    AND csp.CLASS_ID = :ClassId 
                                    AND csp.IS_ACTIVE = 1 
                                    AND csp.REVOKED_AT IS NULL
                                    AND (csp.EXPIRES_AT IS NULL OR csp.EXPIRES_AT > SYSTIMESTAMP)
                                  FETCH FIRST 1 ROWS ONLY";
            var sharedParams = new[]
            {
                OracleDbHelper.CreateParameter("LecturerId", OracleDbType.Varchar2, lecturerId),
                OracleDbHelper.CreateParameter("ClassId", OracleDbType.Varchar2, classId)
            };
            DataTable sharedDt = OracleDbHelper.ExecuteQuery(sharedQuery, sharedParams);
            if (sharedDt.Rows.Count > 0)
            {
                isShared = true;
                permissionType = sharedDt.Rows[0]["PERMISSION_TYPE"].ToString();
                sharedByName = sharedDt.Rows[0]["GRANTED_BY_NAME"].ToString();
                return true;
            }

            return false;
        }

        private MyClassScoresViewModel GetMyClassScoresViewModel(string lecturerId)
        {
            var viewModel = new MyClassScoresViewModel();

            // Get current term
            string termQuery = "SELECT ID, NAME FROM TERMS WHERE IS_CURRENT = 1 FETCH FIRST 1 ROWS ONLY";
            DataTable termDt = OracleDbHelper.ExecuteQuery(termQuery);
            if (termDt.Rows.Count > 0)
            {
                viewModel.CurrentTermId = termDt.Rows[0]["ID"].ToString();
                viewModel.CurrentTermName = termDt.Rows[0]["NAME"].ToString();
            }

            // Get assigned classes (CVHT)
            string classQuery = @"SELECT c.ID, c.CODE, c.NAME, d.NAME as DEPT_NAME,
                                        cla.ASSIGNED_AT,
                                        (SELECT COUNT(*) FROM STUDENTS WHERE CLASS_ID = c.ID) as TOTAL_STUDENTS,
                                        (SELECT COUNT(*) FROM SCORES sc 
                                         INNER JOIN STUDENTS s ON sc.STUDENT_ID = s.USER_ID 
                                         WHERE s.CLASS_ID = c.ID AND sc.TERM_ID = :TermId) as STUDENTS_WITH_SCORES,
                                        (SELECT NVL(AVG(sc.TOTAL_SCORE), 0) FROM SCORES sc 
                                         INNER JOIN STUDENTS s ON sc.STUDENT_ID = s.USER_ID 
                                         WHERE s.CLASS_ID = c.ID AND sc.TERM_ID = :TermId) as AVG_SCORE
                                 FROM CLASSES c
                                 INNER JOIN CLASS_LECTURER_ASSIGNMENTS cla ON c.ID = cla.CLASS_ID
                                 LEFT JOIN DEPARTMENTS d ON c.DEPARTMENT_ID = d.ID
                                 WHERE cla.LECTURER_ID = :LecturerId AND cla.IS_ACTIVE = 1
                                 ORDER BY c.CODE";

            var parameters = new[]
            {
                OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, viewModel.CurrentTermId ?? ""),
                OracleDbHelper.CreateParameter("LecturerId", OracleDbType.Varchar2, lecturerId)
            };

            DataTable classDt = OracleDbHelper.ExecuteQuery(classQuery, parameters);

            foreach (DataRow row in classDt.Rows)
            {
                viewModel.Classes.Add(new AssignedClassItem
                {
                    ClassId = row["ID"].ToString(),
                    ClassCode = row["CODE"].ToString(),
                    ClassName = row["NAME"].ToString(),
                    DepartmentName = row["DEPT_NAME"] != DBNull.Value ? row["DEPT_NAME"].ToString() : "",
                    TotalStudents = Convert.ToInt32(row["TOTAL_STUDENTS"]),
                    StudentsWithScores = Convert.ToInt32(row["STUDENTS_WITH_SCORES"]),
                    AverageScore = row["AVG_SCORE"] != DBNull.Value ? Convert.ToDouble(row["AVG_SCORE"]) : 0,
                    AssignedAt = Convert.ToDateTime(row["ASSIGNED_AT"])
                });
            }

            // Get shared classes (via CLASS_SCORE_PERMISSIONS)
            string sharedQuery = @"SELECT c.ID, c.CODE, c.NAME, d.NAME as DEPT_NAME,
                                          csp.PERMISSION_TYPE, csp.GRANTED_AT, csp.EXPIRES_AT,
                                          csp.GRANTED_BY, u_by.FULL_NAME as GRANTED_BY_NAME,
                                          (SELECT COUNT(*) FROM STUDENTS WHERE CLASS_ID = c.ID) as TOTAL_STUDENTS,
                                          (SELECT COUNT(*) FROM SCORES sc 
                                           INNER JOIN STUDENTS s ON sc.STUDENT_ID = s.USER_ID 
                                           WHERE s.CLASS_ID = c.ID AND sc.TERM_ID = :TermId) as STUDENTS_WITH_SCORES,
                                          (SELECT NVL(AVG(sc.TOTAL_SCORE), 0) FROM SCORES sc 
                                           INNER JOIN STUDENTS s ON sc.STUDENT_ID = s.USER_ID 
                                           WHERE s.CLASS_ID = c.ID AND sc.TERM_ID = :TermId) as AVG_SCORE
                                   FROM CLASS_SCORE_PERMISSIONS csp
                                   INNER JOIN CLASSES c ON csp.CLASS_ID = c.ID
                                   INNER JOIN USERS u_by ON csp.GRANTED_BY = u_by.MAND
                                   LEFT JOIN DEPARTMENTS d ON c.DEPARTMENT_ID = d.ID
                                   WHERE csp.GRANTED_TO = :LecturerId 
                                     AND csp.IS_ACTIVE = 1
                                     AND csp.REVOKED_AT IS NULL
                                     AND (csp.EXPIRES_AT IS NULL OR csp.EXPIRES_AT > SYSTIMESTAMP)
                                   ORDER BY csp.GRANTED_AT DESC";

            var sharedParams = new[]
            {
                OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, viewModel.CurrentTermId ?? ""),
                OracleDbHelper.CreateParameter("LecturerId", OracleDbType.Varchar2, lecturerId)
            };

            DataTable sharedDt = OracleDbHelper.ExecuteQuery(sharedQuery, sharedParams);

            foreach (DataRow row in sharedDt.Rows)
            {
                viewModel.SharedClasses.Add(new SharedClassItem
                {
                    ClassId = row["ID"].ToString(),
                    ClassCode = row["CODE"].ToString(),
                    ClassName = row["NAME"].ToString(),
                    DepartmentName = row["DEPT_NAME"] != DBNull.Value ? row["DEPT_NAME"].ToString() : "",
                    TotalStudents = Convert.ToInt32(row["TOTAL_STUDENTS"]),
                    StudentsWithScores = Convert.ToInt32(row["STUDENTS_WITH_SCORES"]),
                    AverageScore = row["AVG_SCORE"] != DBNull.Value ? Convert.ToDouble(row["AVG_SCORE"]) : 0,
                    PermissionType = row["PERMISSION_TYPE"].ToString(),
                    GrantedById = row["GRANTED_BY"].ToString(),
                    GrantedByName = row["GRANTED_BY_NAME"].ToString(),
                    GrantedAt = Convert.ToDateTime(row["GRANTED_AT"]),
                    ExpiresAt = row["EXPIRES_AT"] != DBNull.Value ? Convert.ToDateTime(row["EXPIRES_AT"]) : (DateTime?)null
                });
            }

            return viewModel;
        }

        private LecturerClassScoreDetailViewModel GetClassScoreDetailViewModel(string classId)
        {
            // Get class info
            string classQuery = @"SELECT c.ID, c.CODE, c.NAME, d.NAME as DEPT_NAME,
                                        t.ID as TERM_ID, t.NAME as TERM_NAME
                                 FROM CLASSES c
                                 LEFT JOIN DEPARTMENTS d ON c.DEPARTMENT_ID = d.ID
                                 LEFT JOIN TERMS t ON t.IS_CURRENT = 1
                                 WHERE c.ID = :ClassId";
            var classParams = new[] { OracleDbHelper.CreateParameter("ClassId", OracleDbType.Varchar2, classId) };
            DataTable classDt = OracleDbHelper.ExecuteQuery(classQuery, classParams);

            if (classDt.Rows.Count == 0) return null;

            DataRow classRow = classDt.Rows[0];
            string termId = classRow["TERM_ID"] != DBNull.Value ? classRow["TERM_ID"].ToString() : null;

            var viewModel = new LecturerClassScoreDetailViewModel
            {
                ClassId = classId,
                ClassCode = classRow["CODE"].ToString(),
                ClassName = classRow["NAME"].ToString(),
                DepartmentName = classRow["DEPT_NAME"] != DBNull.Value ? classRow["DEPT_NAME"].ToString() : "",
                TermId = termId,
                TermName = classRow["TERM_NAME"] != DBNull.Value ? classRow["TERM_NAME"].ToString() : ""
            };

            if (string.IsNullOrEmpty(termId))
            {
                viewModel.Statistics = new ClassScoreStatisticsReadOnly();
                return viewModel;
            }

            // Get students and scores (including SCORE_ID for edit/approve)
            string studentsQuery = @"SELECT s.USER_ID, s.STUDENT_CODE, u.FULL_NAME,
                                           sc.ID as SCORE_ID, sc.TOTAL_SCORE, sc.CLASSIFICATION, sc.STATUS,
                                           (SELECT COUNT(*) FROM REGISTRATIONS r
                                            INNER JOIN ACTIVITIES a ON r.ACTIVITY_ID = a.ID
                                            WHERE r.STUDENT_ID = s.USER_ID
                                            AND a.TERM_ID = :TermId
                                            AND r.STATUS = 'CHECKED_IN') as ACTIVITY_COUNT
                                    FROM STUDENTS s
                                    INNER JOIN USERS u ON s.USER_ID = u.MAND
                                    LEFT JOIN SCORES sc ON s.USER_ID = sc.STUDENT_ID AND sc.TERM_ID = :TermId
                                    WHERE s.CLASS_ID = :ClassId
                                    ORDER BY s.STUDENT_CODE";

            var studentsParams = new[]
            {
                OracleDbHelper.CreateParameter("TermId", OracleDbType.Varchar2, termId),
                OracleDbHelper.CreateParameter("ClassId", OracleDbType.Varchar2, classId)
            };

            DataTable studentsDt = OracleDbHelper.ExecuteQuery(studentsQuery, studentsParams);

            int excellentCount = 0, goodCount = 0, fairCount = 0, averageCount = 0, weakCount = 0;
            int approvedCount = 0;
            int submittedCount = 0;

            foreach (DataRow row in studentsDt.Rows)
            {
                int totalScore = row["TOTAL_SCORE"] != DBNull.Value ? Convert.ToInt32(row["TOTAL_SCORE"]) : 0;
                string status = row["STATUS"] != DBNull.Value ? row["STATUS"].ToString() : "N/A";
                string classification = row["CLASSIFICATION"] != DBNull.Value 
                    ? row["CLASSIFICATION"].ToString() 
                    : GetClassification(totalScore);

                viewModel.Students.Add(new StudentScoreReadOnlyItem
                {
                    ScoreId = row["SCORE_ID"] != DBNull.Value ? Convert.ToInt32(row["SCORE_ID"]) : 0,
                    StudentId = row["USER_ID"].ToString(),
                    StudentCode = row["STUDENT_CODE"].ToString(),
                    StudentName = row["FULL_NAME"].ToString(),
                    TotalScore = totalScore,
                    Classification = classification,
                    Status = status,
                    ActivityCount = Convert.ToInt32(row["ACTIVITY_COUNT"])
                });

                if (status == "APPROVED" || status == "OFFICIAL") approvedCount++;
                if (status == "DRAFT_PUBLISHED") submittedCount++;

                if (totalScore >= 90) excellentCount++;
                else if (totalScore >= 80) goodCount++;
                else if (totalScore >= 65) fairCount++;
                else if (totalScore >= 50) averageCount++;
                else weakCount++;
            }

            // Check if all scores are submitted
            viewModel.IsSubmitted = submittedCount > 0 && submittedCount == viewModel.Students.Count;

            viewModel.Statistics = new ClassScoreStatisticsReadOnly
            {
                TotalStudents = viewModel.Students.Count,
                ApprovedStudents = approvedCount,
                AverageScore = viewModel.Students.Count > 0 ? viewModel.Students.Average(x => x.TotalScore) : 0,
                HighestScore = viewModel.Students.Count > 0 ? viewModel.Students.Max(x => x.TotalScore) : 0,
                LowestScore = viewModel.Students.Count > 0 ? viewModel.Students.Min(x => x.TotalScore) : 0,
                ExcellentCount = excellentCount,
                GoodCount = goodCount,
                FairCount = fairCount,
                AverageCount = averageCount,
                WeakCount = weakCount
            };

            return viewModel;
        }

        private string GetClassification(int score)
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
