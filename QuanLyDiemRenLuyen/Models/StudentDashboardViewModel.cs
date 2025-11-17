using System;
using System.Collections.Generic;

namespace QuanLyDiemRenLuyen.Models
{
    /// <summary>
    /// ViewModel cho dashboard sinh viên
    /// </summary>
    public class StudentDashboardViewModel
    {
        public User User { get; set; }
        public StudentInfo StudentInfo { get; set; }
        public List<TermScore> TermScores { get; set; }
        public List<ActivityRegistration> RecentActivities { get; set; }
        public List<Notification> UnreadNotifications { get; set; }
        public DashboardStatistics Statistics { get; set; }
    }

    public class StudentInfo
    {
        public string StudentCode { get; set; }
        public string ClassName { get; set; }
        public string DepartmentName { get; set; }
        public DateTime? DOB { get; set; }
        public string Gender { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }

    public class TermScore
    {
        public string TermId { get; set; }
        public string TermName { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }

    public class ActivityRegistration
    {
        public string ActivityId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Location { get; set; }
        public decimal? Points { get; set; }
        public string RegistrationStatus { get; set; }
        public DateTime RegisteredAt { get; set; }
    }

    public class Notification
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }

    public class DashboardStatistics
    {
        public int TotalActivitiesRegistered { get; set; }
        public int TotalActivitiesCompleted { get; set; }
        public decimal CurrentTermScore { get; set; }
        public int UnreadNotificationCount { get; set; }
    }
}

