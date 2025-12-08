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

                if (!IsAssignedToClass(mand, id))
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
                if (!IsAssignedToClass(mand, id))
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

        #region Helper Methods

        private bool IsAssignedToClass(string lecturerId, string classId)
        {
            string query = @"SELECT COUNT(*) FROM CLASS_LECTURER_ASSIGNMENTS 
                            WHERE LECTURER_ID = :LecturerId AND CLASS_ID = :ClassId AND IS_ACTIVE = 1";
            var parameters = new[]
            {
                OracleDbHelper.CreateParameter("LecturerId", OracleDbType.Varchar2, lecturerId),
                OracleDbHelper.CreateParameter("ClassId", OracleDbType.Varchar2, classId)
            };
            return Convert.ToInt32(OracleDbHelper.ExecuteScalar(query, parameters)) > 0;
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

            // Get assigned classes
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

            // Get students and scores
            string studentsQuery = @"SELECT s.USER_ID, s.STUDENT_CODE, u.FULL_NAME,
                                           sc.TOTAL_SCORE, sc.CLASSIFICATION, sc.STATUS,
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

            foreach (DataRow row in studentsDt.Rows)
            {
                int totalScore = row["TOTAL_SCORE"] != DBNull.Value ? Convert.ToInt32(row["TOTAL_SCORE"]) : 0;
                string status = row["STATUS"] != DBNull.Value ? row["STATUS"].ToString() : "N/A";
                string classification = row["CLASSIFICATION"] != DBNull.Value 
                    ? row["CLASSIFICATION"].ToString() 
                    : GetClassification(totalScore);

                viewModel.Students.Add(new StudentScoreReadOnlyItem
                {
                    StudentId = row["USER_ID"].ToString(),
                    StudentCode = row["STUDENT_CODE"].ToString(),
                    StudentName = row["FULL_NAME"].ToString(),
                    TotalScore = totalScore,
                    Classification = classification,
                    Status = status,
                    ActivityCount = Convert.ToInt32(row["ACTIVITY_COUNT"])
                });

                if (status == "APPROVED" || status == "OFFICIAL") approvedCount++;

                if (totalScore >= 90) excellentCount++;
                else if (totalScore >= 80) goodCount++;
                else if (totalScore >= 65) fairCount++;
                else if (totalScore >= 50) averageCount++;
                else weakCount++;
            }

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
