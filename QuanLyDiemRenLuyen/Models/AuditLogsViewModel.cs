using System;
using System.Collections.Generic;

namespace QuanLyDiemRenLuyen.Models
{
    // ========== AUDIT LOGS VIEWMODELS ==========

    /// <summary>
    /// ViewModel chính cho trang Audit Logs
    /// </summary>
    public class AuditLogsViewModel
    {
        public List<AuditLogItem> AuditLogs { get; set; }
        public AuditStatistics Statistics { get; set; }
        
        // Pagination
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        
        // Filters
        public string TableName { get; set; }
        public string Operation { get; set; }
        public string UserId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        
        // Filter options
        public List<string> AvailableTables { get; set; }
        public List<string> AvailableOperations { get; set; }

        public AuditLogsViewModel()
        {
            AuditLogs = new List<AuditLogItem>();
            Statistics = new AuditStatistics();
            AvailableTables = new List<string>();
            AvailableOperations = new List<string>();
        }
    }

    /// <summary>
    /// Chi tiết một bản ghi audit
    /// </summary>
    public class AuditLogItem
    {
        public string Id { get; set; }
        public string TableName { get; set; }
        public string RecordId { get; set; }
        public string Operation { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public string ChangedColumns { get; set; }
        public string PerformedBy { get; set; }
        public string PerformerName { get; set; }
        public DateTime PerformedAt { get; set; }
        public string SessionUser { get; set; }
        public string OsUser { get; set; }
        public string ClientIp { get; set; }
        public string ClientHost { get; set; }
        public string Justification { get; set; }

        // Computed properties
        public string OperationBadgeClass
        {
            get
            {
                switch (Operation)
                {
                    case "INSERT": return "badge-success";
                    case "UPDATE": return "badge-warning";
                    case "DELETE": return "badge-danger";
                    default: return "badge-secondary";
                }
            }
        }

        public string OperationDisplayName
        {
            get
            {
                switch (Operation)
                {
                    case "INSERT": return "Thêm mới";
                    case "UPDATE": return "Cập nhật";
                    case "DELETE": return "Xóa";
                    default: return Operation;
                }
            }
        }

        public string TableDisplayName
        {
            get
            {
                switch (TableName)
                {
                    case "SCORES": return "Điểm rèn luyện";
                    case "USERS": return "Người dùng";
                    case "FEEDBACKS": return "Phản hồi";
                    case "ACTIVITIES": return "Hoạt động";
                    case "PROOFS": return "Minh chứng";
                    case "CLASS_LECTURER_ASSIGNMENTS": return "Phân công CVHT";
                    default: return TableName;
                }
            }
        }
    }

    /// <summary>
    /// Thống kê audit
    /// </summary>
    public class AuditStatistics
    {
        public int TodayCount { get; set; }
        public int WeekCount { get; set; }
        public int TodayUsers { get; set; }
        public int ScoresChanges { get; set; }
    }

    /// <summary>
    /// Thống kê theo ngày
    /// </summary>
    public class DailySummaryItem
    {
        public DateTime Date { get; set; }
        public string TableName { get; set; }
        public string Operation { get; set; }
        public int ChangeCount { get; set; }
        public int UniqueUsers { get; set; }
    }

    // ========== FGA (Fine-Grained Auditing) VIEWMODELS ==========

    /// <summary>
    /// ViewModel chính cho trang FGA Logs
    /// </summary>
    public class FgaLogsViewModel
    {
        public List<FgaLogItem> FgaLogs { get; set; }
        public FgaStatistics Statistics { get; set; }
        
        // Pagination
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        
        // Filters
        public string PolicyName { get; set; }
        public string ObjectName { get; set; }
        public string DbUser { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        
        // Filter options
        public List<string> AvailablePolicies { get; set; }
        public List<string> AvailableObjects { get; set; }

        public FgaLogsViewModel()
        {
            FgaLogs = new List<FgaLogItem>();
            Statistics = new FgaStatistics();
            AvailablePolicies = new List<string>();
            AvailableObjects = new List<string>();
        }
    }

    /// <summary>
    /// Chi tiết một bản ghi FGA
    /// </summary>
    public class FgaLogItem
    {
        public DateTime Timestamp { get; set; }
        public string DbUser { get; set; }
        public string OsUser { get; set; }
        public string UserHost { get; set; }
        public string ClientIp { get; set; }
        public string ObjectSchema { get; set; }
        public string ObjectName { get; set; }
        public string PolicyName { get; set; }
        public string SqlText { get; set; }
        public string SqlBind { get; set; }
        public string StatementType { get; set; }
        public string ExtendedTimestamp { get; set; }
        public string SessionId { get; set; }
        public string EntryId { get; set; }

        // Computed properties
        public string ObjectDisplayName
        {
            get
            {
                switch (ObjectName)
                {
                    case "STUDENTS": return "Sinh viên";
                    case "SCORES": return "Điểm rèn luyện";
                    case "FEEDBACKS": return "Phản hồi";
                    case "USERS": return "Người dùng";
                    default: return ObjectName;
                }
            }
        }

        public string PolicyDisplayName
        {
            get
            {
                switch (PolicyName)
                {
                    case "FGA_STUDENTS_SENSITIVE": return "Thông tin nhạy cảm SV";
                    case "FGA_SCORES_READ": return "Xem điểm";
                    case "FGA_FEEDBACKS_CONTENT": return "Nội dung phản hồi";
                    case "FGA_USERS_PASSWORD": return "Mật khẩu người dùng";
                    default: return PolicyName;
                }
            }
        }

        public string TruncatedSqlText
        {
            get
            {
                if (string.IsNullOrEmpty(SqlText)) return "";
                return SqlText.Length > 200 ? SqlText.Substring(0, 200) + "..." : SqlText;
            }
        }
    }

    /// <summary>
    /// Thống kê FGA
    /// </summary>
    public class FgaStatistics
    {
        public int TodayCount { get; set; }
        public int WeekCount { get; set; }
        public int SensitiveAccessCount { get; set; }
        public int UniqueUsersAccessing { get; set; }
    }
}
