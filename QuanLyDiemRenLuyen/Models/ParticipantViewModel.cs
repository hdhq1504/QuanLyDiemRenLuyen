using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemRenLuyen.Models
{
    // ==================== PARTICIPANTS (CHECK-IN) ====================
    
    /// <summary>
    /// Model cho từng sinh viên tham gia hoạt động
    /// </summary>
    public class ParticipantItem
    {
        public string RegistrationId { get; set; }
        public string StudentId { get; set; }
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string ClassName { get; set; }
        public string Status { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public bool HasProof { get; set; }
        public string ProofStatus { get; set; }
        public string CheckedInBy { get; set; }
        public string CheckedInByName { get; set; }
        public string AttendanceStatus { get; set; }
        public bool ScoreApplied { get; set; }
    }

    /// <summary>
    /// ViewModel cho trang điểm danh
    /// </summary>
    public class ParticipantsViewModel
    {
        public string ActivityId { get; set; }
        public string ActivityTitle { get; set; }
        public DateTime ActivityStartAt { get; set; }
        public DateTime ActivityEndAt { get; set; }
        public List<ParticipantItem> Participants { get; set; }
        public int TotalRegistered { get; set; }
        public int TotalCheckedIn { get; set; }
        public decimal AttendanceRate { get; set; }
        public string SearchKeyword { get; set; }
        public string FilterStatus { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }

    /// <summary>
    /// Model cho bulk check-in (điểm danh hàng loạt)
    /// </summary>
    public class BulkCheckInViewModel
    {
        [Required(ErrorMessage = "Activity ID là bắt buộc")]
        public string ActivityId { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn ít nhất một sinh viên")]
        public List<string> RegistrationIds { get; set; }
    }

    // ==================== ATTENDANCE IMPROVEMENT ====================

    /// <summary>
    /// ViewModel cho trang điểm danh cải tiến (Lecturer)
    /// </summary>
    public class AttendanceViewModel
    {
        public string ActivityId { get; set; }
        public string ActivityTitle { get; set; }
        public DateTime ActivityStartAt { get; set; }
        public DateTime ActivityEndAt { get; set; }
        public string Location { get; set; }
        public int ActivityPoints { get; set; }
        public int AbsencePenalty { get; set; }
        public string AttendanceStatus { get; set; }
        public string ConfirmedByName { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public List<AttendanceRecordItem> Records { get; set; }
        public int TotalStudents { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int PendingCount { get; set; }
        public string SearchKeyword { get; set; }
        public string FilterStatus { get; set; }
    }

    /// <summary>
    /// Model cho từng bản ghi điểm danh
    /// </summary>
    public class AttendanceRecordItem
    {
        public string RegistrationId { get; set; }
        public string StudentId { get; set; }
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public string AttendanceStatus { get; set; }
        public bool ScoreApplied { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? CheckedInAt { get; set; }
    }

    /// <summary>
    /// Model cho cập nhật điểm danh hàng loạt
    /// </summary>
    public class BulkAttendanceUpdateModel
    {
        [Required]
        public string ActivityId { get; set; }
        
        [Required]
        public string Status { get; set; }
        public List<string> RegistrationIds { get; set; }
    }

    /// <summary>
    /// Model cho cập nhật điểm danh từng người
    /// </summary>
    public class SingleAttendanceUpdateModel
    {
        [Required]
        public string RegistrationId { get; set; }
        
        [Required]
        public string Status { get; set; }
    }
}
