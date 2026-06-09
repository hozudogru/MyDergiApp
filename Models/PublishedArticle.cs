using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDergiApp.Models
{
    public class PublishedArticle
    {
        public int Id { get; set; }

        public int IssueId { get; set; }

        public Issue? Issue { get; set; }

        public int SubmissionId { get; set; }

        public Submission? Submission { get; set; }

        [StringLength(300)]
        public string? TitleOverride { get; set; }

        [StringLength(1000)]
        public string? AuthorsText { get; set; }

        [StringLength(50)]
        public string? Pages { get; set; }

        public int SortOrder { get; set; } = 0;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        [StringLength(250)]
        public string? Doi { get; set; }

        public string? AbstractOverride { get; set; }

        [StringLength(500)]
        public string? Keywords { get; set; }

        public string? PdfFilePath { get; set; }

        public string? OriginalFilePath { get; set; }

        [NotMapped]
        public string DisplayTitle => !string.IsNullOrWhiteSpace(TitleOverride)
            ? TitleOverride
            : Submission?.Title ?? "-";
    }
}
