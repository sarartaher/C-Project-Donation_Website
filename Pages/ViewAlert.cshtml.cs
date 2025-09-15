using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Donation_Website.Models;

namespace Donation_Website.Pages
{
    public class ViewAlertModel : PageModel
    {
        private readonly DBConnection _db = new();
        public List<Alert> Alerts { get; set; } = new();

        public void OnGet()
        {
            // treat AuditLog.Action = 'ALERT' as an alert
            using var cmd = _db.GetQuery("SELECT Name, Action, CreatedAt FROM AuditLog WHERE Action LIKE 'ALERT%' ORDER BY CreatedAt DESC");
            cmd.Connection.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                Alerts.Add(new Alert
                {
                    Title = r["Action"].ToString()!,
                    Message = r["Name"].ToString()!,
                    Posted = Convert.ToDateTime(r["CreatedAt"])
                });
            }
        }
    }
}

