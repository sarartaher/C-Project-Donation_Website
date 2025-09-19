using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Donation_Website.Pages
{
    public class DonationModel : PageModel
    {
        private readonly DBConnection _db = new DBConnection();
        private readonly Users _users = new Users();

        public List<FundraiserViewModel> Fundraisers { get; set; } = new List<FundraiserViewModel>();
        public int SelectedFundraiserId { get; set; }
        public decimal PrefilledAmount { get; set; }

        public void OnGet(int? fundraiserId = null, double? amount = null)
        {
            LoadFundraisers();
            if (fundraiserId.HasValue)
            {
                SelectedFundraiserId = fundraiserId.Value;
            }
            if (amount.HasValue)
            {
                PrefilledAmount = (decimal)amount.Value;
            }
        }

        private void LoadFundraisers()
        {
            string query = @"SELECT FundraiserID, Title, TargetAmount, StartDate, EndDate, ProjectID, IsActive
                            FROM Fundraiser
                            WHERE IsActive = 1
                            ORDER BY StartDate ASC";

            using (var cmd = _db.GetQuery(query))
            {
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Fundraisers.Add(new FundraiserViewModel
                        {
                            FundraiserId = reader.GetInt32("FundraiserID"),
                            ProjectId = reader.GetInt32("ProjectID"),
                            Title = reader.GetString("Title"),
                            TargetAmount = reader["TargetAmount"] == DBNull.Value ? 0m : reader.GetDecimal("TargetAmount"),
                            StartDate = reader["StartDate"] == DBNull.Value ? DateTime.MinValue : reader.GetDateTime("StartDate"),
                            EndDate = reader["EndDate"] == DBNull.Value ? DateTime.MinValue : reader.GetDateTime("EndDate")
                        });
                    }
                }
                cmd.Connection.Close();
            }
        }

        public IActionResult OnPost(int fundraiserId, decimal amount, string? secretName = null)
        {
            if (!ModelState.IsValid)
            {
                LoadFundraisers();
                return Page();
            }

            try
            {
                // Get logged-in donor ID
                string email = User.Identity?.Name ?? "donor1@gmail.com"; // Fallback for testing
                var (user, userType) = _users.SearchUser(email);

                if (userType != "Donor" || user == null)
                {
                    ModelState.AddModelError("", "You must be logged in as a donor to donate.");
                    LoadFundraisers();
                    return Page();
                }

                int donorId = (user as Donor)?.DonorID ?? 0;
                if (donorId == 0)
                {
                    ModelState.AddModelError("", "Invalid donor ID.");
                    LoadFundraisers();
                    return Page();
                }

                using (var cmd = _db.GetQuery("SELECT 1"))
                {
                    cmd.Connection.Open();

                    // Get or create pending cart
                    int cartId;
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
                            string insertCart = "INSERT INTO Cart (DonorID, Status, CreatedAt, UpdatedAt) VALUES (@DonorID, 'Pending', GETDATE(), GETDATE()); SELECT SCOPE_IDENTITY();";
                            using (var insertCmd = new SqlCommand(insertCart, cmd.Connection))
                            {
                                insertCmd.Parameters.AddWithValue("@DonorID", donorId);
                                cartId = Convert.ToInt32(insertCmd.ExecuteScalar());
                            }
                        }
                    }

                    // Add donation to cart items
                    string insertItem = "INSERT INTO CartItems (CartID, FundraiserID, Amount, CreatedAt) VALUES (@CartID, @FundraiserID, @Amount, GETDATE())";
                    using (var insertItemCmd = new SqlCommand(insertItem, cmd.Connection))
                    {
                        insertItemCmd.Parameters.AddWithValue("@CartID", cartId);
                        insertItemCmd.Parameters.AddWithValue("@FundraiserID", fundraiserId);
                        insertItemCmd.Parameters.AddWithValue("@Amount", amount);
                        insertItemCmd.ExecuteNonQuery();
                    }

                    cmd.Connection.Close();
                }

                return RedirectToPage("/cart");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while adding to cart. Please try again.");
                LoadFundraisers();
                return Page();
            }
        }
    }
}