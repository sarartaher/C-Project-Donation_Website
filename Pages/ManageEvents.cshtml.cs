using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Donation_Website.Models;
namespace Donation_Website.Pages
{
    public class ManageEventsModel : PageModel
    {
        private readonly IConfiguration _config;

        public ManageEventsModel(IConfiguration config)
        {
            _config = config;
        }

        // ===== View Data =====
        [BindProperty(SupportsGet = true)]
        public string? q { get; set; }

        public List<CategoryDto> Categories { get; set; } = new();
        public List<EventDto> Events { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        // ===== Input binding (Create/Edit) =====
        public class EventInput
        {
            public int? EventId { get; set; }

            [Required]
            public int CategoryID { get; set; }

            [Required, StringLength(100)]
            public string Title { get; set; } = string.Empty;

            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }

            [StringLength(255)]
            public string? Description { get; set; }

            // New: Targeted fundraising amount
            public decimal? TargetAmount { get; set; }
        }

        [BindProperty]
        public EventInput Input { get; set; } = new();

        private string GetConnectionString() =>
            _config.GetConnectionString("DefaultConnection")
            ?? "Server=.;Database=DonationManagementDB;Trusted_Connection=True;TrustServerCertificate=True;";

        // ===== Page lifecycle =====
        public async Task OnGetAsync()
        {
            await LoadCategoriesAsync();
            await LoadEventsAsync(q);
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                await LoadEventsAsync(q);
                return Page();
            }

            using var conn = new SqlConnection(GetConnectionString());
            await conn.OpenAsync();

            using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Insert Project (Event)
                var insertProjectCmd = new SqlCommand(@"
INSERT INTO Project (CategoryID, Title, Description, StartDate, EndDate, CreatedAt)
VALUES (@CategoryID, @Title, @Description, @StartDate, @EndDate, GETDATE());
SELECT SCOPE_IDENTITY();", conn, (SqlTransaction)tx);

                insertProjectCmd.Parameters.AddWithValue("@CategoryID", Input.CategoryID);
                insertProjectCmd.Parameters.AddWithValue("@Title", (object?)Input.Title ?? DBNull.Value);
                insertProjectCmd.Parameters.AddWithValue("@Description", (object?)Input.Description ?? DBNull.Value);
                insertProjectCmd.Parameters.AddWithValue("@StartDate", (object?)Input.StartDate ?? DBNull.Value);
                insertProjectCmd.Parameters.AddWithValue("@EndDate", (object?)Input.EndDate ?? DBNull.Value);

                var newProjectIdObj = await insertProjectCmd.ExecuteScalarAsync();
                int newProjectId = Convert.ToInt32(newProjectIdObj);

                // 2) If TargetAmount provided, upsert a Fundraiser row
                if (Input.TargetAmount.HasValue)
                {
                    var insertFundraiserCmd = new SqlCommand(@"
INSERT INTO Fundraiser (ProjectID, Title, TargetAmount, StartDate, EndDate, IsActive, CreatedAt)
VALUES (@ProjectID, @Title, @TargetAmount, @StartDate, @EndDate, 1, GETDATE());", conn, (SqlTransaction)tx);

                    insertFundraiserCmd.Parameters.AddWithValue("@ProjectID", newProjectId);
                    insertFundraiserCmd.Parameters.AddWithValue("@Title", $"{Input.Title} Fundraiser");
                    insertFundraiserCmd.Parameters.AddWithValue("@TargetAmount", Input.TargetAmount.Value);
                    insertFundraiserCmd.Parameters.AddWithValue("@StartDate", (object?)Input.StartDate ?? DBNull.Value);
                    insertFundraiserCmd.Parameters.AddWithValue("@EndDate", (object?)Input.EndDate ?? DBNull.Value);

                    await insertFundraiserCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                StatusMessage = "Event created successfully.";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Create failed: " + ex.Message);
            }

            await LoadCategoriesAsync();
            await LoadEventsAsync(q);
            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            // Bind from posted form manually (safe for multiple modals)
            Input.EventId = TryParseInt(Request.Form["EventId"]);
            Input.CategoryID = TryParseInt(Request.Form["CategoryID"]) ?? 0;
            Input.Title = Request.Form["Title"];
            Input.Description = Request.Form["Description"];
            Input.StartDate = TryParseDate(Request.Form["StartDate"]);
            Input.EndDate = TryParseDate(Request.Form["EndDate"]);
            Input.TargetAmount = TryParseDecimal(Request.Form["TargetAmount"]);

            if (!Input.EventId.HasValue || string.IsNullOrWhiteSpace(Input.Title) || Input.CategoryID == 0)
            {
                ModelState.AddModelError(string.Empty, "Please fill all required fields.");
                await LoadCategoriesAsync();
                await LoadEventsAsync(q);
                return Page();
            }

            using var conn = new SqlConnection(GetConnectionString());
            await conn.OpenAsync();

            using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Update Project
                var updateProjectCmd = new SqlCommand(@"
UPDATE Project
SET CategoryID=@CategoryID, Title=@Title, Description=@Description, StartDate=@StartDate, EndDate=@EndDate
WHERE ProjectID=@ProjectID;", conn, (SqlTransaction)tx);

                updateProjectCmd.Parameters.AddWithValue("@CategoryID", Input.CategoryID);
                updateProjectCmd.Parameters.AddWithValue("@Title", Input.Title);
                updateProjectCmd.Parameters.AddWithValue("@Description", (object?)Input.Description ?? DBNull.Value);
                updateProjectCmd.Parameters.AddWithValue("@StartDate", (object?)Input.StartDate ?? DBNull.Value);
                updateProjectCmd.Parameters.AddWithValue("@EndDate", (object?)Input.EndDate ?? DBNull.Value);
                updateProjectCmd.Parameters.AddWithValue("@ProjectID", Input.EventId.Value);

                await updateProjectCmd.ExecuteNonQueryAsync();

                // 2) Upsert TargetAmount on active fundraiser
                // If TargetAmount is provided => update existing active fundraiser or create one
                // If TargetAmount is empty => set NULL on existing active fundraiser (if any)
                var findActiveFundraiserCmd = new SqlCommand(@"
SELECT TOP 1 FundraiserID
FROM Fundraiser
WHERE ProjectID=@ProjectID AND IsActive=1
ORDER BY StartDate DESC, FundraiserID DESC;", conn, (SqlTransaction)tx);
                findActiveFundraiserCmd.Parameters.AddWithValue("@ProjectID", Input.EventId.Value);

                var existingFundraiserIdObj = await findActiveFundraiserCmd.ExecuteScalarAsync();
                int? existingFundraiserId = existingFundraiserIdObj != null ? Convert.ToInt32(existingFundraiserIdObj) : (int?)null;

                if (Input.TargetAmount.HasValue)
                {
                    if (existingFundraiserId.HasValue)
                    {
                        var updateTargetCmd = new SqlCommand(@"
UPDATE Fundraiser
SET TargetAmount=@TargetAmount
WHERE FundraiserID=@FundraiserID;", conn, (SqlTransaction)tx);

                        updateTargetCmd.Parameters.AddWithValue("@TargetAmount", Input.TargetAmount.Value);
                        updateTargetCmd.Parameters.AddWithValue("@FundraiserID", existingFundraiserId.Value);
                        await updateTargetCmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        var insertFundraiserCmd = new SqlCommand(@"
INSERT INTO Fundraiser (ProjectID, Title, TargetAmount, StartDate, EndDate, IsActive, CreatedAt)
VALUES (@ProjectID, @Title, @TargetAmount, @StartDate, @EndDate, 1, GETDATE());", conn, (SqlTransaction)tx);

                        insertFundraiserCmd.Parameters.AddWithValue("@ProjectID", Input.EventId.Value);
                        insertFundraiserCmd.Parameters.AddWithValue("@Title", $"{Input.Title} Fundraiser");
                        insertFundraiserCmd.Parameters.AddWithValue("@TargetAmount", Input.TargetAmount.Value);
                        insertFundraiserCmd.Parameters.AddWithValue("@StartDate", (object?)Input.StartDate ?? DBNull.Value);
                        insertFundraiserCmd.Parameters.AddWithValue("@EndDate", (object?)Input.EndDate ?? DBNull.Value);

                        await insertFundraiserCmd.ExecuteNonQueryAsync();
                    }
                }
                else
                {
                    // Clear target by setting it to NULL (if an active fundraiser exists)
                    if (existingFundraiserId.HasValue)
                    {
                        var clearTargetCmd = new SqlCommand(@"
UPDATE Fundraiser
SET TargetAmount=NULL
WHERE FundraiserID=@FundraiserID;", conn, (SqlTransaction)tx);

                        clearTargetCmd.Parameters.AddWithValue("@FundraiserID", existingFundraiserId.Value);
                        await clearTargetCmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
                StatusMessage = "Event updated successfully.";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Update failed: " + ex.Message);
            }

            await LoadCategoriesAsync();
            await LoadEventsAsync(q);
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var eventId = TryParseInt(Request.Form["EventId"]);
            if (!eventId.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Invalid EventId.");
                await LoadCategoriesAsync();
                await LoadEventsAsync(q);
                return Page();
            }

            using var conn = new SqlConnection(GetConnectionString());
            await conn.OpenAsync();

            using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Try to remove dependent Fundraisers first (to avoid FK errors)
                var deleteFundraisers = new SqlCommand(@"
DELETE FROM Fundraiser WHERE ProjectID=@ProjectID;", conn, (SqlTransaction)tx);
                deleteFundraisers.Parameters.AddWithValue("@ProjectID", eventId.Value);
                await deleteFundraisers.ExecuteNonQueryAsync();

                // Attempt to delete the Project
                var deleteProject = new SqlCommand(@"
DELETE FROM Project WHERE ProjectID=@ProjectID;", conn, (SqlTransaction)tx);
                deleteProject.Parameters.AddWithValue("@ProjectID", eventId.Value);
                var rows = await deleteProject.ExecuteNonQueryAsync();

                if (rows == 0)
                {
                    throw new Exception("Delete failed. The project may have related records (e.g., WorkOfOrganization, Reviews, Donations) blocking deletion.");
                }

                await tx.CommitAsync();
                StatusMessage = "Event deleted successfully.";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Delete failed: " + ex.Message);
            }

            await LoadCategoriesAsync();
            await LoadEventsAsync(q);
            return Page();
        }

        // ===== Helpers =====
        private async Task LoadCategoriesAsync()
        {
            Categories.Clear();
            using var conn = new SqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"SELECT CategoryID, Name FROM DonationCategory ORDER BY Name;", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Categories.Add(new CategoryDto
                {
                    CategoryID = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
        }

        private async Task LoadEventsAsync(string? search)
        {
            Events.Clear();
            using var conn = new SqlConnection(GetConnectionString());
            await conn.OpenAsync();

            // Pull active fundraiser's TargetAmount per project (if any), prefer latest StartDate
            var sql = @"
                        SELECT 
                            p.ProjectID,
                            p.CategoryID,
                            c.Name AS CategoryName,
                            p.Title,
                            p.Description,
                            p.StartDate,
                            p.EndDate,
                            fa.TargetAmount
                        FROM Project p
                        INNER JOIN DonationCategory c ON c.CategoryID = p.CategoryID
                        OUTER APPLY (
                            SELECT TOP 1 f.TargetAmount
                            FROM Fundraiser f
                            WHERE f.ProjectID = p.ProjectID AND f.IsActive = 1
                            ORDER BY f.StartDate DESC, f.FundraiserID DESC
                        ) fa
                        WHERE
                            (@q IS NULL OR @q = '')
                            OR (
                                p.Title LIKE '%' + @q + '%'
                                OR p.Description LIKE '%' + @q + '%'
                                OR c.Name LIKE '%' + @q + '%'
                            )
                        ORDER BY p.ProjectID DESC;";

            var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@q", (object?)search ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Events.Add(new EventDto
                {
                    EventId = reader.GetInt32(0),
                    CategoryID = reader.GetInt32(1),
                    CategoryName = reader.GetString(2),
                    Title = reader.GetString(3),
                    Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                    StartDate = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                    EndDate = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                    TargetAmount = reader.IsDBNull(7) ? (decimal?)null : reader.GetDecimal(7)
                });
            }
        }

        private static int? TryParseInt(string? val) =>
            int.TryParse(val, out var x) ? x : (int?)null;

        private static DateTime? TryParseDate(string? val) =>
            DateTime.TryParse(val, out var d) ? d : (DateTime?)null;

        private static decimal? TryParseDecimal(string? val) =>
            decimal.TryParse(val, out var m) ? m : (decimal?)null;
    }

    // ===== DTOs for the view =====



}
