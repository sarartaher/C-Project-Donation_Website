using Donation_Website.Models;
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
                int currentUserId = 1; // Replace with actual logged-in user logic
                string query = @"
                    SELECT d.DonationId, d.UserId, d.Amount, d.Date, e.EventName
                    FROM Donations d
                    INNER JOIN Events e ON d.EventId = e.EventId
                    WHERE d.UserId = @UserId
                    ORDER BY d.Date DESC";

                DBConnection db = new DBConnection();
                using (SqlCommand cmd = db.GetQuery(query))
                {
                    cmd.Parameters.AddWithValue("@UserId", currentUserId);

                    // Open the connection from the SqlCommand
                    cmd.Connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Donations.Add(new DonationViewModel
                            {
                                DonationId = reader.GetInt32(reader.GetOrdinal("DonationId")),
                                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                                Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                                EventName = reader["EventName"]?.ToString() ?? ""
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
    }

}
