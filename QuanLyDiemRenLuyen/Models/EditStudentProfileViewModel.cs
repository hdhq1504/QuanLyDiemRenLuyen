using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemRenLuyen.Models
{
    /// <summary>
    /// ViewModel cho chỉnh sửa thông tin sinh viên
    /// </summary>
    public class EditStudentProfileViewModel
    {
        [Required(ErrorMessage = "Mã người dùng là bắt buộc")]
        public string MAND { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        [StringLength(255, ErrorMessage = "Họ và tên không được vượt quá 255 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Giới tính")]
        public string Gender { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Display(Name = "URL Avatar")]
        [Url(ErrorMessage = "URL không hợp lệ")]
        public string AvatarUrl { get; set; }

        // Thông tin chỉ đọc (không cho phép chỉnh sửa)
        public string StudentCode { get; set; }
        public string ClassName { get; set; }
        public string DepartmentName { get; set; }
    }
}

