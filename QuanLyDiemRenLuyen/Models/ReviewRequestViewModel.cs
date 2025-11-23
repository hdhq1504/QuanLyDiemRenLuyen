using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemRenLuyen.Models
{
    /// <summary>
    /// ViewModel cho danh sách đơn phúc khảo
    /// </summary>
    public class ReviewRequestListViewModel
    {
        public List<ReviewRequestItem> Requests { get; set; }
        public string FilterStatus { get; set; } // ALL, SUBMITTED, IN_REVIEW, APPROVED, REJECTED
        public string FilterTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }

        public ReviewRequestListViewModel()
        {
            Requests = new List<ReviewRequestItem>();
            CurrentPage = 1;
            PageSize = 20;
            FilterStatus = "ALL";
        }
    }

    /// <summary>
    /// Thông tin đơn phúc khảo
    /// </summary>
    public class ReviewRequestItem
    {
        public string RequestId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentCode { get; set; }
        public string ClassName { get; set; }
        public string TermId { get; set; }
        public string TermName { get; set; }
        public int TermYear { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Status { get; set; } // SUBMITTED, IN_REVIEW, APPROVED, REJECTED, CLOSED
        public string Response { get; set; }
        public string RespondedBy { get; set; }
        public string RespondedByName { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal CurrentScore { get; set; }
        public decimal RequestedScore { get; set; }
        public string CriterionName { get; set; }
    }

    /// <summary>
    /// ViewModel cho chi tiết đơn phúc khảo
    /// </summary>
    public class ReviewRequestDetailViewModel
    {
        public string Id { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentCode { get; set; }
        public string ClassName { get; set; }
        public string TermId { get; set; }
        public string TermName { get; set; }
        public string CriterionId { get; set; }
        public string CriterionName { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Status { get; set; }
        public string Response { get; set; }
        public string RespondedBy { get; set; }
        public string RespondedByName { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // Thông tin điểm
        public decimal CurrentScore { get; set; }
        public decimal? RequestedScore { get; set; }
        public decimal? ApprovedScore { get; set; }

        // File đính kèm
        public List<AttachmentItem> Attachments { get; set; }

        public ReviewRequestDetailViewModel()
        {
            Attachments = new List<AttachmentItem>();
        }
    }

    /// <summary>
    /// File đính kèm
    /// </summary>
    public class AttachmentItem
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    /// <summary>
    /// ViewModel cho tạo đơn phúc khảo
    /// </summary>
    public class CreateReviewRequestViewModel
    {
        public string ScoreId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn học kỳ")]
        public string TermId { get; set; }

        public string TermName { get; set; }

        public string CriterionId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung phúc khảo")]
        public string Content { get; set; }

        public decimal CurrentScore { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập điểm yêu cầu")]
        [Range(0, 100, ErrorMessage = "Điểm phải từ 0 đến 100")]
        public decimal RequestedScore { get; set; }
    }

    /// <summary>
    /// ViewModel cho phản hồi đơn phúc khảo
    /// </summary>
    public class RespondReviewRequestViewModel
    {
        [Required]
        public string ReviewRequestId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        public string Status { get; set; } // APPROVED, REJECTED, CLOSED

        [Required(ErrorMessage = "Vui lòng nhập phản hồi")]
        public string Response { get; set; }

        public decimal? ApprovedScore { get; set; }
    }

    // ==================== STUDENT FEEDBACK VIEWMODELS ====================

    /// <summary>
    /// ViewModel cho danh sách phản hồi của sinh viên
    /// </summary>
    public class StudentFeedbackListViewModel
    {
        public List<StudentFeedbackItem> Feedbacks { get; set; }
        public string FilterStatus { get; set; } // ALL, SUBMITTED, RESPONDED, CLOSED
        public string FilterTermId { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public List<TermOption> AvailableTerms { get; set; }

        public StudentFeedbackListViewModel()
        {
            Feedbacks = new List<StudentFeedbackItem>();
            AvailableTerms = new List<TermOption>();
            CurrentPage = 1;
            PageSize = 10;
            FilterStatus = "ALL";
        }
    }

    /// <summary>
    /// Thông tin phản hồi trong danh sách
    /// </summary>
    public class StudentFeedbackItem
    {
        public string Id { get; set; }
        public string TermId { get; set; }
        public string TermName { get; set; }
        public string CriterionName { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public bool HasResponse { get; set; }
    }

    /// <summary>
    /// ViewModel cho chi tiết phản hồi của sinh viên
    /// </summary>
    public class StudentFeedbackDetailViewModel
    {
        public string Id { get; set; }
        public string TermId { get; set; }
        public string TermName { get; set; }
        public string CriterionId { get; set; }
        public string CriterionName { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Status { get; set; }
        public string Response { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public List<FeedbackAttachmentItem> Attachments { get; set; }

        public StudentFeedbackDetailViewModel()
        {
            Attachments = new List<FeedbackAttachmentItem>();
        }
    }

    /// <summary>
    /// File đính kèm của phản hồi
    /// </summary>
    public class FeedbackAttachmentItem
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string StoredPath { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    /// <summary>
    /// ViewModel cho tạo phản hồi mới
    /// </summary>
    public class CreateFeedbackViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn học kỳ")]
        public string TermId { get; set; }

        public string TermName { get; set; }

        public string CriterionId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung phản hồi")]
        public string Content { get; set; }

        public List<TermOption> AvailableTerms { get; set; }
        public List<CriterionOption> AvailableCriteria { get; set; }

        public CreateFeedbackViewModel()
        {
            AvailableTerms = new List<TermOption>();
            AvailableCriteria = new List<CriterionOption>();
        }
    }

    /// <summary>
    /// Option cho dropdown học kỳ
    /// </summary>
    public class TermOption
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Year { get; set; }
    }

    /// <summary>
    /// Option cho dropdown tiêu chí
    /// </summary>
    public class CriterionOption
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int GroupNo { get; set; }
    }

}

