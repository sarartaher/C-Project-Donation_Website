namespace Donation_Website.Models
{
    public class WorksOfOrganization
    {
        public int WorksOfOrganizationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? ProjectId { get; set; }

        // Navigation
        public Project? Project { get; set; }
    }
}
