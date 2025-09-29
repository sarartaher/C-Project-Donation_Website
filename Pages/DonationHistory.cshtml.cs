using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace Donation_Website.Pages.Donations
{
    public class DonationHistoryModel : PageModel
    {
        private readonly DBConnection _db = new DBConnection();

        // -------- Filters (from querystring) --------
        [BindProperty(SupportsGet = true)] public string? Search { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Date { get; set; }
        [BindProperty(SupportsGet = true)] public decimal? MinAmount { get; set; }
        [BindProperty(SupportsGet = true)] public decimal? MaxAmount { get; set; }
        [BindProperty(SupportsGet = true)] public List<string> Events { get; set; } = new();

        // Optional: quick testing override => /Donations/DonationHistory?DonorId=1
        [BindProperty(SupportsGet = true)] public int? DonorId { get; set; }

        // -------- Data for the view --------
        public List<Row> Records { get; private set; } = new();
        public List<SelectListItem> EventSelectList { get; private set; } = new();

        public IActionResult OnGet()
        {
            int? donorId = DonorId ?? TryGetDonorId();   // prefer explicit ?DonorId=, else claims/session/email
            if (donorId is null)
            {
                // Not logged in / donor not resolvable -> empty state
                Records = new();
                EventSelectList = new();
                return Page();
            }

            // 1) Build Event options from ALL donations for this donor (no filters)
            var allEventNames = FetchEventNames(donorId.Value);

            // populate multi-select; keep previous selections checked
            var selected = new HashSet<string>(Events ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            EventSelectList = allEventNames
                .OrderBy(n => n)
                .Select(n => new SelectListItem { Text = n, Value = n, Selected = selected.Contains(n) })
                .ToList();

            // 2) Load filtered rows (or all if no filters)
            var rows = FetchDonationsFiltered(donorId.Value);

            // 3) If Event filter chosen, apply in-memory (no STRING_SPLIT dependency)
            if (Events != null && Events.Count > 0)
            {
                var set = Events.Where(e => !string.IsNullOrWhiteSpace(e))
                                .Select(e => e.Trim())
                                .ToHashSet(StringComparer.OrdinalIgnoreCase);
                rows = rows.Where(r => set.Contains(r.EventName)).ToList();
            }

            Records = rows.OrderByDescending(r => r.Date).ToList();
            return Page();
        }

        // === Queries ===

        // Distinct event titles for this donor (for the multiselect)
        private List<string> FetchEventNames(int donorId)
        {
            const string sql = @"
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
SELECT DISTINCT f.Title
FROM Donation d WITH (NOLOCK)
JOIN Fundraiser f WITH (NOLOCK) ON f.FundraiserID = d.FundraiserID
WHERE d.DonorID = @DonorID;";

            var names = new List<String>();
            using (var cmd = _db.GetQuery(sql))
            using (cmd.Connection)
            {
                cmd.CommandTimeout = 15;
                cmd.Parameters.Add(new SqlParameter("@DonorID", SqlDbType.Int) { Value = donorId });

                cmd.Connection.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var title = rdr[0]?.ToString();
                        if (!string.IsNullOrWhiteSpace(title))
                            names.Add(title!);
                    }
                }
            }
            return names;
        }

        // All donations for this donor with server-side filters (except Event list, which we do in-memory)
        private List<Row> FetchDonationsFiltered(int donorId)
        {
            const string sql = @"
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
SELECT
    d.DonationID,
    d.[Date]            AS DonationDate,
    ISNULL(d.Amount,0)  AS Amount,
    f.Title             AS EventName
FROM Donation d WITH (NOLOCK)
JOIN Fundraiser f WITH (NOLOCK) ON f.FundraiserID = d.FundraiserID
WHERE d.DonorID = @DonorID
  AND (@Date IS NULL OR CAST(d.[Date] AS DATE) = @Date)
  AND (@MinAmount IS NULL OR d.Amount >= @MinAmount)
  AND (@MaxAmount IS NULL OR d.Amount <= @MaxAmount)
  AND (
        @Search IS NULL OR
        f.Title LIKE '%' + @Search + '%' OR
        CONVERT(varchar(10), CAST(d.[Date] AS DATE), 120) LIKE '%' + @Search + '%' OR
        CONVERT(varchar(32), d.Amount) LIKE '%' + @Search + '%'
      )
ORDER BY d.[Date] DESC;";

            var rows = new List<Row>();
            using (var cmd = _db.GetQuery(sql))
            using (cmd.Connection)
            {
                cmd.CommandTimeout = 15;
                cmd.Parameters.Add(new SqlParameter("@DonorID", SqlDbType.Int) { Value = donorId });
                cmd.Parameters.Add(new SqlParameter("@Date", SqlDbType.Date) { Value = (object?)Date ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@MinAmount", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = (object?)MinAmount ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@MaxAmount", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = (object?)MaxAmount ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Search", SqlDbType.NVarChar, 100) { Value = (object?)NullIfEmpty(Search) ?? DBNull.Value });

                cmd.Connection.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    int iDonationId = rdr.GetOrdinal("DonationID");
                    int iDonationDt = rdr.GetOrdinal("DonationDate");
                    int iAmount = rdr.GetOrdinal("Amount");
                    int iEvent = rdr.GetOrdinal("EventName");

                    while (rdr.Read())
                    {
                        rows.Add(new Row
                        {
                            DonationID = rdr.GetInt32(iDonationId),
                            Date = rdr.GetDateTime(iDonationDt),
                            EventName = rdr.IsDBNull(iEvent) ? "" : rdr.GetString(iEvent),
                            Amount = rdr.GetDecimal(iAmount)
                        });
                    }
                }
            }
            return rows;
        }

        private static object? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

        // Resolve the current donor
        private int? TryGetDonorId()
        {
            // 1) Explicit DonorID claim (ideal)
            var idClaim = User?.FindFirst("DonorID") ?? User?.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim != null && int.TryParse(idClaim.Value, out var idFromClaim))
                return idFromClaim;

            // 2) Session
            var idFromSession = HttpContext.Session.GetInt32("DonorID");
            if (idFromSession.HasValue) return idFromSession;

            // 3) Email claim -> lookup
            var emailClaim = User?.FindFirst(ClaimTypes.Email) ?? User?.FindFirst("email");
            var email = emailClaim?.Value;
            if (!string.IsNullOrWhiteSpace(email))
            {
                const string sql = @"
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
SELECT TOP 1 DonorID
FROM Donor WITH (NOLOCK)
WHERE Email = @Email;";

                using (var cmd = _db.GetQuery(sql))
                using (cmd.Connection)
                {
                    cmd.CommandTimeout = 10;
                    cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 100) { Value = email });
                    cmd.Connection.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        return Convert.ToInt32(result);
                }
            }

            return null; // no donor context
        }

        // Row for the table
        public class Row
        {
            public int DonationID { get; set; }
            [Display(Name = "Date")] public DateTime Date { get; set; }
            [Display(Name = "Event Name")] public string EventName { get; set; } = "";
            [Display(Name = "Amount")] public decimal Amount { get; set; }
        }
    }
}