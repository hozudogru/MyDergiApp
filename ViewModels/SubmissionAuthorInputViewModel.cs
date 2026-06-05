using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.ViewModels
{
    public class SubmissionAuthorInputViewModel
    {
        [Required(ErrorMessage = "Yazar adı zorunludur.")]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Kurum")]
        public string? Institution { get; set; }

        [Display(Name = "ORCID")]
        public string? Orcid { get; set; }

        [Display(Name = "Rol")]
        public string Role { get; set; } = "Yazar";

        [Display(Name = "Sorumlu Yazar")]
        public bool IsCorrespondingAuthor { get; set; }
    }
}