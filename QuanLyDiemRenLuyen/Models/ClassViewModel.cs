using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemRenLuyen.Models
{
    /// <summary>
    /// Thông tin cơ bản của lớp học cho danh sách và chỉnh sửa
    /// </summary>
    public class ClassViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã lớp")]
        [Display(Name = "Mã lớp")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên lớp")]
        [Display(Name = "Tên lớp")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khoa")]
        [Display(Name = "Khoa")]
        public string DepartmentId { get; set; }

        public string DepartmentName { get; set; }
        public int StudentCount { get; set; }
        
        // Thông tin CVHT
        public string CvhtId { get; set; }
        public string CvhtName { get; set; }
        public string CvhtEmail { get; set; }
        public DateTime? CvhtAssignedDate { get; set; }
    }

    /// <summary>
    /// Model hiển thị danh sách lớp với phân trang và lọc
    /// </summary>
    public class ClassIndexViewModel
    {
        public List<ClassViewModel> Classes { get; set; }
        public string SearchKeyword { get; set; }
        public string FilterDepartmentId { get; set; }
        public List<System.Web.Mvc.SelectListItem> Departments { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }

    /// <summary>
    /// Chi tiết lớp học với danh sách sinh viên và thông tin CVHT
    /// </summary>
    public class ClassDetailsViewModel
    {
        public ClassViewModel ClassInfo { get; set; }
        public List<ClassStudentItem> Students { get; set; }
        
        // Thông tin phân công CVHT
        public ClassAdvisorInfo AdvisorInfo { get; set; }
        public List<ClassAdvisorHistoryItem> AssignmentHistory { get; set; }
    }

    /// <summary>
    /// Thông tin sinh viên trong chi tiết lớp
    /// </summary>
    public class ClassStudentItem
    {
        public string Id { get; set; }
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Phone { get; set; }
    }

    /// <summary>
    /// Thông tin chi tiết về CVHT của lớp
    /// </summary>
    public class ClassAdvisorInfo
    {
        public string AssignmentId { get; set; }
        public string ClassId { get; set; }
        public string ClassName { get; set; }
        public string ClassCode { get; set; }
        public string LecturerId { get; set; }
        public string LecturerName { get; set; }
        public string LecturerEmail { get; set; }
        public int StudentCount { get; set; }
        public DateTime? AssignedDate { get; set; }
        public string AssignedBy { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>
    /// Bản ghi lịch sử phân công CVHT
    /// </summary>
    public class ClassAdvisorHistoryItem
    {
        public string Id { get; set; }
        public string LecturerId { get; set; }
        public string LecturerName { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? RemovedAt { get; set; }
        public string AssignedBy { get; set; }
        public string RemovedBy { get; set; }
        public string Notes { get; set; }
        
        public bool IsActive => RemovedAt == null;
        public string StatusBadge => IsActive ? "success" : "secondary";
        public string StatusText => IsActive ? "Đang hoạt động" : "Đã kết thúc";
    }

    /// <summary>
    /// Model yêu cầu phân công CVHT
    /// </summary>
    public class AssignAdvisorRequest
    {
        [Required(ErrorMessage = "Class ID is required")]
        public string ClassId { get; set; }
        
        [Required(ErrorMessage = "Lecturer ID is required")]
        public string LecturerId { get; set; }
        
        public string Notes { get; set; }
    }

    /// <summary>
    /// Thông tin khối lượng công việc của giảng viên
    /// </summary>
    public class LecturerWorkload
    {
        public string LecturerId { get; set; }
        public string LecturerName { get; set; }
        public string Email { get; set; }
        public int ClassCount { get; set; }
        public int TotalStudents { get; set; }
        public List<ClassViewModel> Classes { get; set; }
        
        public string WorkloadLevel
        {
            get
            {
                if (ClassCount == 0) return "none";
                if (ClassCount <= 2) return "low";
                if (ClassCount <= 4) return "medium";
                return "high";
            }
        }
        
        public string WorkloadColor
        {
            get
            {
                switch (WorkloadLevel)
                {
                    case "none": return "secondary";
                    case "low": return "success";
                    case "medium": return "warning";
                    case "high": return "danger";
                    default: return "secondary";
                }
            }
        }
    }

    /// <summary>
    /// Model hiển thị báo cáo khối lượng công việc
    /// </summary>
    public class LecturerWorkloadViewModel
    {
        public List<LecturerWorkload> Lecturers { get; set; }
        public int TotalLecturers { get; set; }
        public int AssignedLecturers { get; set; }
        public int UnassignedLecturers { get; set; }
        public int TotalClasses { get; set; }
        public int UnassignedClasses { get; set; }
        
        public double AverageClassesPerLecturer
        {
            get
            {
                if (AssignedLecturers == 0) return 0;
                return (double)TotalClasses / AssignedLecturers;
            }
        }
    }

    /// <summary>
    /// Mục phân công hàng loạt
    /// </summary>
    public class BulkAssignmentItem
    {
        public string ClassId { get; set; }
        public string ClassName { get; set; }
        public string ClassCode { get; set; }
        public string CurrentCvht { get; set; }
        public string SelectedLecturerId { get; set; }
        public int StudentCount { get; set; }
    }

    /// <summary>
    /// Model hiển thị giao diện phân công hàng loạt
    /// </summary>
    public class BulkAssignmentViewModel
    {
        public List<BulkAssignmentItem> Classes { get; set; }
        public List<System.Web.Mvc.SelectListItem> AvailableLecturers { get; set; }
        public string FilterDepartmentId { get; set; }
        public string FilterStatus { get; set; } // "assigned", "unassigned", "all"
        public List<System.Web.Mvc.SelectListItem> Departments { get; set; }
        
        public int TotalClasses { get; set; }
        public int AssignedClasses { get; set; }
        public int UnassignedClasses { get; set; }
    }

    /// <summary>
    /// Model yêu cầu phân công hàng loạt
    /// </summary>
    public class BulkAssignmentRequest
    {
        public List<SingleAssignment> Assignments { get; set; }
        public string Notes { get; set; }
    }

    public class SingleAssignment
    {
        public string ClassId { get; set; }
        public string LecturerId { get; set; }
    }

    /// <summary>
    /// Kết quả phân công hàng loạt
    /// </summary>
    public class BulkAssignmentResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<AssignmentError> Errors { get; set; }
        
        public bool HasErrors => FailureCount > 0;
        public string SummaryMessage
        {
            get
            {
                if (FailureCount == 0)
                    return $"Đã phân công thành công {SuccessCount} lớp";
                return $"Thành công: {SuccessCount}, Thất bại: {FailureCount}";
            }
        }
    }

    public class AssignmentError
    {
        public string ClassId { get; set; }
        public string ClassName { get; set; }
        public string ErrorMessage { get; set; }
    }
}
