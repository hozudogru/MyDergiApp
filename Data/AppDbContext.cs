using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Entities;

namespace MyDergiApp.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Journal> Journals => Set<Journal>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<SubmissionReviewer> SubmissionReviewers => Set<SubmissionReviewer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Submission>()
            .HasOne(s => s.Author)
            .WithMany()
            .HasForeignKey(s => s.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SubmissionReviewer>()
            .HasOne(sr => sr.Submission)
            .WithMany()
            .HasForeignKey(sr => sr.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SubmissionReviewer>()
            .HasOne(sr => sr.Reviewer)
            .WithMany()
            .HasForeignKey(sr => sr.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Submission>()
            .HasOne(s => s.Editor)
            .WithMany()
            .HasForeignKey(s => s.EditorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}