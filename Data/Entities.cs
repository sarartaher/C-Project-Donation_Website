using System;
using System.Collections.Generic;

namespace Donation_Website.Data
{
    public class User  // Admin, Donor, Volunteer (by Role)
    {
        public int UserId { get; set; }
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string Role { get; set; } = "Donor"; // Admin | Donor | Volunteer
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Cart> Carts { get; set; } = new List<Cart>();
        public ICollection<Donation> Donations { get; set; } = new List<Donation>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<VolunteerAssignment> VolunteerAssignments { get; set; } = new List<VolunteerAssignment>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }

    public class DonationCategory  // Segments; has Priority
    {
        public int DonationCategoryId { get; set; }
        public string Name { get; set; } = default!;
        public int Priority { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }

    public class Project  // inside a Category
    {
        public int ProjectId { get; set; }
        public int DonationCategoryId { get; set; }
        public DonationCategory DonationCategory { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Fundraiser> Fundraisers { get; set; } = new List<Fundraiser>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<WorksOfOrganization> Works { get; set; } = new List<WorksOfOrganization>();
        public ICollection<VolunteerAssignment> VolunteerAssignments { get; set; } = new List<VolunteerAssignment>();
    }

    public class Fundraiser  // public campaign instance
    {
        public int FundraiserId { get; set; }
        public int ProjectId { get; set; }
        public Project Project { get; set; } = default!;
        public string Title { get; set; } = default!;
        public decimal TargetAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<Donation> Donations { get; set; } = new List<Donation>();
    }

    public class Cart  // one open cart per donor
    {
        public int CartId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = default!;
        public bool IsCheckedOut { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }

    public class CartItem
    {
        public int CartItemId { get; set; }
        public int CartId { get; set; }
        public Cart Cart { get; set; } = default!;
        public int FundraiserId { get; set; }
        public Fundraiser Fundraiser { get; set; } = default!;
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Donation  // finalized at checkout
    {
        public int DonationId { get; set; }
        public int UserId { get; set; }            // Donor
        public User User { get; set; } = default!;
        public int FundraiserId { get; set; }
        public Fundraiser Fundraiser { get; set; } = default!;
        public int? CartId { get; set; }
        public Cart? Cart { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public class Payment  // multiple attempts per donation
    {
        public int PaymentId { get; set; }
        public int DonationId { get; set; }
        public Donation Donation { get; set; } = default!;
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending"; // Pending|Succeeded|Failed|Refunded
        public string? Provider { get; set; }           // e.g., SSLCOMMERZ, Stripe
        public string? TransactionId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public class Review  // one per donor per project
    {
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = default!;
        public int ProjectId { get; set; }
        public Project Project { get; set; } = default!;
        public int Rating { get; set; }  // 1..5
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class WorksOfOrganization
    {
        public int WorksOfOrganizationId { get; set; }
        public int? ProjectId { get; set; } // optional link
        public Project? Project { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public string? MediaUrl { get; set; }
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        public ICollection<VolunteerAssignment> VolunteerAssignments { get; set; } = new List<VolunteerAssignment>();
    }

    public class VolunteerAssignment
    {
        public int VolunteerAssignmentId { get; set; }
        public int UserId { get; set; }  // Volunteer user
        public User User { get; set; } = default!;
        public int? ProjectId { get; set; }
        public Project? Project { get; set; }
        public int? WorksOfOrganizationId { get; set; }
        public WorksOfOrganization? WorksOfOrganization { get; set; }
        public string Status { get; set; } = "Assigned"; // Assigned|InProgress|Done
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public int? Hours { get; set; }
    }

    public class AuditLog
    {
        public int AuditLogId { get; set; }
        public int AdminUserId { get; set; }  // who did the action
        public User AdminUser { get; set; } = default!;
        public string Action { get; set; } = default!; // Create|Update|Delete
        public string EntityName { get; set; } = default!;
        public string EntityId { get; set; } = default!;
        public string? BeforeData { get; set; }
        public string? AfterData { get; set; }
        public string? Ip { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Keyless entity mapped to a SQL View
    public class FundraiserLiveMonitor
    {
        public int FundraiserId { get; set; }
        public decimal TotalCollected { get; set; }
        public DateTime? LastPaymentAt { get; set; }
    }
}

