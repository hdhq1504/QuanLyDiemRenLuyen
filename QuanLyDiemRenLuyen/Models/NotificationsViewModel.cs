using System;
using System.Collections.Generic;

namespace QuanLyDiemRenLuyen.Models
{
    /// <summary>
    /// ViewModel cho trang thông báo của sinh viên
    /// </summary>
    public class NotificationsViewModel
    {
        public List<NotificationItem> Notifications { get; set; }
        public int UnreadCount { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string CurrentFilter { get; set; } // "all", "unread", "read"

        public NotificationsViewModel()
        {
            Notifications = new List<NotificationItem>();
            CurrentFilter = "all";
            CurrentPage = 1;
        }
    }

    public class NotificationItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public string Icon { get; set; }

        /// <summary>
        /// Lấy icon dựa trên nội dung thông báo
        /// </summary>
        public string GetIcon()
        {
            if (string.IsNullOrEmpty(Icon))
            {
                var combined = (Title + " " + Content).ToLower();

                if (combined.Contains("hoạt động") || combined.Contains("activity"))
                    return "fas fa-calendar-alt";
                if (combined.Contains("điểm") || combined.Contains("score"))
                    return "fas fa-chart-line";
                if (combined.Contains("deadline") || combined.Contains("hạn"))
                    return "fas fa-clock";
                if (combined.Contains("duyệt") || combined.Contains("approved"))
                    return "fas fa-check-circle";
                if (combined.Contains("cảnh báo") || combined.Contains("warning") || combined.Contains("nhắc"))
                    return "fas fa-exclamation-triangle";

                return "fas fa-bell";
            }
            return Icon;
        }

        /// <summary>
        /// Lấy thời gian tương đối
        /// </summary>
        public string GetRelativeTime()
        {
            var timeSpan = DateTime.Now - CreatedAt;

            if (timeSpan.TotalMinutes < 1) return "Vừa xong";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} giờ trước";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} ngày trước";
            if (timeSpan.TotalDays < 30) return $"{(int)(timeSpan.TotalDays / 7)} tuần trước";

            return CreatedAt.ToString("dd/MM/yyyy");
        }

        /// <summary>
        /// Lấy màu của icon
        /// </summary>
        public string GetIconColor()
        {
            var icon = GetIcon();
            if (icon.Contains("calendar")) return "#2563eb"; // Blue
            if (icon.Contains("chart")) return "#16a34a"; // Green
            if (icon.Contains("clock")) return "#f59e0b"; // Orange
            if (icon.Contains("check")) return "#16a34a"; // Green
            if (icon.Contains("exclamation")) return "#dc2626"; // Red
            return "#64748b"; // Gray
        }
    }
}
