using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDiemRenLuyen.Models
{
    public class LecturerActivityListViewModel
    {
        public List<LecturerActivityItem> Activities { get; set; } = new List<LecturerActivityItem>();
        public string SearchKeyword { get; set; }
        public string FilterStatus { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }

    public class LecturerActivityItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Location { get; set; }
        public decimal? Points { get; set; }
        public int MaxSeats { get; set; }
        public int CurrentParticipants { get; set; }
        public string Status { get; set; }
        public string ApprovalStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ActivityFormViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên hoạt động")]
        [Display(Name = "Tên hoạt động")]
        public string Title { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Display(Name = "Yêu cầu")]
        public string Requirements { get; set; }

        [Display(Name = "Quyền lợi")]
        public string Benefits { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn học kỳ")]
        [Display(Name = "Học kỳ")]
        public string TermId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thời gian bắt đầu")]
        [Display(Name = "Thời gian bắt đầu")]
        public DateTime StartAt { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thời gian kết thúc")]
        [Display(Name = "Thời gian kết thúc")]
        public DateTime EndAt { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tối đa")]
        [Display(Name = "Số lượng tối đa")]
        [Range(1, 10000, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int MaxSeats { get; set; }

        [Display(Name = "Địa điểm")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập điểm")]
        [Display(Name = "Điểm rèn luyện")]
        [Range(0, 100, ErrorMessage = "Điểm phải từ 0 đến 100")]
        public decimal Points { get; set; }
    }
}
