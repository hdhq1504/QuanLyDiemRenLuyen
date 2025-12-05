using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;

namespace QuanLyDiemRenLuyen.Models
{
    /// <summary>
    /// Model for Database User credentials
    /// </summary>
    public class DbUserCredential
    {
        public string Id { get; set; }
        public string AppUserMand { get; set; }
        public string DbUsername { get; set; }
        public string DbPasswordHash { get; set; }
        public string DbRole { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; }
        
        // For display purposes
        public string AppUserFullName { get; set; }
        public string AppUserEmail { get; set; }
        public string AppRole {get; set; }
    }

    /// <summary>
    /// View model for Database User Management page
    /// </summary>
    public class DatabaseUserManagementViewModel
    {
        public List<DbUserCredential> DbUsers { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        
        // Statistics by role
        public Dictionary<string, int> UsersByRole { get; set; }
        
        public DatabaseUserManagementViewModel()
        {
            DbUsers = new List<DbUserCredential>();
            UsersByRole = new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// Model for creating new DB user
    /// </summary>
    public class CreateDbUserRequest
    {
        public string AppUserMand { get; set; }
        public string Result { get; set; }
        public string DbUsername { get; set; }
        public string DbPassword { get; set; } // Only returned on creation
    }

    /// <summary>
    /// Role permission detail
    /// </summary>
    public class RolePermission
    {
        public string RoleName { get; set; }
        public string TableName { get; set; }
        public string Privilege { get; set; }
        public bool IsGrantable { get; set; }
    }

    /// <summary>
    /// Access Control Matrix View Model
    /// </summary>
    public class AccessControlMatrixViewModel
    {
        public List<RolePermission> Permissions { get; set; }
        public List<string> Roles { get; set; }
        public List<string> Tables { get; set; }
        
        public AccessControlMatrixViewModel()
        {
            Permissions = new List<RolePermission>();
            Roles = new List<string> { "ROLE_STUDENT", "ROLE_LECTURER", "ROLE_ADMIN", "ROLE_READONLY" };
            Tables = new List<string>();
        }
        
        /// <summary>
        /// Check if role has permission on table
        /// </summary>
        public string GetPermission(string role, string table)
        {
            var perms = Permissions.FindAll(p => p.RoleName == role && p.TableName == table);
            if (perms.Count == 0) return "-";
            
            var privList = new List<string>();
            foreach (var p in perms)
            {
                privList.Add(p.Privilege.Substring(0, 1)); // S, I, U, D (Select, Insert, Update, Delete)
            }
            return string.Join(",", privList);
        }
    }
}
