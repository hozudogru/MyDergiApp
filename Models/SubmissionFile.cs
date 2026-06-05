using MyDergiApp.Entities;

namespace MyDergiApp.Models
{
    public class SubmissionFile
    {
        public int Id { get; set; }

        public int SubmissionId { get; set; }
        public Submission? Submission { get; set; }

        // AnaDosya, EkDosya, RevizyonDosyasi, HakemDosyasi, YayinlanmisPDF
        public string FileType { get; set; } = string.Empty;

        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFilePath { get; set; } = string.Empty;
        public int ReviewRound { get; set; } = 1;

        public long? FileSize { get; set; }

        public string? UploadedByUserId { get; set; }
        public AppUser? UploadedByUser { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}