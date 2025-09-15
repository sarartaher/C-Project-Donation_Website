using Donation_Website.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Donation_Website.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly EmailSettings _emailSettings;

        public ResetPasswordModel(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        [BindProperty]
        [Required, EmailAddress]
        public string Email { get; set; }

        [BindProperty]
        [Required]
        public string? OTP { get; set; }

        [BindProperty]
        [DataType(DataType.Password)]
        [Required]
        public string NewPassword { get; set; }

        public string? Message { get; set; }

        private const string SESSION_OTP = "_OTP";
        private const string SESSION_EMAIL = "_Email";
        private const string SESSION_EXPIRY = "_OTPExpiry";

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            string sessionOtp = HttpContext.Session.GetString(SESSION_OTP);
            string sessionEmail = HttpContext.Session.GetString(SESSION_EMAIL);
            string sessionExpiryStr = HttpContext.Session.GetString(SESSION_EXPIRY);
            DateTime sessionExpiry = sessionExpiryStr != null ? DateTime.Parse(sessionExpiryStr) : DateTime.MinValue;

            // Step 1: Send OTP if not sent
            if (string.IsNullOrEmpty(sessionOtp))
            {
                string generatedOTP = new Random().Next(100000, 999999).ToString();
                HttpContext.Session.SetString(SESSION_OTP, generatedOTP);
                HttpContext.Session.SetString(SESSION_EMAIL, Email);
                HttpContext.Session.SetString(SESSION_EXPIRY, DateTime.UtcNow.AddMinutes(10).ToString());

                try
                {
                    await SendOTPEmailAsync(Email, generatedOTP);
                    Message = "OTP sent to your email. It is valid for 10 minutes.";
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to send OTP: " + ex.Message);
                    return Page();
                }

                return Page();
            }

            // Step 2: Verify OTP
            if (sessionEmail != Email)
            {
                ModelState.AddModelError("", "Email does not match OTP request.");
                return Page();
            }

            if (DateTime.UtcNow > sessionExpiry)
            {
                ModelState.AddModelError("", "OTP expired. Request a new one.");
                HttpContext.Session.Remove(SESSION_OTP);
                return Page();
            }

            if (OTP != sessionOtp)
            {
                ModelState.AddModelError("OTP", "Invalid OTP.");
                return Page();
            }

            // Step 3: Update password
            try
            {
                UpdatePasswordInDatabase(Email, NewPassword);
                Message = "Password reset successfully!";
                HttpContext.Session.Remove(SESSION_OTP);
                HttpContext.Session.Remove(SESSION_EMAIL);
                HttpContext.Session.Remove(SESSION_EXPIRY);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to update password: " + ex.Message);
            }

            return Page();
        }

        private async Task SendOTPEmailAsync(string toEmail, string otp)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Donation App", _emailSettings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Password Reset OTP";
            message.Body = new TextPart("plain")
            {
                Text = $"Your OTP for password reset is: {otp}. It is valid for 10 minutes."
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.AppPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }

        private void UpdatePasswordInDatabase(string email, string newPassword)
        {
            string hashedPassword = Users.HashPasswordForDatabase(newPassword);

            using (var connection = new SqlConnection("Data Source=USER\\SARAR;Initial Catalog=DonationManagementDB;Integrated Security=True;Encrypt=False;Trust Server Certificate=True"))
            {
                connection.Open();
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = connection;

                    cmd.CommandText = "UPDATE [Admin] SET PasswordHash=@PasswordHash, UpdatedAt=GETDATE() WHERE Email=@Email";
                    cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                    cmd.Parameters.AddWithValue("@Email", email);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        cmd.CommandText = "UPDATE [Donor] SET PasswordHash=@PasswordHash, UpdatedAt=GETDATE() WHERE Email=@Email";
                        rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            cmd.CommandText = "UPDATE [Volunteer] SET PasswordHash=@PasswordHash, UpdatedAt=GETDATE() WHERE Email=@Email";
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}
