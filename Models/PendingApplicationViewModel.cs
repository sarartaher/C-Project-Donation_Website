namespace Donation_Website.Models
{
    public class PendingApplicationViewModel
    {
        public int VolunteerID { get; set; }
        public string VolunteerName { get; set; }
        public int ProjectID { get; set; }
        public string ProjectTitle { get; set; }
        public DateTime ApplicationDate { get; set; }
    }

}
