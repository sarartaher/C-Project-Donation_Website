namespace Donation_Website.Models
{
    public class UserViewModel
    {
        public int Id { get; set; }        
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Table { get; set; } = ""; // Admin / Donor / Volunteer
    }
}
