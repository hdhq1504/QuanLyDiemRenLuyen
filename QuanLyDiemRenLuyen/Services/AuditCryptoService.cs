using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using QuanLyDiemRenLuyen.Helpers;

namespace QuanLyDiemRenLuyen.Services
{
    /// <summary>
    /// Service xử lý ghi log audit thống nhất.
    /// </summary>
    public class AuditCryptoService
    {
        #region System Events (Login, Logout, Page Access)

        /// <summary>
        /// Ghi log sự kiện hệ thống (login, logout, page access)
        /// </summary>
        public void LogSystemEvent(
            string eventType,
            string performedBy,
            string description = null,
            string clientIp = null,
            string userAgent = null,
            string status = "SUCCESS")
        {
            if (string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(performedBy))
                return;

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        BEGIN 
                            PKG_AUDIT_EVENTS.LOG_SYSTEM_EVENT(
                                p_event_type => :p_event_type,
                                p_performed_by => :p_performed_by,
                                p_description => :p_description,
                                p_client_ip => :p_client_ip,
                                p_user_agent => :p_user_agent,
                                p_status => :p_status
                            ); 
                        END;";
                    
                    cmd.Parameters.Add("p_event_type", OracleDbType.Varchar2).Value = eventType;
                    cmd.Parameters.Add("p_performed_by", OracleDbType.Varchar2).Value = performedBy;
                    cmd.Parameters.Add("p_description", OracleDbType.Varchar2).Value = 
                        string.IsNullOrEmpty(description) ? (object)DBNull.Value : description;
                    cmd.Parameters.Add("p_client_ip", OracleDbType.Varchar2).Value = 
                        string.IsNullOrEmpty(clientIp) ? (object)DBNull.Value : clientIp;
                    cmd.Parameters.Add("p_user_agent", OracleDbType.Varchar2).Value = 
                        string.IsNullOrEmpty(userAgent) ? (object)DBNull.Value : userAgent;
                    cmd.Parameters.Add("p_status", OracleDbType.Varchar2).Value = status;
                    
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region Business Events (Approve, Reject, Update)

        /// <summary>
        /// Ghi log sự kiện nghiệp vụ (approve, reject, update)
        /// </summary>
        public void LogBusinessEvent(
            string eventType,
            string performedBy,
            string entityType,
            string entityId,
            string description = null,
            string details = null,
            string clientIp = null,
            string status = "SUCCESS",
            string errorMessage = null)
        {
            if (string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(performedBy))
                return;

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        BEGIN 
                            PKG_AUDIT_EVENTS.LOG_BUSINESS_EVENT(
                                p_event_type => :p_event_type,
                                p_performed_by => :p_performed_by,
                                p_entity_type => :p_entity_type,
                                p_entity_id => :p_entity_id,
                                p_description => :p_description,
                                p_details => :p_details,
                                p_client_ip => :p_client_ip,
                                p_status => :p_status,
                                p_error_message => :p_error_message
                            ); 
                        END;";
                    
                    cmd.Parameters.Add("p_event_type", OracleDbType.Varchar2).Value = eventType;
                    cmd.Parameters.Add("p_performed_by", OracleDbType.Varchar2).Value = performedBy;
                    cmd.Parameters.Add("p_entity_type", OracleDbType.Varchar2).Value = 
                        string.IsNullOrEmpty(entityType) ? (object)DBNull.Value : entityType;
                    cmd.Parameters.Add("p_entity_id", OracleDbType.Varchar2).Value = 
                        string.IsNullOrEmpty(entityId) ? (object)DBNull.Value : entityId;
                    cmd.Parameters.Add("p_description", OracleDbType.Varchar2).Value = 
                        string.IsNullOrEmpty(description) ? (object)DBNull.Value : description;
                    cmd.Parameters.Add("p_details", OracleDbType.Clob).Value = 
                        string.IsNullOrEmpty(details) ? (object)DBNull.Value : details;
                    cmd.Parameters.Add("p_client_ip", OracleDbType.Varchar2).Value = 
                        string.IsNullOrEmpty(clientIp) ? (object)DBNull.Value : clientIp;
                    cmd.Parameters.Add("p_status", OracleDbType.Varchar2).Value = status;
                    cmd.Parameters.Add("p_error_message", OracleDbType.Varchar2).Value = 
                        string.IsNullOrEmpty(errorMessage) ? (object)DBNull.Value : errorMessage;
                    
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region Security Events (Encryption, Access)

        /// <summary>
        /// Ghi log sự kiện bảo mật (encryption, decryption, sensitive access)
        /// Chi tiết nhạy cảm sẽ được mã hóa tự động bởi PKG_AUDIT_EVENTS
        /// </summary>
        public void LogSecurityEvent(
            string eventType,
            string performedBy,
            string entityType = null,
            string entityId = null,
            string description = null,
            string sensitiveDetails = null,
            string clientIp = null)
        {
            if (string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(performedBy))
                return;

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        BEGIN 
                            PKG_AUDIT_EVENTS.LOG_SECURITY_EVENT(
                                p_event_type => :p_event_type,
                                p_performed_by => :p_performed_by,
                                p_entity_type => :p_entity_type,
                                p_entity_id => :p_entity_id,
                                p_description => :p_description,
                                p_sensitive_details => :p_sensitive_details,
                                p_client_ip => :p_client_ip
                            ); 
                        END;";
                    
                    cmd.Parameters.Add("p_event_type", OracleDbType.Varchar2).Value = eventType;
                    cmd.Parameters.Add("p_performed_by", OracleDbType.Varchar2).Value = performedBy;
                    cmd.Parameters.Add("p_entity_type", OracleDbType.Varchar2).Value = 
                        string.IsNullOrEmpty(entityType) ? (object)DBNull.Value : entityType;
                    cmd.Parameters.Add("p_entity_id", OracleDbType.Varchar2).Value = 
                        string.IsNullOrEmpty(entityId) ? (object)DBNull.Value : entityId;
                    cmd.Parameters.Add("p_description", OracleDbType.Varchar2).Value = 
                        string.IsNullOrEmpty(description) ? (object)DBNull.Value : description;
                    cmd.Parameters.Add("p_sensitive_details", OracleDbType.Varchar2).Value = 
                        string.IsNullOrEmpty(sensitiveDetails) ? (object)DBNull.Value : sensitiveDetails;
                    cmd.Parameters.Add("p_client_ip", OracleDbType.Varchar2).Value = 
                        string.IsNullOrEmpty(clientIp) ? (object)DBNull.Value : clientIp;
                    
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Lấy lịch sử audit events của một user
        /// </summary>
        public DataTable GetUserEvents(string userId, int limit = 100)
        {
            if (string.IsNullOrEmpty(userId))
                return new DataTable();

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        BEGIN 
                            :result := PKG_AUDIT_EVENTS.GET_USER_EVENTS(
                                p_user_id => :p_user_id,
                                p_limit => :p_limit
                            ); 
                        END;";
                    
                    var resultParam = cmd.Parameters.Add("result", OracleDbType.RefCursor);
                    resultParam.Direction = ParameterDirection.Output;
                    
                    cmd.Parameters.Add("p_user_id", OracleDbType.Varchar2).Value = userId;
                    cmd.Parameters.Add("p_limit", OracleDbType.Int32).Value = limit;
                    
                    cmd.ExecuteNonQuery();

                    var dt = new DataTable();
                    using (var reader = ((OracleRefCursor)resultParam.Value).GetDataReader())
                    {
                        dt.Load(reader);
                    }
                    return dt;
                }
            }
        }

        /// <summary>
        /// Lấy lịch sử audit events của một entity
        /// </summary>
        public DataTable GetEntityEvents(string entityType, string entityId)
        {
            if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(entityId))
                return new DataTable();

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        BEGIN 
                            :result := PKG_AUDIT_EVENTS.GET_ENTITY_EVENTS(
                                p_entity_type => :p_entity_type,
                                p_entity_id => :p_entity_id
                            ); 
                        END;";
                    
                    var resultParam = cmd.Parameters.Add("result", OracleDbType.RefCursor);
                    resultParam.Direction = ParameterDirection.Output;
                    
                    cmd.Parameters.Add("p_entity_type", OracleDbType.Varchar2).Value = entityType;
                    cmd.Parameters.Add("p_entity_id", OracleDbType.Varchar2).Value = entityId;
                    
                    cmd.ExecuteNonQuery();

                    var dt = new DataTable();
                    using (var reader = ((OracleRefCursor)resultParam.Value).GetDataReader())
                    {
                        dt.Load(reader);
                    }
                    return dt;
                }
            }
        }

        #endregion

        #region Convenience Methods

        /// <summary>
        /// Log login success/failure
        /// </summary>
        public void LogLogin(string userId, string clientIp, string userAgent, bool success = true)
        {
            var eventType = success ? "LOGIN_SUCCESS" : "LOGIN_FAILED";
            var description = $"Login attempt at {DateTime.UtcNow:O}";
            
            if (success)
            {
                LogSystemEvent(eventType, userId, description, clientIp, userAgent, "SUCCESS");
            }
            else
            {
                LogSecurityEvent(eventType, userId, "USER", userId, description, $"Failed login for {userId}", clientIp);
            }
        }

        /// <summary>
        /// Log logout
        /// </summary>
        public void LogLogout(string userId, string clientIp = null)
        {
            LogSystemEvent("LOGOUT", userId, "User logged out", clientIp);
        }

        /// <summary>
        /// Log score approval
        /// </summary>
        public void LogScoreApproval(string approvedBy, string studentId, string termId, bool approved, string reason = null)
        {
            var eventType = approved ? "APPROVE_SCORE" : "REJECT_SCORE";
            var description = approved ? "Score approved" : "Score rejected";
            LogBusinessEvent(eventType, approvedBy, "SCORE", $"{studentId}_{termId}", description, reason);
        }

        /// <summary>
        /// Log activity approval
        /// </summary>
        public void LogActivityApproval(string approvedBy, string activityId, bool approved, string reason = null)
        {
            var eventType = approved ? "APPROVE_ACTIVITY" : "REJECT_ACTIVITY";
            var description = approved ? "Activity approved" : "Activity rejected";
            LogBusinessEvent(eventType, approvedBy, "ACTIVITY", activityId, description, reason);
        }

        /// <summary>
        /// Log feedback response
        /// </summary>
        public void LogFeedbackResponse(string respondedBy, string feedbackId, string response)
        {
            LogBusinessEvent("RESPOND_FEEDBACK", respondedBy, "FEEDBACK", feedbackId, "Feedback responded", response);
        }

        /// <summary>
        /// Log data access for sensitive data
        /// </summary>
        public void LogDataAccess(string userId, string tableName, string recordId, string clientIp = null)
        {
            LogSecurityEvent("DATA_ACCESS", userId, tableName, recordId, $"Accessed {tableName} record", null, clientIp);
        }

        /// <summary>
        /// Log encryption/decryption operation
        /// </summary>
        public void LogEncryptionOperation(string userId, string operation, string entityType, string entityId, string clientIp = null)
        {
            LogSecurityEvent($"CRYPTO_{operation}", userId, entityType, entityId, $"{operation} operation performed", null, clientIp);
        }

        #endregion
    }
}
