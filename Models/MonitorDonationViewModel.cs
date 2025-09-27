namespace Donation_Website.Models
{
    public class MonitorDonationViewModel
    {
        public string DonorName { get; set; }
        public string FundraiserTitle { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
    }
}
