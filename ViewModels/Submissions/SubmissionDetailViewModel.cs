using MyDergiApp.Models;



namespace MyDergiApp.ViewModels.Submissions
{
    public class SubmissionDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public string Keywords { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string? AuthorName { get; set; }
        public string? AuthorEmail { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? NoteToEditor { get; set; }
        public string? DecisionNote { get; set; }
        public bool CanEdit { get; set; }
        public bool CanManageStatus { get; set; }
        public List<ReviewerAssignmentListItemViewModel> Reviewers { get; set; } = new();
       
    }
}