using Donation_Website.Data;
using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using Donation_Website.Models;
using System.Text.RegularExpressions;

namespace Donation_Website.Pages
{
    public partial class IndexModel : PageModel
    {
        private readonly DBConnection _db = new DBConnection();

        // ===================== EXISTING LOGIC (KEEP UNCHANGED) =====================
        public decimal TotalCollected { get; set; }
        public int Percentage { get; set; }
        private const decimal GoalAmount = 100000m;
        public List<LeaderboardItem> TopDonors { get; set; }
        public List<FundraiserViewModel> Fundraisers { get; set; } = new List<FundraiserViewModel>();
        // ===========================================================================

        // ===================== COMMENT SECTION =====================
        // Fully qualified to avoid ambiguity
        public List<Donation_Website.Models.Review> Comments { get; set; } = new List<Donation_Website.Models.Review>();


        [BindProperty]
        public string CommentName { get; set; }
        [BindProperty]
        public int SelectedFundraiserId { get; set; } // For showing selected dropdown after post
        [BindProperty]
        public int ProjectId { get; set; }
        [BindProperty]
        public int Rating { get; set; }
        [BindProperty]
        public string CommentContent { get; set; }
        // ===========================================================================

        public void OnGet()
        {
            // ===================== EXISTING LOGIC (KEEP UNCHANGED) =====================
            // Total Collected & Percentage (overall)
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

            // Load Fundraisers with progress
            string query = @"
                SELECT f.FundraiserID, f.Title, f.TargetAmount, ISNULL(SUM(d.Amount), 0) AS TotalCollected
                FROM Fundraiser f
                LEFT JOIN Donation d ON f.FundraiserID = d.FundraiserID
                GROUP BY f.FundraiserID, f.Title, f.TargetAmount
                ORDER BY f.FundraiserID
            ";

            using (var cmd = _db.GetQuery(query))
            {
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        decimal collected = reader["TotalCollected"] != DBNull.Value ? Convert.ToDecimal(reader["TotalCollected"]) : 0;
                        decimal goal = reader["TargetAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TargetAmount"]) : 100000m;

                        Fundraisers.Add(new FundraiserViewModel
                        {
                            FundraiserId = Convert.ToInt32(reader["FundraiserID"]),
                            Title = reader["Title"].ToString(),
                            TargetAmount = goal,
                            StartDate = DateTime.Now, // placeholder
                            EndDate = DateTime.Now,   // placeholder
                            IsActive = true,
                        });

                        ViewData[$"Collected_{reader["FundraiserID"]}"] = collected;
                        ViewData[$"Percentage_{reader["FundraiserID"]}"] = (int)Math.Min(100, Math.Round((collected / goal) * 100));
                    }
                }
                cmd.Connection.Close();
            }
            // ===========================================================================

            // Load Comments for selected fundraiser (if any)
            LoadComments();
        }

        // ===================== COMMENT METHODS =====================
        private void LoadComments()
        {
            Comments.Clear();

            string commentQuery = @"
        SELECT r.ReviewId, r.ProjectID, r.Rating, r.Comment, r.Date,
               d.Name AS DonorName, p.Title AS ProjectTitle
        FROM Review r
        LEFT JOIN Donor d ON r.DonorID = d.DonorID
        LEFT JOIN Project p ON r.ProjectID = p.ProjectID
        ORDER BY r.Date DESC";

            using (var cmd = _db.GetQuery(commentQuery))
            {
                cmd.Connection.Open();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Comments.Add(new Donation_Website.Models.Review
                    {
                        ReviewId = (int)reader["ReviewId"],
                        ProjectId = (int)reader["ProjectID"],
                        Rating = (int)reader["Rating"],
                        Comment = reader["Comment"].ToString(),
                        Date = (DateTime)reader["Date"],
                        User = new Donation_Website.Models.User
                        {
                            Name = reader["DonorName"] != DBNull.Value && !string.IsNullOrEmpty(reader["DonorName"].ToString())
                           ? reader["DonorName"].ToString()
                           : "Anonymous"
                        },
                        Project = new Donation_Website.Models.Project { Title = reader["ProjectTitle"].ToString() }
                    });
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
                    CommentName = CommentName.Trim();
                    if (CommentName.Length > 100) CommentName = CommentName.Substring(0, 100);

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
                            string insertDonorQuery = @"
                                INSERT INTO Donor (Name, Email, PasswordHash, CreatedAt, UpdatedAt)
                                OUTPUT INSERTED.DonorID
                                VALUES (@Name, @Email, @PasswordHash, GETDATE(), GETDATE())";

                            string sanitizedName = Regex.Replace(CommentName.ToLower(), @"[^a-z0-9]", "_");
                            string placeholderEmail = $"{sanitizedName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}@comment.donation";
                            string dummyPasswordHash = "AQAAAAEAACcQAAAAE..."; // Replace with actual hash

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

                string query = @"
                    INSERT INTO Review (DonorID, ProjectID, Rating, Comment, Date)
                    VALUES (@DonorID, @ProjectID, @Rating, @Comment, @Date)";

                var parameters = new[]
                {
                    new SqlParameter("@DonorID", donorId != null ? (object)donorId : DBNull.Value),
                    new SqlParameter("@ProjectID", SelectedFundraiserId),
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

            return RedirectToPage(new { SelectedFundraiserId });
        }
        // ===========================================================================

    }
}
