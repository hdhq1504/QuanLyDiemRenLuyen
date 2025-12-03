using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemRenLuyen.Models
{
    /// <summary>
    /// ViewModel cho thông tin nhạy cảm của sinh viên (đã mã hóa)
    /// </summary>
    public class EncryptedStudentViewModel
    {
        // Thông tin cơ bản (không mã hóa)
        public string UserId { get; set; }
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public string ClassName { get; set; }
        public string Email { get; set; }

        // Thông tin nhạy cảm (plaintext - sau khi giải mã)
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự")]
        public string Phone { get; set; }

        [Display(Name = "Địa chỉ")]
        [StringLength(500, ErrorMessage = "Địa chỉ tối đa 500 ký tự")]
        public string Address { get; set; }

        [Display(Name = "Số CMND/CCCD")]
        [StringLength(20, ErrorMessage = "Số CMND/CCCD tối đa 20 ký tự")]
        [RegularExpression(@"^\d{9}(\d{3})?$", ErrorMessage = "Số CMND/CCCD phải là 9 hoặc 12 chữ số")]
        public string IdCardNumber { get; set; }

        // Metadata về encryption
        public bool IsEncrypted { get; set; }
        public DateTime? EncryptedAt { get; set; }
        public string EncryptionKeyId { get; set; }

        // Dữ liệu masked để hiển thị an toàn
        public string MaskedPhone { get; set; }
        public string MaskedIdCard { get; set; }

        // Flags
        public bool CanViewSensitiveData { get; set; }  // User có quyền xem data nhạy cảm không
        public bool HasEncryptedData { get; set; }       // Có dữ liệu mã hóa trong DB không
    }

    /// <summary>
    /// ViewModel cho form edit thông tin nhạy cảm
    /// </summary>
    public class EditSensitiveInfoViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Số điện thoại phải từ 10-15 ký tự")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [Display(Name = "Địa chỉ")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Địa chỉ phải từ 10-500 ký tự")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số CMND/CCCD")]
        [Display(Name = "Số CMND/CCCD")]
        [RegularExpression(@"^\d{9}(\d{3})?$", ErrorMessage = "Số CMND/CCCD phải là 9 hoặc 12 chữ số")]
        public string IdCardNumber { get; set; }

        // Thông tin hiển thị
        public string StudentCode { get; set; }
        public string FullName { get; set; }

        // Current encrypted values (để compare)
        public string CurrentEncryptedPhone { get; set; }
        public string CurrentEncryptedAddress { get; set; }
        public string CurrentEncryptedIdCard { get; set; }
    }
}
