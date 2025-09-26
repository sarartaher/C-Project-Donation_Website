using Donation_Website.Data;

namespace Donation_Website.Models
{
    public class Project
    {
        public int ProjectID { get; set; }
        public int CategoryID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public DonationCategory Category { get; set; }
        public ICollection<Fundraiser> Fundraisers { get; set; }
        public ICollection<WorkOfOrganization> Works { get; set; }
        public ICollection<VolunteerAssignment> Assignments { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<FinanceLog> FinanceLogs { get; set; }
    }
}