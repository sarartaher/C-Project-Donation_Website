namespace Donation_Website.Models
{
    public class DonationRecord
    {
        public int DonationID { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public string Gateway { get; set; }
        public DateTime TransactionDate { get; set; }
        public string DonorName { get; set; } = "";
        public string DonorEmail { get; set; } = "";
    }

}
