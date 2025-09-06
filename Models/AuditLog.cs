namespace Donation_Website.Models
{
    public class AuditLog
    {
        public int AuditLogId { get; set; }
        public int AdminUserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? Before { get; set; }
        public string? After { get; set; }
        public string? IPAddress { get; set; }

        // Navigation
        public User? AdminUser { get; set; }
    }
}
