using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemRenLuyen.Models
{
    /// <summary>
    /// ViewModel cho Score với thông tin chữ ký điện tử
    /// </summary>
    public class ScoreSignatureViewModel
    {
        // Score information
        public string ScoreId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentCode { get; set; }
        public string TermId { get; set; }
        public string TermName { get; set; }
        public int TotalScore { get; set; }
        public string Classification { get; set; }
        public string Status { get; set; }

        // Signature information
        public string DigitalSignature { get; set; }
        public string SignatureAlgorithm { get; set; }
        public string SignedDataHash { get; set; }
        public string SignatureKeyId { get; set; }
        public bool SignatureVerified { get; set; }
        public DateTime? LastVerifiedAt { get; set; }
        public string SignedBy { get; set; }
        public DateTime? SignedAt { get; set; }

        // Computed properties
        public bool IsSigned => !string.IsNullOrEmpty(DigitalSignature);
        public bool IsTampered { get; set; }
        public string VerificationStatus { get; set; } // "SIGNED", "VERIFIED", "TAMPERED", "UNSIGNED"
        
        // Audit trail
        public List<SignatureAuditItem> AuditHistory { get; set; }

        public ScoreSignatureViewModel()
        {
            AuditHistory = new List<SignatureAuditItem>();
        }
    }

    /// <summary>
    /// Item trong audit log của signature
    /// </summary>
    public class SignatureAuditItem
    {
        public string Id { get; set; }
        public string ActionType { get; set; }  // SIGN, VERIFY, TAMPER_DETECTED
        public string PerformedBy { get; set; }
        public DateTime PerformedAt { get; set; }
        public string VerificationResult { get; set; }
        public string Notes { get; set; }
        public string DataHashBefore { get; set; }
        public string DataHashAfter { get; set; }
    }

    /// <summary>
    /// ViewModel cho approve score với signature
    /// </summary>
    public class ApproveScoreWithSignatureViewModel
    {
        public string ScoreId { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập ghi chú duyệt điểm")]
        [StringLength(500)]
        public string ApprovalNotes { get; set; }

        public bool CreateDigitalSignature { get; set; } = true;
    }
}
