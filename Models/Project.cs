using Donation_Website.Data;

namespace Donation_Website.Models
{
    public class Project
    {
        public int ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public DonationCategory? DonationCategory { get; set; }
        public ICollection<Fundraiser>? Fundraisers { get; set; }
        public ICollection<Review>? Reviews { get; set; }
        public ICollection<WorksOfOrganization>? Works { get; set; }
    }
}
