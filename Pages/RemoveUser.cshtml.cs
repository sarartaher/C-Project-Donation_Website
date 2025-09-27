using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Donation_Website.Models;
using System.Collections.Generic;

namespace Donation_Website.Pages
{
    public class RemoveUserModel : PageModel
    {
        public List<RemoveModel> Users { get; set; } = new();
        [BindProperty] public string? Message { get; set; }
        public bool IsSuccess { get; set; }

        public void OnGet()
        {
            LoadUsers();
        }

        private void LoadUsers()
        {
            Users.Clear();
            string query = @"
        SELECT AdminID      AS Id, Name, Email, Phone, 'Admin'      AS TableName FROM Admin
        UNION ALL
        SELECT DonorID      AS Id, Name, Email, Phone, 'Donor'      AS TableName FROM Donor
        UNION ALL
        SELECT VolunteerID  AS Id, Name, Email, Phone, 'Volunteer' AS TableName FROM Volunteer";

            using var cmd = new DBConnection().GetQuery(query);
            using var con = cmd.Connection;
            con.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Users.Add(new RemoveModel
                {
                    Id = reader.GetInt32(0),
                    Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Email = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Phone = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Table = reader.IsDBNull(4) ? "" : reader.GetString(4)
                });
            }
        }

        public IActionResult OnPostDelete(int userId, string tableName)
        {
            try
            {
                if (tableName == "Admin")
                {
                    Message = "Admin cannot delete other Admin.";
                    return Page();
                }

                if (tableName == "Donor")
                {
                    DeleteDonorWithReviews(userId);
                }
                else if (tableName == "Volunteer")
                {
                    DeleteGeneric(userId, "Volunteer", "VolunteerID");
                }

                IsSuccess = true;
                Message = "User removed successfully.";
            }
            catch (SqlException ex)
            {
                IsSuccess = false;
                Message = "Database error: " + ex.Message;
            }

            LoadUsers();
            return Page();
        }

        // First delete all Reviews referencing the donor, then delete donor
        private void DeleteDonorWithReviews(int donorId)
        {
            using (var cmdChild = new DBConnection().GetQuery(
                "DELETE FROM Review WHERE DonorId = @id"))
            {
                cmdChild.Parameters.AddWithValue("@id", donorId);
                using var con = cmdChild.Connection;
                con.Open();
                cmdChild.ExecuteNonQuery();
            }

            DeleteGeneric(donorId, "Donor", "DonorID");
        }

        private void DeleteGeneric(int id, string table, string key)
        {
            using var cmd = new DBConnection().GetQuery(
                $"DELETE FROM {table} WHERE {key} = @id");
            cmd.Parameters.AddWithValue("@id", id);
            using var con = cmd.Connection;
            con.Open();
            cmd.ExecuteNonQuery();
        }
    }
}
