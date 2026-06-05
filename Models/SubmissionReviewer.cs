using MyDergiApp.Entities;

namespace MyDergiApp.Models
{
    public class SubmissionReviewer
    {
        public int Id { get; set; }

        public int SubmissionId { get; set; }
        public Submission? Submission { get; set; }

        public string ReviewerId { get; set; } = string.Empty;
        public AppUser? Reviewer { get; set; }

        public ReviewerAssignmentStatus Status { get; set; } = ReviewerAssignmentStatus.Assigned;
        public int ReviewRound { get; set; } = 1;

        public string? ReviewNote { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? DueDate { get; set; }

        public DateTime? ReminderSentAt { get; set; }

        public int ReminderCount { get; set; } = 0;

        public string? CancelReason { get; set; }

        public DateTime? CancelledAt { get; set; }

        public string? CancelledByUserId { get; set; }
    }
}