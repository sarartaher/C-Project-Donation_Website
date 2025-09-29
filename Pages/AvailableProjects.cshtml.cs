using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Donation_Website.Models;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System;

namespace Donation_Website.Pages
{
    public class AvailableProjectsModel : PageModel
    {
        private readonly DBConnection _db = new();
        public List<Project> Projects { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        public void OnGet()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            int volunteerId = 0;
            var appliedProjectIds = new HashSet<int>();

            if (!string.IsNullOrEmpty(userEmail))
            {
                // Get VolunteerID by email
                using (var cmd = _db.GetQuery("SELECT VolunteerID FROM Volunteer WHERE Email=@Email"))
                {
                    cmd.Parameters.AddWithValue("@Email", userEmail);
                    cmd.Connection.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        volunteerId = Convert.ToInt32(result);
                    }
                }

                // Get IDs of projects the volunteer has applied to
                if (volunteerId > 0)
                {
                    using (var cmd = _db.GetQuery("SELECT ProjectID FROM VolunteerAssignment WHERE VolunteerID=@VolunteerID"))
                    {
                        cmd.Parameters.AddWithValue("@VolunteerID", volunteerId);
                        cmd.Connection.Open();
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                appliedProjectIds.Add(r.GetInt32(0));
                            }
                        }
                    }
                }
            }

            var sql = "SELECT ProjectID, Title, Description, StartDate, EndDate FROM Project";
            if (!string.IsNullOrEmpty(SearchString))
            {
                sql += " WHERE Title LIKE @SearchString OR Description LIKE @SearchString";
            }

            try
            {
                using (var cmd = _db.GetQuery(sql))
                {
                    if (!string.IsNullOrEmpty(SearchString))
                    {
                        cmd.Parameters.AddWithValue("@SearchString", $"%{SearchString}%");
                    }
                    cmd.Connection.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var project = new Project
                            {
                                ProjectId = r.GetInt32(0),
                                Title = r.GetString(1),
                                Description = r.GetString(2),
                                StartDate = r.GetDateTime(3),
                                EndDate = r.GetDateTime(4)
                            };
                            if (appliedProjectIds.Contains(project.ProjectId))
                            {
                                project.IsApplied = true;
                            }
                            Projects.Add(project);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public JsonResult OnPostApply(int projectId)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return new JsonResult(new { success = false, message = "Not logged in" });

            // Get VolunteerID by email
            int volunteerId = 0;
            using (var cmd = _db.GetQuery("SELECT VolunteerID FROM Volunteer WHERE Email=@Email"))
            {
                cmd.Parameters.AddWithValue("@Email", userEmail);
                cmd.Connection.Open();
                var result = cmd.ExecuteScalar();
                if (result == null)
                    return new JsonResult(new { success = false, message = "Volunteer not found" });
                volunteerId = Convert.ToInt32(result);
            }

            // Check if already applied
            using (var cmd = _db.GetQuery("SELECT COUNT(*) FROM VolunteerAssignment WHERE VolunteerID=@VolunteerID AND ProjectID=@ProjectID"))
            {
                cmd.Parameters.AddWithValue("@VolunteerID", volunteerId);
                cmd.Parameters.AddWithValue("@ProjectID", projectId);
                cmd.Connection.Open();
                int count = (int)cmd.ExecuteScalar();
                if (count > 0)
                    return new JsonResult(new { success = false, message = "Already applied" });
            }

            // Get a valid WorkID for the project
            int workId = 0;
            using (var cmd = _db.GetQuery("SELECT TOP 1 WorkID FROM WorkOfOrganization WHERE ProjectID=@ProjectID"))
            {
                cmd.Parameters.AddWithValue("@ProjectID", projectId);
                cmd.Connection.Open();

                // DEBUG: Check what's in the database
                Console.WriteLine($"Looking for WorkID with ProjectID: {projectId}");

                var result = cmd.ExecuteScalar();
                Console.WriteLine($"Query result: {result}");

                if (result == null)
                {
                    // DEBUG: Check if ANY work records exist
                    using (var debugCmd = _db.GetQuery("SELECT COUNT(*) FROM WorkOfOrganization"))
                    {
                        debugCmd.Connection.Open();
                        var totalCount = debugCmd.ExecuteScalar();
                        Console.WriteLine($"Total WorkOfOrganization records: {totalCount}");
                    }

                    // DEBUG: Check what ProjectIDs exist in WorkOfOrganization  
                    using (var debugCmd = _db.GetQuery("SELECT DISTINCT ProjectID FROM WorkOfOrganization"))
                    {
                        debugCmd.Connection.Open();
                        using (var reader = debugCmd.ExecuteReader())
                        {
                            var projectIds = new List<int>();
                            while (reader.Read())
                            {
                                projectIds.Add(reader.GetInt32(0));
                            }
                            Console.WriteLine($"ProjectIDs in WorkOfOrganization: {string.Join(", ", projectIds)}");
                        }
                    }

                    return new JsonResult(new { success = false, message = "No work available for this project. Please contact support." });
                }
                workId = Convert.ToInt32(result);
                Console.WriteLine($"Found WorkID: {workId} for ProjectID: {projectId}");
            }

            // Insert application (Status = 'Pending')
            using (var cmd = _db.GetQuery("INSERT INTO VolunteerAssignment (VolunteerID, ProjectID, WorkID, Status, AssignDate) VALUES (@VolunteerID, @ProjectID, @WorkID, 'Pending', @AssignDate)"))
            {
                cmd.Parameters.AddWithValue("@VolunteerID", volunteerId);
                cmd.Parameters.AddWithValue("@ProjectID", projectId);
                cmd.Parameters.AddWithValue("@WorkID", workId);
                cmd.Parameters.AddWithValue("@AssignDate", DateTime.UtcNow);
                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
            }

            return new JsonResult(new { success = true });
        }
    }
}
