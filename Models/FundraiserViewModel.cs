using Donation_Website.Data;

namespace Donation_Website.Models
{
    public class FundraiserViewModel
    {
        public int FundraiserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int ProjectId { get; set; }

        // Navigation
        public Project? Project { get; set; }
        public ICollection<CartItem>? CartItems { get; set; }
    }
}
