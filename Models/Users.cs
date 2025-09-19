using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace Donation_Website.Models
{
    public class Users
    {
        DBConnection sda = new DBConnection();
        
        public (object? User, string UserType) SearchUser(string email)
        {
            using (var cmd = sda.GetQuery("SELECT AdminID, Name, Email, PasswordHash, Phone, Address, IsActive, CreatedAt, UpdatedAt FROM [Admin] WHERE Email = @Email"))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {

                        var admin = new Admin
                        {
                            AdminId = Convert.ToInt32(reader["AdminID"]),
                            Name = reader["Name"].ToString()!,
                            Email = reader["Email"].ToString()!,
                            PasswordHash = reader["PasswordHash"]?.ToString() ?? "",  // read as string safely
                            Phone = reader["Phone"]?.ToString()!,
                            Address = reader["Address"]?.ToString()!,
                            IsActive = Convert.ToBoolean(reader["IsActive"]),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["UpdatedAt"])
                        };

                        return (admin, "Admin");
                    }
                }
            }

            // 2. Check Donor
            using (var cmd = sda.GetQuery("SELECT DonorID, Name, Email, PasswordHash, Phone, Address, CreatedAt, UpdatedAt FROM [Donor] WHERE Email = @Email"))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        
                        var donor = new Donor
                        {
                            DonorID = Convert.ToInt32(reader["DonorID"]),
                            Name = reader["Name"].ToString()!,
                            Email = reader["Email"].ToString()!,
                            PasswordHash = reader["PasswordHash"]?.ToString() ?? "",  // read as string safely
                            Phone = reader["Phone"].ToString()!,
                            Address = reader["Address"].ToString()!,
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["UpdatedAt"])
                        };

                        return (donor, "Donor");
                    }
                }
            }

            // 3. Check Volunteer
            using (var cmd = sda.GetQuery("SELECT VolunteerID, Name, Email, PasswordHash, Phone, Address, Skill, Availability, CreatedAt, UpdatedAt, IsActive FROM [Volunteer] WHERE Email = @Email"))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var hashString = reader["PasswordHash"]?.ToString() ?? "";
                        var volunteer = new Volunteer
                        {
                            VolunteerID = Convert.ToInt32(reader["VolunteerID"]),
                            Name = reader["Name"].ToString()!,
                            Email = reader["Email"].ToString()!,
                            PasswordHash = reader["PasswordHash"]?.ToString() ?? "",  // read as string safely
                            Phone = reader["Phone"].ToString(),
                            Address = reader["Address"].ToString(),
                            Skill = reader["Skill"].ToString(),
                            Availability = reader["Availability"].ToString(),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["UpdatedAt"]),
                            IsActive = Convert.ToBoolean(reader["IsActive"])
                        };
                        return (volunteer, "Volunteer");
                    }
                }
            }

            return (null, "NotFound");
        }

        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            using var sha256 = SHA256.Create();
            byte[] enteredBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(enteredPassword));
            string enteredHash = Convert.ToBase64String(enteredBytes);
            return enteredHash == storedHash;
        }

        public static string HashPasswordForDatabase(string plainPassword)
        {
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(plainPassword));
               
            return Convert.ToBase64String(hashBytes);
        }

    }
}
