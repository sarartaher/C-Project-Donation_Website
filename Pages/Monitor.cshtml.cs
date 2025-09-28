using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using Donation_Website.Models;

namespace Donation_Website.Pages
{
    public class MonitorModel : PageModel
    {
        public List<DonationRecord> Donations { get; set; } = new();

        public void OnGet()
        {
            var db = new DBConnection();
            // use your helper to create the command & connection
            using var cmd = db.GetQuery(@"
                SELECT dn.DonationID,
                           dn.Amount,
                           dn.Currency,
                           dn.Status AS DonationStatus,
                           dn.Date,
                           p.Name,
                           p.Email,
                           p.PaymentMethod,
                           p.PaymentStatus,
                           p.Gateway,
                           p.TransactionDate,
                           d.Name AS DonorName,
                           d.Email AS DonorEmail
                    FROM Donation dn
                    LEFT JOIN Payment p ON dn.DonationID = p.DonationID
                    LEFT JOIN Donor d ON dn.DonorID = d.DonorID
                    ORDER BY dn.Date DESC

                                    ");


            // open the connection that belongs to the command
            cmd.Connection.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Donations.Add(new DonationRecord
                {
                    DonationID = reader.GetInt32(reader.GetOrdinal("DonationID")),
                    Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                    Currency = reader["Currency"]?.ToString() ?? "",
                    Status = reader["DonationStatus"]?.ToString() ?? "",
                    Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                    Name = reader["Name"]?.ToString() ?? "",
                    Email = reader["Email"]?.ToString() ?? "",
                    PaymentMethod = reader["PaymentMethod"]?.ToString() ?? "",
                    PaymentStatus = reader["PaymentStatus"]?.ToString() ?? "",
                    Gateway = reader["Gateway"]?.ToString() ?? "",
                    TransactionDate = reader.IsDBNull(reader.GetOrdinal("TransactionDate"))
                                        ? DateTime.MinValue
                                        : reader.GetDateTime(reader.GetOrdinal("TransactionDate")),
                    DonorName = reader["DonorName"]?.ToString() ?? "",
                    DonorEmail = reader["DonorEmail"]?.ToString() ?? ""
                });
            }
        }
    }
}
