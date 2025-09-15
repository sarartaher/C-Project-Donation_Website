namespace Donation_Website.Models
{
    public class EmailSettings
    {
        public string ?SenderEmail { get; set; }
        public string ?AppPassword { get; set; }
        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; }
    }
}
