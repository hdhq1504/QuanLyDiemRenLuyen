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
    public class AuditLogsController : AdminBaseController
    {
        // GET: Admin/AuditLogs
        public ActionResult Index(string tableName = null, string operation = null, 
            string userId = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            // Only ADMIN can view audit logs
            if (Session["RoleName"]?.ToString() != "ADMIN")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            try
            {
                int pageSize = 20;
                var viewModel = new AuditLogsViewModel
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TableName = tableName,
                    Operation = operation,
                    UserId = userId,
                    FromDate = fromDate ?? DateTime.Today.AddDays(-7),
                    ToDate = toDate ?? DateTime.Today.AddDays(1),
                    AuditLogs = new List<AuditLogItem>(),
                    Statistics = new AuditStatistics()
                };

                // Build query with filters
                string whereClause = "WHERE 1=1";
                var parameters = new List<OracleParameter>();

                if (!string.IsNullOrEmpty(tableName))
                {
                    whereClause += " AND TABLE_NAME = :tableName";
                    parameters.Add(new OracleParameter("tableName", tableName));
                }

                if (!string.IsNullOrEmpty(operation))
                {
                    whereClause += " AND OPERATION = :operation";
                    parameters.Add(new OracleParameter("operation", operation));
                }

                if (!string.IsNullOrEmpty(userId))
                {
                    whereClause += " AND PERFORMED_BY = :userId";
                    parameters.Add(new OracleParameter("userId", userId));
                }

                whereClause += " AND PERFORMED_AT >= :fromDate AND PERFORMED_AT < :toDate";
                parameters.Add(new OracleParameter("fromDate", viewModel.FromDate));
                parameters.Add(new OracleParameter("toDate", viewModel.ToDate));

                // Get total count
                string countQuery = $"SELECT COUNT(*) FROM AUDIT_CHANGE_LOGS {whereClause}";
                object countResult = OracleDbHelper.ExecuteScalar(countQuery, parameters.ToArray());
                viewModel.TotalRecords = Convert.ToInt32(countResult);
                viewModel.TotalPages = (int)Math.Ceiling((double)viewModel.TotalRecords / pageSize);

                // Get paginated data
                int offset = (page - 1) * pageSize;
                string query = $@"
                    SELECT acl.*, u.FULL_NAME as PERFORMER_NAME
                    FROM AUDIT_CHANGE_LOGS acl
                    LEFT JOIN USERS u ON u.MAND = acl.PERFORMED_BY
                    {whereClause}
                    ORDER BY acl.PERFORMED_AT DESC
                    OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

                DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters.ToArray());
                foreach (DataRow row in dt.Rows)
                {
                    viewModel.AuditLogs.Add(new AuditLogItem
                    {
                        Id = row["ID"].ToString(),
                        TableName = row["TABLE_NAME"].ToString(),
                        RecordId = row["RECORD_ID"].ToString(),
                        Operation = row["OPERATION"].ToString(),
                        OldValues = row["OLD_VALUES"] != DBNull.Value ? row["OLD_VALUES"].ToString() : null,
                        NewValues = row["NEW_VALUES"] != DBNull.Value ? row["NEW_VALUES"].ToString() : null,
                        ChangedColumns = row["CHANGED_COLUMNS"] != DBNull.Value ? row["CHANGED_COLUMNS"].ToString() : null,
                        PerformedBy = row["PERFORMED_BY"] != DBNull.Value ? row["PERFORMED_BY"].ToString() : null,
                        PerformerName = row["PERFORMER_NAME"] != DBNull.Value ? row["PERFORMER_NAME"].ToString() : null,
                        PerformedAt = Convert.ToDateTime(row["PERFORMED_AT"]),
                        ClientIp = row["CLIENT_IP"] != DBNull.Value ? row["CLIENT_IP"].ToString() : null,
                        Justification = row["JUSTIFICATION"] != DBNull.Value ? row["JUSTIFICATION"].ToString() : null
                    });
                }

                // Get statistics
                string statsQuery = @"
                    SELECT 
                        (SELECT COUNT(*) FROM AUDIT_CHANGE_LOGS WHERE PERFORMED_AT >= TRUNC(SYSDATE)) as TODAY_COUNT,
                        (SELECT COUNT(*) FROM AUDIT_CHANGE_LOGS WHERE PERFORMED_AT >= TRUNC(SYSDATE) - 7) as WEEK_COUNT,
                        (SELECT COUNT(DISTINCT PERFORMED_BY) FROM AUDIT_CHANGE_LOGS WHERE PERFORMED_AT >= TRUNC(SYSDATE)) as TODAY_USERS,
                        (SELECT COUNT(*) FROM AUDIT_CHANGE_LOGS WHERE TABLE_NAME = 'SCORES' AND PERFORMED_AT >= TRUNC(SYSDATE) - 7) as SCORES_CHANGES
                    FROM DUAL";

                DataTable statsTable = OracleDbHelper.ExecuteQuery(statsQuery, null);
                if (statsTable.Rows.Count > 0)
                {
                    DataRow statsRow = statsTable.Rows[0];
                    viewModel.Statistics.TodayCount = Convert.ToInt32(statsRow["TODAY_COUNT"]);
                    viewModel.Statistics.WeekCount = Convert.ToInt32(statsRow["WEEK_COUNT"]);
                    viewModel.Statistics.TodayUsers = Convert.ToInt32(statsRow["TODAY_USERS"]);
                    viewModel.Statistics.ScoresChanges = Convert.ToInt32(statsRow["SCORES_CHANGES"]);
                }

                // Get available tables for filter
                viewModel.AvailableTables = GetDistinctValues("TABLE_NAME");
                viewModel.AvailableOperations = GetDistinctValues("OPERATION");

                return View("~/Views/Admin/AuditLogs/Index.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/AuditLogs/Index.cshtml", new AuditLogsViewModel());
            }
        }

        // GET: Admin/AuditLogs/Detail/id
        public ActionResult Detail(string id)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            if (Session["RoleName"]?.ToString() != "ADMIN")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            try
            {
                string query = @"
                    SELECT acl.*, u.FULL_NAME as PERFORMER_NAME
                    FROM AUDIT_CHANGE_LOGS acl
                    LEFT JOIN USERS u ON u.MAND = acl.PERFORMED_BY
                    WHERE acl.ID = :id";

                DataTable dt = OracleDbHelper.ExecuteQuery(query, new[] { new OracleParameter("id", id) });
                
                if (dt.Rows.Count == 0)
                {
                    return HttpNotFound();
                }

                DataRow row = dt.Rows[0];
                var item = new AuditLogItem
                {
                    Id = row["ID"].ToString(),
                    TableName = row["TABLE_NAME"].ToString(),
                    RecordId = row["RECORD_ID"].ToString(),
                    Operation = row["OPERATION"].ToString(),
                    OldValues = row["OLD_VALUES"] != DBNull.Value ? row["OLD_VALUES"].ToString() : null,
                    NewValues = row["NEW_VALUES"] != DBNull.Value ? row["NEW_VALUES"].ToString() : null,
                    ChangedColumns = row["CHANGED_COLUMNS"] != DBNull.Value ? row["CHANGED_COLUMNS"].ToString() : null,
                    PerformedBy = row["PERFORMED_BY"] != DBNull.Value ? row["PERFORMED_BY"].ToString() : null,
                    PerformerName = row["PERFORMER_NAME"] != DBNull.Value ? row["PERFORMER_NAME"].ToString() : null,
                    PerformedAt = Convert.ToDateTime(row["PERFORMED_AT"]),
                    SessionUser = row["SESSION_USER"] != DBNull.Value ? row["SESSION_USER"].ToString() : null,
                    OsUser = row["OS_USER"] != DBNull.Value ? row["OS_USER"].ToString() : null,
                    ClientIp = row["CLIENT_IP"] != DBNull.Value ? row["CLIENT_IP"].ToString() : null,
                    ClientHost = row["CLIENT_HOST"] != DBNull.Value ? row["CLIENT_HOST"].ToString() : null,
                    Justification = row["JUSTIFICATION"] != DBNull.Value ? row["JUSTIFICATION"].ToString() : null
                };

                return View("~/Views/Admin/AuditLogs/Detail.cshtml", item);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: Admin/AuditLogs/RecordHistory
        public ActionResult RecordHistory(string tableName, string recordId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            if (Session["RoleName"]?.ToString() != "ADMIN")
            {
                return new HttpStatusCodeResult(403);
            }

            try
            {
                string query = @"
                    SELECT acl.*, u.FULL_NAME as PERFORMER_NAME
                    FROM AUDIT_CHANGE_LOGS acl
                    LEFT JOIN USERS u ON u.MAND = acl.PERFORMED_BY
                    WHERE acl.TABLE_NAME = :tableName AND acl.RECORD_ID = :recordId
                    ORDER BY acl.PERFORMED_AT DESC";

                DataTable dt = OracleDbHelper.ExecuteQuery(query, new[] { 
                    new OracleParameter("tableName", tableName),
                    new OracleParameter("recordId", recordId)
                });

                var history = new List<AuditLogItem>();
                foreach (DataRow row in dt.Rows)
                {
                    history.Add(new AuditLogItem
                    {
                        Id = row["ID"].ToString(),
                        TableName = row["TABLE_NAME"].ToString(),
                        RecordId = row["RECORD_ID"].ToString(),
                        Operation = row["OPERATION"].ToString(),
                        OldValues = row["OLD_VALUES"] != DBNull.Value ? row["OLD_VALUES"].ToString() : null,
                        NewValues = row["NEW_VALUES"] != DBNull.Value ? row["NEW_VALUES"].ToString() : null,
                        ChangedColumns = row["CHANGED_COLUMNS"] != DBNull.Value ? row["CHANGED_COLUMNS"].ToString() : null,
                        PerformedBy = row["PERFORMED_BY"] != DBNull.Value ? row["PERFORMED_BY"].ToString() : null,
                        PerformerName = row["PERFORMER_NAME"] != DBNull.Value ? row["PERFORMER_NAME"].ToString() : null,
                        PerformedAt = Convert.ToDateTime(row["PERFORMED_AT"]),
                        Justification = row["JUSTIFICATION"] != DBNull.Value ? row["JUSTIFICATION"].ToString() : null
                    });
                }

                ViewBag.TableName = tableName;
                ViewBag.RecordId = recordId;

                return PartialView("~/Views/Admin/AuditLogs/_RecordHistory.cshtml", history);
            }
            catch (Exception ex)
            {
                return Content("<p class='text-danger'>Lỗi: " + ex.Message + "</p>");
            }
        }

        // GET: Admin/AuditLogs/DailySummary
        public ActionResult DailySummary()
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            if (Session["RoleName"]?.ToString() != "ADMIN")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            try
            {
                string query = @"
                    SELECT 
                        TRUNC(PERFORMED_AT) as AUDIT_DATE,
                        TABLE_NAME,
                        OPERATION,
                        COUNT(*) as CHANGE_COUNT,
                        COUNT(DISTINCT PERFORMED_BY) as UNIQUE_USERS
                    FROM AUDIT_CHANGE_LOGS
                    WHERE PERFORMED_AT >= TRUNC(SYSDATE) - 30
                    GROUP BY TRUNC(PERFORMED_AT), TABLE_NAME, OPERATION
                    ORDER BY AUDIT_DATE DESC, TABLE_NAME, OPERATION";

                DataTable dt = OracleDbHelper.ExecuteQuery(query, null);
                
                var summary = new List<DailySummaryItem>();
                foreach (DataRow row in dt.Rows)
                {
                    summary.Add(new DailySummaryItem
                    {
                        Date = Convert.ToDateTime(row["AUDIT_DATE"]),
                        TableName = row["TABLE_NAME"].ToString(),
                        Operation = row["OPERATION"].ToString(),
                        ChangeCount = Convert.ToInt32(row["CHANGE_COUNT"]),
                        UniqueUsers = Convert.ToInt32(row["UNIQUE_USERS"])
                    });
                }

                return View("~/Views/Admin/AuditLogs/DailySummary.cshtml", summary);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Views/Admin/AuditLogs/DailySummary.cshtml", new List<DailySummaryItem>());
            }
        }

        // ========== FGA (Fine-Grained Auditing) ==========

        // GET: Admin/AuditLogs/FgaLogs
        public ActionResult FgaLogs(string policyName = null, string objectName = null, 
            string dbUser = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            if (Session["RoleName"]?.ToString() != "ADMIN")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            try
            {
                int pageSize = 20;
                var viewModel = new FgaLogsViewModel
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    PolicyName = policyName,
                    ObjectName = objectName,
                    DbUser = dbUser,
                    FromDate = fromDate ?? DateTime.Today.AddDays(-7),
                    ToDate = toDate ?? DateTime.Today.AddDays(1),
                    FgaLogs = new List<FgaLogItem>(),
                    Statistics = new FgaStatistics()
                };

                // Build query with filters
                string whereClause = "WHERE OBJECT_SCHEMA = 'QLDIEMRENLUYEN'";
                var parameters = new List<OracleParameter>();

                if (!string.IsNullOrEmpty(policyName))
                {
                    whereClause += " AND POLICY_NAME = :policyName";
                    parameters.Add(new OracleParameter("policyName", policyName));
                }

                if (!string.IsNullOrEmpty(objectName))
                {
                    whereClause += " AND OBJECT_NAME = :objectName";
                    parameters.Add(new OracleParameter("objectName", objectName));
                }

                if (!string.IsNullOrEmpty(dbUser))
                {
                    whereClause += " AND DB_USER LIKE '%' || :dbUser || '%'";
                    parameters.Add(new OracleParameter("dbUser", dbUser));
                }

                whereClause += " AND TIMESTAMP >= :fromDate AND TIMESTAMP < :toDate";
                parameters.Add(new OracleParameter("fromDate", viewModel.FromDate));
                parameters.Add(new OracleParameter("toDate", viewModel.ToDate));

                // Get total count
                string countQuery = $"SELECT COUNT(*) FROM DBA_FGA_AUDIT_TRAIL {whereClause}";
                object countResult = OracleDbHelper.ExecuteScalar(countQuery, parameters.ToArray());
                viewModel.TotalRecords = Convert.ToInt32(countResult);
                viewModel.TotalPages = (int)Math.Ceiling((double)viewModel.TotalRecords / pageSize);

                // Get paginated data
                int offset = (page - 1) * pageSize;
                string query = $@"
                    SELECT 
                        TIMESTAMP, DB_USER, OS_USER, USERHOST, CLIENT_ID,
                        OBJECT_SCHEMA, OBJECT_NAME, POLICY_NAME, 
                        SQL_TEXT, SQL_BIND, STATEMENT_TYPE,
                        EXTENDED_TIMESTAMP, SESSION_ID, ENTRYID
                    FROM DBA_FGA_AUDIT_TRAIL
                    {whereClause}
                    ORDER BY TIMESTAMP DESC
                    OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

                DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters.ToArray());
                foreach (DataRow row in dt.Rows)
                {
                    viewModel.FgaLogs.Add(new FgaLogItem
                    {
                        Timestamp = row["TIMESTAMP"] != DBNull.Value ? Convert.ToDateTime(row["TIMESTAMP"]) : DateTime.MinValue,
                        DbUser = row["DB_USER"] != DBNull.Value ? row["DB_USER"].ToString() : null,
                        OsUser = row["OS_USER"] != DBNull.Value ? row["OS_USER"].ToString() : null,
                        UserHost = row["USERHOST"] != DBNull.Value ? row["USERHOST"].ToString() : null,
                        ClientIp = row["CLIENT_ID"] != DBNull.Value ? row["CLIENT_ID"].ToString() : null,
                        ObjectSchema = row["OBJECT_SCHEMA"] != DBNull.Value ? row["OBJECT_SCHEMA"].ToString() : null,
                        ObjectName = row["OBJECT_NAME"] != DBNull.Value ? row["OBJECT_NAME"].ToString() : null,
                        PolicyName = row["POLICY_NAME"] != DBNull.Value ? row["POLICY_NAME"].ToString() : null,
                        SqlText = row["SQL_TEXT"] != DBNull.Value ? row["SQL_TEXT"].ToString() : null,
                        SqlBind = row["SQL_BIND"] != DBNull.Value ? row["SQL_BIND"].ToString() : null,
                        StatementType = row["STATEMENT_TYPE"] != DBNull.Value ? row["STATEMENT_TYPE"].ToString() : null,
                        ExtendedTimestamp = row["EXTENDED_TIMESTAMP"] != DBNull.Value ? row["EXTENDED_TIMESTAMP"].ToString() : null,
                        SessionId = row["SESSION_ID"] != DBNull.Value ? row["SESSION_ID"].ToString() : null,
                        EntryId = row["ENTRYID"] != DBNull.Value ? row["ENTRYID"].ToString() : null
                    });
                }

                // Get statistics
                string statsQuery = @"
                    SELECT 
                        (SELECT COUNT(*) FROM DBA_FGA_AUDIT_TRAIL WHERE OBJECT_SCHEMA = 'QLDIEMRENLUYEN' AND TIMESTAMP >= TRUNC(SYSDATE)) as TODAY_COUNT,
                        (SELECT COUNT(*) FROM DBA_FGA_AUDIT_TRAIL WHERE OBJECT_SCHEMA = 'QLDIEMRENLUYEN' AND TIMESTAMP >= TRUNC(SYSDATE) - 7) as WEEK_COUNT,
                        (SELECT COUNT(*) FROM DBA_FGA_AUDIT_TRAIL WHERE OBJECT_SCHEMA = 'QLDIEMRENLUYEN' AND POLICY_NAME IN ('FGA_STUDENTS_SENSITIVE', 'FGA_USERS_PASSWORD') AND TIMESTAMP >= TRUNC(SYSDATE) - 7) as SENSITIVE_COUNT,
                        (SELECT COUNT(DISTINCT DB_USER) FROM DBA_FGA_AUDIT_TRAIL WHERE OBJECT_SCHEMA = 'QLDIEMRENLUYEN' AND TIMESTAMP >= TRUNC(SYSDATE)) as UNIQUE_USERS
                    FROM DUAL";

                DataTable statsTable = OracleDbHelper.ExecuteQuery(statsQuery, null);
                if (statsTable.Rows.Count > 0)
                {
                    DataRow statsRow = statsTable.Rows[0];
                    viewModel.Statistics.TodayCount = Convert.ToInt32(statsRow["TODAY_COUNT"]);
                    viewModel.Statistics.WeekCount = Convert.ToInt32(statsRow["WEEK_COUNT"]);
                    viewModel.Statistics.SensitiveAccessCount = Convert.ToInt32(statsRow["SENSITIVE_COUNT"]);
                    viewModel.Statistics.UniqueUsersAccessing = Convert.ToInt32(statsRow["UNIQUE_USERS"]);
                }

                // Get available policies and objects for filters
                viewModel.AvailablePolicies = GetFgaDistinctValues("POLICY_NAME");
                viewModel.AvailableObjects = GetFgaDistinctValues("OBJECT_NAME");

                return View("~/Views/Admin/AuditLogs/FgaLogs.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                ViewBag.ErrorDetails = "Lưu ý: Để xem FGA logs, schema cần có quyền SELECT trên DBA_FGA_AUDIT_TRAIL.";
                return View("~/Views/Admin/AuditLogs/FgaLogs.cshtml", new FgaLogsViewModel());
            }
        }

        // GET: Admin/AuditLogs/FgaDetail
        public ActionResult FgaDetail(string sessionId, string entryId)
        {
            var authCheck = CheckAuth();
            if (authCheck != null) return authCheck;

            if (Session["RoleName"]?.ToString() != "ADMIN")
            {
                return new HttpStatusCodeResult(403);
            }

            try
            {
                string query = @"
                    SELECT 
                        TIMESTAMP, DB_USER, OS_USER, USERHOST, CLIENT_ID,
                        OBJECT_SCHEMA, OBJECT_NAME, POLICY_NAME, 
                        SQL_TEXT, SQL_BIND, STATEMENT_TYPE,
                        EXTENDED_TIMESTAMP, SESSION_ID, ENTRYID,
                        TRANSACTIONID, SCN
                    FROM DBA_FGA_AUDIT_TRAIL
                    WHERE OBJECT_SCHEMA = 'QLDIEMRENLUYEN'
                    AND SESSION_ID = :sessionId 
                    AND ENTRYID = :entryId";

                DataTable dt = OracleDbHelper.ExecuteQuery(query, new[] { 
                    new OracleParameter("sessionId", sessionId),
                    new OracleParameter("entryId", entryId)
                });

                if (dt.Rows.Count == 0)
                {
                    return HttpNotFound();
                }

                DataRow row = dt.Rows[0];
                var item = new FgaLogItem
                {
                    Timestamp = row["TIMESTAMP"] != DBNull.Value ? Convert.ToDateTime(row["TIMESTAMP"]) : DateTime.MinValue,
                    DbUser = row["DB_USER"] != DBNull.Value ? row["DB_USER"].ToString() : null,
                    OsUser = row["OS_USER"] != DBNull.Value ? row["OS_USER"].ToString() : null,
                    UserHost = row["USERHOST"] != DBNull.Value ? row["USERHOST"].ToString() : null,
                    ClientIp = row["CLIENT_ID"] != DBNull.Value ? row["CLIENT_ID"].ToString() : null,
                    ObjectSchema = row["OBJECT_SCHEMA"] != DBNull.Value ? row["OBJECT_SCHEMA"].ToString() : null,
                    ObjectName = row["OBJECT_NAME"] != DBNull.Value ? row["OBJECT_NAME"].ToString() : null,
                    PolicyName = row["POLICY_NAME"] != DBNull.Value ? row["POLICY_NAME"].ToString() : null,
                    SqlText = row["SQL_TEXT"] != DBNull.Value ? row["SQL_TEXT"].ToString() : null,
                    SqlBind = row["SQL_BIND"] != DBNull.Value ? row["SQL_BIND"].ToString() : null,
                    StatementType = row["STATEMENT_TYPE"] != DBNull.Value ? row["STATEMENT_TYPE"].ToString() : null,
                    ExtendedTimestamp = row["EXTENDED_TIMESTAMP"] != DBNull.Value ? row["EXTENDED_TIMESTAMP"].ToString() : null,
                    SessionId = row["SESSION_ID"] != DBNull.Value ? row["SESSION_ID"].ToString() : null,
                    EntryId = row["ENTRYID"] != DBNull.Value ? row["ENTRYID"].ToString() : null
                };

                return PartialView("~/Views/Admin/AuditLogs/_FgaDetail.cshtml", item);
            }
            catch (Exception ex)
            {
                return Content("<div class='alert alert-danger'>Lỗi: " + ex.Message + "</div>");
            }
        }

        private List<string> GetDistinctValues(string columnName)
        {
            var values = new List<string>();
            try
            {
                string query = $"SELECT DISTINCT {columnName} FROM AUDIT_CHANGE_LOGS ORDER BY {columnName}";
                DataTable dt = OracleDbHelper.ExecuteQuery(query, null);
                foreach (DataRow row in dt.Rows)
                {
                    if (row[columnName] != DBNull.Value)
                    {
                        values.Add(row[columnName].ToString());
                    }
                }
            }
            catch { }
            return values;
        }

        private List<string> GetFgaDistinctValues(string columnName)
        {
            var values = new List<string>();
            try
            {
                string query = $"SELECT DISTINCT {columnName} FROM DBA_FGA_AUDIT_TRAIL WHERE OBJECT_SCHEMA = 'QLDIEMRENLUYEN' ORDER BY {columnName}";
                DataTable dt = OracleDbHelper.ExecuteQuery(query, null);
                foreach (DataRow row in dt.Rows)
                {
                    if (row[columnName] != DBNull.Value)
                    {
                        values.Add(row[columnName].ToString());
                    }
                }
            }
            catch { }
            return values;
        }
    }
}
