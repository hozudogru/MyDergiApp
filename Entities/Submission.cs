using MyDergiApp.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDergiApp.Entities
{
    public class Submission
    {
        public string? EditorId { get; set; }

        [ForeignKey(nameof(EditorId))]
        public AppUser? Editor { get; set; }
        public int Id { get; set; }

        [Required]
        [StringLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Abstract { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Keywords { get; set; }

        [StringLength(500)]
        public string? FilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        [StringLength(50)]
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;
        [StringLength(1000)]
        public string? EditorNote { get; set; }

        [Required]
        public string AuthorId { get; set; } = string.Empty;

        [ForeignKey(nameof(AuthorId))]
        public AppUser Author { get; set; } = null!;
        public string? FinalDecision { get; set; }
    }
}