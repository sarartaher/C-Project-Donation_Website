using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions; // For potential sanitization, if needed

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
                            // This part is crucial for displaying either the donor name or "Anonymous"
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
            if (!string.IsNullOrEmpty(CommentContent))
            {
                int? donorId = null;

                if (!string.IsNullOrEmpty(CommentName))
                {
                    // Normalize the name to avoid whitespace issues
                    CommentName = CommentName.Trim();

                    // Optional: Validate name length (up to 100 chars as per DB)
                    if (CommentName.Length > 100)
                    {
                        CommentName = CommentName.Substring(0, 100);
                    }

                    // Check if donor exists (case-sensitive; adjust to LOWER if needed for case-insensitivity)
                    string checkDonorQuery = "SELECT TOP 1 DonorID FROM Donor WHERE Name = @Name";
                    using (var cmd = _db.GetQuery(checkDonorQuery))
                    {
                        cmd.Parameters.AddWithValue("@Name", CommentName);
                        cmd.Connection.Open();
                        var result = cmd.ExecuteScalar();
                        cmd.Connection.Close();

                        if (result != null && result != DBNull.Value)
                        {
                            donorId = Convert.ToInt32(result);
                        }
                        else
                        {
                            // Insert new donor with minimal required fields
                            string insertDonorQuery = @"
                                INSERT INTO Donor (Name, Email, PasswordHash, CreatedAt, UpdatedAt)
                                OUTPUT INSERTED.DonorID
                                VALUES (@Name, @Email, @PasswordHash, GETDATE(), GETDATE())";

                            // Generate a unique placeholder email to avoid UNIQUE constraint violations
                            string sanitizedName = Regex.Replace(CommentName.ToLower(), @"[^a-z0-9]", "_");
                            string placeholderEmail = $"{sanitizedName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}@comment.donation";
                            // Use a fixed dummy password hash (in production, consider BCrypt or similar for security)
                            string dummyPasswordHash = "AQAAAAEAACcQAAAAE..."; // Replace with a actual hashed value, e.g., BCrypt.HashPassword("dummy", 10)

                            using (var donorCmd = _db.GetQuery(insertDonorQuery))
                            {
                                donorCmd.Parameters.AddWithValue("@Name", CommentName);
                                donorCmd.Parameters.AddWithValue("@Email", placeholderEmail);
                                donorCmd.Parameters.AddWithValue("@PasswordHash", dummyPasswordHash);
                                donorCmd.Connection.Open();
                                donorId = (int)donorCmd.ExecuteScalar();
                                donorCmd.Connection.Close();
                            }
                        }
                    }
                }
                // If no name provided, donorId remains null, which will display as "Anonymous"

                // Insert review with the (possibly new) DonorID
                string query = @"
                    INSERT INTO Review (DonorID, ProjectID, Rating, Comment, Date)
                    VALUES (@DonorID, @ProjectID, @Rating, @Comment, @Date)";

                var parameters = new[]
                {
                    new SqlParameter("@DonorID", donorId != null ? (object)donorId : DBNull.Value),
                    new SqlParameter("@ProjectID", ProjectId),
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