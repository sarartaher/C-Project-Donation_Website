using System.ComponentModel.DataAnnotations;

namespace Donation_Website.Models
{
    public class Row
    {
        public int DonationID { get; set; }
        [Display(Name = "Date")] public DateTime Date { get; set; }
        [Display(Name = "Event Name")] public string EventName { get; set; } = "";
        [Display(Name = "Amount")] public decimal Amount { get; set; }
    }
}
