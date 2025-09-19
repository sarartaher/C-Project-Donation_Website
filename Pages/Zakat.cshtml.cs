using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Donation_Website.Models;

namespace Donation_Website.Pages
{
    public class ZakatModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Please enter your money amount")]
        [Range(0, double.MaxValue, ErrorMessage = "Enter a valid number")]
        public double Money { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Please enter your property valuation")]
        [Range(0, double.MaxValue, ErrorMessage = "Enter a valid number")]
        public double Property { get; set; }

        public double ZakatAmount { get; set; }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Calculate Zakat (2.5% of total wealth)
            ZakatAmount = (Money + Property) * 0.025;

            return Page(); // Stay on page to show the amount; Donate button handles redirect
        }
    }
}