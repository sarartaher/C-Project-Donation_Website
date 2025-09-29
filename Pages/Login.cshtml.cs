using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Donation_Website.Models;  // AuditLogger
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Donation_Website.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public User NewUser { get; set; } = new User();

        public async Task<IActionResult> OnPost()
        {
            var userService = new Users();
            var (user, userType) = userService.SearchUser(NewUser.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return Page();
            }

            bool passwordValid = false;

            switch (user)
            {
                // ===== ADMIN =====
                case Admin admin:
                    passwordValid = Users.VerifyPassword(NewUser.PasswordHash, admin.PasswordHash);
                    if (!passwordValid) break;

                    var adminId = await GetAdminIdByEmailAsync(admin.Email);
                    var adminClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, adminId.ToString()),
                        new Claim(ClaimTypes.Name, admin.Name ?? admin.Email),
                        new Claim(ClaimTypes.Email, admin.Email),
                        new Claim(ClaimTypes.Role, "Admin")
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(new ClaimsIdentity(adminClaims, CookieAuthenticationDefaults.AuthenticationScheme))
                    );

                    HttpContext.Session.SetString("UserName", admin.Name);
                    HttpContext.Session.SetString("UserType", "Admin");
                    HttpContext.Session.SetString("UserEmail", admin.Email);

                    try { if (adminId > 0) await AuditLogger.LogAsync(new DBConnection(), adminId, "Admin Login"); } catch { }

                    return RedirectToPage("/AdminDashboard");

                // ===== DONOR =====
                case Donor donor:
                    passwordValid = Users.VerifyPassword(NewUser.PasswordHash, donor.PasswordHash);
                    if (!passwordValid) break;

                    var donorClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, donor.Email),
                        new Claim(ClaimTypes.Name, donor.Name ?? donor.Email),
                        new Claim(ClaimTypes.Email, donor.Email),
                        new Claim(ClaimTypes.Role, "Donor")
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(new ClaimsIdentity(donorClaims, CookieAuthenticationDefaults.AuthenticationScheme))
                    );

                    HttpContext.Session.SetString("UserName", donor.Name);
                    HttpContext.Session.SetString("UserType", "Donor");
                    HttpContext.Session.SetString("UserEmail", donor.Email);

                    return RedirectToPage("/DonorDashboard");

                // ===== VOLUNTEER =====
                case Volunteer volunteer:
                    passwordValid = Users.VerifyPassword(NewUser.PasswordHash, volunteer.PasswordHash);
                    if (!passwordValid) break;

                    // Gate: only verified volunteers may log in
                    if (!volunteer.IsActive)
                    {
                        ModelState.AddModelError(string.Empty, "Your volunteer profile is pending admin verification.");
                        return Page();
                    }

                    var volunteerClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, volunteer.Email),
                        new Claim(ClaimTypes.Name, volunteer.Name ?? volunteer.Email),
                        new Claim(ClaimTypes.Email, volunteer.Email),
                        new Claim(ClaimTypes.Role, "Volunteer")
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(new ClaimsIdentity(volunteerClaims, CookieAuthenticationDefaults.AuthenticationScheme))
                    );

                    HttpContext.Session.SetString("UserName", volunteer.Name);
                    HttpContext.Session.SetString("UserType", "Volunteer");
                    HttpContext.Session.SetString("UserEmail", volunteer.Email);

                    return RedirectToPage("/VolunteerDashboard");
            }

            // If we got here, password was invalid for the found user
            ModelState.AddModelError(string.Empty, "Invalid password.");
            return Page();
        }

        private static async Task<int> GetAdminIdByEmailAsync(string email)
        {
            var db = new DBConnection();
            using var cmd = db.GetQuery("SELECT AdminID FROM Admin WHERE Email=@e;");
            cmd.Parameters.AddWithValue("@e", email);

            try
            {
                await cmd.Connection!.OpenAsync();
                var scalar = await cmd.ExecuteScalarAsync();
                return (scalar == null || scalar == DBNull.Value) ? 0 : Convert.ToInt32(scalar);
            }
            finally
            {
                if (cmd.Connection?.State == System.Data.ConnectionState.Open)
                    await cmd.Connection.CloseAsync();
            }
        }
    }
}