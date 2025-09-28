using Donation_Website.Data;

namespace Donation_Website.Models
{
    public class FundraiserViewModel
    {
        public int FundraiserId { get; set; }
        public int ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }

        public decimal TotalCollected { get; set; }
        public int Percentage { get; set; }
    }
}
