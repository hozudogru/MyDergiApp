using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.ViewModels.Submissions
{
    public class SubmissionStatusUpdateViewModel
    {
        public int SubmissionId { get; set; }

        [Required]
        [Display(Name = "Durum")]
        public string Status { get; set; } = "Submitted";

        [Display(Name = "Editör Notu")]
        [StringLength(1000)]
        public string? EditorNote { get; set; }
    }
}