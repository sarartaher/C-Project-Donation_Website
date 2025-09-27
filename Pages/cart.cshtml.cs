using Donation_Website.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Donation_Website.Pages
{
    public class cartModel : PageModel
    {
        private readonly DBConnection _db = new DBConnection();
        private readonly Users _users = new Users();

        public List<DonationViewModel> Donations { get; set; } = new List<DonationViewModel>();
        public decimal SubTotal { get; set; }
        public decimal TotalAmount { get; set; }

        public void LoadCart()
        {
            Donations.Clear();
            int donorId = GetDonorId();

            string query = @"
                SELECT ci.CartItemsId, ci.CartId, ci.Amount,
                       f.FundraiserID, f.Title AS EventName, ci.CreatedAt
                FROM CartItems ci
                INNER JOIN Cart c ON ci.CartId = c.CartID
                INNER JOIN Fundraiser f ON ci.FundraiserId = f.FundraiserID
                WHERE c.DonorID = @DonorID AND (c.Status IS NULL OR c.Status = 'Pending')
                ORDER BY ci.CreatedAt DESC
            ";

            using (SqlCommand cmd = _db.GetQuery(query))
            {
                cmd.Parameters.AddWithValue("@DonorID", donorId);
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Donations.Add(new DonationViewModel
                        {
                            CartItemsId = reader.GetInt32(reader.GetOrdinal("CartItemsId")),
                            CartId = reader.GetInt32(reader.GetOrdinal("CartId")),
                            FundraiserId = reader.GetInt32(reader.GetOrdinal("FundraiserID")),
                            EventName = reader.GetString(reader.GetOrdinal("EventName")),
                            Amount = reader.IsDBNull(reader.GetOrdinal("Amount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Amount")),
                            SecretName = "Anonymous",
                            Date = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                        });
                    }
                }
                cmd.Connection.Close();
            }

            SubTotal = Donations.Sum(d => d.Amount ?? 0);
            TotalAmount = SubTotal;
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

        public IActionResult OnPostRemove(int cartItemsId)
        {
            int donorId = GetDonorId();
            using (var cmd = _db.GetQuery(@"
                DELETE FROM CartItems 
                WHERE CartItemsId = @CartItemsId 
                  AND CartId IN (SELECT CartID FROM Cart WHERE DonorID=@DonorID)
            "))
            {
                cmd.Parameters.AddWithValue("@CartItemsId", cartItemsId);
                cmd.Parameters.AddWithValue("@DonorID", donorId);
                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
                cmd.Connection.Close();
            }

            TempData["Success"] = "Donation removed from cart!";
            return RedirectToPage();
        }

        public void OnGet()
        {
            LoadCart();
        }

        // ====================== DOWNLOAD RECEIPT ======================
        public IActionResult OnGetDownloadReceipt(int cartId)
        {
            LoadCart(); // Reload the cart to get latest donations

            var items = Donations.Where(d => d.CartId == cartId).ToList();
            if (!items.Any())
                items = Donations; // fallback if no items found

            using (MemoryStream ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 36, 36, 36, 36);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                Paragraph title = new Paragraph("Donation Receipt", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20f
                };
                doc.Add(title);

                // Date
                var dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                Paragraph date = new Paragraph($"Date: {DateTime.Now:dd MMM yyyy}", dateFont)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingAfter = 10f
                };
                doc.Add(date);

                // Donor Name
                string donorName = Donations.FirstOrDefault()?.SecretName ?? "Guest";
                Paragraph donor = new Paragraph($"Donor Name: {donorName}", dateFont)
                {
                    Alignment = Element.ALIGN_LEFT,
                    SpacingAfter = 15f
                };
                doc.Add(donor);

                // Table
                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 10f, 50f, 20f, 20f });

                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                var textFont = FontFactory.GetFont(FontFactory.HELVETICA, 11);

                // Header
                table.AddCell(new PdfPCell(new Phrase("S/N", headerFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("Fundraiser", headerFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("Amount (৳)", headerFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("Date", headerFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });

                // Rows
                int sn = 1;
                foreach (var d in items)
                {
                    table.AddCell(new PdfPCell(new Phrase(sn.ToString(), textFont)));
                    table.AddCell(new PdfPCell(new Phrase(d.EventName, textFont)));
                    table.AddCell(new PdfPCell(new Phrase((d.Amount ?? 0m).ToString("N2"), textFont)));
                    table.AddCell(new PdfPCell(new Phrase(d.Date.ToString("dd MMM yyyy"), textFont)));
                    sn++;
                }

                doc.Add(table);

                // Total
                decimal totalAmount = items.Sum(d => d.Amount ?? 0m);
                Paragraph total = new Paragraph($"Total Amount: ৳{totalAmount:N2}", headerFont)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingBefore = 15f
                };
                doc.Add(total);

                doc.Close();

                byte[] fileBytes = ms.ToArray();
                return File(fileBytes, "application/pdf", $"DonationReceipt_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
        }


    }
}
