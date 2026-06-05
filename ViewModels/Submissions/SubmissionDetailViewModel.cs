using MyDergiApp.Models;

namespace MyDergiApp.ViewModels.Submissions
{
    public class SubmissionDetailViewModel
    {
        public int Id { get; set; }

        public string? Prefix { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }

        public string Abstract { get; set; } = string.Empty;
        public string Keywords { get; set; } = string.Empty;
        public string? ReferencesText { get; set; }
        public string? CoverLetter { get; set; }

        public string AuthorId { get; set; } = string.Empty;
        public string? AuthorName { get; set; }
        public string? AuthorEmail { get; set; }

        public string? FilePath { get; set; }

        public SubmissionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string? NoteToEditor { get; set; }
        public string? DecisionNote { get; set; }

        public bool CanEdit { get; set; }
        public bool CanManageStatus { get; set; }

        public List<Review> Reviews { get; set; } = new();
        public List<ReviewerAssignmentListItemViewModel> Reviewers { get; set; } = new();

        public List<SubmissionAuthor> Authors { get; set; } = new();
        public List<SubmissionFile> Files { get; set; } = new();
    }
    public class SubmissionReviewerViewModel
    {
        public int AssignmentId { get; set; }

        public string? ReviewerId { get; set; }

        public string? ReviewerName { get; set; }

        public string? ReviewerEmail { get; set; }

        public string? Status { get; set; }

        public DateTime AssignedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public string? ReviewNote { get; set; }
    }
}