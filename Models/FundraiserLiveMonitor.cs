using Microsoft.EntityFrameworkCore;

namespace Donation_Website.Models
{
    [Keyless]
    public class FundraiserLiveMonitor
    {
        public int FundraiserId { get; set; }
        public decimal TotalRaised { get; set; }
    }
}

