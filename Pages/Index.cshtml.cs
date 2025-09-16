using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;

namespace Donation_Website.Pages
{
    public partial class IndexModel : PageModel
    {
        private readonly DBConnection _db = new DBConnection();

        public decimal TotalCollected { get; set; }
        public int Percentage { get; set; }
        private const decimal GoalAmount = 100000m;
        public List<LeaderboardItem> TopDonors { get; set; }

        // Comment Form Properties
        [BindProperty]
        public string CommentName { get; set; }
        [BindProperty]
        public int ProjectId { get; set; }
        [BindProperty]
        public int Rating { get; set; }
        [BindProperty]
        public string CommentContent { get; set; }

        public List<Review> Comments { get; set; } = new List<Review>();

        public void OnGet()
        {
            // Total Collected & Percentage
            using (var cmd = _db.GetQuery("SELECT SUM(Amount) FROM Donation"))
            {
                cmd.Connection.Open();
                var result = cmd.ExecuteScalar();
                cmd.Connection.Close();

                TotalCollected = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                Percentage = (int)Math.Min(100, Math.Round((TotalCollected / GoalAmount) * 100));
            }

            // Load Top Donors
            var leaderboard = new LeaderboardService();
            TopDonors = leaderboard.GetTopDonors();

            // Load Comments
            LoadComments();
        }

        public void LoadComments()
        {
            string query = @"
                SELECT r.*, d.Name AS UserName
                FROM Review r
                LEFT JOIN Donor d ON r.DonorId = d.DonorId
                ORDER BY Date DESC";

            Comments.Clear();

            using (var cmd = _db.GetQuery(query))
            {
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Comments.Add(new Review
                        {
                            ReviewId = reader["ReviewId"] != DBNull.Value ? Convert.ToInt32(reader["ReviewId"]) : 0,
                            DonorId = reader["DonorId"] != DBNull.Value ? Convert.ToInt32(reader["DonorId"]) : 0,
                            ProjectId = reader["ProjectId"] != DBNull.Value ? Convert.ToInt32(reader["ProjectId"]) : 0,
                            Rating = reader["Rating"] != DBNull.Value ? Convert.ToInt32(reader["Rating"]) : 0,
                            Comment = reader["Comment"] != DBNull.Value ? reader["Comment"].ToString() : string.Empty,
                            Date = reader["Date"] != DBNull.Value ? Convert.ToDateTime(reader["Date"]) : DateTime.MinValue,
                            User = new User
                            {
                                Name = reader["UserName"] != DBNull.Value ? reader["UserName"].ToString() : "Anonymous"
                            }
                        });
                    }
                }
                cmd.Connection.Close();
            }
        }

        public IActionResult OnPostAddComment()
        {
            if (!string.IsNullOrEmpty(CommentName) && !string.IsNullOrEmpty(CommentContent))
            {
                string query = @"
                    INSERT INTO Review (DonorId, ProjectId, Rating, Comment, Date)
                    VALUES (
                        (SELECT TOP 1 DonorId FROM Donor WHERE Name = @Name), 
                        @ProjectId, 
                        @Rating, 
                        @Comment, 
                        @Date
                    )";

                var parameters = new[]
                {
                    new SqlParameter("@Name", CommentName),
                    new SqlParameter("@ProjectId", ProjectId),
                    new SqlParameter("@Rating", Rating),
                    new SqlParameter("@Comment", CommentContent),
                    new SqlParameter("@Date", DateTime.Now)
                };

                using (var cmd = _db.GetQuery(query))
                {
                    cmd.Parameters.AddRange(parameters);
                    cmd.Connection.Open();
                    cmd.ExecuteNonQuery();
                    cmd.Connection.Close();
                }
            }

            return RedirectToPage();
        }
    }
}
