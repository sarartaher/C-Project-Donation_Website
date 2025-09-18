using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace Donation_Website.Pages
{
    public class cartModel : PageModel
    {
        public List<DonationViewModel> Donations { get; set; } = new List<DonationViewModel>();

        public void OnGet()
        {
            try
            {
                int donorId = 1; // Replace with actual logged-in donor

                string query = @"
                    SELECT ci.CartItemID, ci.CartID, ci.Amount, f.FundraiserID, f.Title AS EventName, c.Status, c.DonorID, c.CreatedAt
                    FROM CartItems ci
                    INNER JOIN Cart c ON ci.CartID = c.CartID
                    INNER JOIN Fundraiser f ON ci.FundraiserID = f.FundraiserID
                    WHERE c.DonorID = @DonorID AND c.Status='Active'
                    ORDER BY ci.CartItemID DESC";

                DBConnection db = new DBConnection();
                using (SqlCommand cmd = db.GetQuery(query))
                {
                    cmd.Parameters.AddWithValue("@DonorID", donorId);
                    cmd.Connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Donations.Add(new DonationViewModel
                            {
                                DonationId = reader.GetInt32(reader.GetOrdinal("CartItemID")),
                                CartId = reader.GetInt32(reader.GetOrdinal("CartID")),
                                FundraiserId = reader.GetInt32(reader.GetOrdinal("FundraiserID")),
                                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                                Date = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                            });
                        }
                    }

                    cmd.Connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // Remove item from cart
        public IActionResult OnPostRemove(int cartItemId)
        {
            try
            {
                DBConnection db = new DBConnection();
                using (var cmd = db.GetQuery("DELETE FROM CartItems WHERE CartItemID=@CartItemID"))
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
                ModelState.AddModelError("", ex.Message);
                return Page();
            }
        }

        // Checkout: mark cart as completed and create donation records
        public IActionResult OnPostCheckout()
        {
            int donorId = 1; // Replace with actual donor
            try
            {
                DBConnection db = new DBConnection();
                using (var cmd = db.GetQuery("SELECT 1")) // dummy command for connection
                {
                    cmd.Connection.Open();

                    // Get active cart
                    int cartId = 0;
                    string getCart = "SELECT TOP 1 CartID FROM Cart WHERE DonorID=@DonorID AND Status='Active'";
                    using (var cartCmd = new SqlCommand(getCart, cmd.Connection))
                    {
                        cartCmd.Parameters.AddWithValue("@DonorID", donorId);
                        var result = cartCmd.ExecuteScalar();
                        if (result != null)
                            cartId = (int)result;
                        else
                            return RedirectToPage(); // no active cart
                    }

                    // Insert into Donation table
                    string getItems = "SELECT FundraiserID, Amount FROM CartItems WHERE CartID=@CartID";
                    using (var itemsCmd = new SqlCommand(getItems, cmd.Connection))
                    {
                        itemsCmd.Parameters.AddWithValue("@CartID", cartId);
                        using (var reader = itemsCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int fundraiserId = (int)reader["FundraiserID"];
                                decimal amount = (decimal)reader["Amount"];

                                using (var insertDonation = new SqlCommand(
                                    @"INSERT INTO Donation(DonorID, CartID, FundraiserID, Amount, Currency, Status) 
                                      VALUES(@DonorID, @CartID, @FundraiserID, @Amount, 'USD', 'Completed')", cmd.Connection))
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
                    using (var updateCart = new SqlCommand("UPDATE Cart SET Status='Completed', UpdatedAt=GETDATE() WHERE CartID=@CartID", cmd.Connection))
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
                ModelState.AddModelError("", ex.Message);
                return Page();
            }
        }
    }
}
