using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemRenLuyen.Models
{
    /// <summary>
    /// ViewModel cho trang đăng ký
    /// </summary>
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Mã người dùng là bắt buộc")]
        [Display(Name = "Mã người dùng (MSSV/MSGV)")]
        [StringLength(50)]
        public string MAND { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        [StringLength(255)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [Display(Name = "Họ và tên")]
        [StringLength(255)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        [Display(Name = "Vai trò")]
        public string RoleName { get; set; }
    }
}

