using System;
using System.Collections.Generic;
using System.Data;
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

                var viewModel = new ClassDetailsViewModel
                {
                    ClassInfo = classInfo,
                    Students = students
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
                                     (SELECT COUNT(*) FROM STUDENTS s WHERE s.CLASS_ID = c.ID) as STUDENT_COUNT
                                     FROM CLASSES c
                                     LEFT JOIN DEPARTMENTS d ON c.DEPARTMENT_ID = d.ID
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
                        StudentCount = Convert.ToInt32(row["STUDENT_COUNT"])
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
    }
}
