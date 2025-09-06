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
            // 1. Check Admin
            using (var cmd = sda.GetQuery("SELECT AdminID, Name, Email, PasswordHash, Role, IsActive, CreatedAt, UpdatedAt FROM [Admin] WHERE Email = @Email"))
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
                            PasswordHash = (byte[])reader["PasswordHash"],
                            Role = reader["Role"].ToString()!,
                            IsActive = Convert.ToInt32(reader["IsActive"]),
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
                            PasswordHash = (byte[])reader["PasswordHash"],
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
            using (var cmd = sda.GetQuery("SELECT VolunteerID, Name, Email, PasswordHash, Phone, Address, Skills, Availability, CreatedAt, UpdatedAt FROM [Volunteer] WHERE Email = @Email"))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var volunteer = new Volunteer
                        {
                            VolunteerID = Convert.ToInt32(reader["VolunteerID"]),
                            Name = reader["Name"].ToString()!,
                            Email = reader["Email"].ToString()!,
                            PasswordHash = reader["PasswordHash"].ToString()!, // Assuming volunteer password is stored as string
                            Phone = reader["Phone"].ToString(),
                            Address = reader["Address"].ToString(),
                            //Skills = reader["Skills"].ToString(),
                            //Availability = reader["Availability"].ToString(),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["UpdatedAt"])
                        };
                        return (volunteer, "Volunteer");
                    }
                }
            }

            return (null, "NotFound");
        }

        public static bool VerifyPassword(string enteredPassword, byte[] storedHash)
        {
            using var sha256 = SHA256.Create();
            var enteredBytes = Encoding.UTF8.GetBytes(enteredPassword);
            var enteredHash = sha256.ComputeHash(enteredBytes);
            return enteredHash.SequenceEqual(storedHash);
        }

        public static byte[] HashPasswordForDatabase(string plainPassword)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(plainPassword));
        }
    }
}
