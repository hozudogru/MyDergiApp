using MyDergiApp.Entities;
using MyDergiApp.Models;
using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.ViewModels.Submissions
{
    public class EditorDecisionViewModel
    {
        public int SubmissionId { get; set; }

        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lütfen karar seçin.")]
        public SubmissionStatus Status { get; set; }

        [Display(Name = "Editör Karar Notu")]
        public string DecisionNote { get; set; } = string.Empty;

        public string SuggestedDecision { get; set; } = string.Empty;

        public int TotalReviewerCount { get; set; }
        public int CompletedReviewerCount { get; set; }
       

        public List<ReviewerDecisionItemViewModel> Reviews { get; set; } = new();
        public bool HasConflict { get; set; }
        public string ConflictMessage { get; set; } = string.Empty;
    }

    public class ReviewerDecisionItemViewModel
    {
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewerEmail { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }
        public bool IsCompleted { get; set; }
    }
}