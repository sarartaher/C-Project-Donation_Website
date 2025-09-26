namespace Donation_Website.Models
{
        
            public class EventViewModel
    {
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public int EventID { get; set; }
            public string Name { get; set; } = "";
            public string Location { get; set; } = "";
            public DateTime EventDate { get; set; }
            public string Status { get; set; } = "";
    }
}
