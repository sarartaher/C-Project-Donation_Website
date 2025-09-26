using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using Donation_Website.Models;
using System.Collections.Generic;

namespace Donation_Website.Pages
{
    public class DonorRemarkPositionModel : PageModel
    {
        private readonly DBConnection _db;

        public DonorRemarkPositionModel(DBConnection db)
        {
            _db = db;
        }

        public List<EventDonationRanking> DonationRankings { get; set; } = new();
        public List<EventTimeRanking> TimeRankings { get; set; } = new();

        public void OnGet(int donorId)
        {
            LoadDonationRanking(donorId);
            LoadTimeRanking(donorId);
        }
        private void LoadDonationRanking(int donorId)
        {
            string query = @"
                SELECT 
                    f.Title AS EventName,
                    d.DonorID,
                    SUM(d.Amount) AS TotalAmount,
                    MIN(d.Date) AS FirstDonationDate
                    FROM Donation d
                    INNER JOIN Fundraiser f ON d.FundraiserID = f.FundraiserID
                    INNER JOIN Payment p ON d.DonationID = p.DonationID
                    WHERE p.PaymentStatus = 'Completed'
                    GROUP BY f.Title, d.DonorID
                    ORDER BY f.Title, TotalAmount DESC";

            using (var cmd = _db.GetQuery(query))
            {
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    string currentEvent = "";
                    int position = 0;

                    while (reader.Read())
                    {
                        string eventName = reader["EventName"].ToString();

                        // Reset ranking per event
                        if (currentEvent != eventName)
                        {
                            currentEvent = eventName;
                            position = 0;
                        }

                        position++;

                        if ((int)reader["DonorID"] == donorId)
                        {
                            DonationRankings.Add(new EventDonationRanking
                            {
                                Date = Convert.ToDateTime(reader["FirstDonationDate"]).ToString("yyyy-MM-dd"),
                                EventName = eventName,
                                DonorPosition = GetOrdinal(position),
                                Money = Convert.ToDecimal(reader["TotalAmount"])
                            });
                        }
                    }
                }
                cmd.Connection.Close();
            }
        }

        private void LoadTimeRanking(int donorId)
        {
            string query = @"
                SELECT 
                    f.Title AS EventName,
                    d.DonorID,
                    d.Date
                FROM Donation d
                INNER JOIN Fundraiser f ON d.FundraiserID = f.FundraiserID
                INNER JOIN Payment p ON d.DonationID = p.DonationID
                WHERE p.PaymentStatus = 'Completed'
                ORDER BY f.Title, d.Date ASC";

            using (var cmd = _db.GetQuery(query))
            {
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    string currentEvent = "";
                    int position = 0;

                    while (reader.Read())
                    {
                        string eventName = reader["EventName"].ToString();

                        // Reset ranking per event
                        if (currentEvent != eventName)
                        {
                            currentEvent = eventName;
                            position = 0;
                        }

                        position++;

                        if ((int)reader["DonorID"] == donorId)
                        {
                            var date = Convert.ToDateTime(reader["Date"]);
                            TimeRankings.Add(new EventTimeRanking
                            {
                                Date = date.ToString("yyyy-MM-dd"),
                                Time = date.ToString("hh:mm tt"),
                                EventName = eventName,
                                TimePosition = GetOrdinal(position)
                            });
                        }
                    }
                }
                cmd.Connection.Close();
            }
        }
        private string GetOrdinal(int number)
        {
            if (number <= 0) return number.ToString();
            return (number % 100) switch
            {
                11 or 12 or 13 => number + "th",
                _ => (number % 10) switch
                {
                    1 => number + "st",
                    2 => number + "nd",
                    3 => number + "rd",
                    _ => number + "th",
                }
            };
        }
    }
}