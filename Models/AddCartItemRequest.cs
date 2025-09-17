namespace Donation_Website.Models
{
    public class AddCartItemRequest
    {
        public int FundraiserID { get; set; }
        public decimal Amount { get; set; }
    }
}
