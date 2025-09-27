using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace Donation_Website.Pages
{
    public class ManageRolesModel : PageModel
    {
        private readonly DBConnection db = new DBConnection();

        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
        public string SelectedRole { get; set; } = "";
        public string Message { get; set; } = "";

        private readonly string[] RolesList = new string[] { "Admin", "Donor", "Volunteer" };

        public void OnGet(string selectedRole)
        {
            SelectedRole = selectedRole ?? "";
            LoadUsers();
        }

        private void LoadUsers()
        {
            Users.Clear();

            foreach (var role in RolesList)
            {
                if (!string.IsNullOrEmpty(SelectedRole) && SelectedRole != role)
                    continue;

                Users.AddRange(GetUsersFromTable(role));
            }
        }

        private List<UserViewModel> GetUsersFromTable(string tableName)
        {
            var users = new List<UserViewModel>();

            string query = $"SELECT * FROM {tableName}";

            using (var cmd = db.GetQuery(query))
            {
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    // Dynamically get primary key (first column)
                    string pkColumn = reader.GetSchemaTable().Rows[0]["ColumnName"].ToString();

                    while (reader.Read())
                    {
                        users.Add(new UserViewModel
                        {
                            Id = Convert.ToInt32(reader[pkColumn]),
                            Name = reader["Name"] != DBNull.Value ? reader["Name"].ToString() : "",
                            Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : "",
                            Phone = reader["Phone"] != DBNull.Value ? reader["Phone"].ToString() : "",
                            Table = tableName
                        });
                    }
                }
                cmd.Connection.Close();
            }

            return users;
        }

        public IActionResult OnPostChangeRole(int userId, string currentRole, string newRole)
        {
            if (currentRole == newRole)
            {
                Message = "Role is the same. No changes made.";
                LoadUsers();
                return Page();
            }

            try
            {
                // Detect primary key dynamically for current table
                string pkColumn = "";
                using (var cmd = db.GetQuery($"SELECT * FROM {currentRole} WHERE 1=0"))
                {
                    cmd.Connection.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        pkColumn = reader.GetSchemaTable().Rows[0]["ColumnName"].ToString();
                    }
                    cmd.Connection.Close();
                }

                // Fetch the user from currentRole table
                object name, email, phone, passwordHash;
                using (var cmd = db.GetQuery($"SELECT * FROM {currentRole} WHERE {pkColumn}=@Id"))
                {
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.Connection.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            Message = "User not found.";
                            return Page();
                        }

                        name = reader["Name"];
                        email = reader["Email"];
                        phone = reader["Phone"];
                        passwordHash = reader["PasswordHash"]; // copy password
                    }
                    cmd.Connection.Close();
                }

                // Insert into newRole table
                using (var cmd = db.GetQuery($"INSERT INTO {newRole} (Name, Email, Phone, PasswordHash) VALUES (@Name,@Email,@Phone,@PasswordHash)"))
                {
                    cmd.Parameters.AddWithValue("@Name", name ?? "");
                    cmd.Parameters.AddWithValue("@Email", email ?? "");
                    cmd.Parameters.AddWithValue("@Phone", phone ?? "");
                    cmd.Parameters.AddWithValue("@PasswordHash", passwordHash ?? DBNull.Value);

                    cmd.Connection.Open();
                    cmd.ExecuteNonQuery();
                    cmd.Connection.Close();
                }

                // Delete from currentRole table
                using (var cmd = db.GetQuery($"DELETE FROM {currentRole} WHERE {pkColumn}=@Id"))
                {
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.Connection.Open();
                    cmd.ExecuteNonQuery();
                    cmd.Connection.Close();
                }

                Message = $"User moved from {currentRole} to {newRole}.";
            }
            catch (Exception ex)
            {
                Message = $"Error changing role: {ex.Message}";
            }

            LoadUsers();
            return Page();
        }
    }
}
