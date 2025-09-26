namespace Donation_Website.Models
{
    public class AuditLog
    {
        public int AuditID { get; set; }
        public int AdminID { get; set; }
        public string? Name { get; set; }
        public string? Action { get; set; }
        public string? BeforeData { get; set; }
        public string? AfterData { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public User? AdminUser { get; set; }
    }
}