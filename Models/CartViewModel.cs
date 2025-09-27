namespace Donation_Website.Models
{
    public class CartViewModel
    {
        public int CartItemsId { get; set; }
        public int CartId { get; set; }
        public string DonorName { get; set; }
        public string DonorEmail { get; set; }
        public List<CartItemViewModel> Items { get; set; }
    }
}
