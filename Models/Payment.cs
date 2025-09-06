namespace Donation_Website.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int DonationId { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Success, Failed
        public DateTime Date { get; set; }

        // Navigation
        public Donation? Donation { get; set; }
    }
}
