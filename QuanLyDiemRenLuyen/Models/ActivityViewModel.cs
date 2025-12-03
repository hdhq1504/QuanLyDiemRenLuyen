using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace QuanLyDiemRenLuyen.Models
{
    /// <summary>
    /// ViewModel cho danh sách hoạt động
    /// </summary>
    public class ActivityListViewModel
    {
        public List<ActivityItem> Activities { get; set; }
        public string SearchKeyword { get; set; }
        public string FilterStatus { get; set; } // ALL, UPCOMING, ONGOING, COMPLETED
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }

        public ActivityListViewModel()
        {
            Activities = new List<ActivityItem>();
            CurrentPage = 1;
            PageSize = 10;
            FilterStatus = "ALL";
        }
    }

    /// <summary>
    /// Model cho một hoạt động trong danh sách
    /// </summary>
    public class ActivityItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Location { get; set; }
        public int? Points { get; set; }
        public int MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; }
        public string Status { get; set; } // UPCOMING, ONGOING, COMPLETED, CANCELLED
        public bool IsRegistered { get; set; }
        public string RegistrationStatus { get; set; } // PENDING, APPROVED, REJECTED
        public DateTime? RegistrationDeadline { get; set; }
        public string CategoryName { get; set; }
        public string OrganizerName { get; set; }
    }

    /// <summary>
    /// ViewModel cho chi tiết hoạt động
    /// </summary>
    public class ActivityDetailViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Location { get; set; }
        public int? Points { get; set; }
        public int MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; }
        public string Status { get; set; }
        public DateTime? RegistrationDeadline { get; set; }
        public string CategoryName { get; set; }
        public string OrganizerName { get; set; }
        public string Requirements { get; set; }
        public string Benefits { get; set; }
        public DateTime CreatedAt { get; set; }

        // Thông tin đăng ký của sinh viên
        public bool IsRegistered { get; set; }
        public string RegistrationStatus { get; set; }
        public DateTime? RegisteredAt { get; set; }
        public string ProofStatus { get; set; } // NOT_UPLOADED, PENDING, APPROVED, REJECTED
        public string ProofFilePath { get; set; }
        public string ProofNote { get; set; }
        public DateTime? ProofUploadedAt { get; set; }
        public int? ApprovedPoints { get; set; }

        // Kiểm tra có thể đăng ký không
        public bool CanRegister { get; set; }
        public bool CanCancelRegistration { get; set; }
        public bool CanUploadProof { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// ViewModel cho upload minh chứng
    /// </summary>
    public class UploadProofViewModel
    {
        public string ActivityId { get; set; }
        public string ActivityTitle { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn file minh chứng")]
        public HttpPostedFileBase ProofFile { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string Note { get; set; }

        public string CurrentProofFilePath { get; set; }
        public string CurrentProofStatus { get; set; }
        public DateTime? CurrentProofUploadedAt { get; set; }
    }

    /// <summary>
    /// ViewModel cho kết quả upload
    /// </summary>
    public class UploadProofResultViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    /// <summary>
    /// ViewModel cho đăng ký hoạt động
    /// </summary>
    public class RegisterActivityViewModel
    {
        public string ActivityId { get; set; }
        public string StudentId { get; set; }
        public string Note { get; set; }
    }

    /// <summary>
    /// ViewModel cho kết quả đăng ký
    /// </summary>
    public class RegisterActivityResultViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime? RegisteredAt { get; set; }
    }
}

