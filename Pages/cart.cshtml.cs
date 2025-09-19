using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Donation_Website.Pages
{
    public class cartModel : PageModel
    {
        private readonly DBConnection _db = new DBConnection();
        private readonly Users _users = new Users();

        public List<DonationViewModel> Donations { get; set; } = new List<DonationViewModel>();

        public void OnGet()
        {
            try
            {
                // Get logged-in donor ID
                string email = User.Identity?.Name ?? "donor1@gmail.com"; // Fallback for testing
                var (user, userType) = _users.SearchUser(email);

                if (userType != "Donor" || user == null)
                {
                    ModelState.AddModelError("", "You must be logged in as a donor to view the cart.");
                    return;
                }

                int donorId = (user as Donor)?.DonorID ?? 0;
                if (donorId == 0)
                {
                    ModelState.AddModelError("", "Invalid donor ID.");
                    return;
                }

                string query = @"
                    SELECT ci.CartItemID, ci.CartID, ci.Amount, f.FundraiserID, f.Title AS EventName, c.Status, c.DonorID, ci.CreatedAt
                    FROM CartItems ci
                    INNER JOIN Cart c ON ci.CartID = c.CartID
                    INNER JOIN Fundraiser f ON ci.FundraiserID = f.FundraiserID
                    WHERE c.DonorID = @DonorID AND c.Status = 'Pending'
                    ORDER BY ci.CreatedAt DESC";

                using (SqlCommand cmd = _db.GetQuery(query))
                {
                    cmd.Parameters.AddWithValue("@DonorID", donorId);
                    cmd.Connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Donations.Add(new DonationViewModel
                            {
                                DonationId = reader.GetInt32("CartItemID"),
                                CartId = reader.GetInt32("CartID"),
                                FundraiserId = reader.GetInt32("FundraiserID"),
                                EventName = reader.GetString("EventName"),
                                Amount = reader.GetDecimal("Amount"),
                                SecretName = "Anonymous", // Default for Zakat/other donations
                                Date = reader.GetDateTime("CreatedAt")
                            });
                        }
                    }

                    cmd.Connection.Close();
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while loading the cart. Please try again.");
            }
        }

        public IActionResult OnPostRemove(int cartItemId)
        {
            try
            {
                using (var cmd = _db.GetQuery("DELETE FROM CartItems WHERE CartItemID = @CartItemID"))
                {
                    cmd.Parameters.AddWithValue("@CartItemID", cartItemId);
                    cmd.Connection.Open();
                    cmd.ExecuteNonQuery();
                    cmd.Connection.Close();
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while removing the item. Please try again.");
                return Page();
            }
        }

        public IActionResult OnPostCheckout()
        {
            try
            {
                // Get logged-in donor ID
                string email = User.Identity?.Name ?? "donor1@gmail.com"; // Fallback for testing
                var (user, userType) = _users.SearchUser(email);

                if (userType != "Donor" || user == null)
                {
                    ModelState.AddModelError("", "You must be logged in as a donor to checkout.");
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

                    // Get pending cart
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
                            ModelState.AddModelError("", "No pending cart found.");
                            cmd.Connection.Close();
                            return Page();
                        }
                    }

                    // Insert into Donation table for each item
                    string getItems = "SELECT FundraiserID, Amount FROM CartItems WHERE CartID = @CartID";
                    using (var itemsCmd = new SqlCommand(getItems, cmd.Connection))
                    {
                        itemsCmd.Parameters.AddWithValue("@CartID", cartId);
                        using (var reader = itemsCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int fundraiserId = reader.GetInt32("FundraiserID");
                                decimal amount = reader.GetDecimal("Amount");

                                using (var insertDonation = new SqlCommand(
                                    @"INSERT INTO Donation (DonorID, CartID, FundraiserID, Amount, Currency, Status, [Date])
                                      VALUES (@DonorID, @CartID, @FundraiserID, @Amount, 'USD', 'Completed', GETDATE())", cmd.Connection))
                                {
                                    insertDonation.Parameters.AddWithValue("@DonorID", donorId);
                                    insertDonation.Parameters.AddWithValue("@CartID", cartId);
                                    insertDonation.Parameters.AddWithValue("@FundraiserID", fundraiserId);
                                    insertDonation.Parameters.AddWithValue("@Amount", amount);
                                    insertDonation.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    // Mark cart as completed
                    using (var updateCart = new SqlCommand("UPDATE Cart SET Status = 'Completed', UpdatedAt = GETDATE() WHERE CartID = @CartID", cmd.Connection))
                    {
                        updateCart.Parameters.AddWithValue("@CartID", cartId);
                        updateCart.ExecuteNonQuery();
                    }

                    cmd.Connection.Close();
                }

                return RedirectToPage("/Donation");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred during checkout. Please try again.");
                return Page();
            }
        }
    }
}