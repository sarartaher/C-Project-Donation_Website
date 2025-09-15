using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Donation_Website.Models;

namespace Donation_Website.Pages
{
    public class ViewTaskModel : PageModel
    {
        private readonly DBConnection _db = new();
        public List<VolunteerAssignment> Assignments { get; set; } = new();

        public void OnGet()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return;

            string sql = @"
                SELECT VA.AssignID, VA.RoleTask, VA.Status, VA.AssignDate, VA.Hours,
                       P.Title AS ProjectTitle, W.Title AS WorkTitle
                FROM VolunteerAssignment VA
                JOIN Volunteer V ON V.VolunteerID = VA.VolunteerID
                JOIN Project P ON P.ProjectID = VA.ProjectID
                JOIN WorkOfOrganization W ON W.WorkID = VA.WorkID
                WHERE V.Email=@Email";

            using var cmd = _db.GetQuery(sql);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Connection.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                Assignments.Add(new VolunteerAssignment
                {
                    AssignID = (int)r["AssignID"],
                    RoleTask = r["RoleTask"].ToString()!,
                    Status = r["Status"].ToString()!,
                    AssignDate = Convert.ToDateTime(r["AssignDate"]),
                    Hours = (int)(r["Hours"] ?? 0),
                    ProjectTitle = r["ProjectTitle"].ToString()!,
                    WorkTitle = r["WorkTitle"].ToString()!
                });
            }
        }
    }
}
