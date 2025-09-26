using Donation_Website;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;

namespace Donation_Website.Pages
{
    public class DonorProgressModel : PageModel
    {
        private readonly DBConnection _db;

        public DonorProgressModel(DBConnection db)
        {
            _db = db;
        }

        public decimal TotalDonation { get; set; }
        public string RewardLevel { get; set; }

        public void OnGet()
        {
            int donorId = Convert.ToInt32(HttpContext.Session.GetString("DonorID"));
            TotalDonation = GetTotalDonation(donorId);
            RewardLevel = GetRewardLevel(TotalDonation);
        }

        private decimal GetTotalDonation(int donorId)
        {
            decimal total = 0;

            string query = @"
                SELECT ISNULL(SUM(Amount), 0)
                FROM Donation
                WHERE DonorID = @DonorID AND Status = 'Completed'";

            using (SqlCommand cmd = _db.GetQuery(query))
            {
                cmd.Parameters.AddWithValue("@DonorID", donorId);
                cmd.Connection.Open();
                object result = cmd.ExecuteScalar();
                if (result != DBNull.Value)
                    total = Convert.ToDecimal(result);
                cmd.Connection.Close();
            }

            return total;
        }

        private string GetRewardLevel(decimal totalDonation)
        {
            if (totalDonation >= 200000) return "Crown";
            if (totalDonation >= 120000) return "Ruby";
            if (totalDonation >= 60000) return "Emerald";
            if (totalDonation >= 30000) return "Diamond";
            if (totalDonation >= 15000) return "Platinum";
            if (totalDonation >= 8000) return "Gold";
            if (totalDonation >= 2000) return "Silver";
            return "No Reward Yet";
        }
    }
}
