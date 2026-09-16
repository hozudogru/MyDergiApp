using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Entities;
using MyDergiApp.Models;
using System.Reflection.Emit;

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
    public DbSet<SubmissionRevision> SubmissionRevisions { get; set; }
    public DbSet<HomePageSettings> HomePageSettings { get; set; }
    public DbSet<JournalIndex> JournalIndexes { get; set; }
    public DbSet<Issue> Issues { get; set; }
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<SubmissionFile> SubmissionFiles { get; set; }
    public DbSet<SubmissionAuthor> SubmissionAuthors { get; set; }
    public DbSet<PublishedArticle> PublishedArticles { get; set; }
    public DbSet<IssueArticle> IssueArticles { get; set; }
    public DbSet<SmtpSetting> SmtpSettings { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Submission>()
            .HasOne(s => s.Author)
            .WithMany()
            .HasForeignKey(s => s.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // SubmissionReviewer -> Submission: Submission.SubmissionReviewers koleksiyonuna acikca baglanir.
        // Onceden WithMany() ile iki kez tanimlandigi icin EF koleksiyon icin golge bir "SubmissionId1" FK'si
        // uretmis ve koleksiyon her zaman bos gelmisti (hakem Detail/DownloadFile'da 403 aliyordu).
        builder.Entity<SubmissionReviewer>()
            .HasOne(sr => sr.Submission)
            .WithMany(s => s.SubmissionReviewers)
            .HasForeignKey(sr => sr.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SubmissionReviewer>()
            .HasOne(sr => sr.Reviewer)
            .WithMany()
            .HasForeignKey(sr => sr.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ayni hakem farkli turlarda yeniden atanabilir; benzersizlik tur bazindadir.
        // (Eski (SubmissionId, ReviewerId) benzersiz indeksi 20260915170320_FixReviewerUniqueIndex ile kaldirildi.)
        builder.Entity<SubmissionReviewer>()
            .HasIndex(x => new { x.SubmissionId, x.ReviewerId, x.ReviewRound })
            .IsUnique();

        builder.Entity<SubmissionAuthor>()
            .HasOne(sa => sa.Submission)
            .WithMany(s => s.Authors)
            .HasForeignKey(sa => sa.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Submission>()
    .HasOne(s => s.AssignedChiefEditor)
    .WithMany()
    .HasForeignKey(s => s.AssignedChiefEditorId)
    .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Submission>()
            .HasOne(s => s.AssignedSectionEditor)
            .WithMany()
            .HasForeignKey(s => s.AssignedSectionEditorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SubmissionFile>()
            .HasOne(sf => sf.Submission)
            .WithMany(s => s.Files)
            .HasForeignKey(sf => sf.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SubmissionFile>()
            .HasOne(sf => sf.UploadedByUser)
            .WithMany()
            .HasForeignKey(sf => sf.UploadedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Entity<IssueArticle>()
            .HasOne(x => x.Issue)
            .WithMany(x => x.IssueArticles)
            .HasForeignKey(x => x.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IssueArticle>()
            .HasOne(x => x.Submission)
            .WithMany()
            .HasForeignKey(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}