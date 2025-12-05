using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;

namespace QuanLyDiemRenLuyen.Controllers.Admin
{
    public class ClassesController : AdminBaseController
    {
        // GET: Admin/Classes
        public ActionResult Index(string search, string departmentId, int page = 1)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = new ClassIndexViewModel
                {
                    SearchKeyword = search,
                    FilterDepartmentId = departmentId,
                    CurrentPage = page,
                    PageSize = 20,
                    Departments = GetDepartmentsList()
                };

                viewModel.Classes = GetClassesList(search, departmentId, page, viewModel.PageSize, out int totalCount);
                viewModel.TotalPages = (int)Math.Ceiling((double)totalCount / viewModel.PageSize);

                return View("~/Views/Admin/Classes/Index.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/Classes/Index.cshtml", new ClassIndexViewModel { Classes = new List<ClassViewModel>() });
            }
        }

        // GET: Admin/Classes/Create
        public ActionResult Create()
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            ViewBag.Departments = GetDepartmentsList();
            return View("~/Views/Admin/Classes/Create.cshtml");
        }

        // POST: Admin/Classes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ClassViewModel model)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = GetDepartmentsList();
                return View("~/Views/Admin/Classes/Create.cshtml", model);
            }

            try
            {
                // Check duplicate code
                string checkQuery = "SELECT COUNT(*) FROM CLASSES WHERE CODE = :Code";
                var checkParams = new[] { OracleDbHelper.CreateParameter("Code", OracleDbType.Varchar2, model.Code) };
                int count = Convert.ToInt32(OracleDbHelper.ExecuteScalar(checkQuery, checkParams));

                if (count > 0)
                {
                    ModelState.AddModelError("Code", "Mã lớp đã tồn tại");
                    ViewBag.Departments = GetDepartmentsList();
                    return View("~/Views/Admin/Classes/Create.cshtml", model);
                }

                string insertQuery = @"INSERT INTO CLASSES (CODE, NAME, DEPARTMENT_ID)
                                       VALUES (:Code, :Name, :DepartmentId)";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("Code", OracleDbType.Varchar2, model.Code),
                    OracleDbHelper.CreateParameter("Name", OracleDbType.Varchar2, model.Name),
                    OracleDbHelper.CreateParameter("DepartmentId", OracleDbType.Varchar2, model.DepartmentId)
                };

                OracleDbHelper.ExecuteNonQuery(insertQuery, parameters);
                TempData["SuccessMessage"] = "Thêm lớp thành công";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi: " + ex.Message;
                ViewBag.Departments = GetDepartmentsList();
                return View("~/Views/Admin/Classes/Create.cshtml", model);
            }
        }

        // GET: Admin/Classes/Edit/id
        public ActionResult Edit(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string query = "SELECT * FROM CLASSES WHERE ID = :Id";
                var parameters = new[] { OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id) };
                DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);

                if (dt.Rows.Count == 0)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy lớp";
                    return RedirectToAction("Index");
                }

                DataRow row = dt.Rows[0];
                var model = new ClassViewModel
                {
                    Id = row["ID"].ToString(),
                    Code = row["CODE"].ToString(),
                    Name = row["NAME"].ToString(),
                    DepartmentId = row["DEPARTMENT_ID"] != DBNull.Value ? row["DEPARTMENT_ID"].ToString() : ""
                };

                ViewBag.Departments = GetDepartmentsList();
                return View("~/Views/Admin/Classes/Edit.cshtml", model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Admin/Classes/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ClassViewModel model)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = GetDepartmentsList();
                return View("~/Views/Admin/Classes/Edit.cshtml", model);
            }

            try
            {
                // Check duplicate code (excluding current id)
                string checkQuery = "SELECT COUNT(*) FROM CLASSES WHERE CODE = :Code AND ID != :Id";
                var checkParams = new[]
                {
                    OracleDbHelper.CreateParameter("Code", OracleDbType.Varchar2, model.Code),
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, model.Id)
                };
                int count = Convert.ToInt32(OracleDbHelper.ExecuteScalar(checkQuery, checkParams));

                if (count > 0)
                {
                    ModelState.AddModelError("Code", "Mã lớp đã tồn tại");
                    ViewBag.Departments = GetDepartmentsList();
                    return View("~/Views/Admin/Classes/Edit.cshtml", model);
                }

                string updateQuery = @"UPDATE CLASSES
                                       SET CODE = :Code,
                                           NAME = :Name,
                                           DEPARTMENT_ID = :DepartmentId
                                       WHERE ID = :Id";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("Code", OracleDbType.Varchar2, model.Code),
                    OracleDbHelper.CreateParameter("Name", OracleDbType.Varchar2, model.Name),
                    OracleDbHelper.CreateParameter("DepartmentId", OracleDbType.Varchar2, model.DepartmentId),
                    OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, model.Id)
                };

                OracleDbHelper.ExecuteNonQuery(updateQuery, parameters);
                TempData["SuccessMessage"] = "Cập nhật lớp thành công";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi: " + ex.Message;
                ViewBag.Departments = GetDepartmentsList();
                return View("~/Views/Admin/Classes/Edit.cshtml", model);
            }
        }

        // POST: Admin/Classes/Delete/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                // Check for students
                string checkQuery = "SELECT COUNT(*) FROM STUDENTS WHERE CLASS_ID = :Id";
                var checkParams = new[] { OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id) };
                int studentCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(checkQuery, checkParams));

                if (studentCount > 0)
                {
                    TempData["ErrorMessage"] = $"Không thể xóa lớp này vì đang có {studentCount} sinh viên.";
                    return RedirectToAction("Index");
                }

                string deleteQuery = "DELETE FROM CLASSES WHERE ID = :Id";
                var parameters = new[] { OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id) };
                OracleDbHelper.ExecuteNonQuery(deleteQuery, parameters);

                TempData["SuccessMessage"] = "Xóa lớp thành công";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: Admin/Classes/Details/id
        public ActionResult Details(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                // Get Class Info
                string classQuery = @"SELECT c.ID, c.CODE, c.NAME, c.DEPARTMENT_ID, d.NAME as DEPARTMENT_NAME,
                                     (SELECT COUNT(*) FROM STUDENTS s WHERE s.CLASS_ID = c.ID) as STUDENT_COUNT
                                     FROM CLASSES c
                                     LEFT JOIN DEPARTMENTS d ON c.DEPARTMENT_ID = d.ID
                                     WHERE c.ID = :Id";
                var classParams = new[] { OracleDbHelper.CreateParameter("Id", OracleDbType.Varchar2, id) };
                DataTable dtClass = OracleDbHelper.ExecuteQuery(classQuery, classParams);

                if (dtClass.Rows.Count == 0)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy lớp";
                    return RedirectToAction("Index");
                }

                DataRow row = dtClass.Rows[0];
                var classInfo = new ClassViewModel
                {
                    Id = row["ID"].ToString(),
                    Code = row["CODE"].ToString(),
                    Name = row["NAME"].ToString(),
                    DepartmentId = row["DEPARTMENT_ID"] != DBNull.Value ? row["DEPARTMENT_ID"].ToString() : "",
                    DepartmentName = row["DEPARTMENT_NAME"] != DBNull.Value ? row["DEPARTMENT_NAME"].ToString() : "",
                    StudentCount = Convert.ToInt32(row["STUDENT_COUNT"])
                };

                // Get Students List
                var students = new List<ClassStudentItem>();
                string studentQuery = @"SELECT u.MAND, u.FULL_NAME, u.EMAIL, s.DATE_OF_BIRTH, s.GENDER, s.PHONE, s.STUDENT_CODE
                                        FROM STUDENTS s
                                        JOIN USERS u ON s.USER_ID = u.MAND
                                        WHERE s.CLASS_ID = :ClassId
                                        ORDER BY s.STUDENT_CODE";
                var studentParams = new[] { OracleDbHelper.CreateParameter("ClassId", OracleDbType.Varchar2, id) };
                DataTable dtStudents = OracleDbHelper.ExecuteQuery(studentQuery, studentParams);

                foreach (DataRow sRow in dtStudents.Rows)
                {
                    students.Add(new ClassStudentItem
                    {
                        Id = sRow["MAND"].ToString(),
                        StudentCode = sRow["STUDENT_CODE"].ToString(),
                        FullName = sRow["FULL_NAME"].ToString(),
                        Email = sRow["EMAIL"] != DBNull.Value ? sRow["EMAIL"].ToString() : "",
                        DateOfBirth = sRow["DATE_OF_BIRTH"] != DBNull.Value ? Convert.ToDateTime(sRow["DATE_OF_BIRTH"]) : (DateTime?)null,
                        Gender = sRow["GENDER"] != DBNull.Value ? sRow["GENDER"].ToString() : "",
                        Phone = sRow["PHONE"] != DBNull.Value ? sRow["PHONE"].ToString() : ""
                    });
                }

                // Get CVHT Info
                ClassAdvisorInfo advisorInfo = null;
                string cvhtQuery = @"SELECT cl.ID as ASSIGNMENT_ID, cl.LECTURER_ID, u.FULL_NAME, u.EMAIL,
                                     cl.ASSIGNED_AT as ASSIGNED_DATE, cl.ASSIGNED_BY
                                     FROM CLASS_LECTURER_ASSIGNMENTS cl
                                     JOIN USERS u ON cl.LECTURER_ID = u.MAND
                                     WHERE cl.CLASS_ID = :ClassId AND cl.IS_ACTIVE = 1";
                var cvhtParams = new[] { OracleDbHelper.CreateParameter("ClassId", OracleDbType.Varchar2, id) };
                DataTable dtCvht = OracleDbHelper.ExecuteQuery(cvhtQuery, cvhtParams);

                if (dtCvht.Rows.Count > 0)
                {
                    DataRow cvhtRow = dtCvht.Rows[0];
                    advisorInfo = new ClassAdvisorInfo
                    {
                        AssignmentId = cvhtRow["ASSIGNMENT_ID"].ToString(),
                        ClassId = id,
                        ClassName = classInfo.Name,
                        ClassCode = classInfo.Code,
                        LecturerId = cvhtRow["LECTURER_ID"].ToString(),
                        LecturerName = cvhtRow["FULL_NAME"].ToString(),
                        LecturerEmail = cvhtRow["EMAIL"] != DBNull.Value ? cvhtRow["EMAIL"].ToString() : "",
                        StudentCount = classInfo.StudentCount,
                        AssignedDate = cvhtRow["ASSIGNED_DATE"] != DBNull.Value ? Convert.ToDateTime(cvhtRow["ASSIGNED_DATE"]) : (DateTime?)null,
                        AssignedBy = cvhtRow["ASSIGNED_BY"] != DBNull.Value ? cvhtRow["ASSIGNED_BY"].ToString() : ""
                    };
                }

                // Get Assignment History
                var assignmentHistory = new List<ClassAdvisorHistoryItem>();
                string historyQuery = @"SELECT h.ID, h.LECTURER_ID, u.FULL_NAME, h.ASSIGNED_AT, h.REMOVED_AT,
                                        h.ASSIGNED_BY, h.REMOVED_BY, h.NOTES
                                        FROM CLASS_LECTURER_HISTORY h
                                        JOIN USERS u ON h.LECTURER_ID = u.MAND
                                        WHERE h.CLASS_ID = :ClassId
                                        ORDER BY h.ASSIGNED_AT DESC";
                var historyParams = new[] { OracleDbHelper.CreateParameter("ClassId", OracleDbType.Varchar2, id) };
                DataTable dtHistory = OracleDbHelper.ExecuteQuery(historyQuery, historyParams);

                foreach (DataRow hRow in dtHistory.Rows)
                {
                    assignmentHistory.Add(new ClassAdvisorHistoryItem
                    {
                        Id = hRow["ID"].ToString(),
                        LecturerId = hRow["LECTURER_ID"].ToString(),
                        LecturerName = hRow["FULL_NAME"].ToString(),
                        AssignedAt = Convert.ToDateTime(hRow["ASSIGNED_AT"]),
                        RemovedAt = hRow["REMOVED_AT"] != DBNull.Value ? Convert.ToDateTime(hRow["REMOVED_AT"]) : (DateTime?)null,
                        AssignedBy = hRow["ASSIGNED_BY"] != DBNull.Value ? hRow["ASSIGNED_BY"].ToString() : "",
                        RemovedBy = hRow["REMOVED_BY"] != DBNull.Value ? hRow["REMOVED_BY"].ToString() : "",
                        Notes = hRow["NOTES"] != DBNull.Value ? hRow["NOTES"].ToString() : ""
                    });
                }

                // Get lecturers list for assignment modal
                var lecturers = new List<SelectListItem>();
                string lecturerQuery = "SELECT MAND, FULL_NAME FROM USERS WHERE ROLE_NAME = 'LECTURER' AND IS_ACTIVE = 1 ORDER BY FULL_NAME";
                DataTable dtLecturers = OracleDbHelper.ExecuteQuery(lecturerQuery, null);
                foreach (DataRow lRow in dtLecturers.Rows)
                {
                    lecturers.Add(new SelectListItem
                    {
                        Value = lRow["MAND"].ToString(),
                        Text = lRow["FULL_NAME"].ToString()
                    });
                }

                ViewBag.Lecturers = lecturers;

                var viewModel = new ClassDetailsViewModel
                {
                    ClassInfo = classInfo,
                    Students = students,
                    AdvisorInfo = advisorInfo,
                    AssignmentHistory = assignmentHistory
                };

                return View("~/Views/Admin/Classes/Details.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        #region Helper Methods

        private List<SelectListItem> GetDepartmentsList()
        {
            var list = new List<SelectListItem>();
            try
            {
                string query = "SELECT ID, NAME FROM DEPARTMENTS ORDER BY NAME";
                DataTable dt = OracleDbHelper.ExecuteQuery(query, null);
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new SelectListItem
                    {
                        Value = row["ID"].ToString(),
                        Text = row["NAME"].ToString()
                    });
                }
            }
            catch { }
            return list;
        }

        private List<ClassViewModel> GetClassesList(string search, string departmentId, int page, int pageSize, out int totalCount)
        {
            var classes = new List<ClassViewModel>();
            totalCount = 0;

            try
            {
                string countQuery = "SELECT COUNT(*) FROM CLASSES c WHERE 1=1";
                string dataQuery = @"SELECT c.ID, c.CODE, c.NAME, c.DEPARTMENT_ID, d.NAME as DEPARTMENT_NAME,
                                     (SELECT COUNT(*) FROM STUDENTS s WHERE s.CLASS_ID = c.ID) as STUDENT_COUNT,
                                     cl.LECTURER_ID as CVHT_ID,
                                     u.FULL_NAME as CVHT_NAME,
                                     u.EMAIL as CVHT_EMAIL,
                                     cl.ASSIGNED_AT as CVHT_ASSIGNED_DATE
                                     FROM CLASSES c
                                     LEFT JOIN DEPARTMENTS d ON c.DEPARTMENT_ID = d.ID
                                     LEFT JOIN CLASS_LECTURER_ASSIGNMENTS cl ON c.ID = cl.CLASS_ID AND cl.IS_ACTIVE = 1
                                     LEFT JOIN USERS u ON cl.LECTURER_ID = u.MAND
                                     WHERE 1=1";

                var parameters = new List<OracleParameter>();

                if (!string.IsNullOrEmpty(search))
                {
                    countQuery += " AND (UPPER(c.CODE) LIKE :Search OR UPPER(c.NAME) LIKE :Search)";
                    dataQuery += " AND (UPPER(c.CODE) LIKE :Search OR UPPER(c.NAME) LIKE :Search)";
                    parameters.Add(OracleDbHelper.CreateParameter("Search", OracleDbType.Varchar2, "%" + search.ToUpper() + "%"));
                }

                if (!string.IsNullOrEmpty(departmentId))
                {
                    countQuery += " AND c.DEPARTMENT_ID = :DepartmentId";
                    dataQuery += " AND c.DEPARTMENT_ID = :DepartmentId";
                    parameters.Add(OracleDbHelper.CreateParameter("DepartmentId", OracleDbType.Varchar2, departmentId));
                }

                totalCount = Convert.ToInt32(OracleDbHelper.ExecuteScalar(countQuery, parameters.ToArray()));

                dataQuery += " ORDER BY c.CODE";
                int offset = (page - 1) * pageSize;
                dataQuery = $@"SELECT * FROM (
                                SELECT a.*, ROWNUM rnum FROM ({dataQuery}) a
                                WHERE ROWNUM <= {offset + pageSize}
                              ) WHERE rnum > {offset}";

                DataTable dt = OracleDbHelper.ExecuteQuery(dataQuery, parameters.ToArray());

                foreach (DataRow row in dt.Rows)
                {
                    classes.Add(new ClassViewModel
                    {
                        Id = row["ID"].ToString(),
                        Code = row["CODE"].ToString(),
                        Name = row["NAME"].ToString(),
                        DepartmentId = row["DEPARTMENT_ID"] != DBNull.Value ? row["DEPARTMENT_ID"].ToString() : "",
                        DepartmentName = row["DEPARTMENT_NAME"] != DBNull.Value ? row["DEPARTMENT_NAME"].ToString() : "",
                        StudentCount = Convert.ToInt32(row["STUDENT_COUNT"]),
                        CvhtId = row["CVHT_ID"] != DBNull.Value ? row["CVHT_ID"].ToString() : null,
                        CvhtName = row["CVHT_NAME"] != DBNull.Value ? row["CVHT_NAME"].ToString() : null,
                        CvhtEmail = row["CVHT_EMAIL"] != DBNull.Value ? row["CVHT_EMAIL"].ToString() : null,
                        CvhtAssignedDate = row["CVHT_ASSIGNED_DATE"] != DBNull.Value ? Convert.ToDateTime(row["CVHT_ASSIGNED_DATE"]) : (DateTime?)null
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return classes;
        }

        #endregion

        #region CVHT Management Actions

        // POST: Admin/Classes/AssignAdvisor (AJAX)
        [HttpPost]
        public JsonResult AssignAdvisor(AssignAdvisorRequest request)
        {
            var authCheck = CheckAuth();
            if (authCheck != null)
                return Json(new { success = false, message = "Unauthorized" });

            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid request data" });

            try
            {
                string currentUser = Session["UserName"]?.ToString();
                string result = CallAssignAdvisorProcedure(request.ClassId, request.LecturerId, currentUser, request.Notes);

                if (result.StartsWith("SUCCESS"))
                {
                    string message = result.Substring(result.IndexOf('|') + 1);
                    return Json(new { success = true, message });
                }
                else
                {
                    string errorMsg = result.StartsWith("ERROR:") ? result.Substring(6) : result;
                    return Json(new { success = false, message = errorMsg });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/Classes/RemoveAdvisor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveAdvisor(string classId, string notes)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string currentUser = Session["UserName"]?.ToString();
                string result = CallRemoveAdvisorProcedure(classId, currentUser, notes);

                if (result.StartsWith("SUCCESS"))
                {
                    TempData["SuccessMessage"] = result.Substring(result.IndexOf('|') + 1);
                }
                else
                {
                    TempData["ErrorMessage"] = result.StartsWith("ERROR:") ? result.Substring(6) : result;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("Details", new { id = classId });
        }

        // GET: Admin/Classes/LecturerWorkload
        public ActionResult LecturerWorkload()
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetLecturerWorkloadData();
                return View("~/Views/Admin/Classes/LecturerWorkload.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi: " + ex.Message;
                return View("~/Views/Admin/Classes/LecturerWorkload.cshtml", new LecturerWorkloadViewModel
                {
                    Lecturers = new List<LecturerWorkload>()
                });
            }
        }

        // GET: Admin/Classes/BulkAssignment
        public ActionResult BulkAssignment(string departmentId, string status = "unassigned")
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetBulkAssignmentData(departmentId, status);
                return View("~/Views/Admin/Classes/BulkAssignment.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi: " + ex.Message;
                return View("~/Views/Admin/Classes/BulkAssignment.cshtml", new BulkAssignmentViewModel
                {
                    Classes = new List<BulkAssignmentItem>(),
                    AvailableLecturers = new List<SelectListItem>(),
                    Departments = GetDepartmentsList()
                });
            }
        }

        // POST: Admin/Classes/BulkAssignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkAssignment(BulkAssignmentRequest request)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string currentUser = Session["UserName"]?.ToString();
                var result = ProcessBulkAssignment(request, currentUser);

                if (result.HasErrors)
                {
                    TempData["WarningMessage"] = result.SummaryMessage;
                    TempData["BulkErrors"] = result.Errors;
                }
                else
                {
                    TempData["SuccessMessage"] = result.SummaryMessage;
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("BulkAssignment");
            }
        }

        #endregion

        #region CVHT Helper Methods

        private string CallAssignAdvisorProcedure(string classId, string lecturerId, string assignedBy, string notes)
        {
            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SP_ASSIGN_CLASS_ADVISOR";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_class_id", OracleDbType.Varchar2).Value = classId;
                    cmd.Parameters.Add("p_lecturer_id", OracleDbType.Varchar2).Value = lecturerId;
                    cmd.Parameters.Add("p_assigned_by", OracleDbType.Varchar2).Value = assignedBy;
                    cmd.Parameters.Add("p_notes", OracleDbType.Varchar2).Value = notes ?? (object)DBNull.Value;

                    var resultParam = cmd.Parameters.Add("p_result", OracleDbType.Varchar2, 1000);
                    resultParam.Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();

                    return resultParam.Value?.ToString() ?? "ERROR: No result returned";
                }
            }
        }

        private string CallRemoveAdvisorProcedure(string classId, string removedBy, string notes)
        {
            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SP_REMOVE_CLASS_ADVISOR";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_class_id", OracleDbType.Varchar2).Value = classId;
                    cmd.Parameters.Add("p_removed_by", OracleDbType.Varchar2).Value = removedBy;
                    cmd.Parameters.Add("p_notes", OracleDbType.Varchar2).Value = notes ?? (object)DBNull.Value;

                    var resultParam = cmd.Parameters.Add("p_result", OracleDbType.Varchar2, 1000);
                    resultParam.Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();

                    return resultParam.Value?.ToString() ?? "ERROR: No result returned";
                }
            }
        }

        private LecturerWorkloadViewModel GetLecturerWorkloadData()
        {
            var lecturers = new List<LecturerWorkload>();

            string query = @"
                SELECT 
                    u.MAND as LECTURER_ID,
                    u.FULL_NAME,
                    u.EMAIL,
                    COUNT(cl.ID) as CLASS_COUNT,
                    COALESCE(SUM((SELECT COUNT(*) FROM STUDENTS WHERE CLASS_ID = c.ID)), 0) as TOTAL_STUDENTS
                FROM USERS u
                LEFT JOIN CLASS_LECTURER_ASSIGNMENTS cl ON u.MAND = cl.LECTURER_ID AND cl.IS_ACTIVE = 1
                LEFT JOIN CLASSES c ON cl.CLASS_ID = c.ID
                WHERE u.ROLE_NAME = 'LECTURER' AND u.IS_ACTIVE = 1
                GROUP BY u.MAND, u.FULL_NAME, u.EMAIL
                ORDER BY CLASS_COUNT DESC, u.FULL_NAME";

            DataTable dt = OracleDbHelper.ExecuteQuery(query, null);

            foreach (DataRow row in dt.Rows)
            {
                lecturers.Add(new LecturerWorkload
                {
                    LecturerId = row["LECTURER_ID"].ToString(),
                    LecturerName = row["FULL_NAME"].ToString(),
                    Email = row["EMAIL"] != DBNull.Value ? row["EMAIL"].ToString() : "",
                    ClassCount = Convert.ToInt32(row["CLASS_COUNT"]),
                    TotalStudents = Convert.ToInt32(row["TOTAL_STUDENTS"]),
                    Classes = new List<ClassViewModel>()
                });
            }

            // Get statistics
            int totalLecturers = lecturers.Count;
            int assignedLecturers = lecturers.Where(l => l.ClassCount > 0).Count();
            int totalClasses = lecturers.Sum(l => l.ClassCount);

            // Count unassigned classes
            string unassignedQuery = @"
                SELECT COUNT(*) FROM CLASSES c
                WHERE NOT EXISTS (SELECT 1 FROM CLASS_LECTURER_ASSIGNMENTS WHERE CLASS_ID = c.ID AND IS_ACTIVE = 1)";
            int unassignedClasses = Convert.ToInt32(OracleDbHelper.ExecuteScalar(unassignedQuery, null));

            return new LecturerWorkloadViewModel
            {
                Lecturers = lecturers,
                TotalLecturers = totalLecturers,
                AssignedLecturers = assignedLecturers,
                UnassignedLecturers = totalLecturers - assignedLecturers,
                TotalClasses = totalClasses,
                UnassignedClasses = unassignedClasses
            };
        }

        private BulkAssignmentViewModel GetBulkAssignmentData(string departmentId, string status)
        {
            var classes = new List<BulkAssignmentItem>();

            string query = @"
                SELECT 
                    c.ID as CLASS_ID,
                    c.CODE as CLASS_CODE,
                    c.NAME as CLASS_NAME,
                    c.DEPARTMENT_ID,
                    u.FULL_NAME as CVHT_NAME,
                    (SELECT COUNT(*) FROM STUDENTS WHERE CLASS_ID = c.ID) as STUDENT_COUNT
                FROM CLASSES c
                LEFT JOIN CLASS_LECTURER_ASSIGNMENTS cl ON c.ID = cl.CLASS_ID AND cl.IS_ACTIVE = 1
                LEFT JOIN USERS u ON cl.LECTURER_ID = u.MAND
                WHERE 1=1";

            var parameters = new List<OracleParameter>();

            if (!string.IsNullOrEmpty(departmentId))
            {
                query += " AND c.DEPARTMENT_ID = :DepartmentId";
                parameters.Add(OracleDbHelper.CreateParameter("DepartmentId", OracleDbType.Varchar2, departmentId));
            }

            if (status == "assigned")
                query += " AND cl.ID IS NOT NULL";
            else if (status == "unassigned")
                query += " AND cl.ID IS NULL";

            query += " ORDER BY c.CODE";

            DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters.ToArray());

            foreach (DataRow row in dt.Rows)
            {
                classes.Add(new BulkAssignmentItem
                {
                    ClassId = row["CLASS_ID"].ToString(),
                    ClassCode = row["CLASS_CODE"].ToString(),
                    ClassName = row["CLASS_NAME"].ToString(),
                    CurrentCvht = row["CVHT_NAME"] != DBNull.Value ? row["CVHT_NAME"].ToString() : null,
                    StudentCount = Convert.ToInt32(row["STUDENT_COUNT"])
                });
            }

            // Get available lecturers
            var lecturers = new List<SelectListItem>();
            string lecturerQuery = "SELECT MAND, FULL_NAME FROM USERS WHERE ROLE_NAME = 'LECTURER' AND IS_ACTIVE = 1 ORDER BY FULL_NAME";
            DataTable lecturerDt = OracleDbHelper.ExecuteQuery(lecturerQuery, null);

            foreach (DataRow row in lecturerDt.Rows)
            {
                lecturers.Add(new SelectListItem
                {
                    Value = row["MAND"].ToString(),
                    Text = row["FULL_NAME"].ToString()
                });
            }

            int assignedCount = classes.Where(c => c.CurrentCvht != null).Count();

            return new BulkAssignmentViewModel
            {
                Classes = classes,
                AvailableLecturers = lecturers,
                Departments = GetDepartmentsList(),
                FilterDepartmentId = departmentId,
                FilterStatus = status,
                TotalClasses = classes.Count,
                AssignedClasses = assignedCount,
                UnassignedClasses = classes.Count - assignedCount
            };
        }

        private BulkAssignmentResult ProcessBulkAssignment(BulkAssignmentRequest request, string assignedBy)
        {
            var result = new BulkAssignmentResult
            {
                Errors = new List<AssignmentError>()
            };

            foreach (var assignment in request.Assignments)
            {
                if (string.IsNullOrEmpty(assignment.LecturerId))
                    continue;

                try
                {
                    string procResult = CallAssignAdvisorProcedure(
                        assignment.ClassId,
                        assignment.LecturerId,
                        assignedBy,
                        request.Notes
                    );

                    if (procResult.StartsWith("SUCCESS") || procResult.StartsWith("INFO"))
                    {
                        result.SuccessCount++;
                    }
                    else
                    {
                        result.FailureCount++;
                        result.Errors.Add(new AssignmentError
                        {
                            ClassId = assignment.ClassId,
                            ErrorMessage = procResult
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new AssignmentError
                    {
                        ClassId = assignment.ClassId,
                        ErrorMessage = ex.Message
                    });
                }
            }

            return result;
        }

        #endregion
    }
}
