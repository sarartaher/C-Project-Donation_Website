namespace Donation_Website.Models
{
    public class VolunteerAssignment
    {
        public int VolunteerAssignmentId { get; set; }
        public int VolunteerId { get; set; }
        public int? ProjectId { get; set; }
        public int? WorksOfOrganizationId { get; set; }
        public int Hours { get; set; }

        // Navigation
        public Volunteer? Volunteer { get; set; }
        public Project? Project { get; set; }
        public WorksOfOrganization? WorksOfOrganization { get; set; }
    }
}
