using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;

namespace Donation_Website.Pages
{
    public class DonorPakageModel : PageModel
    {
        private readonly DBConnection _db = new DBConnection();
        private readonly Users _users = new Users();

        public void OnGet()
        {
            // Just loads the donor package page
        }

        // ===================== DONATE HANDLER =====================
        public IActionResult OnPostDonate(int FundraiserId, decimal Amount)
        {
            int donorId = GetDonorId();

            // 1. Check if there is an existing pending cart for donor
            int cartId = GetOrCreateCart(donorId);

            // 2. Insert into CartItems
            using (var cmd = _db.GetQuery(@"
                INSERT INTO CartItems (CartID, FundraiserID, Amount, CreatedAt)
                VALUES (@CartID, @FundraiserID, @Amount, GETDATE())
            "))
            {
                cmd.Parameters.AddWithValue("@CartID", cartId);
                cmd.Parameters.AddWithValue("@FundraiserID", FundraiserId);
                cmd.Parameters.AddWithValue("@Amount", Amount);
                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
                cmd.Connection.Close();
            }

            TempData["Success"] = "Donation package added to cart!";
            return RedirectToPage("/cart"); // Redirect to cart page
        }

        // ===================== HELPER: GET OR CREATE CART =====================
        private int GetOrCreateCart(int donorId)
        {
            int cartId = 0;

            using (var cmd = _db.GetQuery(@"
                SELECT TOP 1 CartID 
                FROM Cart 
                WHERE DonorID=@DonorID AND (Status IS NULL OR Status='Pending') 
                ORDER BY CreatedAt DESC"))
            {
                cmd.Parameters.AddWithValue("@DonorID", donorId);
                cmd.Connection.Open();
                var result = cmd.ExecuteScalar();
                cmd.Connection.Close();

                if (result != null)
                {
                    cartId = (int)result;
                }
                else
                {
                    using (var insertCmd = _db.GetQuery(@"
                        INSERT INTO Cart (DonorID, Status, CreatedAt, UpdatedAt)
                        OUTPUT INSERTED.CartID
                        VALUES (@DonorID, 'Pending', GETDATE(), GETDATE())
                    "))
                    {
                        insertCmd.Parameters.AddWithValue("@DonorID", donorId);
                        insertCmd.Connection.Open();   // ? FIXED
                        cartId = (int)insertCmd.ExecuteScalar();
                        insertCmd.Connection.Close();
                    }
                }
            }
            return cartId;
        }

        // ===================== HELPER: GET DONOR ID =====================
        private int GetDonorId()
        {
            int donorId = 0;
            if (User.Identity?.IsAuthenticated == true)
            {
                string email = User.Identity.Name;
                var (user, userType) = _users.SearchUser(email);
                donorId = (userType == "Donor" && user != null) ? (user as Donor).DonorID : CreateGuestDonor();
            }
            else
            {
                donorId = CreateGuestDonor();
            }
            return donorId;
        }

        // ===================== HELPER: CREATE GUEST DONOR =====================
        private int CreateGuestDonor()
        {
            int guestId = 0;

            string sessionKey = HttpContext.Session.GetString("GuestDonorEmail");
            string guestEmail;

            if (!string.IsNullOrEmpty(sessionKey))
            {
                guestEmail = sessionKey;
                using (var cmdCheck = _db.GetQuery("SELECT DonorID FROM Donor WHERE Email=@Email"))
                {
                    cmdCheck.Parameters.AddWithValue("@Email", guestEmail);
                    cmdCheck.Connection.Open();
                    var result = cmdCheck.ExecuteScalar();
                    cmdCheck.Connection.Close();
                    if (result != null)
                        return (int)result;
                }
            }

            guestEmail = $"guest{Guid.NewGuid().ToString("N").Substring(0, 12)}@guest.com";
            HttpContext.Session.SetString("GuestDonorEmail", guestEmail);

            string dummyPassword = "GUEST";

            using (var cmd = _db.GetQuery(@"
                INSERT INTO Donor (Name, Email, PasswordHash, CreatedAt)
                OUTPUT INSERTED.DonorID
                VALUES ('Guest', @Email, @PasswordHash, GETDATE())
            "))
            {
                cmd.Parameters.AddWithValue("@Email", guestEmail);
                cmd.Parameters.AddWithValue("@PasswordHash", dummyPassword);
                cmd.Connection.Open();
                guestId = (int)cmd.ExecuteScalar();
                cmd.Connection.Close();
            }

            return guestId;
        }
    }
}

