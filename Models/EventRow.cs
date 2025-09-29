namespace Donation_Website.Models
{
    public class EventRow
    {
        public int EventId { get; set; }           // Project.ProjectID
        public int CategoryID { get; set; }        // DonationCategory.CategoryID
        public string CategoryName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

}
