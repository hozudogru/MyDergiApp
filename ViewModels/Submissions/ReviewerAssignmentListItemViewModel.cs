namespace MyDergiApp.ViewModels.Submissions
{
    public class ReviewerAssignmentListItemViewModel
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