using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace Donation_Website.Pages
{
    public class ChatModel : PageModel
    {
        private readonly DBConnection _db = new();
        public List<(string User, string Msg, DateTime Time)> Messages { get; set; } = new();

        public void OnGet()
        {
            using var cmd = _db.GetQuery("SELECT Name, AfterData, CreatedAt FROM AuditLog WHERE Action='CHAT' ORDER BY CreatedAt DESC");
            cmd.Connection.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                Messages.Add((r["Name"].ToString()!, r["AfterData"].ToString()!, Convert.ToDateTime(r["CreatedAt"])));
            }
        }

        public IActionResult OnPostSend(string text)
        {
            var name = HttpContext.Session.GetString("UserName") ?? "Anon";
            using var cmd = _db.GetQuery("INSERT INTO AuditLog(AdminID,Name,Action,AfterData) VALUES(1,@Name,'CHAT',@Msg)");
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Msg", text);
            cmd.Connection.Open();
            cmd.ExecuteNonQuery();
            return RedirectToPage();
        }
    }
}