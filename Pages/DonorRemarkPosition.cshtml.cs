using Donation_Website.Data;
using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace Donation_Website.Pages
{
    public class DonorRemarkPositionModel : PageModel
    {
        private readonly DBConnection _db = new DBConnection();

        public class EventDonationRank
        {
            public DateTime Date { get; set; }
            public TimeSpan Time { get; set; }
            public string ProjectTitle { get; set; }
            public decimal DonationAmount { get; set; }
            public int Position { get; set; }
            public decimal TotalMoney { get; set; }
        }

        public class EventTimeRank
        {
            public DateTime Date { get; set; }
            public TimeSpan Time { get; set; }
            public string ProjectTitle { get; set; }
            public int TimePosition { get; set; }
            public decimal DonationAmount { get; set; }
        }

        public List<EventDonationRank> DonationRanks { get; set; } = new List<EventDonationRank>();
        public List<EventTimeRank> TimeRanks { get; set; } = new List<EventTimeRank>();

        public int CurrentDonorId => GetDonorId();

        public void OnGet()
        {
            LoadDonationRanking();
            LoadTimeRanking();
        }

        private int GetDonorId()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                string email = User.Identity.Name;
                using var cmd = _db.GetQuery("SELECT DonorID FROM Donor WHERE Email=@Email");
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Connection.Open();
                var result = cmd.ExecuteScalar();
                cmd.Connection.Close();
                if (result != null) return (int)result;
            }
            return 0; // guest/fallback
        }

        private void LoadDonationRanking()
        {
            string query = @"
                SELECT 
                    d.[Date],
                    CAST(d.[Date] AS TIME) AS TimeOnly,
                    p.Title AS ProjectTitle,
                    d.Amount AS DonationAmount,
                    (SELECT COUNT(*) + 1
                     FROM Donation dd
                     WHERE dd.FundraiserID = d.FundraiserID
                       AND dd.Amount > d.Amount) AS Position,
                    (SELECT SUM(d2.Amount) 
                     FROM Donation d2 
                     WHERE d2.DonorID = d.DonorID) AS TotalMoney
                FROM Donation d
                INNER JOIN Project p ON d.FundraiserID = p.ProjectID
                WHERE d.DonorID = @DonorID
                ORDER BY d.[Date] DESC";

            using (var cmd = _db.GetQuery(query))
            {
                cmd.Parameters.AddWithValue("@DonorID", CurrentDonorId);
                cmd.Connection.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    DonationRanks.Add(new EventDonationRank
                    {
                        Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                        Time = reader.GetTimeSpan(reader.GetOrdinal("TimeOnly")),
                        ProjectTitle = reader.GetString(reader.GetOrdinal("ProjectTitle")),
                        DonationAmount = reader.GetDecimal(reader.GetOrdinal("DonationAmount")),
                        Position = reader.GetInt32(reader.GetOrdinal("Position")),
                        TotalMoney = reader.IsDBNull(reader.GetOrdinal("TotalMoney"))
                                        ? 0
                                        : reader.GetDecimal(reader.GetOrdinal("TotalMoney"))
                    });
                }
                reader.Close();
                cmd.Connection.Close();
            }
        }

        private void LoadTimeRanking()
        {
            string query = @"
                SELECT 
                    d.[Date],
                    CAST(d.[Date] AS TIME) AS TimeOnly,
                    p.Title AS ProjectTitle,
                    (SELECT COUNT(*) + 1
                     FROM Donation dd
                     INNER JOIN Project pp ON dd.FundraiserID = pp.ProjectID
                     WHERE dd.FundraiserID = d.FundraiserID
                       AND DATEDIFF(SECOND, pp.StartDate, dd.[Date]) < DATEDIFF(SECOND, pp.StartDate, d.[Date])) AS TimePosition,
                    d.Amount AS DonationAmount
                FROM Donation d
                INNER JOIN Project p ON d.FundraiserID = p.ProjectID
                WHERE d.DonorID = @DonorID
                ORDER BY d.[Date] DESC";

            using (var cmd = _db.GetQuery(query))
            {
                cmd.Parameters.AddWithValue("@DonorID", CurrentDonorId);
                cmd.Connection.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    TimeRanks.Add(new EventTimeRank
                    {
                        Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                        Time = reader.GetTimeSpan(reader.GetOrdinal("TimeOnly")),
                        ProjectTitle = reader.GetString(reader.GetOrdinal("ProjectTitle")),
                        TimePosition = reader.GetInt32(reader.GetOrdinal("TimePosition")),
                        DonationAmount = reader.GetDecimal(reader.GetOrdinal("DonationAmount"))
                    });
                }
                reader.Close();
                cmd.Connection.Close();
            }
        }
    }
}

