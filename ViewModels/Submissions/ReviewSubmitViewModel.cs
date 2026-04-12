using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.ViewModels.Submissions
{
    public class ReviewSubmitViewModel
    {
        public int AssignmentId { get; set; }
        public int SubmissionId { get; set; }
        public string SubmissionTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Değerlendirme notu zorunludur.")]
        [StringLength(2000)]
        [Display(Name = "Reviewer Notu")]
        public string ReviewNote { get; set; } = string.Empty;
    }
}