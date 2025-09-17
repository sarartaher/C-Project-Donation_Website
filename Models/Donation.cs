using Donation_Website.Data;

namespace Donation_Website.Models
{
    public class Donation
    {
        public int DonationId { get; set; }
        public int CartId { get; set; }
        public int FundraiserId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Completed, Cancelled
        public int DonatorId { get; set; } 
        public string Currency { get; set; } = "BDT";
        public DateTime Date { get; set; }

        // Navigation
        public User? User { get; set; }
        public ICollection<Payment>? Payments { get; set; }
    }
}
