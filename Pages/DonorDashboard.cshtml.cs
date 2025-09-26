using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
namespace Donation_Website.Pages
{
    public class DonorDashboardModel : PageModel
    {
        public string UserName { get; set; } = "Guest";
        public List<EventModel> Events { get; set; } = new List<EventModel>();
        public void OnGet()
        {
            // Fetch user name from session
            var sessionUser = HttpContext.Session.GetString("UserName");
            if (!string.IsNullOrEmpty(sessionUser))
            {
                UserName = sessionUser;
            }
            // Example events - replace with DB query later
            Events = new List<EventModel>
 {
 new EventModel
 {
 Name = "Education Fundraiser",
 StartTime = DateTime.Now,
 EndTime = DateTime.Now.AddHours(5)
 },
 new EventModel
 {
 Name = "Health Campaign",
 StartTime = DateTime.Now,
 EndTime = DateTime.Now.AddHours(10)
 },
 new EventModel
 {
 Name = "Disaster Relief",
 StartTime = DateTime.Now,
 EndTime = DateTime.Now.AddDays(1)
 }
 };
        }
        public class EventModel
        {
            public string Name { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
        }
    }
}