using System.Text.Json;

namespace Donation_Website.Models
{
    public class AuditLogger
    {
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public static async Task LogAsync(DBConnection db, int adminId, string action, object? before = null, object? after = null)
        {
            using var cmd = db.GetQuery(@"
            INSERT INTO AuditLog (AdminID, Name, Action, BeforeData, AfterData, CreatedAt)
            VALUES (@aid, NULL, @act, @before, @after, GETDATE());");
            cmd.Parameters.AddWithValue("@aid", adminId);
            cmd.Parameters.AddWithValue("@act", action);
            cmd.Parameters.AddWithValue("@before", (object?)Serialize(before) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@after", (object?)Serialize(after) ?? DBNull.Value);

            await cmd.Connection!.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            await cmd.Connection.CloseAsync();
        }

        private static string? Serialize(object? o) =>
            o is null ? null : JsonSerializer.Serialize(o, JsonOpts);

    }
}
