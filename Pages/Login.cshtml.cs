using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
                            HttpContext.Session.SetString("UserEmail", admin.Email); 
                            return RedirectToPage("/AdminDashboard");
                        }
                        break;

                    case Donor donor:
                        passwordValid = Users.VerifyPassword(NewUser.PasswordHash, donor.PasswordHash);
                        if (passwordValid)
                        {
                            HttpContext.Session.SetString("UserName", donor.Name);
                            HttpContext.Session.SetString("UserType", "Donor");
                            HttpContext.Session.SetString("UserEmail", donor.Email); 
                            return RedirectToPage("/DonorDashboard");
                        }
                        break;

                    case Volunteer volunteer:
                        passwordValid = Users.VerifyPassword(NewUser.PasswordHash, volunteer.PasswordHash);
                        if (passwordValid)
                        {
                            HttpContext.Session.SetString("UserName", volunteer.Name);
                            HttpContext.Session.SetString("UserType", "Volunteer");
                            HttpContext.Session.SetString("UserEmail", volunteer.Email); 
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
