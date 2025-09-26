using Donation_Website.Data;

namespace Donation_Website.Models
{
    public class Cart
    {
        public int CartID { get; set; }
        public int DonorID { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public Donor Donor { get; set; }
        public User? User { get; set; }
        public ICollection<Data.CartItem> CartItems { get; set; }
        public ICollection<Donation> Donations { get; set; }
        public ICollection<Payment> Payments { get; set; }
    }
}