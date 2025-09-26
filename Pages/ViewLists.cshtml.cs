using Microsoft.AspNetCore.Mvc.RazorPages;
using Donation_Website.Models;
using Microsoft.Data.SqlClient;
using Donation_Website.Models;
using System.Collections.Generic;

namespace Donation_Website.Pages
{
    public class ViewListsModel : PageModel
    {
        private readonly DBConnection sda = new DBConnection();

        public List<VolunteerViewModel> Volunteers { get; set; } = new();
        public List<OrganizationViewModel> Organizations { get; set; } = new();
        public List<EventViewModel> Events { get; set; } = new();

        public void OnGet()
        {
            LoadVolunteers();
            LoadOrganizations();
            LoadEvents();
        }

        private void LoadVolunteers()
        {
            using var cmd = sda.GetQuery("SELECT VolunteerID, Name, Email, Phone, Address, Skill, Availability, IsActive FROM Volunteer");
            cmd.Connection.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Volunteers.Add(new VolunteerViewModel
                {
                    VolunteerID = reader.GetInt32(reader.GetOrdinal("VolunteerID")),
                    Name = reader["Name"].ToString() ?? "",
                    Email = reader["Email"].ToString() ?? "",
                    Phone = reader["Phone"].ToString() ?? "",
                    Address = reader["Address"].ToString() ?? "",
                    Skill = reader["Skill"].ToString() ?? "",
                    Availability = reader["Availability"].ToString() ?? "",
                    Status = (bool)reader["IsActive"] ? "Verified" : "Pending",
                    StatusClass = (bool)reader["IsActive"] ? "text-success" : "text-danger"
                });
            }
        }

        private void LoadOrganizations()
        {
            using var cmd = sda.GetQuery("SELECT AdminID AS OrganizationID, Name, Email, Phone, Address, IsActive AS Status FROM Admin");
            cmd.Connection.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Organizations.Add(new OrganizationViewModel
                {
                    OrganizationID = reader.GetInt32(reader.GetOrdinal("OrganizationID")),
                    Name = reader["Name"].ToString() ?? "",
                    Email = reader["Email"].ToString() ?? "",
                    Phone = reader["Phone"].ToString() ?? "",
                    Address = reader["Address"].ToString() ?? "",
                    Status = (bool)reader["Status"] ? "Active" : "Inactive"
                });
            }
        }

        private void LoadEvents()
        {
            using var cmd = sda.GetQuery("SELECT ProjectID AS EventID, Title AS Name, Description AS Location, StartDate AS EventDate, EndDate FROM Project");
            cmd.Connection.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Events.Add(new EventViewModel
                {
                    EventID = reader.GetInt32(reader.GetOrdinal("EventID")),
                    Name = reader["Name"].ToString() ?? "",
                    Location = reader["Location"].ToString() ?? "",
                    EventDate = Convert.ToDateTime(reader["EventDate"]),
                    Status = "Active" 
                });
            }
        }
    }
}
