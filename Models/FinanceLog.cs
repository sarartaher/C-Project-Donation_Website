namespace Donation_Website.Models
{
    public class FinanceLog
    {
        public int FinanceLogId { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public decimal ChangesinAmount { get; set; }
        public string Description { get; set; } = string.Empty;
        public int DonorID { get; set; }
        public int DonationId { get; set; }
        public int ProjectId { get; set; }

        // Navigation
        public Donor Donor { get; set; }
        public Donation Donation { get; set; }
        public Project Project { get; set; }
    }
}