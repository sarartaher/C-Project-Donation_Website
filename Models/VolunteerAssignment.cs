namespace Donation_Website.Models
{
    public class VolunteerAssignment
    {
        public int AssignID { get; set; }
        public string RoleTask { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime AssignDate { get; set; }
        public int Hours { get; set; }
        public string ProjectTitle { get; set; } = "";
        public string WorkTitle { get; set; } = "";

        // Navigation
        public Volunteer Volunteer { get; set; }
        public Project Project { get; set; }
        public WorkOfOrganization Work { get; set; }
    }
}