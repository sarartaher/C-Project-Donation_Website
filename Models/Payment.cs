namespace Donation_Website.Models
{
    public class Payment
    {
        public int PaymentID { get; set; }
        public int DonationID { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
        public string? Gateway { get; set; }
        public string? GatewayTxnID { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public Donation Donation { get; set; }
    }
}