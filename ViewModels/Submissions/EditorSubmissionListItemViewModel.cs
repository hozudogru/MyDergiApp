using MyDergiApp.Models;

namespace MyDergiApp.ViewModels.Submissions
{
    public class EditorSubmissionListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int CurrentReviewRound { get; set; }

        public string? CorrespondingAuthorName { get; set; }
        public string? CorrespondingAuthorEmail { get; set; }
        

        public int AuthorCount { get; set; }
        public int FileCount { get; set; }
        public int AssignedReviewerCount { get; set; }
        public int CompletedReviewerCount { get; set; }
        public SubmissionStatus RawStatus { get; set; }
    }
}