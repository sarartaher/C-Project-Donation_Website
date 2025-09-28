namespace Donation_Website.Models
{
    public class Review
    {
        public int ReviewId { get; set; }
        public int DonorId { get; set; }
        public int ProjectId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        // Navigation
        public User? User { get; set; }
        public Project? Project { get; set; }

        // Computed properties for display
        public string UserName => User?.Name ?? "Anonymous";
        public string ProjectTitle => Project?.Title ?? "Unknown Event";
    }
}
