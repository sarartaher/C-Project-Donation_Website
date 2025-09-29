namespace Donation_Website.Models
{
    public class EventDto
    {
        public int EventId { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // New: surfaced from active fundraiser (if any)
        public decimal? TargetAmount { get; set; }
    }

}
