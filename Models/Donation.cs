using Donation_Website.Data;

namespace Donation_Website.Models
{
    public class Donation
    {
        public int DonationID { get; set; }
        public int DonorID { get; set; }
        public int CartID { get; set; }
        public int FundraiserID { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string? Status { get; set; }
        public DateTime? Date { get; set; }

        // Navigation
        public Donor Donor { get; set; }
        public Cart Cart { get; set; }
        public Fundraiser Fundraiser { get; set; }
        public ICollection<Payment> Payments { get; set; }
    }
}