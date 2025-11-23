using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemRenLuyen.Models
{
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
    }

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

    public class ClassDetailsViewModel
    {
        public ClassViewModel ClassInfo { get; set; }
        public List<ClassStudentItem> Students { get; set; }
    }

    public class ClassStudentItem
    {
        public string Id { get; set; } // User ID (MAND)
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Phone { get; set; }
    }
}
