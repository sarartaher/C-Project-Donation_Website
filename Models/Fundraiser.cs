namespace Donation_Website.Models
{
    public class Fundraiser
    {
        public int FundraiserID { get; set; }    
        public int ProjectID { get; set; }
        public string Title { get; set; }
        public decimal? TargetAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
