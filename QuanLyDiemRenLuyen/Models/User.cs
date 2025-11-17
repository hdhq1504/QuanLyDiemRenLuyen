using System;

namespace QuanLyDiemRenLuyen.Models
{
    /// <summary>
    /// Model đại diện cho người dùng trong hệ thống
    /// </summary>
    public class User
    {
        public string MAND { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
        public string RoleName { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime? LockoutEndUtc { get; set; }
    }
}

