using System;
using System.Collections.Generic;

namespace QuanLyDiemRenLuyen.Models
{
    /// <summary>
    /// Class Score Permission - represents temporary access grant
    /// </summary>
    public class ClassScorePermission
    {
        public string Id { get; set; }
        public string ClassId { get; set; }
        public string ClassName { get; set; }
        public string ClassCode { get; set; }
        public string GrantedBy { get; set; }
        public string GrantedByName { get; set; }
        public string GrantedTo { get; set; }
        public string GrantedToName { get; set; }
        public string GranteeRole { get; set; }
        public string PermissionType { get; set; } // VIEW, EDIT, APPROVE
        public DateTime GrantedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string RevokedBy { get; set; }
        public bool IsActive { get; set; }
        public string Notes { get; set; }

        // Helper properties
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.Now;
        public bool IsRevoked => RevokedAt.HasValue;
        public string StatusDisplay =>
            IsRevoked ? "Revoked" :
            IsExpired ? "Expired" :
            IsActive ? "Active" : "Inactive";
    }

    /// <summary>
    /// Request model for granting new permission
    /// </summary>
    public class GrantPermissionRequest
    {
        public string ClassId { get; set; }
        public string GrantedTo { get; set; }
        public string PermissionType { get; set; } // VIEW, EDIT, APPROVE
        public DateTime? ExpiresAt { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>
    /// View model for permission management page
    /// </summary>
    public class PermissionManagementViewModel
    {
        public string ClassId { get; set; }
        public string ClassName { get; set; }
        public string ClassCode { get; set; }
        
        // Current active permissions
        public List<ClassScorePermission> ActivePermissions { get; set; }
        
        // All permissions (including revoked/expired)
        public List<ClassScorePermission> AllPermissions { get; set; }
        
        // Available users to grant to
        public List<AvailableGrantee> AvailableGrantees { get; set; }
        
        // Statistics
        public int TotalPermissions { get; set; }
        public int ActiveCount { get; set; }
        public int ExpiredCount { get; set; }
        public int RevokedCount { get; set; }

        public PermissionManagementViewModel()
        {
            ActivePermissions = new List<ClassScorePermission>();
            AllPermissions = new List<ClassScorePermission>();
            AvailableGrantees = new List<AvailableGrantee>();
        }
    }

    /// <summary>
    /// Available user who can be granted permission
    /// </summary>
    public class AvailableGrantee
    {
        public string Mand { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }

    /// <summary>
    /// View model for My Classes list (CVHT)
    /// </summary>
    public class CvhtClassesViewModel
    {
        public List<CvhtClassInfo> Classes { get; set; }

        public CvhtClassesViewModel()
        {
            Classes = new List<CvhtClassInfo>();
        }
    }

    public class CvhtClassInfo
    {
        public string ClassId { get; set; }
        public string ClassName { get; set; }
        public string ClassCode { get; set; }
        public int StudentCount { get; set; }
        public int ActivePermissionsCount { get; set; }
        public DateTime? LastPermissionGranted { get; set; }
    }

    /// <summary>
    /// Audit log entry for score access
    /// </summary>
    public class ScoreAccessLogEntry
    {
        public string Id { get; set; }
        public string Who { get; set; }
        public string WhoName { get; set; }
        public string Action { get; set; }
        public DateTime EventTime { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }

        // Parsed fields from ACTION
        public string AccessType { get; set; } // VIEW, EDIT, APPROVE, DENIED
        public string ScoreId { get; set; }
        public string PermissionId { get; set; }
        public string Result { get; set; } // SUCCESS, DENIED
        public string AccessMethod { get; set; } // CVHT, GRANTED
    }

    /// <summary>
    /// View model for access log viewer
    /// </summary>
    public class AccessLogViewModel
    {
        public string ClassId { get; set; }
        public string ClassName { get; set; }
        public List<ScoreAccessLogEntry> LogEntries { get; set; }
        
        // Filters
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string FilterUser { get; set; }
        public string FilterAccessType { get; set; }

        public AccessLogViewModel()
        {
            LogEntries = new List<ScoreAccessLogEntry>();
        }
    }
}
