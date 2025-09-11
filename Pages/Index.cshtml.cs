using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace Donation_Website.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }
        public decimal TotalCollected { get; set; }
        public int Percentage { get; set; }
        private const decimal GoalAmount = 100000m;
        public List<LeaderboardItem> TopDonors { get; set; }

        public void OnGet()
        {
            var db = new DBConnection();
            using (var cmd = db.GetQuery("SELECT SUM(Amount) FROM Donation"))
            {
                cmd.Connection.Open();
                var result = cmd.ExecuteScalar();
                cmd.Connection.Close();

                TotalCollected = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                Percentage = (int)Math.Min(100, Math.Round((TotalCollected / GoalAmount) * 100));

            }

            var leaderboard = new LeaderboardService();
            TopDonors = leaderboard.GetTopDonors();

        }

    }
}
