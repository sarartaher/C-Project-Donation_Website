using System.ComponentModel.DataAnnotations;

namespace Donation_Website.Models
{
    public class AuditLogRow
    {
        public int AuditID { get; set; }
        public int AdminID { get; set; }
        public string AdminName { get; set; } = string.Empty;
        [Required] public string Action { get; set; } = string.Empty;
        public string? BeforeData { get; set; }
        public string? AfterData { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
