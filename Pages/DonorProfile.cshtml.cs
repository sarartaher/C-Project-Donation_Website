using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
namespace Donation_Website.Pages
{
    public class DonorProfileModel : PageModel
    {
        private readonly DBConnection _db = new DBConnection();
        [BindProperty] public int DonorID { get; set; }
        [BindProperty] public string Name { get; set; }
        [BindProperty] public string Email { get; set; }
        [BindProperty] public string Phone { get; set; }
        [BindProperty] public string Address { get; set; }
        public string CreatedAt { get; set; }
        public void OnGet(int id = 1)
        {
            using var cmd = _db.GetQuery("SELECT * FROM Donor WHERE DonorID=@DonorID");
            cmd.Parameters.AddWithValue("@DonorID", id);
            cmd.Connection.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                DonorID = (int)reader["DonorID"];
                Name = reader["Name"].ToString();
                Email = reader["Email"].ToString();
                Phone = reader["Phone"].ToString();
                Address = reader["Address"].ToString();
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                .ToString("MMMM dd, yyyy");
            }
            cmd.Connection.Close();
        }
        public IActionResult OnPost()
        {
            using var cmd = _db.GetQuery(@"
 UPDATE Donor
 SET Name=@Name, Email=@Email, Phone=@Phone, Address=@Address,
UpdatedAt=GETDATE()
 WHERE DonorID=@DonorID");
            cmd.Parameters.AddWithValue("@Name", Name);
            cmd.Parameters.AddWithValue("@Email", Email);
            cmd.Parameters.AddWithValue("@Phone", (object)Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", (object)Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DonorID", DonorID);
            cmd.Connection.Open();
            int rows = cmd.ExecuteNonQuery();
            cmd.Connection.Close();
            if (rows > 0)
                TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToPage(new { id = DonorID });
        }
    }
}