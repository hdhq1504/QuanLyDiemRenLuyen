using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;

namespace QuanLyDiemRenLuyen.Controllers.Lecturer
{
    public class PermissionsController : LecturerBaseController
    {
        // GET: /Lecturer/Permissions/MyClasses
        public ActionResult MyClasses()
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetCvhtClasses();
                return View("~/Views/Lecturer/Permissions/MyClasses.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Lecturer/Permissions/MyClasses.cshtml", new CvhtClassesViewModel());
            }
        }

        // GET: /Lecturer/Permissions/Manage/{classId}
        public ActionResult Manage(string classId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            string currentUser = Session["MAND"].ToString();

            // Verify user is CVHT of this class
            if (!IsCvht(currentUser, classId))
            {
                TempData["ErrorMessage"] = "Bạn không phải là cố vấn học tập của lớp này";
                return RedirectToAction("MyClasses");
            }

            try
            {
                var viewModel = GetPermissionsForClass(classId);
                return View("~/Views/Lecturer/Permissions/Manage.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("MyClasses");
            }
        }

        // POST: /Lecturer/Permissions/Grant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Grant(GrantPermissionRequest request)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            string currentUser = Session["MAND"].ToString();

            // Verify user is CVHT
            if (!IsCvht(currentUser, request.ClassId))
            {
                return Json(new { success = false, message = "Bạn không phải là cố vấn học tập của lớp này" });
            }

            try
            {
                string result = GrantPermission(
                    request.ClassId,
                    currentUser,
                    request.GrantedTo,
                    request.PermissionType,
                    request.ExpiresAt,
                    request.Notes
                );

                if (result.StartsWith("SUCCESS"))
                {
                    return Json(new { success = true, message = "Cấp quyền thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = result });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: /Lecturer/Permissions/Revoke/{permissionId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Revoke(string permissionId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            string currentUser = Session["MAND"].ToString();

            try
            {
                string result = RevokePermission(permissionId, currentUser);

                if (result == "SUCCESS")
                {
                    TempData["SuccessMessage"] = "Thu hồi quyền thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = result;
                }

                return RedirectToAction("MyClasses");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("MyClasses");
            }
        }

        // GET: /Lecturer/Permissions/AccessLog/{classId}
        public ActionResult AccessLog(string classId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            string currentUser = Session["MAND"].ToString();

            if (!IsCvht(currentUser, classId))
            {
                TempData["ErrorMessage"] = "Bạn không phải là cố vấn học tập của lớp này";
                return RedirectToAction("MyClasses");
            }

            try
            {
                var viewModel = GetAccessLog(classId);
                return View("~/Views/Lecturer/Permissions/AccessLog.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("MyClasses");
            }
        }

        #region Helper Methods

        private bool IsCvht(string userId, string classId)
        {
            string query = @"SELECT COUNT(*) FROM CLASS_LECTURER_ASSIGNMENTS 
                            WHERE CLASS_ID = :classId AND LECTURER_ID = :userId AND IS_ACTIVE = 1";

            var result = OracleDbHelper.ExecuteScalar(query,
                OracleDbHelper.CreateParameter("classId", OracleDbType.Varchar2, classId),
                OracleDbHelper.CreateParameter("userId", OracleDbType.Varchar2, userId)
            );

            return Convert.ToInt32(result) > 0;
        }

        private CvhtClassesViewModel GetCvhtClasses()
        {
            var viewModel = new CvhtClassesViewModel();
            string currentUser = Session["MAND"].ToString();

            string query = @"SELECT 
                                c.ID, c.CODE, c.NAME,
                                (SELECT COUNT(*) FROM STUDENTS WHERE CLASS_ID = c.ID) as STUDENT_COUNT,
                                (SELECT COUNT(*) FROM V_ACTIVE_SCORE_PERMISSIONS WHERE CLASS_ID = c.ID) as ACTIVE_PERMS,
                                (SELECT MAX(GRANTED_AT) FROM CLASS_SCORE_PERMISSIONS WHERE CLASS_ID = c.ID) as LAST_GRANTED
                            FROM CLASSES c
                            JOIN CLASS_LECTURER_ASSIGNMENTS cl ON c.ID = cl.CLASS_ID AND cl.IS_ACTIVE = 1
                            WHERE cl.LECTURER_ID = :userId
                            ORDER BY c.NAME";

            DataTable dt = OracleDbHelper.ExecuteQuery(query,
                OracleDbHelper.CreateParameter("userId", OracleDbType.Varchar2, currentUser)
            );

            foreach (DataRow row in dt.Rows)
            {
                viewModel.Classes.Add(new CvhtClassInfo
                {
                    ClassId = row["ID"].ToString(),
                    ClassName = row["NAME"].ToString(),
                    ClassCode = row["CODE"].ToString(),
                    StudentCount = Convert.ToInt32(row["STUDENT_COUNT"]),
                    ActivePermissionsCount = Convert.ToInt32(row["ACTIVE_PERMS"]),
                    LastPermissionGranted = row["LAST_GRANTED"] != DBNull.Value ? 
                        (DateTime?)Convert.ToDateTime(row["LAST_GRANTED"]) : null
                });
            }

            return viewModel;
        }

        private PermissionManagementViewModel GetPermissionsForClass(string classId)
        {
            var viewModel = new PermissionManagementViewModel { ClassId = classId };

            // Get class info
            string classQuery = "SELECT CODE, NAME FROM CLASSES WHERE ID = :classId";
            DataTable classInfo = OracleDbHelper.ExecuteQuery(classQuery,
                OracleDbHelper.CreateParameter("classId", OracleDbType.Varchar2, classId)
            );

            if (classInfo.Rows.Count > 0)
            {
                viewModel.ClassName = classInfo.Rows[0]["NAME"].ToString();
                viewModel.ClassCode = classInfo.Rows[0]["CODE"].ToString();
            }

            // Get all permissions
            string permQuery = "SELECT * FROM V_ACTIVE_SCORE_PERMISSIONS WHERE CLASS_ID = :classId ORDER BY GRANTED_AT DESC";
            DataTable perms = OracleDbHelper.ExecuteQuery(permQuery,
                OracleDbHelper.CreateParameter("classId", OracleDbType.Varchar2, classId)
            );

            foreach (DataRow row in perms.Rows)
            {
                var perm = new ClassScorePermission
                {
                    Id = row["ID"].ToString(),
                    ClassId = row["CLASS_ID"].ToString(),
                    ClassName = row["CLASS_NAME"].ToString(),
                    ClassCode = row["CLASS_CODE"].ToString(),
                    GrantedBy = row["GRANTED_BY"].ToString(),
                    GrantedByName = row["GRANTED_BY_NAME"].ToString(),
                    GrantedTo = row["GRANTED_TO"].ToString(),
                    GrantedToName = row["GRANTED_TO_NAME"].ToString(),
                    GranteeRole = row["GRANTEE_ROLE"].ToString(),
                    PermissionType = row["PERMISSION_TYPE"].ToString(),
                    GrantedAt = Convert.ToDateTime(row["GRANTED_AT"]),
                    ExpiresAt = row["EXPIRES_AT"] != DBNull.Value ? 
                        (DateTime?)Convert.ToDateTime(row["EXPIRES_AT"]) : null,
                    RevokedAt = row["REVOKED_AT"] != DBNull.Value ? 
                        (DateTime?)Convert.ToDateTime(row["REVOKED_AT"]) : null,
                    RevokedBy = row["REVOKED_BY"] != DBNull.Value ? row["REVOKED_BY"].ToString() : null,
                    IsActive = Convert.ToInt32(row["IS_ACTIVE"]) == 1,
                    Notes = row["NOTES"] != DBNull.Value ? row["NOTES"].ToString() : null
                };

                viewModel.ActivePermissions.Add(perm);
                viewModel.ActiveCount++;
            }

            viewModel.TotalPermissions = viewModel.ActivePermissions.Count;

            // Get available grantees (lecturers + class leaders)
            string granteeQuery = @"SELECT MAND, FULL_NAME, EMAIL, ROLE_NAME 
                                   FROM USERS 
                                   WHERE ROLE_NAME IN ('LECTURER', 'STUDENT') 
                                   AND IS_ACTIVE = 1
                                   AND MAND != :currentUser
                                   ORDER BY ROLE_NAME, FULL_NAME";

            string currentUser = Session["MAND"].ToString();
            DataTable grantees = OracleDbHelper.ExecuteQuery(granteeQuery,
                OracleDbHelper.CreateParameter("currentUser", OracleDbType.Varchar2, currentUser)
            );

            foreach (DataRow row in grantees.Rows)
            {
                viewModel.AvailableGrantees.Add(new AvailableGrantee
                {
                    Mand = row["MAND"].ToString(),
                    FullName = row["FULL_NAME"].ToString(),
                    Email = row["EMAIL"].ToString(),
                    Role = row["ROLE_NAME"].ToString()
                });
            }

            return viewModel;
        }

        private string GrantPermission(string classId, string grantedBy, string grantedTo, 
            string permissionType, DateTime? expiresAt, string notes)
        {
            using (var conn = OracleDbHelper.GetConnection())
            {
                // Connection already opened by GetConnection()
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SP_GRANT_SCORE_PERMISSION";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_class_id", OracleDbType.Varchar2).Value = classId;
                    cmd.Parameters.Add("p_granted_by", OracleDbType.Varchar2).Value = grantedBy;
                    cmd.Parameters.Add("p_granted_to", OracleDbType.Varchar2).Value = grantedTo;
                    cmd.Parameters.Add("p_permission_type", OracleDbType.Varchar2).Value = permissionType;
                    cmd.Parameters.Add("p_expires_at", OracleDbType.TimeStamp).Value = 
                        (object)expiresAt ?? DBNull.Value;
                    cmd.Parameters.Add("p_notes", OracleDbType.Varchar2, 500).Value = 
                        (object)notes ?? DBNull.Value;

                    var resultParam = cmd.Parameters.Add("p_result", OracleDbType.Varchar2, 500);
                    resultParam.Direction = ParameterDirection.Output;

                    var permIdParam = cmd.Parameters.Add("p_permission_id", OracleDbType.Varchar2, 32);
                    permIdParam.Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();

                    return resultParam.Value.ToString();
                }
            }
        }

        private string RevokePermission(string permissionId, string revokedBy)
        {
            using (var conn = OracleDbHelper.GetConnection())
            {
                // Connection already opened by GetConnection()
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SP_REVOKE_SCORE_PERMISSION";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_permission_id", OracleDbType.Varchar2).Value = permissionId;
                    cmd.Parameters.Add("p_revoked_by", OracleDbType.Varchar2).Value = revokedBy;

                    var resultParam = cmd.Parameters.Add("p_result", OracleDbType.Varchar2, 500);
                    resultParam.Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();

                    return resultParam.Value.ToString();
                }
            }
        }

        private AccessLogViewModel GetAccessLog(string classId)
        {
            var viewModel = new AccessLogViewModel();

            // Get class info
            string classQuery = "SELECT NAME FROM CLASSES WHERE ID = :classId";
            var className = OracleDbHelper.ExecuteScalar(classQuery,
                OracleDbHelper.CreateParameter("classId", OracleDbType.Varchar2, classId)
            );

            viewModel.ClassId = classId;
            viewModel.ClassName = className?.ToString();

            // Get access logs from AUDIT_TRAIL
            string logQuery = @"SELECT 
                                   at.ID, at.WHO, u.FULL_NAME as WHO_NAME, 
                                   at.ACTION, at.EVENT_AT_UTC, at.CLIENT_IP, at.USER_AGENT
                               FROM AUDIT_TRAIL at
                               JOIN USERS u ON at.WHO = u.MAND
                               WHERE at.ACTION LIKE 'SCORE_ACCESS%'
                               ORDER BY at.EVENT_AT_UTC DESC";

            DataTable logs = OracleDbHelper.ExecuteQuery(logQuery);

            foreach (DataRow row in logs.Rows)
            {
                string action = row["ACTION"].ToString();
                
                // Parse action string: SCORE_ACCESS|METHOD|ACCESS_TYPE|SCORE=id|...
                var entry = new ScoreAccessLogEntry
                {
                    Id = row["ID"].ToString(),
                    Who = row["WHO"].ToString(),
                    WhoName = row["WHO_NAME"].ToString(),
                    Action = action,
                    EventTime = Convert.ToDateTime(row["EVENT_AT_UTC"]),
                    IpAddress = row["CLIENT_IP"] != DBNull.Value ? row["CLIENT_IP"].ToString() : null,
                    UserAgent = row["USER_AGENT"] != DBNull.Value ? row["USER_AGENT"].ToString() : null
                };

                // Parse action components
                string[] parts = action.Split('|');
                if (parts.Length >= 3)
                {
                    entry.AccessMethod = parts[1]; // CVHT, GRANTED, DENIED
                    entry.AccessType = parts[2];   // VIEW, EDIT, APPROVE
                    
                    foreach (var part in parts)
                    {
                        if (part.StartsWith("SCORE="))
                            entry.ScoreId = part.Substring(6);
                        else if (part.StartsWith("PERM="))
                            entry.PermissionId = part.Substring(5);
                        else if (part == "SUCCESS" || part == "DENIED")
                            entry.Result = part;
                    }
                }

                viewModel.LogEntries.Add(entry);
            }

            return viewModel;
        }

        #endregion
    }
}
