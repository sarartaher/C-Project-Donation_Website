namespace Donation_Website.Models
{
    public class Review
    {
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public int ProjectId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;

        // Navigation
        public User? User { get; set; }
        public Project? Project { get; set; }
    }
}
