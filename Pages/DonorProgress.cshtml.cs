using Donation_Website.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using Donation_Website.Models;

namespace Donation_Website.Pages
{
    public class DonorProgressModel : PageModel
    {
        private readonly DBConnection _db = new DBConnection();

        public decimal TotalDonation { get; set; }
        public string CurrentLevel { get; set; } = "None";

        public List<DonorLevel> Levels { get; set; } = new List<DonorLevel>
        {
            new DonorLevel("Silver", 2000, "#C0C0C0", "fas fa-medal"),
            new DonorLevel("Gold", 8000, "#FFD700", "fas fa-award"),
            new DonorLevel("Platinum", 15000, "#708090", "fas fa-gem"),
            new DonorLevel("Diamond", 30000, "#1E90FF", "fas fa-diamond"),
            new DonorLevel("Emerald", 60000, "#228B22", "fas fa-leaf"),
            new DonorLevel("Ruby", 120000, "#B22222", "fas fa-heart"),
            new DonorLevel("Crown", 200000, "#8B4513", "fas fa-crown")
        };

        public void OnGet()
        {
            int donorId = GetDonorId();
            LoadTotalDonation(donorId);
            DetermineCurrentLevel();
        }

        private int GetDonorId()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                string email = User.Identity.Name;
                using (var cmd = _db.GetQuery("SELECT DonorID FROM Donor WHERE Email=@Email"))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Connection.Open();
                    var result = cmd.ExecuteScalar();
                    cmd.Connection.Close();
                    if (result != null) return (int)result;
                }
            }
            return 0; // fallback
        }

        private void LoadTotalDonation(int donorId)
        {
            using (var cmd = _db.GetQuery("SELECT ISNULL(SUM(Amount),0) FROM Donation WHERE DonorID=@DonorID AND Status='Completed'"))
            {
                cmd.Parameters.AddWithValue("@DonorID", donorId);
                cmd.Connection.Open();
                TotalDonation = Convert.ToDecimal(cmd.ExecuteScalar());
                cmd.Connection.Close();
            }
        }

        private void DetermineCurrentLevel()
        {
            foreach (var level in Levels)
            {
                if (TotalDonation >= level.Threshold)
                {
                    CurrentLevel = level.Name;
                }
            }
        }

    }
}

