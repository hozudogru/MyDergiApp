using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyDergiApp.ViewModels.Submissions
{
    public class AssignReviewerViewModel
    {
        public int SubmissionId { get; set; }

        public string SubmissionTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lütfen bir reviewer seçin.")]
        public string ReviewerId { get; set; } = string.Empty;

        public List<SelectListItem> AvailableReviewers { get; set; } = new();
    }
}