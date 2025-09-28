namespace Donation_Website.Models
{
    public class DonationViewModel
    {
        public int CartItemsId { get; set; }
        public int CartId { get; set; }
        public int FundraiserId { get; set; }
        public string EventName { get; set; }
        public decimal? Amount { get; set; }
        public string SecretName { get; set; }
        public DateTime Date { get; set; }

        // NEW: PaymentId
        public int PaymentId { get; set; }
    }
}
