namespace Donation_Website.Models
{
    public class OrganizationViewModel
    {
        public int OrganizationID { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
