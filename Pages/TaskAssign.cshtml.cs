using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Donation_Website.Models; // your existing EmailSettings model

namespace Donation_Website.Pages
{
    public class TaskAssignModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TaskAssignModel> _logger;

        // ✅ Use the same DB helper pattern you’re using elsewhere
        private readonly DBConnection _db = new DBConnection();

        public TaskAssignModel(IConfiguration config, ILogger<TaskAssignModel> logger)
        {
            _config = config;
            _logger = logger;
        }

        // View model
        public List<VolunteerDto> Volunteers { get; set; } = new();

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        // Bind EmailSettings from appsettings.json (with safe defaults)
        private EmailSettings GetEmailSettings()
        {
            var s = new EmailSettings();
            _config.GetSection("EmailSettings").Bind(s);

            // Safe defaults if not provided
            s.SmtpHost ??= "smtp.gmail.com";
            if (s.SmtpPort == 0) s.SmtpPort = 587;

            return s;
        }

        public async Task OnGetAsync()
        {
            await LoadVolunteersAsync();
        }

        // POST: Assign handler -> <button asp-page-handler="Assign" asp-route-volunteerId="...">
        public async Task<IActionResult> OnPostAssignAsync(int volunteerId)
        {
            try
            {
                var duty = Request.Form[$"duties[{volunteerId}]"].ToString()?.Trim();

                if (volunteerId <= 0)
                {
                    ErrorMessage = "Invalid volunteer id.";
                    await LoadVolunteersAsync();
                    return Page();
                }
                if (string.IsNullOrWhiteSpace(duty))
                {
                    ErrorMessage = "Please write a duty before assigning.";
                    await LoadVolunteersAsync();
                    return Page();
                }

                // We need one shared connection + transaction.
                // Use a throwaway command to get at the underlying connection.
                using var bootstrap = _db.GetQuery("SELECT 1;");

                try
                {
                    await bootstrap.Connection!.OpenAsync();
                    using var tx = (SqlTransaction)await bootstrap.Connection.BeginTransactionAsync();

                    try
                    {
                        // 1) Ensure 'Internal Tasks' category exists -> get CategoryID
                        int categoryId = await EnsureCategoryAsync(bootstrap.Connection, tx, "Internal Tasks", "Internal/administrative tasks");

                        // 2) Ensure 'Task Assignments' project exists -> get ProjectID
                        int projectId = await EnsureProjectAsync(bootstrap.Connection, tx, categoryId, "Task Assignments",
                            "Container project for one-off internal task assignments.");

                        // 3) Create a WorkOfOrganization row for this specific duty -> get WorkID
                        var title = duty.Length > 100 ? duty.Substring(0, 100) : duty;
                        var desc = duty.Length > 255 ? duty.Substring(0, 255) : duty;

                        int workId = await InsertWorkAsync(bootstrap.Connection, tx, projectId, title, desc, DateTime.UtcNow);

                        // 4) Insert the VolunteerAssignment row (Status = 'Assigned')
                        await InsertAssignmentAsync(bootstrap.Connection, tx, volunteerId, projectId, workId, duty);

                        await tx.CommitAsync();

                        // 5) Email notify the volunteer (best-effort; DB already committed)
                        try { await SendAssignmentEmailAsync(volunteerId, duty); }
                        catch (Exception mailEx)
                        {
                            _logger.LogWarning(mailEx, "Email notification failed for VolunteerID={VolunteerID}", volunteerId);
                        }

                        SuccessMessage = "Task assigned successfully.";
                    }
                    catch (Exception ex)
                    {
                        await tx.RollbackAsync();
                        _logger.LogError(ex, "Assign failed");
                        ErrorMessage = "Assign failed: " + ex.Message;
                    }
                }
                finally
                {
                    if (bootstrap.Connection?.State == ConnectionState.Open)
                        await bootstrap.Connection.CloseAsync();
                }
            }
            catch (Exception outer)
            {
                _logger.LogError(outer, "Unexpected error in OnPostAssign");
                ErrorMessage = "Unexpected error while assigning the task.";
            }

            await LoadVolunteersAsync();
            return Page();
        }

        // ================== Data Access ==================

        private async Task LoadVolunteersAsync()
        {
            Volunteers.Clear();

            const string sql = @"
SELECT VolunteerID, Name, Email, Phone, Address, Skill, Availability, IsActive
FROM Volunteer
WHERE IsActive = 1
ORDER BY Name;";

            using var cmd = _db.GetQuery(sql);
            try
            {
                await cmd.Connection!.OpenAsync();
                using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    Volunteers.Add(new VolunteerDto
                    {
                        VolunteerID = rdr.GetInt32(0),
                        Name = rdr.GetString(1),
                        Email = rdr.IsDBNull(2) ? null : rdr.GetString(2),
                        Phone = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                        Address = rdr.IsDBNull(4) ? null : rdr.GetString(4),
                        Skill = rdr.IsDBNull(5) ? null : rdr.GetString(5),
                        Availability = rdr.IsDBNull(6) ? null : rdr.GetString(6),
                        IsActive = !rdr.IsDBNull(7) && rdr.GetBoolean(7)
                    });
                }
            }
            finally
            {
                if (cmd.Connection?.State == ConnectionState.Open)
                    await cmd.Connection.CloseAsync();
            }
        }

        private static async Task<int> EnsureCategoryAsync(SqlConnection conn, SqlTransaction tx, string name, string? desc)
        {
            using var getCmd = new SqlCommand(@"SELECT CategoryID FROM DonationCategory WHERE Name = @Name;", conn, tx);
            getCmd.Parameters.AddWithValue("@Name", name);
            var existing = await getCmd.ExecuteScalarAsync();
            if (existing != null && existing != DBNull.Value)
                return Convert.ToInt32(existing);

            using var insCmd = new SqlCommand(@"
INSERT INTO DonationCategory (Name, Description, Priority, CreatedAt)
VALUES (@Name, @Description, 0, GETDATE());
SELECT SCOPE_IDENTITY();", conn, tx);
            insCmd.Parameters.AddWithValue("@Name", name);
            insCmd.Parameters.AddWithValue("@Description", (object?)desc ?? DBNull.Value);
            var newId = await insCmd.ExecuteScalarAsync();
            return Convert.ToInt32(newId);
        }

        private static async Task<int> EnsureProjectAsync(SqlConnection conn, SqlTransaction tx, int categoryId, string title, string? desc)
        {
            using var getCmd = new SqlCommand(@"
SELECT TOP 1 ProjectID 
FROM Project 
WHERE CategoryID=@CategoryID AND Title=@Title;", conn, tx);
            getCmd.Parameters.AddWithValue("@CategoryID", categoryId);
            getCmd.Parameters.AddWithValue("@Title", title);
            var existing = await getCmd.ExecuteScalarAsync();
            if (existing != null && existing != DBNull.Value)
                return Convert.ToInt32(existing);

            using var insCmd = new SqlCommand(@"
INSERT INTO Project (CategoryID, Title, Description, StartDate, EndDate, CreatedAt)
VALUES (@CategoryID, @Title, @Description, NULL, NULL, GETDATE());
SELECT SCOPE_IDENTITY();", conn, tx);
            insCmd.Parameters.AddWithValue("@CategoryID", categoryId);
            insCmd.Parameters.AddWithValue("@Title", title);
            insCmd.Parameters.AddWithValue("@Description", (object?)desc ?? DBNull.Value);

            var newId = await insCmd.ExecuteScalarAsync();
            return Convert.ToInt32(newId);
        }

        private static async Task<int> InsertWorkAsync(SqlConnection conn, SqlTransaction tx, int projectId, string title, string? description, DateTime whenUtc)
        {
            using var cmd = new SqlCommand(@"
INSERT INTO WorkOfOrganization (ProjectID, Title, Description, Date, PublishedAt)
VALUES (@ProjectID, @Title, @Description, @Date, NULL);
SELECT SCOPE_IDENTITY();", conn, tx);

            cmd.Parameters.AddWithValue("@ProjectID", projectId);
            cmd.Parameters.AddWithValue("@Title", (object?)title ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Date", whenUtc);

            var newId = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(newId);
        }

        private static async Task InsertAssignmentAsync(SqlConnection conn, SqlTransaction tx, int volunteerId, int projectId, int workId, string roleTask)
        {
            using var cmd = new SqlCommand(@"
INSERT INTO VolunteerAssignment 
(VolunteerID, ProjectID, WorkID, RoleTask, Status, AssignDate, Hours)
VALUES (@VolunteerID, @ProjectID, @WorkID, @RoleTask, @Status, GETDATE(), NULL);", conn, tx);

            cmd.Parameters.AddWithValue("@VolunteerID", volunteerId);
            cmd.Parameters.AddWithValue("@ProjectID", projectId);
            cmd.Parameters.AddWithValue("@WorkID", workId);
            cmd.Parameters.AddWithValue("@RoleTask", roleTask);
            cmd.Parameters.AddWithValue("@Status", "Assigned");

            await cmd.ExecuteNonQueryAsync();
        }

        // =============== Email (best-effort) ===============
        private async Task SendAssignmentEmailAsync(int volunteerId, string duty)
        {
            // Look up volunteer email/name via DBConnection
            string? email = null;
            string? name = null;

            using (var cmd = _db.GetQuery(@"SELECT TOP 1 Name, Email FROM Volunteer WHERE VolunteerID=@Id;"))
            {
                cmd.Parameters.AddWithValue("@Id", volunteerId);

                try
                {
                    await cmd.Connection!.OpenAsync();
                    using var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
                    if (await rdr.ReadAsync())
                    {
                        name = rdr.IsDBNull(0) ? "Volunteer" : rdr.GetString(0);
                        email = rdr.IsDBNull(1) ? null : rdr.GetString(1);
                    }
                }
                finally
                {
                    if (cmd.Connection?.State == ConnectionState.Open)
                        await cmd.Connection.CloseAsync();
                }
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogInformation("Volunteer {VolunteerID} has no email; skip notification.", volunteerId);
                return;
            }

            var s = GetEmailSettings();
            if (string.IsNullOrWhiteSpace(s.SenderEmail) || string.IsNullOrWhiteSpace(s.AppPassword))
            {
                _logger.LogInformation("Email settings missing; skip notification.");
                return;
            }

            using var smtp = new SmtpClient(s.SmtpHost ?? "smtp.gmail.com", s.SmtpPort == 0 ? 587 : s.SmtpPort)
            {
                EnableSsl = true, // STARTTLS/TLS
                Credentials = new NetworkCredential(s.SenderEmail, s.AppPassword)
            };

            var msg = new MailMessage
            {
                From = new MailAddress(s.SenderEmail!, "Donation Website"),
                Subject = "New Task Assigned",
                Body = $"Hello {name},\n\nYou have been assigned a new task:\n\n{duty}\n\nPlease check your dashboard for details.\n\nThanks.",
                IsBodyHtml = false
            };
            msg.To.Add(new MailAddress(email!, name ?? email));

            await smtp.SendMailAsync(msg);
        }

        // =============== Types ===============
        public class VolunteerDto
        {
            public int VolunteerID { get; set; }
            public string Name { get; set; } = "";
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? Address { get; set; }
            public string? Skill { get; set; }
            public string? Availability { get; set; }
            public bool IsActive { get; set; }
        }
    }
}
