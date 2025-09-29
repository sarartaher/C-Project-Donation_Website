using System;                               // DateTime, StringComparison
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Donation_Website.Models;  

namespace Donation_Website.Pages
{
    [Authorize(Roles = "Admin")]
    public class LogsModel : PageModel
    {
        public List<FeedRow> Feed { get; set; } = new();

        private readonly DBConnection _db = new DBConnection();

        public async Task OnGetAsync()
        {
            using var cmd = _db.GetQuery(@"
                    SELECT TOP (200)
                           a.AdminID,
                           COALESCE(a.Name, ad.Name, ad.Email) AS AdminName,
                           a.Action,
                           a.BeforeData,
                           a.AfterData,
                           a.CreatedAt
                    FROM AuditLog a
                    LEFT JOIN Admin ad ON ad.AdminID = a.AdminID
                    ORDER BY a.CreatedAt DESC;");

            try
            {
                await cmd.Connection!.OpenAsync();
                using var r = await cmd.ExecuteReaderAsync();

                while (await r.ReadAsync())
                {
                    var adminId = r.IsDBNull(0) ? 0 : r.GetInt32(0);
                    var adminName = r.IsDBNull(1) ? "" : r.GetString(1);
                    var action = r.IsDBNull(2) ? "" : r.GetString(2);
                    var beforeJson = r.IsDBNull(3) ? null : r.GetString(3);
                    var afterJson = r.IsDBNull(4) ? null : r.GetString(4);
                    var created = r.GetDateTime(5);

                    Feed.Add(new FeedRow
                    {
                        CreatedAt = created,
                        Message = BuildMessage(adminName, adminId, action, beforeJson, afterJson)
                    });
                }
            }
            finally
            {
                if (cmd.Connection?.State == System.Data.ConnectionState.Open)
                    await cmd.Connection.CloseAsync();
            }
        }

        // Build a single human-friendly line, including the actor and entity details
        private static string BuildMessage(string adminName, int adminId, string action, string? beforeJson, string? afterJson)
        {
            var who = !string.IsNullOrWhiteSpace(adminName) ? adminName : (adminId > 0 ? $"Admin #{adminId}" : "Admin");

            // Read both title and an identifier from either JSON (case-insensitive)
            var beforeTitle = TryGetStringCI(beforeJson, "Title");
            var afterTitle = TryGetStringCI(afterJson, "Title");

            var beforeId = TryGetFirstIntCI(beforeJson, "EventId", "ProjectID", "Id");
            var afterId = TryGetFirstIntCI(afterJson, "EventId", "ProjectID", "Id");
            var anyId = beforeId ?? afterId;

            string IdSuffix() => anyId.HasValue ? $" (ID {anyId.Value})" : string.Empty;

            switch (action)
            {
                case "Project Create":
                    return $"{who} created event \"{afterTitle ?? "(no title)"}\"{IdSuffix()}";

                case "Project Delete":
                    return $"{who} deleted event \"{beforeTitle ?? "(no title)"}\"{IdSuffix()}";

                case "Project Edit":
                    if (!string.IsNullOrWhiteSpace(beforeTitle) &&
                        !string.Equals(beforeTitle, afterTitle, StringComparison.Ordinal))
                    {
                        return $"{who} renamed event \"{beforeTitle}\" → \"{afterTitle ?? "(no title)"}\"{IdSuffix()}";
                    }
                    return $"{who} edited event \"{afterTitle ?? beforeTitle ?? "(no title)"}\"{IdSuffix()}";

                case "Admin Login":
                    return $"{who} logged in";

                default:
                    return !string.IsNullOrWhiteSpace(action) ? $"{who}: {action}" : $"{who}: Activity";
            }
        }

        // ===== JSON helpers (case-insensitive) =====

        private static string? TryGetStringCI(string? json, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return null;

                // 1) Try exact name first (PascalCase)
                if (root.TryGetProperty(propertyName, out var p1))
                    return ExtractString(p1);

                // 2) Try camelCase version
                var camel = char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
                if (root.TryGetProperty(camel, out var p2))
                    return ExtractString(p2);

                // 3) Fallback: enumerate properties and compare ignoring case
                foreach (var prop in root.EnumerateObject())
                {
                    if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                        return ExtractString(prop.Value);
                }
            }
            catch { /* ignore malformed */ }

            return null;
        }

        private static int? TryGetFirstIntCI(string? json, params string[] propertyNames)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return null;

                foreach (var name in propertyNames)
                {
                    // exact
                    if (root.TryGetProperty(name, out var pExact))
                    {
                        var v = ExtractInt(pExact);
                        if (v.HasValue) return v;
                    }
                    // camelCase
                    var camel = char.ToLowerInvariant(name[0]) + name.Substring(1);
                    if (root.TryGetProperty(camel, out var pCamel))
                    {
                        var v = ExtractInt(pCamel);
                        if (v.HasValue) return v;
                    }
                    // enum, case-insensitive
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            var v = ExtractInt(prop.Value);
                            if (v.HasValue) return v;
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private static string? ExtractString(JsonElement el) =>
            el.ValueKind switch
            {
                JsonValueKind.String => el.GetString(),
                JsonValueKind.Number => el.ToString(),
                JsonValueKind.Null => null,
                _ => el.ToString()
            };

        private static int? ExtractInt(JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Number => el.TryGetInt32(out var n) ? n : (int?)null,
                JsonValueKind.String => int.TryParse(el.GetString(), out var s) ? s : (int?)null,
                _ => null
            };
        }
    }

    
}