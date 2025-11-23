using System;
using System.Collections.Generic;

namespace QuanLyDiemRenLuyen.Models
{
    public class ProofItem
    {
        public string Id { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string ActivityId { get; set; }
        public string ActivityTitle { get; set; }
        public string FileName { get; set; }
        public string StoredPath { get; set; }
        public string Status { get; set; } // SUBMITTED, APPROVED, REJECTED
        public DateTime CreatedAt { get; set; }
    }

    public class ProofListViewModel
    {
        public List<ProofItem> Proofs { get; set; }
        public string FilterStatus { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }

    public class ProofDetailViewModel
    {
        public string Id { get; set; }
        public string RegistrationId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentClass { get; set; }
        public string ActivityId { get; set; }
        public string ActivityTitle { get; set; }
        public DateTime ActivityDate { get; set; }
        public string FileName { get; set; }
        public string StoredPath { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public string Note { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    public class MyRegistrationItem
    {
        public string ActivityId { get; set; }
        public string ActivityTitle { get; set; }
        public DateTime StartAt { get; set; }
        public string Location { get; set; }
        public decimal? Points { get; set; }
        public string RegistrationStatus { get; set; } // REGISTERED, CHECKED_IN, CANCELLED
        public DateTime RegisteredAt { get; set; }
        public string ProofStatus { get; set; } // NULL, SUBMITTED, APPROVED, REJECTED
        public string ProofPath { get; set; }
    }

    public class MyRegistrationsViewModel
    {
        public List<MyRegistrationItem> Registrations { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
