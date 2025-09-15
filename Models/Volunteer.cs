using Donation_Website.Data;

namespace Donation_Website.Models
{
    public class Volunteer
    {
        public int VolunteerID { get; set; }
        public string ?Name { get; set; }
        public string ?Email { get; set; }
        public string ?PasswordHash { get; set; }
        public string ? Phone { get; set; }
        public string ? Address { get; set; }
        public string ? Skill { get; set; }
        public string ?Availability { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int IsActive { get; set; }

        // Navigation
        public ICollection<VolunteerAssignment>? Assignments { get; set; }
    }
}
