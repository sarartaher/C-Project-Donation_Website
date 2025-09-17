namespace Donation_Website.Models
{
    public class VolunteerViewModel
    {
        public int VolunteerID { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string Skill { get; set; } = "";
        public string Availability { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusClass { get; set; } = "";
    }
}
