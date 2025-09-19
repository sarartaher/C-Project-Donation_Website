namespace Donation_Website.Models
{
    public class DonationViewModel
    {
        public int DonationId { get; set; }
        public int CartId { get; set; }
        public int FundraiserId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string SecretName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }
}
