using MyDergiApp.Entities;
using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.Models
{
    public class SubmissionReviewer
    {
        public int Id { get; set; }

        public int SubmissionId { get; set; }
        public Submission? Submission { get; set; }

        [Required]
        public string ReviewerId { get; set; } = string.Empty;
        public AppUser? Reviewer { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

        public DateTime? CompletedAt { get; set; }

        public string Recommendation { get; set; } = string.Empty;

        public string ReviewText { get; set; } = string.Empty;
        public string ReviewNote { get; set; } = string.Empty;
    }
}