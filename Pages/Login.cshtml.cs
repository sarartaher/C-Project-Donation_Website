using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Donation_Website.Models;

namespace Donation_Website.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public User NewUser { get; set; } = new User();

        public IActionResult OnPost()
        {
            var userService = new Users();
            var (user, userType) = userService.SearchUser(NewUser.Email);

            if (user != null)
            {
                bool passwordValid = false;

                switch (user)
                {
                    case Admin admin:
                        passwordValid = Users.VerifyPassword(NewUser.PasswordHash, admin.PasswordHash);
                        if (passwordValid)
                        {
                            HttpContext.Session.SetString("UserName", admin.Name);
                            HttpContext.Session.SetString("UserType", "Admin");
                            return RedirectToPage("/AdminDashboard");
                        }
                        break;

                    case Donor donor:
                        passwordValid = Users.VerifyPassword(NewUser.PasswordHash, donor.PasswordHash);
                        if (passwordValid)
                        {
                            HttpContext.Session.SetString("UserName", donor.Name);
                            HttpContext.Session.SetString("UserType", "Donor");
                            return RedirectToPage("/DonorDashboard");
                        }
                        break;

                    case Volunteer volunteer:
                        if (NewUser.Email == volunteer.Email)
                        {
                            HttpContext.Session.SetString("UserName", volunteer.Name);
                            HttpContext.Session.SetString("UserType", "Volunteer");
                            return RedirectToPage("/VolunteerDashboard");
                        }
                        break;
                }

                ModelState.AddModelError("", "Invalid password.");
            }
            else
            {
                ModelState.AddModelError("", "User not found.");
            }

            return Page();
        }
    }
}
