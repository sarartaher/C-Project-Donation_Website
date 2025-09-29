namespace Donation_Website.Models
{

    public class AssignmentRow
    {
        public string VolunteerName { get; set; } = "";
        public string ProjectTitle { get; set; } = "";
        public string WorkTitle { get; set; } = "";
        public string? RoleTask { get; set; }
        public string Status { get; set; } = "Assigned";
        public DateTime AssignDate { get; set; }
        public int? Hours { get; set; }
    }
}
