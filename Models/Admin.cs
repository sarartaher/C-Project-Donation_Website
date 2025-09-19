namespace Donation_Website.Models
{
    public class Admin
    {
        public int AdminId { get; set; }
        public string ?Name { get; set; }
        public string ?Email { get; set; }
        public string ?PasswordHash { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}
