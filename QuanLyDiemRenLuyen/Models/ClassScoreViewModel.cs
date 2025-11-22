using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemRenLuyen.Models
{
    /// <summary>
    /// ViewModel cho danh sách lớp và điểm
    /// </summary>
    public class ClassScoreListViewModel
    {
        public List<ClassItem> Classes { get; set; }
        public string FilterDepartment { get; set; }
        public string FilterTerm { get; set; }
        public string SearchKeyword { get; set; }

        public ClassScoreListViewModel()
        {
            Classes = new List<ClassItem>();
        }
    }

    /// <summary>
    /// Thông tin lớp
    /// </summary>
    public class ClassItem
    {
        public string ClassId { get; set; }
        public string ClassName { get; set; }
        public string ClassCode { get; set; }
        public string DepartmentName { get; set; }
        public string TermId { get; set; }
        public string TermName { get; set; }
        public int TotalStudents { get; set; }
        public int ScoredStudents { get; set; }
        public int ApprovedStudents { get; set; }
        public int PendingApproval { get; set; }
        public decimal AverageScore { get; set; }
        public string AdvisorName { get; set; }
        public bool CanEdit { get; set; }
    }

    /// <summary>
    /// ViewModel cho điểm của một lớp
    /// </summary>
    public class ClassScoreViewModel
    {
        public string ClassId { get; set; }
        public string ClassName { get; set; }
        public string ClassCode { get; set; }
        public string DepartmentName { get; set; }
        public string TermId { get; set; }
        public string TermName { get; set; }
        public string AdvisorId { get; set; }
        public string AdvisorName { get; set; }

        public List<StudentScoreItem> Students { get; set; }
        public ClassScoreStatistics Statistics { get; set; }

        public ClassScoreViewModel()
        {
            Students = new List<StudentScoreItem>();
            Statistics = new ClassScoreStatistics();
        }
    }

    /// <summary>
    /// Điểm của sinh viên trong lớp
    /// </summary>
    public class StudentScoreItem
    {
        public string ScoreId { get; set; }
        public string StudentId { get; set; }
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
        public string Classification { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string ApprovedByName { get; set; }
        public int ActivityCount { get; set; }
        public bool CanEdit { get; set; }
        public bool CanApprove { get; set; }
    }

    /// <summary>
    /// Thống kê điểm lớp
    /// </summary>
    public class ClassScoreStatistics
    {
        public int TotalStudents { get; set; }
        public int ScoredStudents { get; set; }
        public int ApprovedStudents { get; set; }
        public decimal AverageScore { get; set; }
        public decimal HighestScore { get; set; }
        public decimal LowestScore { get; set; }
        public Dictionary<string, int> ClassificationDistribution { get; set; }

        public ClassScoreStatistics()
        {
            ClassificationDistribution = new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// ViewModel cho chỉnh sửa điểm
    /// </summary>
    public class EditScoreViewModel
    {
        [Required]
        public string ScoreId { get; set; }

        [Required]
        public string StudentId { get; set; }

        public string StudentName { get; set; }

        [Required]
        public string TermId { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "Điểm phải từ 0 đến 100")]
        public decimal Total { get; set; }

        public string Reason { get; set; }

        public List<CriterionScoreEdit> CriterionScores { get; set; }

        public EditScoreViewModel()
        {
            CriterionScores = new List<CriterionScoreEdit>();
        }
    }

    /// <summary>
    /// Chỉnh sửa điểm theo tiêu chí
    /// </summary>
    public class CriterionScoreEdit
    {
        public string CriterionId { get; set; }
        public string CriterionName { get; set; }
        public decimal MaxPoints { get; set; }
        public decimal EarnedPoints { get; set; }
    }

    /// <summary>
    /// ViewModel cho phê duyệt điểm
    /// </summary>
    public class ApproveScoreViewModel
    {
        public List<string> ScoreIds { get; set; }
        public string Action { get; set; } // APPROVE, REJECT
        public string Note { get; set; }

        public ApproveScoreViewModel()
        {
            ScoreIds = new List<string>();
        }
    }
}

