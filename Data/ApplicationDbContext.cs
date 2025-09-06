using Microsoft.EntityFrameworkCore;

namespace Donation_Website.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<DonationCategory> DonationCategories => Set<DonationCategory>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<Fundraiser> Fundraisers => Set<Fundraiser>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Donation> Donations => Set<Donation>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<WorksOfOrganization> WorksOfOrganizations => Set<WorksOfOrganization>();
        public DbSet<VolunteerAssignment> VolunteerAssignments => Set<VolunteerAssignment>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        // Keyless view
        public DbSet<FundraiserLiveMonitor> FundraiserLiveMonitor => Set<FundraiserLiveMonitor>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Unique Email
            b.Entity<User>().HasIndex(u => u.Email).IsUnique();

            // Category -> Projects
            b.Entity<Project>()
                .HasOne(p => p.DonationCategory)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.DonationCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Project -> Fundraisers
            b.Entity<Fundraiser>()
                .HasOne(f => f.Project)
                .WithMany(p => p.Fundraisers)
                .HasForeignKey(f => f.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cart: one open cart per user
            b.Entity<Cart>()
                .HasIndex(c => new { c.UserId, c.IsCheckedOut })
                .HasFilter("[IsCheckedOut] = 0")
                .IsUnique();

            // CartItem -> Fundraiser
            b.Entity<CartItem>()
                .HasOne(ci => ci.Fundraiser)
                .WithMany(f => f.CartItems)
                .HasForeignKey(ci => ci.FundraiserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Donation -> Payments
            b.Entity<Payment>()
                .HasOne(p => p.Donation)
                .WithMany(d => d.Payments)
                .HasForeignKey(p => p.DonationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Review: one per donor per project
            b.Entity<Review>()
                .HasIndex(r => new { r.UserId, r.ProjectId })
                .IsUnique();

            // WorksOfOrganization (optional link to project)
            b.Entity<WorksOfOrganization>()
                .HasOne(w => w.Project)
                .WithMany(p => p.Works)
                .HasForeignKey(w => w.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            // VolunteerAssignment target: either Project OR Works must be provided
            b.Entity<VolunteerAssignment>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_VolAssign_Target",
                    "([ProjectId] IS NOT NULL) OR ([WorksOfOrganizationId] IS NOT NULL)");
            });

            // AuditLog -> Admin user
            b.Entity<AuditLog>()
                .HasOne(a => a.AdminUser)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Map keyless view
            b.Entity<FundraiserLiveMonitor>().HasNoKey().ToView("FundraiserLiveMonitor");

            // --- Seed the 5 segments with priority ---
            b.Entity<DonationCategory>().HasData(
                new DonationCategory { DonationCategoryId = 1, Name = "Orphanage Support", Priority = 1, IsActive = true },
                new DonationCategory { DonationCategoryId = 2, Name = "Water in Africa", Priority = 2, IsActive = true },
                new DonationCategory { DonationCategoryId = 3, Name = "Gaza Relief", Priority = 3, IsActive = true },
                new DonationCategory { DonationCategoryId = 4, Name = "Old Age Home Care", Priority = 4, IsActive = true },
                new DonationCategory { DonationCategoryId = 5, Name = "Zakat", Priority = 5, IsActive = true }
            );
        }
    }
}
