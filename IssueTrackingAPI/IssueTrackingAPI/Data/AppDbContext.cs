using IssueTrackingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace IssueTrackingAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<IssueLabel> IssueLabels => Set<IssueLabel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- User ----------
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
        });

        // ---------- Project ----------
        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();

            entity.HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- ProjectMember (n-n User <-> Project) ----------
        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.HasIndex(pm => new { pm.ProjectId, pm.UserId }).IsUnique();

            entity.HasOne(pm => pm.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMemberships)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Issue ----------
        modelBuilder.Entity<Issue>(entity =>
        {
            entity.Property(i => i.Title).HasMaxLength(200).IsRequired();

            entity.HasOne(i => i.Project)
                .WithMany(p => p.Issues)
                .HasForeignKey(i => i.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Reporter)
                .WithMany(u => u.ReportedIssues)
                .HasForeignKey(i => i.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.Assignee)
                .WithMany(u => u.AssignedIssues)
                .HasForeignKey(i => i.AssigneeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Comment ----------
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.Property(c => c.Content).HasMaxLength(4000).IsRequired();

            entity.HasOne(c => c.Issue)
                .WithMany(i => i.Comments)
                .HasForeignKey(c => c.IssueId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Label ----------
        modelBuilder.Entity<Label>(entity =>
        {
            entity.Property(l => l.Name).HasMaxLength(50).IsRequired();
            entity.Property(l => l.Color).HasMaxLength(7).IsRequired();
            entity.HasIndex(l => new { l.ProjectId, l.Name }).IsUnique();

            entity.HasOne(l => l.Project)
                .WithMany(p => p.Labels)
                .HasForeignKey(l => l.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- IssueLabel (n-n Issue <-> Label) ----------
        modelBuilder.Entity<IssueLabel>(entity =>
        {
            entity.HasKey(il => new { il.IssueId, il.LabelId });

            entity.HasOne(il => il.Issue)
                .WithMany(i => i.IssueLabels)
                .HasForeignKey(il => il.IssueId)
                .OnDelete(DeleteBehavior.Cascade);

            // Dùng Restrict (không Cascade) để tránh lỗi "multiple cascade paths" của SQL Server,
            // vì IssueLabels đã nhận cascade từ Project -> Issues -> IssueLabels.
            // Khi xóa 1 Label, LabelService sẽ tự xóa các IssueLabel liên quan trước.
            entity.HasOne(il => il.Label)
                .WithMany(l => l.IssueLabels)
                .HasForeignKey(il => il.LabelId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
