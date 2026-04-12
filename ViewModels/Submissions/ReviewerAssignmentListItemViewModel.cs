namespace MyDergiApp.ViewModels.Submissions
{
    public class ReviewerAssignmentListItemViewModel
    {
        public int AssignmentId { get; set; }
        public int SubmissionId { get; set; }
        public string SubmissionTitle { get; set; } = "";
        public string AuthorName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ReviewNote { get; set; } = "";
        public string ReviewerName { get; set; } = "";
        public string ReviewerEmail { get; set; } = "";
    }
}