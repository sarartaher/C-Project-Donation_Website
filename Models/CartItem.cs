namespace Donation_Website.Models
{
    public class CartItem
    {
        public int CartItemId { get; set; }
        public int CartId { get; set; }
        public int FundraiserId { get; set; }
        public decimal Amount { get; set; }

        // Navigation
        public Cart? Cart { get; set; }
        public FundraiserViewModel? Fundraiser { get; set; }
    }
}
