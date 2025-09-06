using Donation_Website.Data;

namespace Donation_Website.Models
{
    public class Donation
    {
        public int DonationId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }

        // Navigation
        public User? User { get; set; }
        public ICollection<Payment>? Payments { get; set; }
    }
}
