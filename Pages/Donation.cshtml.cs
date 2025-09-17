using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Donation_Website.Models;

namespace Donation_Website.Pages
{
    public class DonationModel : PageModel
    {
        private DBConnection db = new DBConnection();

        // List of fundraisers to populate the dropdown
        public List<FundraiserViewModel> Fundraisers { get; set; } = new List<FundraiserViewModel>();

        // Current user's cart items
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();

        public void OnGet()
        {
            LoadFundraisers();
            LoadCartItems();
        }

        private void LoadFundraisers()
        {
            string query = @"SELECT FundraiserID, ProjectID, Title, TargetAmount, StartDate, EndDate, IsActive 
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

        private void LoadCartItems()
        {
            int donorId = 1; // Replace with logged-in donor logic
            try
            {
                string query = @"
                    SELECT ci.CartItemID, ci.Amount, f.Title AS FundraiserTitle
                    FROM CartItems ci
                    INNER JOIN Cart c ON ci.CartID = c.CartID
                    INNER JOIN Fundraiser f ON ci.FundraiserID = f.FundraiserID
                    WHERE c.DonorID = @DonorID AND c.Status='Active'
                    ORDER BY ci.CartItemID DESC";

                using (var cmd = db.GetQuery(query))
                {
                    cmd.Parameters.AddWithValue("@DonorID", donorId);
                    cmd.Connection.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CartItems.Add(new CartItemViewModel
                            {
                                CartItemId = (int)reader["CartItemID"],
                                FundraiserTitle = reader["FundraiserTitle"].ToString() ?? "",
                                Amount = (decimal)reader["Amount"]
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

        public JsonResult OnPostAddToCart([FromBody] AddCartItemRequest request)
        {
            int donorId = 1; // Replace with logged-in donor logic
            try
            {
                DBConnection db = new DBConnection();

                using (var cmd = db.GetQuery("SELECT 1")) // dummy query to get SqlCommand and Connection
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

                    // Insert item into CartItems
                    string insertItem = @"INSERT INTO CartItems(CartID, FundraiserID, Amount) 
                                  VALUES(@CartID, @FundraiserID, @Amount)";
                    using (var insertItemCmd = new SqlCommand(insertItem, cmd.Connection))
                    {
                        insertItemCmd.Parameters.AddWithValue("@CartID", cartId);
                        insertItemCmd.Parameters.AddWithValue("@FundraiserID", request.FundraiserID);
                        insertItemCmd.Parameters.AddWithValue("@Amount", request.Amount);
                        insertItemCmd.ExecuteNonQuery();
                    }

                    cmd.Connection.Close();
                }

                return new JsonResult(new { success = true, message = "Added to cart successfully!" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }


        // Request DTO


        // Cart item DTO

    }
}
