using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.ViewModels.Submissions
{
    public class SubmissionCreateEditViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Başlık zorunludur.")]
        [StringLength(300)]
        [Display(Name = "Makale Başlığı")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Özet zorunludur.")]
        [Display(Name = "Özet")]
        public string Abstract { get; set; } = string.Empty;

        [Display(Name = "Anahtar Kelimeler")]
        [StringLength(500)]
        public string? Keywords { get; set; }
    }
}