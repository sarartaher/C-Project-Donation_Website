using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace Donation_Website.Pages
{
    public class DonationModel : PageModel
    {
        private DBConnection db = new DBConnection();

        public List<FundraiserViewModel> Fundraisers { get; set; } = new List<FundraiserViewModel>();

        public void OnGet()
        {
            LoadFundraisers();
        }

        private void LoadFundraisers()
        {
            string query = @"SELECT FundraiserID, Title, TargetAmount, StartDate, EndDate, ProjectID, IsActive
                             FROM Fundraiser
                             WHERE IsActive = 1
                             ORDER BY StartDate ASC";

            using (var cmd = db.GetQuery(query))
            {
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Fundraisers.Add(new FundraiserViewModel
                        {
                            FundraiserId = (int)reader["FundraiserID"],
                            ProjectId = (int)reader["ProjectID"],
                            Title = reader["Title"].ToString() ?? "",
                            TargetAmount = reader["TargetAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TargetAmount"]),
                            StartDate = reader["StartDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["StartDate"]),
                            EndDate = reader["EndDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["EndDate"])
                        });
                    }
                }
                cmd.Connection.Close();
            }
        }

        // Handle donation submission
        public IActionResult OnPostDonate(int fundraiserId, decimal amount, string secretName)
        {
            int donorId = 1; // replace with actual logged-in donor

            try
            {
                using (var cmd = db.GetQuery("SELECT 1")) // dummy query for connection
                {
                    cmd.Connection.Open();

                    // Get or create active cart
                    int cartId;
                    string getCart = "SELECT TOP 1 CartID FROM Cart WHERE DonorID=@DonorID AND Status='Active'";
                    using (var cartCmd = new SqlCommand(getCart, cmd.Connection))
                    {
                        cartCmd.Parameters.AddWithValue("@DonorID", donorId);
                        var result = cartCmd.ExecuteScalar();
                        if (result != null)
                            cartId = (int)result;
                        else
                        {
                            string insertCart = "INSERT INTO Cart(DonorID, Status) VALUES(@DonorID, 'Active'); SELECT SCOPE_IDENTITY();";
                            using (var insertCmd = new SqlCommand(insertCart, cmd.Connection))
                            {
                                insertCmd.Parameters.AddWithValue("@DonorID", donorId);
                                cartId = Convert.ToInt32(insertCmd.ExecuteScalar());
                            }
                        }
                    }

                    // Add donation to cart items
                    string insertItem = @"INSERT INTO CartItems(CartID, FundraiserID, Amount) 
                                          VALUES(@CartID, @FundraiserID, @Amount)";
                    using (var insertItemCmd = new SqlCommand(insertItem, cmd.Connection))
                    {
                        insertItemCmd.Parameters.AddWithValue("@CartID", cartId);
                        insertItemCmd.Parameters.AddWithValue("@FundraiserID", fundraiserId);
                        insertItemCmd.Parameters.AddWithValue("@Amount", amount);
                        insertItemCmd.ExecuteNonQuery();
                    }

                    cmd.Connection.Close();
                }

                // Redirect to Cart page
                return RedirectToPage("/cart");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return Page();
            }
        }
    }
}
