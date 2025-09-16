using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Donation_Website.Models;
using Donation_Website.Data;
using Microsoft.Data.SqlClient;

namespace Donation_Website.Pages
{
    public class Profile_AvailabilityModel : PageModel
    {
        private readonly DBConnection _db = new();

        [BindProperty]
        public Volunteer VolunteerInfo { get; set; } = new();

        public void OnGet()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
            {
                TempData["msg"] = "No user logged in!";
                return;
            }

            using var cmd = _db.GetQuery("SELECT * FROM Volunteer WHERE Email=@Email");
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Connection.Open();
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                VolunteerInfo.VolunteerID = (int)r["VolunteerID"];
                VolunteerInfo.Name = r["Name"].ToString()!;
                VolunteerInfo.Email = r["Email"].ToString()!;
                VolunteerInfo.Phone = r["Phone"].ToString();
                VolunteerInfo.Skill = r["Skill"].ToString();
                VolunteerInfo.Availability = r["Availability"].ToString();
            }
        }

        public IActionResult OnPost()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
            {
                TempData["msg"] = "No user logged in!";
                return Page();
            }

            using var cmd = _db.GetQuery(
                @"UPDATE Volunteer 
                  SET Name=@Name, Email=@Email, Phone=@Phone, Skill=@Skill, Availability=@Availability, UpdatedAt=GETDATE() 
                  WHERE Email=@OldEmail");

            cmd.Parameters.AddWithValue("@Name", VolunteerInfo.Name ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", VolunteerInfo.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Phone", VolunteerInfo.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Skill", VolunteerInfo.Skill ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Availability", VolunteerInfo.Availability ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@OldEmail", email); // Match by old email

            cmd.Connection.Open();
            cmd.ExecuteNonQuery();

            // If email was changed, update session
            HttpContext.Session.SetString("UserEmail", VolunteerInfo.Email);

            TempData["msg"] = "Profile updated successfully.";
            return RedirectToPage();
        }
    }
}
