using Donation_Website.Data;

namespace Donation_Website.Models
{
    public class CartItem
    {
        
            public int CartItemsId { get; set; }
            public int CartID { get; set; }
            public int FundraiserID { get; set; }
            public decimal? Amount { get; set; }
            public string SecretName { get; set; } = "Anonymous";
            public DateTime CreatedAt { get; set; }

            // Navigation properties
            public Cart Cart { get; set; }
            public Fundraiser Fundraiser { get; set; }
      
    }
}