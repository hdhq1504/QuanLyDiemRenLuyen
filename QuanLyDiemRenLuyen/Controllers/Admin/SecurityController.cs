using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;
using QuanLyDiemRenLuyen.Models;

namespace QuanLyDiemRenLuyen.Controllers.Admin
{
    public class SecurityController : AdminBaseController
    {
        // GET: Admin/Security/DatabaseUsers
        public ActionResult DatabaseUsers()
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetDatabaseUsers();
                return View("~/Views/Admin/Security/DatabaseUsers.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/Security/DatabaseUsers.cshtml", new DatabaseUserManagementViewModel());
            }
        }

        // POST: Admin/Security/CreateDbUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateDbUser(string mand)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var result = CreateDatabaseUser(mand);
                
                if (result.Result == "SUCCESS")
                {
                    TempData["SuccessMessage"] = $"Đã tạo database user thành công! Username: {result.DbUsername}";
                    TempData["DbPassword"] = result.DbPassword; // Show password only once
                }
                else
                {
                    TempData["ErrorMessage"] = result.Result;
                }

                return RedirectToAction("DatabaseUsers");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("DatabaseUsers");
            }
        }

        // POST: Admin/Security/DropDbUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DropDbUser(string mand)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string result = DropDatabaseUser(mand);
                
                if (result == "SUCCESS")
                {
                    TempData["SuccessMessage"] = "Đã xóa database user thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = result;
                }

                return RedirectToAction("DatabaseUsers");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("DatabaseUsers");
            }
        }

        // GET: Admin/Security/AccessMatrix
        public ActionResult AccessMatrix()
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = GetAccessControlMatrix();
                return View("~/Views/Admin/Security/AccessMatrix.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/Security/AccessMatrix.cshtml", new AccessControlMatrixViewModel());
            }
        }

        // GET: Admin/Security/Dashboard
        public ActionResult Dashboard()
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                // TODO: Implement security dashboard with overview stats
                return View("~/Views/Admin/Security/Dashboard.cshtml");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/Security/Dashboard.cshtml");
            }
        }

        #region Private Helper Methods

        private DatabaseUserManagementViewModel GetDatabaseUsers()
        {
            var viewModel = new DatabaseUserManagementViewModel();

            string query = @"SELECT * FROM V_USER_ROLE_ASSIGNMENTS ORDER BY DB_USER_CREATED_AT DESC NULLS LAST";
            
            DataTable dt = OracleDbHelper.ExecuteQuery(query, null);

            foreach (DataRow row in dt.Rows)
            {
                viewModel.DbUsers.Add(new DbUserCredential
                {
                    AppUserMand = row["MAND"].ToString(),
                    AppUserFullName = row["FULL_NAME"].ToString(),
                    AppUserEmail = row["EMAIL"].ToString(),
                    AppRole = row["APP_ROLE"].ToString(),
                    DbUsername = row["DB_USERNAME"] != DBNull.Value ? row["DB_USERNAME"].ToString() : null,
                    DbRole = row["DB_ROLE"] != DBNull.Value ? row["DB_ROLE"].ToString() : null,
                    CreatedAt = row["DB_USER_CREATED_AT"] != DBNull.Value ? Convert.ToDateTime(row["DB_USER_CREATED_AT"]) : DateTime.MinValue,
                    LastLogin = row["LAST_LOGIN"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["LAST_LOGIN"]) : null,
                    IsActive = row["DB_USER_ACTIVE"] != DBNull.Value && Convert.ToInt32(row["DB_USER_ACTIVE"]) == 1
                });
            }

            // Calculate statistics
            viewModel.TotalUsers = viewModel.DbUsers.Count;
            viewModel.ActiveUsers = viewModel.DbUsers.FindAll(u => u.IsActive && u.DbUsername != null).Count;
            viewModel.InactiveUsers = viewModel.TotalUsers - viewModel.ActiveUsers;

            // Group by role
            foreach (var user in viewModel.DbUsers)
            {
                if (user.DbRole != null)
                {
                    if (viewModel.UsersByRole.ContainsKey(user.DbRole))
                        viewModel.UsersByRole[user.DbRole]++;
                    else
                        viewModel.UsersByRole[user.DbRole] = 1;
                }
            }

            return viewModel;
        }

        private CreateDbUserRequest CreateDatabaseUser(string mand)
        {
            var result = new CreateDbUserRequest { AppUserMand = mand };

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SP_CREATE_DB_USER_FOR_APP_USER";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_mand", OracleDbType.Varchar2).Value = mand;
                    cmd.Parameters.Add("p_result", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("p_db_username", OracleDbType.Varchar2, 50).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("p_db_password", OracleDbType.Varchar2, 50).Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();

                    result.Result = cmd.Parameters["p_result"].Value.ToString();
                    result.DbUsername = cmd.Parameters["p_db_username"].Value?.ToString();
                    result.DbPassword = cmd.Parameters["p_db_password"].Value?.ToString();
                }
            }

            return result;
        }

        private string DropDatabaseUser(string mand)
        {
            string result;

            using (var conn = OracleDbHelper.GetConnection())
            {
                // Connection already opened by GetConnection()
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SP_DROP_DB_USER";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_mand", OracleDbType.Varchar2).Value = mand;
                    cmd.Parameters.Add("p_result", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();

                    result = cmd.Parameters["p_result"].Value.ToString();
                }
            }

            return result;
        }

        private AccessControlMatrixViewModel GetAccessControlMatrix()
        {
            var viewModel = new AccessControlMatrixViewModel();

            string query = @"SELECT 
                                GRANTEE as ROLE_NAME,
                                TABLE_NAME,
                                PRIVILEGE,
                                GRANTABLE
                             FROM USER_TAB_PRIVS_MADE
                             WHERE GRANTEE IN ('ROLE_STUDENT', 'ROLE_LECTURER', 'ROLE_ADMIN', 'ROLE_READONLY')
                             ORDER BY TABLE_NAME, GRANTEE, PRIVILEGE";

            DataTable dt = OracleDbHelper.ExecuteQuery(query, null);

            foreach (DataRow row in dt.Rows)
            {
                var perm = new RolePermission
                {
                    RoleName = row["ROLE_NAME"].ToString(),
                    TableName = row["TABLE_NAME"].ToString(),
                    Privilege = row["PRIVILEGE"].ToString(),
                    IsGrantable = row["GRANTABLE"].ToString() == "YES"
                };

                viewModel.Permissions.Add(perm);

                if (!viewModel.Tables.Contains(perm.TableName))
                    viewModel.Tables.Add(perm.TableName);
            }

            return viewModel;
        }

        #endregion
    }
}
