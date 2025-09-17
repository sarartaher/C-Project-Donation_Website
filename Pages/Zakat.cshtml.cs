using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;

namespace Donation_Website.Pages
{
    public class ZakatModel : PageModel
    {
        private readonly DBConnection db = new DBConnection();

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

                SaveZakatToCart(ZakatAmount);
            }
        }

        private void SaveZakatToCart(double amount)
        {
            int currentUserId = 1; // Replace with actual logged-in donor ID

            using (SqlCommand cmd = db.GetQuery(@"
                DECLARE @CartId INT;
                SELECT TOP 1 @CartId = CartID FROM Cart WHERE DonorID = @DonorId AND Status = 'Pending';

                IF @CartId IS NULL
                BEGIN
                    INSERT INTO Cart (DonorID, Status) VALUES (@DonorId, 'Pending');
                    SET @CartId = SCOPE_IDENTITY();
                END

                -- Insert as a 'Zakat' donation (fundraiser not linked, use dummy -1)
                INSERT INTO CartItems (CartID, FundraiserID, Amount)
                VALUES (@CartId, -1, @Amount);
            "))
            {
                cmd.Parameters.AddWithValue("@DonorId", currentUserId);
                cmd.Parameters.AddWithValue("@Amount", amount);

                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
                cmd.Connection.Close();
            }
        }
    }
}
