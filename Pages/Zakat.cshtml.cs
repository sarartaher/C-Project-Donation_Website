using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

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

        public void OnPost()
        {
            if (ModelState.IsValid)
            {
                double total = Money + Property;
                ZakatAmount = total * 0.025; // 2.5%
            }
        }
    }
}
