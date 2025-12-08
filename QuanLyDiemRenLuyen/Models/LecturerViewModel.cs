using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemRenLuyen.Models
{
    public class LecturerActivityListViewModel
    {
        public List<LecturerActivityItem> Activities { get; set; } = new List<LecturerActivityItem>();
        public string SearchKeyword { get; set; }
        public string FilterStatus { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }

    public class LecturerActivityItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Location { get; set; }
        public decimal? Points { get; set; }
        public int MaxSeats { get; set; }
        public int CurrentParticipants { get; set; }
        public string Status { get; set; }
        public string ApprovalStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalRegistered { get; set; }
        public int TotalCheckedIn { get; set; }
    }

    public class ActivityFormViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên hoạt động")]
        [Display(Name = "Tên hoạt động")]
        public string Title { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Display(Name = "Yêu cầu")]
        public string Requirements { get; set; }

        [Display(Name = "Quyền lợi")]
        public string Benefits { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn học kỳ")]
        [Display(Name = "Học kỳ")]
        public string TermId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thời gian bắt đầu")]
        [Display(Name = "Thời gian bắt đầu")]
        public DateTime StartAt { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thời gian kết thúc")]
        [Display(Name = "Thời gian kết thúc")]
        public DateTime EndAt { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tối đa")]
        [Display(Name = "Số lượng tối đa")]
        [Range(1, 10000, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int MaxSeats { get; set; }

        [Display(Name = "Địa điểm")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập điểm")]
        [Display(Name = "Điểm rèn luyện")]
        [Range(0, 100, ErrorMessage = "Điểm phải từ 0 đến 100")]
        public decimal Points { get; set; }
        
        [Display(Name = "Thời gian bắt đầu đăng ký")]
        public DateTime? RegistrationStart { get; set; }
        
        [Display(Name = "Hạn đăng ký")]
        public DateTime? RegistrationDeadline { get; set; }
    }

    public class LecturerDashboardViewModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public LecturerStatistics Statistics { get; set; }
        public List<LecturerActivityItem> RecentActivities { get; set; }
        public List<PendingApprovalItem> PendingApprovals { get; set; }
    }

    public class LecturerStatistics
    {
        public int TotalActivities { get; set; }
        public int PendingActivities { get; set; }
        public int ApprovedActivities { get; set; }
        public int TotalStudentsRegistered { get; set; }
    }

    // ===== Lecturer Class Scores ViewModels =====
    
    public class MyClassScoresViewModel
    {
        public List<AssignedClassItem> Classes { get; set; } = new List<AssignedClassItem>();
        public string CurrentTermId { get; set; }
        public string CurrentTermName { get; set; }
    }

    public class AssignedClassItem
    {
        public string ClassId { get; set; }
        public string ClassCode { get; set; }
        public string ClassName { get; set; }
        public string DepartmentName { get; set; }
        public int TotalStudents { get; set; }
        public int StudentsWithScores { get; set; }
        public double AverageScore { get; set; }
        public DateTime AssignedAt { get; set; }
    }

    public class LecturerClassScoreDetailViewModel
    {
        public string ClassId { get; set; }
        public string ClassCode { get; set; }
        public string ClassName { get; set; }
        public string DepartmentName { get; set; }
        public string TermId { get; set; }
        public string TermName { get; set; }
        public List<StudentScoreReadOnlyItem> Students { get; set; } = new List<StudentScoreReadOnlyItem>();
        public ClassScoreStatisticsReadOnly Statistics { get; set; }
    }

    public class StudentScoreReadOnlyItem
    {
        public string StudentId { get; set; }
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public int TotalScore { get; set; }
        public string Classification { get; set; }
        public string Status { get; set; }
        public int ActivityCount { get; set; }
    }

    public class ClassScoreStatisticsReadOnly
    {
        public int TotalStudents { get; set; }
        public int ApprovedStudents { get; set; }
        public double AverageScore { get; set; }
        public int HighestScore { get; set; }
        public int LowestScore { get; set; }
        public int ExcellentCount { get; set; }
        public int GoodCount { get; set; }
        public int FairCount { get; set; }
        public int AverageCount { get; set; }
        public int WeakCount { get; set; }
    }
}
