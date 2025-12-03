using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemRenLuyen.Models
{
    /// <summary>
    /// ViewModel cho encrypted feedback
    /// </summary>
    public class EncryptedFeedbackViewModel
    {
        public string Id { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string TermId { get; set; }
        public string TermName { get; set; }

        // Content (decrypted hoặc masked)
        public string Content { get; set; }
        public string Response { get; set; }
        
        // Encrypted content (raw)
        public string ContentEncrypted { get; set; }
        public string ResponseEncrypted { get; set; }

        // Encryption metadata
        public bool IsEncrypted { get; set; }
        public string EncryptionKeyId { get; set; }
        public DateTime? EncryptedAt { get; set; }
        public string EncryptedBy { get; set; }

        // Access control
        public List<string> AllowedReaders { get; set; }
        public bool CanCurrentUserRead { get; set; }
        public string AccessDeniedMessage { get; set; }

        // Access history
        public List<FeedbackAccessLogItem> AccessHistory { get; set; }

        // Status
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string RespondedBy { get; set; }

        public EncryptedFeedbackViewModel()
        {
            AllowedReaders = new List<string>();
            AccessHistory = new List<FeedbackAccessLogItem>();
        }
    }

    /// <summary>
    /// ViewModel cho tạo encrypted feedback mới
    /// </summary>
    public class CreateEncryptedFeedbackViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn học kỳ")]
        public string TermId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung phúc khảo")]
        [StringLength(2000, ErrorMessage = "Nội dung tối đa 2000 ký tự")]
        [Display(Name = "Nội dung phúc khảo")]
        public string Content { get; set; }

        [Display(Name = "Mã hóa nội dung")]
        public bool EnableEncryption { get; set; } = true;

        [Display(Name = "Người được phép xem")]
        public List<string> AllowedReaders { get; set; }

        public CreateEncryptedFeedbackViewModel()
        {
            AllowedReaders = new List<string>();
        }
    }

    /// <summary>
    /// Item trong feedback access log
    /// </summary>
    public class FeedbackAccessLogItem
    {
        public string Id { get; set; }
        public string AccessedBy { get; set; }
        public string AccessedByName { get; set; }
        public DateTime AccessTime { get; set; }
        public string AccessType { get; set; }  // READ, WRITE, DECRYPT, ACCESS_DENIED
        public string AccessResult { get; set; }  // SUCCESS, DENIED
        public string IpAddress { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>
    /// ViewModel cho respond encrypted feedback
    /// </summary>
    public class RespondEncryptedFeedbackViewModel
    {
        public string FeedbackId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập phản hồi")]
        [StringLength(2000, ErrorMessage = "Phản hồi tối đa 2000 ký tự")]
        [Display(Name = "Phản hồi")]
        public string Response { get; set; }

        [Display(Name = "Mã hóa phản hồi")]
        public bool EncryptResponse { get; set; } = true;
    }
}
