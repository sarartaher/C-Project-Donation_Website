using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;

namespace Donation_Website.Pages
{
    public class DonorProfileModel : PageModel
    {
        private readonly DBConnection _db = new DBConnection();

        [BindProperty]
        public Donor CurrentDonor { get; set; } = new Donor();

        [TempData]
        public string SuccessMessage { get; set; }

        public IActionResult OnGet()
        {
            string email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToPage("/Login");
            }

            LoadDonor(email);
            return Page();
        }

        public IActionResult OnPostUpdate()
        {
            string email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToPage("/Login");
            }

            using (var cmd = _db.GetQuery(@"
                UPDATE Donor
                SET Name=@Name, Phone=@Phone, Address=@Address, UpdatedAt=GETDATE()
                WHERE Email=@Email
            "))
            {
                cmd.Parameters.AddWithValue("@Name", CurrentDonor.Name ?? "");
                cmd.Parameters.AddWithValue("@Phone", CurrentDonor.Phone ?? "");
                cmd.Parameters.AddWithValue("@Address", CurrentDonor.Address ?? "");
                cmd.Parameters.AddWithValue("@Email", email);

                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
                cmd.Connection.Close();
            }

            SuccessMessage = "Profile updated successfully!";
            LoadDonor(email); // Reload updated info
            return Page();
        }

        private void LoadDonor(string email)
        {
            using (var cmd = _db.GetQuery("SELECT DonorID, Name, Email, Phone, Address, CreatedAt FROM Donor WHERE Email=@Email"))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Connection.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        CurrentDonor.DonorID = reader.GetInt32(reader.GetOrdinal("DonorID"));
                        CurrentDonor.Name = reader.GetString(reader.GetOrdinal("Name"));
                        CurrentDonor.Email = reader.GetString(reader.GetOrdinal("Email"));
                        CurrentDonor.Phone = reader["Phone"] != DBNull.Value ? reader.GetString(reader.GetOrdinal("Phone")) : "";
                        CurrentDonor.Address = reader["Address"] != DBNull.Value ? reader.GetString(reader.GetOrdinal("Address")) : "";
                        CurrentDonor.CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                    }
                }

                cmd.Connection.Close();
            }
        }
    }
}

