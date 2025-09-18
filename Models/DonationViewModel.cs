namespace Donation_Website.Models
{
    public class DonationViewModel
    {
        public int DonationId { get; set; }
        public int DonorId { get; set; } // Or DonorId according to your DB
        public int CartId { get; set; } // NEW
        public int FundraiserId { get; set; } // NEW
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string EventName { get; set; } = string.Empty;
    }
}
