namespace Donation_Website.Models
{
    public class DonationCategory
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; }


        // Navigation
        public ICollection<Project>? Projects { get; set; }
    }
}