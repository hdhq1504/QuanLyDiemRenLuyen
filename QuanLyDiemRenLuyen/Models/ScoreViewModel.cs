using System;
using System.Collections.Generic;

namespace QuanLyDiemRenLuyen.Models
{
    /// <summary>
    /// ViewModel cho danh sách điểm rèn luyện của sinh viên
    /// </summary>
    public class ScoreViewModel
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentCode { get; set; }
        public string ClassName { get; set; }
        public List<TermScoreItem> TermScores { get; set; }
        public ScoreStatistics Statistics { get; set; }

        public ScoreViewModel()
        {
            TermScores = new List<TermScoreItem>();
            Statistics = new ScoreStatistics();
        }
    }

    /// <summary>
    /// Điểm rèn luyện theo học kỳ
    /// </summary>
    public class TermScoreItem
    {
        public string ScoreId { get; set; }
        public string TermId { get; set; }
        public string TermName { get; set; }
        public int TermYear { get; set; }
        public int TermNumber { get; set; }
        public int Total { get; set; }
        public string Status { get; set; } // PROVISIONAL, APPROVED
        public string Classification { get; set; } // Xuất sắc, Giỏi, Khá, Trung bình, Yếu
        public string ApprovedBy { get; set; }
        public string ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool CanRequestReview { get; set; }
        public bool HasPendingReview { get; set; }
    }

    /// <summary>
    /// Thống kê điểm rèn luyện
    /// </summary>
    public class ScoreStatistics
    {
        public int AverageScore { get; set; }
        public int HighestScore { get; set; }
        public int LowestScore { get; set; }
        public int TotalTerms { get; set; }
        public int ApprovedTerms { get; set; }
        public int ProvisionalTerms { get; set; }
        public Dictionary<string, int> ClassificationCount { get; set; }

        public ScoreStatistics()
        {
            ClassificationCount = new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// ViewModel cho chi tiết điểm rèn luyện
    /// </summary>
    public class ScoreDetailViewModel
    {
        public string ScoreId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentCode { get; set; }
        public string ClassName { get; set; }
        public string TermId { get; set; }
        public string TermName { get; set; }
        public int TermYear { get; set; }
        public int TermNumber { get; set; }
        public int Total { get; set; }
        public string Status { get; set; }
        public string Classification { get; set; }
        public string ApprovedBy { get; set; }
        public string ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // Danh sách hoạt động
        public List<ActivityScoreItem> Activities { get; set; }

        // Lịch sử thay đổi
        public List<ScoreHistoryItem> History { get; set; }

        // Phúc khảo
        public ReviewRequestInfo ReviewRequest { get; set; }

        public ScoreDetailViewModel()
        {
            History = new List<ScoreHistoryItem>();
        }
    }

    /// <summary>
    /// Điểm hoạt động
    /// </summary>
    public class ActivityScoreItem
    {
        public string ActivityId { get; set; }
        public string ActivityTitle { get; set; }
        public DateTime Date { get; set; }
        public decimal Points { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// Lịch sử thay đổi điểm
    /// </summary>
    public class ScoreHistoryItem
    {
        public string Id { get; set; }
        public string Action { get; set; } // CREATED, UPDATED, APPROVED, REJECTED
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string ChangedBy { get; set; }
        public string ChangedByName { get; set; }
        public string Reason { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    /// <summary>
    /// Thông tin đơn phúc khảo
    /// </summary>
    public class ReviewRequestInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int RequestedScore { get; set; }
        public string Status { get; set; }
        public string Response { get; set; }
        public string RespondedBy { get; set; }
        public string RespondedByName { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

