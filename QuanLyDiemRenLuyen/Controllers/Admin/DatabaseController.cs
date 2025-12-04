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
    /// <summary>
    /// Controller xử lý quản trị Oracle Database
    /// </summary>
    public class DatabaseController : AdminBaseController
    {
        // GET: Admin/Database
        public ActionResult Index()
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = new DatabaseDashboardViewModel
                {
                    TopTablespaces = GetTopTablespaces(5),
                    RecentSessions = GetRecentSessions(10),
                    TotalTablespaces = GetTotalTablespaceCount(),
                    HighestUsagePercent = GetHighestTablespaceUsage(),
                    TotalSessions = GetTotalSessionCount(),
                    ActiveSessionCount = GetActiveSessionCount(),
                    HighUsageTablespaceCount = GetHighUsageTablespaceCount(),
                    LongRunningSessions = GetLongRunningSessionCount()
                };

                return View("~/Views/Admin/Database/Index.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/Database/Index.cshtml", new DatabaseDashboardViewModel());
            }
        }

        // GET: Admin/Database/Tablespaces
        public ActionResult Tablespaces()
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var tablespaces = GetAllTablespaces();
                return View("~/Views/Admin/Database/Tablespaces.cshtml", tablespaces);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/Database/Tablespaces.cshtml", new List<TablespaceInfo>());
            }
        }

        // GET: Admin/Database/Sessions
        public ActionResult Sessions(string filterStatus, string search)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                var viewModel = new SessionListViewModel
                {
                    Sessions = GetFilteredSessions(filterStatus, search),
                    FilterStatus = filterStatus ?? "ALL",
                    SearchKeyword = search,
                    TotalSessions = GetTotalSessionCount(),
                    ActiveSessions = GetActiveSessionCount(),
                    InactiveSessions = GetInactiveSessionCount()
                };

                return View("~/Views/Admin/Database/Sessions.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/Database/Sessions.cshtml", new SessionListViewModel
                {
                    Sessions = new List<SessionInfo>()
                });
            }
        }

        // POST: Admin/Database/KillSession
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult KillSession(int sid, int serial)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            try
            {
                string query = "BEGIN SP_KILL_USER_SESSION(:Sid, :Serial); END;";
                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("Sid", OracleDbType.Int32, sid),
                    OracleDbHelper.CreateParameter("Serial", OracleDbType.Int32, serial)
                };

                OracleDbHelper.ExecuteNonQuery(query, parameters);

                TempData["SuccessMessage"] = $"Session {sid},{serial} đã được terminate thành công";
                return RedirectToAction("Sessions");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi kill session: " + ex.Message;
                return RedirectToAction("Sessions");
            }
        }

        #region Helper Methods - Tablespaces

        private List<TablespaceInfo> GetAllTablespaces()
        {
            var tablespaces = new List<TablespaceInfo>();

            try
            {
                string query = @"SELECT TABLESPACE_NAME, TOTAL_SPACE_MB, USED_SPACE_MB, 
                                        FREE_SPACE_MB, USAGE_PERCENT
                                 FROM V_TABLESPACE_USAGE
                                 ORDER BY USAGE_PERCENT DESC";

                DataTable dt = OracleDbHelper.ExecuteQuery(query);

                foreach (DataRow row in dt.Rows)
                {
                    tablespaces.Add(new TablespaceInfo
                    {
                        Name = row["TABLESPACE_NAME"].ToString(),
                        TotalSpaceMB = Convert.ToDecimal(row["TOTAL_SPACE_MB"]),
                        UsedSpaceMB = Convert.ToDecimal(row["USED_SPACE_MB"]),
                        FreeSpaceMB = Convert.ToDecimal(row["FREE_SPACE_MB"]),
                        UsagePercent = Convert.ToDecimal(row["USAGE_PERCENT"])
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAllTablespaces: {ex.Message}");
            }

            return tablespaces;
        }

        private List<TablespaceInfo> GetTopTablespaces(int topN)
        {
            return GetAllTablespaces().Take(topN).ToList();
        }

        private int GetTotalTablespaceCount()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM V_TABLESPACE_USAGE";
                return Convert.ToInt32(OracleDbHelper.ExecuteScalar(query));
            }
            catch
            {
                return 0;
            }
        }

        private decimal GetHighestTablespaceUsage()
        {
            try
            {
                string query = "SELECT NVL(MAX(USAGE_PERCENT), 0) FROM V_TABLESPACE_USAGE";
                return Convert.ToDecimal(OracleDbHelper.ExecuteScalar(query));
            }
            catch
            {
                return 0;
            }
        }

        private int GetHighUsageTablespaceCount()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM V_TABLESPACE_USAGE WHERE USAGE_PERCENT >= 85";
                return Convert.ToInt32(OracleDbHelper.ExecuteScalar(query));
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Helper Methods - Sessions

        private List<SessionInfo> GetAllSessions()
        {
            var sessions = new List<SessionInfo>();

            try
            {
                string query = @"SELECT SID, SERIAL, USERNAME, STATUS, SCHEMA_NAME, 
                                        OS_USER, MACHINE, PROGRAM, LOGON_TIME, 
                                        MINUTES_CONNECTED, SECONDS_SINCE_LAST_CALL
                                 FROM V_ACTIVE_SESSIONS
                                 ORDER BY LOGON_TIME DESC";

                DataTable dt = OracleDbHelper.ExecuteQuery(query);

                foreach (DataRow row in dt.Rows)
                {
                    sessions.Add(new SessionInfo
                    {
                        Sid = Convert.ToInt32(row["SID"]),
                        Serial = Convert.ToInt32(row["SERIAL"]),
                        Username = row["USERNAME"].ToString(),
                        Status = row["STATUS"].ToString(),
                        SchemaName = row["SCHEMA_NAME"] != DBNull.Value ? row["SCHEMA_NAME"].ToString() : "",
                        OsUser = row["OS_USER"] != DBNull.Value ? row["OS_USER"].ToString() : "",
                        Machine = row["MACHINE"] != DBNull.Value ? row["MACHINE"].ToString() : "",
                        Program = row["PROGRAM"] != DBNull.Value ? row["PROGRAM"].ToString() : "",
                        LogonTime = Convert.ToDateTime(row["LOGON_TIME"]),
                        MinutesConnected = Convert.ToInt32(row["MINUTES_CONNECTED"]),
                        SecondsSinceLastCall = Convert.ToInt32(row["SECONDS_SINCE_LAST_CALL"])
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAllSessions: {ex.Message}");
            }

            return sessions;
        }

        private List<SessionInfo> GetFilteredSessions(string filterStatus, string search)
        {
            var sessions = GetAllSessions();

            // Filter by status
            if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "ALL")
            {
                sessions = sessions.Where(s => s.Status == filterStatus).ToList();
            }

            // Filter by search
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToUpper();
                sessions = sessions.Where(s =>
                    s.Username.ToUpper().Contains(search) ||
                    s.Machine.ToUpper().Contains(search) ||
                    s.Program.ToUpper().Contains(search)
                ).ToList();
            }

            return sessions;
        }

        private List<SessionInfo> GetRecentSessions(int topN)
        {
            return GetAllSessions().Take(topN).ToList();
        }

        private int GetTotalSessionCount()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM V_ACTIVE_SESSIONS";
                return Convert.ToInt32(OracleDbHelper.ExecuteScalar(query));
            }
            catch
            {
                return 0;
            }
        }

        private int GetActiveSessionCount()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM V_ACTIVE_SESSIONS WHERE STATUS = 'ACTIVE'";
                return Convert.ToInt32(OracleDbHelper.ExecuteScalar(query));
            }
            catch
            {
                return 0;
            }
        }

        private int GetInactiveSessionCount()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM V_ACTIVE_SESSIONS WHERE STATUS = 'INACTIVE'";
                return Convert.ToInt32(OracleDbHelper.ExecuteScalar(query));
            }
            catch
            {
                return 0;
            }
        }

        private int GetLongRunningSessionCount()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM V_ACTIVE_SESSIONS WHERE MINUTES_CONNECTED > 60";
                return Convert.ToInt32(OracleDbHelper.ExecuteScalar(query));
            }
            catch
            {
                return 0;
            }
        }

        #endregion
    }
}
