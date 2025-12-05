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
}
