namespace Donation_Website.Models
{
    public class Volunteer
    {
        public int VolunteerID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Skill { get; set; }
        public string? Availability { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public ICollection<VolunteerAssignment> Assignments { get; set; }
    }
}