using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillHub.Domain.Entities;
using SkillHub.Infrastructure.Identity;

namespace SkillHub.Infrastructure.Persistence;

public class SkillHubDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public SkillHubDbContext(DbContextOptions<SkillHubDbContext> options) : base(options)
    {
    }

    public DbSet<Instructor> Instructors { get; set; } = null!;
    public DbSet<Workshop> Workshops { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<ApplicationUser>().Property(u => u.FullName).HasMaxLength(150).IsRequired();
        model.Entity<ApplicationUser>().HasIndex(u => u.NormalizedEmail).IsUnique();
        model.Entity<Instructor>(e =>
        {
            e.Property(i => i.FullName).HasMaxLength(150).IsRequired();
            e.Property(i => i.Email).HasMaxLength(254).IsRequired();
            e.Property(i => i.Specialty).HasMaxLength(150).IsRequired();
            e.HasIndex(i => i.Email).IsUnique();
            e.HasOne<ApplicationUser>().WithOne().HasForeignKey<Instructor>(i => i.Id).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Workshop>(e =>
        {
            e.Property(w => w.Code).HasMaxLength(30).IsRequired();
            e.Property(w => w.Title).HasMaxLength(200).IsRequired();
            e.Property(w => w.Description).HasMaxLength(4000).IsRequired();
            e.HasIndex(w => w.Code).IsUnique();
            e.HasIndex(w => w.StartTime);
            e.HasOne(w => w.Instructor).WithMany().HasForeignKey(w => w.InstructorId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Workshop_Capacity", "\"Capacity\" > 0");
                t.HasCheckConstraint("CK_Workshop_Time", "\"EndTime\" > \"StartTime\"");
            });
        });
        model.Entity<Enrollment>(e =>
        {
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Workshop).WithMany().HasForeignKey(x => x.WorkshopId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CancelledById).OnDelete(DeleteBehavior.Restrict);
            // Historical cancellations coexist; only active duplicates are forbidden.
            e.HasIndex(x => new { x.ParticipantId, x.WorkshopId }).IsUnique().HasFilter("\"Status\" = 'Active'");
            e.HasIndex(x => new { x.WorkshopId, x.Status });
            e.ToTable(t => t.HasCheckConstraint("CK_Enrollment_Cancellation",
                "(\"Status\" = 'Active' AND \"CancelledAt\" IS NULL AND \"CancelledById\" IS NULL) OR (\"Status\" = 'Cancelled' AND \"CancelledAt\" IS NOT NULL AND \"CancelledById\" IS NOT NULL)"));
        });
    }
}
