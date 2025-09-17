namespace Donation_Website.Models
{
    public class DonationViewModel
    {
        public int DonationId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string EventName { get; set; } = string.Empty;
    }
}
