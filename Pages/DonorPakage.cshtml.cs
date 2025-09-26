using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
namespace Donation_Website.Pages
{
    public class DonorPakageModel : PageModel
    {
        private readonly DBConnection _db = new DBConnection();
        private readonly Users _users = new Users();
        [BindProperty]
        public int FundraiserId { get; set; }
        [BindProperty]
        public decimal Amount { get; set; }
        public void OnGet()
        {
        }
        public IActionResult OnPostDonate()
        {
            try
            {
                // Get logged-in donor
                string email = User.Identity?.Name ?? "donor1@gmail.com";
                var (user, userType) = _users.SearchUser(email);
                if (userType != "Donor" || user == null)
                {
                    ModelState.AddModelError("", "You must be logged in as a donor to donate.");
                    return Page();
                }
                int donorId = (user as Donor)?.DonorID ?? 0;
                if (donorId == 0)
                {
                    ModelState.AddModelError("", "Invalid donor ID.");
                    return Page();
                }
                using (var cmd = _db.GetQuery("SELECT 1"))
                {
                    cmd.Connection.Open();
                    // Ensure donor has a pending cart
                    int cartId = 0;
                    string getCart = "SELECT TOP 1 CartID FROM Cart WHERE DonorID = @DonorID AND Status = 'Pending'";
                using (var cartCmd = new SqlCommand(getCart, cmd.Connection))
                    {
                        cartCmd.Parameters.AddWithValue("@DonorID", donorId);
                        var result = cartCmd.ExecuteScalar();
                        if (result != null)
                        {
                            cartId = (int)result;
                        }
                        else
                        {
                            // Create new cart
                            string insertCart = "INSERT INTO Cart (DonorID, Status, CreatedAt) OUTPUT INSERTED.CartID VALUES(@DonorID, 'Pending', GETDATE())";
                        using (var insertCmd = new SqlCommand(insertCart, cmd.Connection))
                            {
                                insertCmd.Parameters.AddWithValue("@DonorID", donorId);
                                cartId = (int)insertCmd.ExecuteScalar();
                            }
                        }
                    }
                    // Insert cart item
                    string insertItem = @"INSERT INTO CartItems (CartID, FundraiserID, Amount, CreatedAt)
                                        VALUES (@CartID, @FundraiserID, @Amount, GETDATE())";
                    using (var itemCmd = new SqlCommand(insertItem, cmd.Connection))
                    {
                        itemCmd.Parameters.AddWithValue("@CartID", cartId);
                        itemCmd.Parameters.AddWithValue("@FundraiserID", FundraiserId);
                        itemCmd.Parameters.AddWithValue("@Amount", Amount);
                        itemCmd.ExecuteNonQuery();
                    }
                    cmd.Connection.Close();
                }
                // Redirect to Cart Page
                return RedirectToPage("/Cart");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while donating. Please try again.");
                return Page();
            }
        }
    }
}