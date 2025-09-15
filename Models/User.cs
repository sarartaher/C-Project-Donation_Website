using Donation_Website.Data;

namespace Donation_Website.Models
{
    public class User
    {

        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Donor"; // Admin, Donor, Volunteer
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Skill { get; set; } // For Volunteers

        // Navigation
        public ICollection<Cart>? Carts { get; set; }
        public ICollection<Review>? Reviews { get; set; }
        public ICollection<AuditLog>? AuditLogs { get; set; }
        public User() { }
    }
}
