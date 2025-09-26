using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using Donation_Website.Models;
namespace Donation_Website.Pages

{
    public class DonationHistoryModel : PageModel
    {
        public List<DonationRecord> Donations { get; set; } = new List<DonationRecord>();
        public void OnGet()
        {
            try
            {
                // SQL query joins Donation with Payment to get donor details
                string query = @"
                 SELECT d.DonationID,d.Amount,d.Currency,d.Status,d.[Date],p.Name,p.Email,p.PaymentMethod,p.PaymentStatus,p.Gateway,
                 p.TransactionDate FROM Donation d INNER JOIN Payment p ON d.DonationID = p.DonationID ORDER BY d.[Date] DESC";
                DBConnection db = new DBConnection();
                var cmd = db.GetQuery(query);
                cmd.Connection.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Donations.Add(new DonationRecord
                        {
                            DonationID = reader.GetInt32(reader.GetOrdinal("DonationID")),
                            Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                            Currency = reader["Currency"]?.ToString(),
                            Status = reader["Status"]?.ToString(),
                            Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                            Name = reader["Name"]?.ToString(),
                            Email = reader["Email"]?.ToString(),
                            PaymentMethod = reader["PaymentMethod"]?.ToString(),
                            PaymentStatus = reader["PaymentStatus"]?.ToString(),
                            Gateway = reader["Gateway"]?.ToString(),
                            TransactionDate = reader.GetDateTime(reader.GetOrdinal("TransactionDate"))
                        });
                    }
                }
                cmd.Connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(" Error fetching donation history: " + ex.Message);
            }
        }
    }
   
}