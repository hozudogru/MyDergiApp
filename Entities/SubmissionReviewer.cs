using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyDergiApp.Models.Enums;

namespace MyDergiApp.Entities
{
    public class SubmissionReviewer
    {
        public int Id { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [ForeignKey(nameof(SubmissionId))]
        public Submission Submission { get; set; } = null!;

        [Required]
        public string ReviewerId { get; set; } = string.Empty;

        [ForeignKey(nameof(ReviewerId))]
        public AppUser Reviewer { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public ReviewStatus Status { get; set; } = ReviewStatus.Assigned;

        [StringLength(2000)]
        public string? ReviewNote { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        [StringLength(50)]
        public string? Recommendation { get; set; }
        // Accept / MinorRevision / MajorRevision / Reject
    }
}