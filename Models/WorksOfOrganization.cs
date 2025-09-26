namespace Donation_Website.Models
{
    public class WorkOfOrganization
    {
        public int WorkID { get; set; }
        public int ProjectID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? Date { get; set; }
        public DateTime? PublishedAt { get; set; }

        // Navigation
        public Project Project { get; set; }
        public ICollection<VolunteerAssignment> Assignments { get; set; }
    }
}
