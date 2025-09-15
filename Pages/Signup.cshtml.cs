using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Donation_Website.Models;
using System.Data.SqlClient;

namespace Donation_Website.Pages
{
    public class SignupModel : PageModel
    {
        DBConnection sda = new DBConnection();

        [BindProperty]
        public User NewUser { get; set; } = new User(); 

        public void OnGet() { }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

           
            string hashedPassword = Users.HashPasswordForDatabase(NewUser.PasswordHash);

            using (var cmd = sda.GetQuery(
                "INSERT INTO Donor (Name, Email, PasswordHash, Phone, Address, CreatedAt, UpdatedAt) " +
                "VALUES (@Name, @Email, @PasswordHash, @Phone, @Address, @CreatedAt, @UpdatedAt)"))
            {
                cmd.Parameters.AddWithValue("@Name", NewUser.Name ?? "");
                cmd.Parameters.AddWithValue("@Email", NewUser.Email ?? "");
                cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                cmd.Parameters.AddWithValue("@Phone", NewUser.Phone ?? "");
                cmd.Parameters.AddWithValue("@Address", NewUser.Address ?? "");
                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@UpdatedAt", DBNull.Value);

                if (cmd.Connection.State != System.Data.ConnectionState.Open)
                    cmd.Connection.Open();

                cmd.ExecuteNonQuery();
                cmd.Connection.Close();
            }

            return RedirectToPage("/Login"); // Redirect to login after signup
        }
    }
}
