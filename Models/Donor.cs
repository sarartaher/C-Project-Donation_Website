namespace Donation_Website.Models
{
    public class Donor
    {
        public int DonorID { get; set; }
        public string ?Name { get; set; }
        public string? Email { get; set; }
        public string ?PasswordHash { get; set; }
        public string ?Phone { get; set; }
        public string ?Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
