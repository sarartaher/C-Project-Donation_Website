using Donation_Website.Data;
using Donation_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using FundraiserModel = Donation_Website.Models.Fundraiser;
namespace Donation_Website.Pages
{
    public class DonationModel : PageModel
    {
        private readonly DBConnection _db = new DBConnection();
        private readonly Users _users = new Users();

        [BindProperty] public int FundraiserId { get; set; }
        [BindProperty] public decimal Amount { get; set; }
        [BindProperty] public string DonorName { get; set; }
        [BindProperty] public string DonorEmail { get; set; }

        public List<FundraiserModel> Fundraisers { get; set; } = new List<FundraiserModel>();
        public int SelectedFundraiserId { get; set; }
        public decimal PrefilledAmount { get; set; } = 100;

        public void OnGet(int? fundraiserId, decimal? amount)
        {
            Fundraisers = GetFundraisers();
            SelectedFundraiserId = fundraiserId ?? 0;
            PrefilledAmount = amount ?? 100;
        }

        public IActionResult OnPostAddToCart()
        {
            // 1️⃣ Get donor ID (anonymous or logged in)
            int donorId = GetDonorId();


            int cartId = GetOrCreateCart(donorId);


            string name;
            string email;

            if (donorId == 1) // Guest donor
            {
                name = "Anonymous";
                email = "";
            }
            else
            {
                // Fetch donor info from DB
                using (var cmd = _db.GetQuery("SELECT Name, Email FROM Donor WHERE DonorID = @DonorId"))
                {
                    cmd.Parameters.AddWithValue("@DonorId", donorId);
                    cmd.Connection.Open();
                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        name = reader["Name"]?.ToString() ?? "Anonymous";
                        email = reader["Email"]?.ToString() ?? "";
                    }
                    else
                    {
                        name = "Anonymous";
                        email = "";
                    }
                    cmd.Connection.Close();
                }
            }


            int cartItemId;
            using (var cmd = _db.GetQuery(@"
        INSERT INTO CartItems (CartId, FundraiserId, Amount, CreatedAt)
        VALUES (@CartId, @FundraiserId, @Amount, GETDATE());
        SELECT SCOPE_IDENTITY();
    "))
            {
                cmd.Parameters.AddWithValue("@CartId", cartId);
                cmd.Parameters.AddWithValue("@FundraiserId", FundraiserId);
                cmd.Parameters.AddWithValue("@Amount", Amount);

                cmd.Connection.Open();
                cartItemId = Convert.ToInt32(cmd.ExecuteScalar());
                cmd.Connection.Close();
            }


            string paymentMethod = Request.Form["paymentMethod"];
            if (string.IsNullOrEmpty(paymentMethod))
            {
                paymentMethod = "Mobile"; // default
            }


            int paymentId;
            using (var paymentCmd = _db.GetQuery(@"
                            INSERT INTO Payment (
                                Amount,
                                PaymentMethod,
                                PaymentStatus,
                                Name,
                                Email,
                                IsActive,
                                CreatedAt,
                                UpdatedAt
                            )
                            VALUES (
                                @Amount,
                                @PaymentMethod,
                                'Pending',
                                @Name,
                                @Email,
                                1,
                                GETDATE(),
                                GETDATE()
                            );
                            SELECT SCOPE_IDENTITY(); 
"))
            {
                paymentCmd.Parameters.AddWithValue("@Amount", Amount);
                paymentCmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
                paymentCmd.Parameters.AddWithValue("@Name", name);
                paymentCmd.Parameters.AddWithValue("@Email", email);

                paymentCmd.Connection.Open();
                paymentId = Convert.ToInt32(paymentCmd.ExecuteScalar()); // <-- now you have PaymentId
                paymentCmd.Connection.Close();
            }

            return RedirectToPage("/cart", new { paymentId = paymentId, FundraiserId = FundraiserId, Amount=Amount});


        }





        private List<FundraiserModel> GetFundraisers()
        {
            var list = new List<FundraiserModel>();
            using (var cmd = _db.GetQuery("SELECT FundraiserID, Title FROM Fundraiser"))
            {
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new FundraiserModel
                        {
                            FundraiserID = (int)reader["FundraiserID"],
                            Title = reader["Title"].ToString()
                        });
                    }
                }
                cmd.Connection.Close();
            }
            return list;
        }


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

        private int CreateGuestDonor()
        {
            int guestId = 0;

            // First, try to find an existing guest donor in session or DB
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
                    {
                        return (int)result; // Reuse existing guest donor
                    }
                }
            }

            // Generate a new unique guest email
            guestEmail = $"guest{Guid.NewGuid().ToString("N").Substring(0, 12)}@guest.com";
            HttpContext.Session.SetString("GuestDonorEmail", guestEmail); // save in session

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


        private int GetOrCreateCart(int donorId)
        {
            int cartId = 0;

            using (var cmd = _db.GetQuery(@"
                SELECT TOP 1 CartID FROM Cart 
                WHERE DonorID=@DonorID AND (Status IS NULL OR Status='Pending') 
                ORDER BY CreatedAt DESC
            "))
            {
                cmd.Parameters.AddWithValue("@DonorID", donorId);
                cmd.Connection.Open();
                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    cartId = (int)result;
                }
                else
                {
                    using (var cmdInsert = _db.GetQuery(@"
                        INSERT INTO Cart (DonorID, Status, CreatedAt) 
                        OUTPUT INSERTED.CartID
                        VALUES (@DonorID, 'Pending', GETDATE())
                    "))
                    {
                        cmdInsert.Parameters.AddWithValue("@DonorID", donorId);
                        cmdInsert.Connection.Open();
                        cartId = (int)cmdInsert.ExecuteScalar();
                        cmdInsert.Connection.Close();
                    }
                }
                cmd.Connection.Close();
            }

            return cartId;
        }
    }
}