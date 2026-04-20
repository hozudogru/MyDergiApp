using System.ComponentModel.DataAnnotations;
using MyDergiApp.Models;

namespace MyDergiApp.ViewModels.Submissions
{
    public class SubmissionStatusUpdateViewModel
    {
        public int SubmissionId { get; set; }

        [Required]
        public SubmissionStatus Status { get; set; }

        public string? NoteToEditor { get; set; }

        public string? DecisionNote { get; set; }
    }
}