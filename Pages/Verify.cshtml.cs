using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace Donation_Website.Pages
{
    public class VerifyModel : PageModel
    {
        private readonly DBConnection _db;

        public VerifyModel()
        {
            _db = new DBConnection();
        }

        [BindProperty]
        public List<VolunteerViewModel> Volunteer { get; set; } = new List<VolunteerViewModel>();

        // GET: Load volunteers
        public void OnGet()
        {
            Volunteer = LoadVolunteers();
        }

        // POST: Verify a volunteer (Admin action)
        public IActionResult OnPostVerify(int id)
        {
            using (var cmd = _db.GetQuery("UPDATE Volunteer SET IsActive = 1, UpdatedAt = GETDATE() WHERE VolunteerID = @VolunteerID"))
            {
                cmd.Parameters.AddWithValue("@VolunteerID", id);
                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
                cmd.Connection.Close();
            }

            // Reload page
            return RedirectToPage();
        }

        // POST: Deactivate a volunteer
        public IActionResult OnPostDeactivate(int id)
        {
            using (var cmd = _db.GetQuery("UPDATE Volunteer SET IsActive = 0, UpdatedAt = GETDATE() WHERE VolunteerID = @VolunteerID"))
            {
                cmd.Parameters.AddWithValue("@VolunteerID", id);
                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
                cmd.Connection.Close();
            }

            return RedirectToPage();
        }

        private List<VolunteerViewModel> LoadVolunteers()
        {
            var list = new List<VolunteerViewModel>();

            using (var cmd = _db.GetQuery("SELECT VolunteerID, Name, Email, Phone, Address, Skill, Availability, IsActive FROM Volunteer"))
            {
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        
                        bool isActive = false;
                        if (reader["IsActive"] != DBNull.Value)
                        {
                           
                            isActive = Convert.ToBoolean(reader["IsActive"]);
                        }

                        list.Add(new VolunteerViewModel
                        {
                            VolunteerID = reader["VolunteerID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["VolunteerID"]),
                            Name = reader["Name"]?.ToString() ?? "",
                            Email = reader["Email"]?.ToString() ?? "",
                            Phone = reader["Phone"]?.ToString() ?? "",
                            Address = reader["Address"]?.ToString() ?? "",
                            Skill = reader["Skill"]?.ToString() ?? "",
                            Availability = reader["Availability"]?.ToString() ?? "",
                            Status = isActive ? "Verified" : "Pending",
                            StatusClass = isActive ? "text-success" : "text-danger"
                        });
                    }
                }
                cmd.Connection.Close();
            }

            return list;
        }
    }

    
}
