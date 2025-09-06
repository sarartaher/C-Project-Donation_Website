using Donation_Website.Data;

namespace Donation_Website.Models
{
    public class DonationCategory
    {
        public int DonationCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Priority { get; set; }
        public bool IsActive { get; set; }

        // Navigation
        public ICollection<Project>? Projects { get; set; }
    }
}
