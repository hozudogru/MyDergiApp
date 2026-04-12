using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyDergiApp.ViewModels.Submissions
{
    public class AssignReviewerViewModel
    {
        public int SubmissionId { get; set; }

        public string SubmissionTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Reviewer seçiniz.")]
        [Display(Name = "Reviewer")]
        public string ReviewerId { get; set; } = string.Empty;

        public List<SelectListItem> Reviewers { get; set; } = new();
    }
}