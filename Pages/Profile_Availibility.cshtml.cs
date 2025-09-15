using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Donation_Website.Models;

namespace Donation_Website.Pages
{
    public class Profile_AvailabilityModel : PageModel
    {
        private readonly DBConnection _db = new();
        [BindProperty] public Volunteer VolunteerInfo { get; set; } = new();

        public void OnGet()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return;

            using var cmd = _db.GetQuery("SELECT * FROM Volunteer WHERE Email=@Email");
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Connection.Open();
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                VolunteerInfo.VolunteerID = (int)r["VolunteerID"];
                VolunteerInfo.Name = r["Name"].ToString()!;
                VolunteerInfo.Phone = r["Phone"].ToString();
                VolunteerInfo.Skill = r["Skill"].ToString();
                VolunteerInfo.Availability = r["Availability"].ToString();
            }
        }

        public IActionResult OnPost()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return Page();

            using var cmd = _db.GetQuery(
                "UPDATE Volunteer SET Phone=@Phone, Skill=@Skill, Availability=@Avail, UpdatedAt=GETDATE() WHERE Email=@Email");
            cmd.Parameters.AddWithValue("@Phone", VolunteerInfo.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Skill", VolunteerInfo.Skill ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Avail", VolunteerInfo.Availability ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", email);

            cmd.Connection.Open();
            cmd.ExecuteNonQuery();
            TempData["msg"] = "Profile updated successfully.";
            return RedirectToPage();
        }
    }
}
