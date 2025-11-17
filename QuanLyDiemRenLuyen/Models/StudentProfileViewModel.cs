using System;
using System.Collections.Generic;

namespace QuanLyDiemRenLuyen.Models
{
    /// <summary>
    /// ViewModel cho trang Profile của sinh viên
    /// </summary>
    public class StudentProfileViewModel
    {
        // Thông tin người dùng
        public string MAND { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Thông tin sinh viên
        public string StudentCode { get; set; }
        public string Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public string ClassName { get; set; }
        public string ClassCode { get; set; }
        public string DepartmentName { get; set; }
        public string DepartmentCode { get; set; }

        // Thống kê điểm rèn luyện
        public List<TermScoreInfo> TermScores { get; set; }
        public int TotalActivitiesRegistered { get; set; }
        public int TotalActivitiesCompleted { get; set; }
        public decimal AverageScore { get; set; }

        public StudentProfileViewModel()
        {
            TermScores = new List<TermScoreInfo>();
        }
    }

    /// <summary>
    /// Thông tin điểm rèn luyện theo học kỳ
    /// </summary>
    public class TermScoreInfo
    {
        public string TermId { get; set; }
        public string TermName { get; set; }
        public int TermYear { get; set; }
        public int TermNumber { get; set; }
        public int TotalScore { get; set; }
        public string Classification { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}

