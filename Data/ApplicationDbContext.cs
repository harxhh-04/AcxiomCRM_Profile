using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Lead> Leads { get; set; } = null!;
        public DbSet<Opportunity> Opportunities { get; set; } = null!;
        public DbSet<FollowUp> FollowUps { get; set; } = null!;
        public DbSet<Activity> Activities { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Customer indexes and constraints
            builder.Entity<Customer>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Phone).IsUnique();
                entity.HasIndex(e => e.CustomerCode).IsUnique();

                entity.HasOne(e => e.AssignedTo)
                    .WithMany(u => u.AssignedCustomers)
                    .HasForeignKey(e => e.AssignedToId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Lead indexes and relationships
            builder.Entity<Lead>(entity =>
            {
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.Phone);
                entity.HasIndex(e => e.LeadCode).IsUnique();

                entity.HasOne(e => e.AssignedTo)
                    .WithMany(u => u.AssignedLeads)
                    .HasForeignKey(e => e.AssignedToId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.ConvertedCustomer)
                    .WithMany()
                    .HasForeignKey(e => e.ConvertedCustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.ConvertedOpportunity)
                    .WithMany()
                    .HasForeignKey(e => e.ConvertedOpportunityId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Opportunity relationships
            builder.Entity<Opportunity>(entity =>
            {
                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Opportunities)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Lead)
                    .WithMany()
                    .HasForeignKey(e => e.LeadId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.AssignedTo)
                    .WithMany(u => u.AssignedOpportunities)
                    .HasForeignKey(e => e.AssignedToId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // FollowUp relationships
            builder.Entity<FollowUp>(entity =>
            {
                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.FollowUps)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Lead)
                    .WithMany(l => l.FollowUps)
                    .HasForeignKey(e => e.LeadId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Opportunity)
                    .WithMany(o => o.FollowUps)
                    .HasForeignKey(e => e.OpportunityId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.AssignedTo)
                    .WithMany(u => u.AssignedFollowUps)
                    .HasForeignKey(e => e.AssignedToId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Activity relationships
            builder.Entity<Activity>(entity =>
            {
                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Activities)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Lead)
                    .WithMany(l => l.Activities)
                    .HasForeignKey(e => e.LeadId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Opportunity)
                    .WithMany(o => o.Activities)
                    .HasForeignKey(e => e.OpportunityId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.AssignedTo)
                    .WithMany(u => u.AssignedActivities)
                    .HasForeignKey(e => e.AssignedToId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // AuditLog index for quick queries
            builder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(e => e.CreatedDate);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.EntityName);
            });
        }
    }
}
