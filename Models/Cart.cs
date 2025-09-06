using Donation_Website.Data;

namespace Donation_Website.Models
{
    public class Cart
    {
        public int CartId { get; set; }
        public int UserId { get; set; }
        public bool IsCheckedOut { get; set; }

        // Navigation
        public User? User { get; set; }
        public ICollection<CartItem>? Items { get; set; }
    }
}
