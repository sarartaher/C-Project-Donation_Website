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

        public void OnGet()
        {
            LoadFundraisers();
        }

        private void LoadFundraisers()
        {
            string query = @"SELECT FundraiserID, Title, TargetAmount, StartDate, EndDate, IsActive 
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
                            Title = reader["Title"].ToString() ?? "",
                            TargetAmount = reader["TargetAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TargetAmount"]),
                            StartDate = reader["StartDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["StartDate"]),
                            EndDate = reader["EndDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["EndDate"])
                        });
                    }
                }
            }
        }
    }

}
