using System;
using System.Collections.Generic;

namespace QuanLyDiemRenLuyen.Models
{
    // ==================== TABLESPACE MANAGEMENT ====================
    
    public class TablespaceInfo
    {
        public string Name { get; set; }
        public decimal TotalSpaceMB { get; set; }
        public decimal UsedSpaceMB { get; set; }
        public decimal FreeSpaceMB { get; set; }
        public decimal UsagePercent { get; set; }
        
        public string GetUsageColor()
        {
            if (UsagePercent >= 90) return "#ef4444"; // Red
            if (UsagePercent >= 75) return "#f59e0b"; // Orange
            return "#10b981"; // Green
        }
    }

    // ==================== PROFILE MANAGEMENT ====================
    
    public class UserProfileInfo
    {
        public string ProfileName { get; set; }
        public string ResourceName { get; set; }
        public string ResourceType { get; set; }
        public string Limit { get; set; }
    }

    public class ProfileListViewModel
    {
        public List<string> ProfileNames { get; set; }
        public Dictionary<string, List<UserProfileInfo>> ProfileResources { get; set; }
    }

    // ==================== SESSION MANAGEMENT ====================
    
    public class SessionInfo
    {
        public int Sid { get; set; }
        public int Serial { get; set; }
        public string Username { get; set; }
        public string Status { get; set; }
        public string SchemaName { get; set; }
        public string OsUser { get; set; }
        public string Machine { get; set; }
        public string Program { get; set; }
        public DateTime LogonTime { get; set; }
        public int MinutesConnected { get; set; }
        public int SecondsSinceLastCall { get; set; }
        
        public bool IsActive => Status == "ACTIVE";
        public bool IsIdle => SecondsSinceLastCall > 300; // 5 minutes
    }

    public class SessionListViewModel
    {
        public List<SessionInfo> Sessions { get; set; }
        public string FilterStatus { get; set; }
        public string SearchKeyword { get; set; }
        public int TotalSessions { get; set; }
        public int ActiveSessions { get; set; }
        public int InactiveSessions { get; set; }
    }

    // ==================== DATABASE DASHBOARD ====================
    
    public class DatabaseDashboardViewModel
    {
        public List<TablespaceInfo> TopTablespaces { get; set; }
        public List<SessionInfo> RecentSessions { get; set; }
        
        // Statistics
        public int TotalTablespaces { get; set; }
        public decimal HighestUsagePercent { get; set; }
        public int TotalSessions { get; set; }
        public int ActiveSessionCount { get; set; }
        
        // Alerts
        public int HighUsageTablespaceCount { get; set; } // > 85%
        public int LongRunningSessions { get; set; } // > 1 hour
    }
}
