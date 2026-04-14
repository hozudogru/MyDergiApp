using MyDergiApp.Models.Enums;

namespace MyDergiApp.ViewModels.Submissions
{
    public class SubmissionDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public string? Keywords { get; set; }
        public string? FilePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public SubmissionStatus Status { get; set; }
        public string? EditorNote { get; set; }

        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorEmail { get; set; } = string.Empty;

        public bool CanEdit { get; set; }
        public bool CanManageStatus { get; set; }

        public List<ReviewerAssignmentListItemViewModel> Reviewers { get; set; } = new();
    }
}