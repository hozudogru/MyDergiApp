using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.Models
{
    public class IssueArticle
    {
        public int Id { get; set; }

        public int IssueId { get; set; }
        public Issue Issue { get; set; } = null!;

        public int SubmissionId { get; set; }
        public Submission Submission { get; set; } = null!;

        public int DisplayOrder { get; set; }

        [StringLength(100)]
        public string? SectionTitle { get; set; } = "Araştırma Makalesi";

        [StringLength(50)]
        public string? PageRange { get; set; }

        [StringLength(250)]
        public string? Doi { get; set; }

        public string? PublishedTitle { get; set; }

        public string? PublishedAuthors { get; set; }

        public string? PublishedAbstract { get; set; }

        public string? PdfFilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}