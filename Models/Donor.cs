namespace Donation_Website.Models
{
    public class Donor
    {
        public int DonorID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public ICollection<Cart> Carts { get; set; }
        public ICollection<Donation> Donations { get; set; }
        public ICollection<Payment> Payments { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<FinanceLog> FinanceLogs { get; set; }
    }
}
