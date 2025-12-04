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
        public string Status { get; set; }  // REGISTERED, CHECKED_IN, CANCELLED
        public DateTime RegisteredAt { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public bool HasProof { get; set; }
        public string ProofStatus { get; set; }
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
        
        // Statistics
        public int TotalRegistered { get; set; }
        public int TotalCheckedIn { get; set; }
        public decimal AttendanceRate { get; set; }  // %
        
        // Pagination & Filters
        public string SearchKeyword { get; set; }
        public string FilterStatus { get; set; }  // ALL, REGISTERED, CHECKED_IN
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
}
