using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Entities;
using MyDergiApp.Models;

namespace MyDergiApp.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Journal> Journals => Set<Journal>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<SubmissionReviewer> SubmissionReviewers => Set<SubmissionReviewer>();
    public DbSet<Review> Reviews { get; set; }
    public DbSet<HomePageSettings> HomePageSettings { get; set; }
    public DbSet<JournalIndex> JournalIndexes { get; set; }
    public DbSet<Issue> Issues { get; set; }
    public DbSet<Announcement> Announcements { get; set; }

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
            .HasIndex(x => new { x.SubmissionId, x.ReviewerId })
            .IsUnique();

        builder.Entity<SubmissionReviewer>()
            .HasOne(sr => sr.Reviewer)
            .WithMany()
            .HasForeignKey(sr => sr.ReviewerId)
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

        builder.Entity<SubmissionReviewer>()
            .HasIndex(sr => new { sr.SubmissionId, sr.ReviewerId })
            .IsUnique();

    }
}