using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

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

        [BindProperty]
        public List<PendingApplicationViewModel> PendingApplications { get; set; } = new List<PendingApplicationViewModel>();

        public void OnGet()
        {
            Volunteer = LoadVolunteers();
            PendingApplications = LoadPendingApplications();
        }

        public async Task<IActionResult> OnPostAcceptAsync(int volunteerId, int projectId)
        {
            // Use _db.GetQuery to get a command and its connection
            using (var cmd = _db.GetQuery("SELECT 1")) // Dummy query to get the connection
            {
                var connection = cmd.Connection;
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Update volunteer to be verified
                        using (var updateVolunteerCmd = new SqlCommand(
                            "UPDATE Volunteer SET IsActive = 1, UpdatedAt = GETDATE() WHERE VolunteerID = @VolunteerID",
                            connection, transaction))
                        {
                            updateVolunteerCmd.Parameters.AddWithValue("@VolunteerID", volunteerId);
                            await updateVolunteerCmd.ExecuteNonQueryAsync();
                        }

                        // Update assignment status to 'Accepted'
                        using (var updateAssignmentCmd = new SqlCommand(
                            "UPDATE VolunteerAssignment SET Status = 'Accepted' WHERE VolunteerID = @VolunteerID AND ProjectID = @ProjectID",
                            connection, transaction))
                        {
                            updateAssignmentCmd.Parameters.AddWithValue("@VolunteerID", volunteerId);
                            updateAssignmentCmd.Parameters.AddWithValue("@ProjectID", projectId);
                            await updateAssignmentCmd.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        // Optionally log the error
                        return RedirectToPage("/Error");
                    }
                }
            }

            return RedirectToPage();
        }

        private List<VolunteerViewModel> LoadVolunteers()
        {
            var list = new List<VolunteerViewModel>();
            using (var cmd = _db.GetQuery("SELECT VolunteerID, Name, Email, Phone, Address, Skill, Availability, IsActive FROM Volunteer WHERE IsActive = 1"))
            {
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new VolunteerViewModel
                        {
                            VolunteerID = Convert.ToInt32(reader["VolunteerID"]),
                            Name = reader["Name"].ToString(),
                            Email = reader["Email"].ToString(),
                            Phone = reader["Phone"].ToString(),
                            Address = reader["Address"].ToString(),
                            Skill = reader["Skill"].ToString(),
                            Availability = reader["Availability"].ToString(),
                            Status = "Verified",
                            StatusClass = "text-success"
                        });
                    }
                }
            }
            return list;
        }

        private List<PendingApplicationViewModel> LoadPendingApplications()
        {
            var list = new List<PendingApplicationViewModel>();
            var sql = @"
                SELECT va.VolunteerID, v.Name AS VolunteerName, va.ProjectID, p.Title AS ProjectTitle, va.AssignDate
                FROM VolunteerAssignment va
                JOIN Volunteer v ON va.VolunteerID = v.VolunteerID
                JOIN Project p ON va.ProjectID = p.ProjectID
                WHERE va.Status = 'Pending'";

            using (var cmd = _db.GetQuery(sql))
            {
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new PendingApplicationViewModel
                        {
                            VolunteerID = Convert.ToInt32(reader["VolunteerID"]),
                            VolunteerName = reader["VolunteerName"].ToString(),
                            ProjectID = Convert.ToInt32(reader["ProjectID"]),
                            ProjectTitle = reader["ProjectTitle"].ToString(),
                            ApplicationDate = Convert.ToDateTime(reader["AssignDate"])
                        });
                    }
                }
            }
            return list;
        }
    }
}

