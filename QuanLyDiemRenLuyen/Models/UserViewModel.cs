using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemRenLuyen.Models
{
    public class UserViewModel
    {
        [Display(Name = "Mã người dùng")]
        public string Id { get; set; } // MAND

        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Vai trò")]
        public string Role { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; }
    }

    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Mã người dùng")]
        [Display(Name = "Mã người dùng (MAND)")]
        public string Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Họ và tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
        [StringLength(100, ErrorMessage = "{0} phải dài ít nhất {2} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Vai trò")]
        [Display(Name = "Vai trò")]
        public string Role { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;
    }

    public class UserEditViewModel
    {
        [Display(Name = "Mã người dùng")]
        public string Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Họ và tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Vai trò")]
        [Display(Name = "Vai trò")]
        public string Role { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới (để trống nếu không đổi)")]
        public string NewPassword { get; set; }
    }

    public class UserIndexViewModel
    {
        public List<UserViewModel> Users { get; set; }
        public string SearchKeyword { get; set; }
        public string FilterRole { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }
}
