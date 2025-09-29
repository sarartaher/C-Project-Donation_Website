using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Donation_Website.Models;
namespace Donation_Website.Pages
{
    public class ViewTaskModel : PageModel
    {
        // Use the same helper you showed in LogsModel
        private readonly DBConnection _db = new DBConnection();

        public List<AssignmentRow> Assignments { get; set; } = new();

        // Used by your .cshtml to toggle columns/UX if needed
        public bool IsVolunteerMode { get; set; } = false;

        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            // Prefer VolunteerID from session
            int? volunteerId = HttpContext.Session.GetInt32("VolunteerID");

            if (volunteerId is null)
            {
                // Try to infer from email if your login stored it
                var email = HttpContext.Session.GetString("Email")
                           ?? HttpContext.Session.GetString("UserEmail")
                           ?? HttpContext.Session.GetString("DonorEmail");

                if (!string.IsNullOrWhiteSpace(email))
                {
                    volunteerId = await FindVolunteerIdByEmailAsync(email);
                    if (volunteerId.HasValue)
                        HttpContext.Session.SetInt32("VolunteerID", volunteerId.Value);
                }

                // As a weaker fallback, try by name
                if (volunteerId is null)
                {
                    var name = HttpContext.Session.GetString("UserName");
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        volunteerId = await FindVolunteerIdByNameAsync(name);
                        if (volunteerId.HasValue)
                            HttpContext.Session.SetInt32("VolunteerID", volunteerId.Value);
                    }
                }
            }

            if (volunteerId is null)
            {
                IsVolunteerMode = false;
                ErrorMessage = "We couldn’t identify your volunteer account. Please log in via the volunteer login.";
                Assignments = new List<AssignmentRow>();
                return;
            }

            IsVolunteerMode = true;
            Assignments = await LoadAssignmentsForVolunteerAsync(volunteerId.Value);
            if (Assignments.Count == 0)
            {
                ErrorMessage = "No tasks assigned to your account yet.";
            }
        }

        // ---------------- Lookups ----------------

        private async Task<int?> FindVolunteerIdByEmailAsync(string email)
        {
            const string sql = @"SELECT TOP 1 VolunteerID FROM Volunteer WHERE Email = @Email AND IsActive = 1;";
            using var cmd = _db.GetQuery(sql);
            cmd.Parameters.AddWithValue("@Email", email);

            try
            {
                await cmd.Connection!.OpenAsync();
                var obj = await cmd.ExecuteScalarAsync();
                return obj == null || obj == DBNull.Value ? (int?)null : Convert.ToInt32(obj);
            }
            finally
            {
                if (cmd.Connection?.State == ConnectionState.Open)
                    await cmd.Connection.CloseAsync();
            }
        }

        private async Task<int?> FindVolunteerIdByNameAsync(string name)
        {
            const string sql = @"SELECT TOP 1 VolunteerID FROM Volunteer WHERE Name = @Name AND IsActive = 1;";
            using var cmd = _db.GetQuery(sql);
            cmd.Parameters.AddWithValue("@Name", name);

            try
            {
                await cmd.Connection!.OpenAsync();
                var obj = await cmd.ExecuteScalarAsync();
                return obj == null || obj == DBNull.Value ? (int?)null : Convert.ToInt32(obj);
            }
            finally
            {
                if (cmd.Connection?.State == ConnectionState.Open)
                    await cmd.Connection.CloseAsync();
            }
        }

        // ---------------- Data load ----------------

        private async Task<List<AssignmentRow>> LoadAssignmentsForVolunteerAsync(int volunteerId)
        {
            var list = new List<AssignmentRow>();
            const string sql = @"
SELECT 
    v.Name              AS VolunteerName,
    p.Title             AS ProjectTitle,
    w.Title             AS WorkTitle,
    va.RoleTask,
    va.Status,
    va.AssignDate,
    va.Hours
FROM VolunteerAssignment va
INNER JOIN Volunteer v ON v.VolunteerID = va.VolunteerID
INNER JOIN Project   p ON p.ProjectID   = va.ProjectID
INNER JOIN WorkOfOrganization w ON w.WorkID = va.WorkID
WHERE va.VolunteerID = @VolunteerID
ORDER BY COALESCE(va.AssignDate, GETDATE()) DESC, va.AssignID DESC;";

            using var cmd = _db.GetQuery(sql);
            cmd.Parameters.AddWithValue("@VolunteerID", volunteerId);

            try
            {
                await cmd.Connection!.OpenAsync();
                using var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
                while (await rdr.ReadAsync())
                {
                    list.Add(new AssignmentRow
                    {
                        VolunteerName = rdr.IsDBNull(0) ? "" : rdr.GetString(0),
                        ProjectTitle = rdr.IsDBNull(1) ? "" : rdr.GetString(1),
                        WorkTitle = rdr.IsDBNull(2) ? "" : rdr.GetString(2),
                        RoleTask = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                        Status = rdr.IsDBNull(4) ? "Assigned" : rdr.GetString(4),
                        AssignDate = rdr.IsDBNull(5) ? DateTime.Now : rdr.GetDateTime(5),
                        Hours = rdr.IsDBNull(6) ? (int?)null : rdr.GetInt32(6)
                    });
                }
            }
            finally
            {
                if (cmd.Connection?.State == ConnectionState.Open)
                    await cmd.Connection.CloseAsync();
            }

            return list;
        }

    }
}
