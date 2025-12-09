using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemRenLuyen.Models
{
    // ==================== DASHBOARD ====================
    public class AdminDashboardViewModel
    {
        public int TotalActivities { get; set; }
        public int PendingActivities { get; set; }
        public int ApprovedActivities { get; set; }
        public int RejectedActivities { get; set; }
        
        public int TotalStudents { get; set; }
        public int TotalRegistrations { get; set; }
        public int TotalCheckIns { get; set; }
        
        public int PendingProofs { get; set; }
        public int ApprovedProofs { get; set; }
        
        public List<RecentActivityItem> RecentActivities { get; set; }
        public List<PendingApprovalItem> PendingApprovals { get; set; }
        public List<RecentFeedbackItem> RecentFeedbacks { get; set; }
    }

    public class RecentFeedbackItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string StudentName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
    }

    public class RecentActivityItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public string ApprovalStatus { get; set; }
        public int RegistrationCount { get; set; }
    }

    public class PendingApprovalItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string OrganizerName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Type { get; set; } // "ACTIVITY" or "PROOF"
    }

    // ==================== ACTIVITY MANAGEMENT ====================
    public class AdminActivityListViewModel
    {
        public List<AdminActivityItem> Activities { get; set; }
        public string SearchKeyword { get; set; }
        public string FilterStatus { get; set; }
        public string FilterApprovalStatus { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }

    public class AdminActivityItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Location { get; set; }
        public decimal? Points { get; set; }
        public int MaxSeats { get; set; }
        public int CurrentParticipants { get; set; }
        public string Status { get; set; }
        public string ApprovalStatus { get; set; }
        public string OrganizerName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ==================== ACTIVITY APPROVAL ====================
    public class ActivityApprovalViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Location { get; set; }
        public decimal? Points { get; set; }
        public int MaxSeats { get; set; }
        public string Status { get; set; }
        public string ApprovalStatus { get; set; }
        public string OrganizerName { get; set; }
        public string OrganizerEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string ApprovedBy { get; set; }
        
        public int CurrentRegistrations { get; set; }
        public int CurrentCheckIns { get; set; }
    }

    public class ApprovalActionViewModel
    {
        [Required(ErrorMessage = "Activity ID là bắt buộc")]
        public string ActivityId { get; set; }
        
        [Required(ErrorMessage = "Hành động là bắt buộc")]
        public string Action { get; set; } // "APPROVE" or "REJECT"
        
        public string Note { get; set; }
    }

    // ==================== REGISTRATION MANAGEMENT ====================
    public class RegistrationManagementViewModel
    {
        public string ActivityId { get; set; }
        public string ActivityTitle { get; set; }
        public List<RegistrationItem> Registrations { get; set; }
        public string FilterStatus { get; set; }
        public string SearchKeyword { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }

    public class RegistrationItem
    {
        public string RegistrationId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string ClassName { get; set; }
        public string Status { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? CheckedInAt { get; set; }
        
        public bool HasProof { get; set; }
        public string ProofStatus { get; set; }
        public string ProofFilePath { get; set; }
        public string ProofFileName { get; set; }
        public DateTime? ProofUploadedAt { get; set; }
    }

    public class ProofApprovalViewModel
    {
        [Required]
        public string ProofId { get; set; }
        
        [Required]
        public string Action { get; set; } // "APPROVE" or "REJECT"
        
        public string Note { get; set; }
    }

    // ==================== FEEDBACK MANAGEMENT ====================
    public class AdminFeedbackListViewModel
    {
        public List<AdminFeedbackItem> Feedbacks { get; set; }
        public string FilterStatus { get; set; }
        public string SearchKeyword { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }

    public class AdminFeedbackItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string TermName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string Response { get; set; }
        public string RespondedBy { get; set; }
        
        // Activity-related fields
        public string ActivityId { get; set; }
        public string ActivityTitle { get; set; }
        public decimal? ActivityPoints { get; set; }
        public bool HasCheckedIn { get; set; }
        public DateTime? CheckedInAt { get; set; }
    }

    // ==================== SCHOOL-WIDE SCORES ====================
    public class SchoolWideScoresViewModel
    {
        public string TermId { get; set; }
        public string TermName { get; set; }
        public string ScoreStatus { get; set; } // PROVISIONAL, DRAFT, OFFICIAL
        public DateTime? DraftPublishedAt { get; set; }
        public DateTime? FeedbackDeadline { get; set; }
        public DateTime? OfficialPublishedAt { get; set; }
        public string PublishedBy { get; set; }
        
        // Statistics
        public int TotalStudents { get; set; }
        public int ExcellentCount { get; set; }  // >= 90
        public int GoodCount { get; set; }       // 80-89
        public int FairCount { get; set; }       // 65-79
        public int AverageCount { get; set; }    // 50-64
        public int WeakCount { get; set; }       // < 50
        public int PendingFeedbacks { get; set; }
        
        // Class submission progress
        public int TotalClasses { get; set; }
        public int SubmittedClasses { get; set; }
        public double SubmissionProgress => TotalClasses > 0 ? (SubmittedClasses * 100.0 / TotalClasses) : 0;
        public bool AllClassesSubmitted => TotalClasses > 0 && SubmittedClasses == TotalClasses;
        
        // Filter
        public string FilterDepartment { get; set; }
        public string FilterClass { get; set; }
        public string FilterClassification { get; set; }
        public string SearchKeyword { get; set; }
        
        // Data
        public List<StudentScorePublicationItem> Students { get; set; }
        public List<TermSelectItem> Terms { get; set; }
        public List<DepartmentSelectItem> Departments { get; set; }
        
        // Helpers
        public bool CanPublishDraft => ScoreStatus == "PROVISIONAL" && AllClassesSubmitted;
        public bool CanPublishOfficial => ScoreStatus == "DRAFT" && FeedbackDeadline.HasValue && DateTime.Now > FeedbackDeadline.Value;
        public bool IsInFeedbackPeriod => ScoreStatus == "DRAFT" && FeedbackDeadline.HasValue && DateTime.Now <= FeedbackDeadline.Value;
        public TimeSpan? TimeRemaining => FeedbackDeadline.HasValue ? (TimeSpan?)(FeedbackDeadline.Value - DateTime.Now) : null;
    }

    public class StudentScorePublicationItem
    {
        public string ScoreId { get; set; }
        public string StudentId { get; set; }
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public string ClassCode { get; set; }
        public string ClassName { get; set; }
        public string DepartmentName { get; set; }
        public int TotalScore { get; set; }
        public string Classification { get; set; }
        public string Status { get; set; }
    }

    public class TermSelectItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class DepartmentSelectItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}

