using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Donation_Website.Models;

namespace Donation_Website.Pages
{
    public class OrphanageModel : PageModel
    {
        private readonly Users _users = new Users();

        [BindProperty]
        [Required(ErrorMessage = "Please enter a donation amount")]
        [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Authenticate user to ensure they are a donor
                string email = User.Identity?.Name ?? "donor1@gmail.com"; // Fallback for testing
                var (user, userType) = _users.SearchUser(email);

                if (userType != "Donor" || user == null)
                {
                    ViewData["ErrorMessage"] = "You must be logged in as a donor to proceed.";
                    return Page();
                }

                // Redirect to Donation page with FundraiserID=2 (Orphanage Support) and amount
                return RedirectToPage("/Donation", new { fundraiserId = 2, amount = Amount });
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "An error occurred. Please try again.";
                return Page();
            }
        }
    }
}