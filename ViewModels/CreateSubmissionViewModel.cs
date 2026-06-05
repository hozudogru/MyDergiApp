using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MyDergiApp.ViewModels
{
    public class CreateSubmissionViewModel
    {
        [Display(Name = "Ön Başlık")]
        public string? Prefix { get; set; }

        [Required(ErrorMessage = "Başlık zorunludur.")]
        [Display(Name = "Başlık")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Alt Başlık")]
        public string? Subtitle { get; set; }

        [Required(ErrorMessage = "Özet zorunludur.")]
        [Display(Name = "Özet")]
        public string Abstract { get; set; } = string.Empty;

        [Display(Name = "Anahtar Kelimeler")]
        public string? Keywords { get; set; }

        [Display(Name = "Kaynakça")]
        public string? ReferencesText { get; set; }

        [Display(Name = "Editöre Not")]
        public string? CoverLetter { get; set; }

        [Required(ErrorMessage = "Makale dosyası zorunludur.")]
        [Display(Name = "Makale Dosyası")]
        public IFormFile? MainManuscriptFile { get; set; }

        [Display(Name = "Kapak Yazısı")]
        public IFormFile? CoverLetterFile { get; set; }

        [Display(Name = "Etik Kurul Belgesi")]
        public IFormFile? EthicsApprovalFile { get; set; }

        [Display(Name = "Telif Devir Formu")]
        public IFormFile? CopyrightTransferFile { get; set; }

        [Display(Name = "Benzerlik Raporu")]
        public IFormFile? SimilarityReportFile { get; set; }

        [Display(Name = "Ek Dosyalar")]
        public List<IFormFile>? SupplementaryFiles { get; set; }

        public List<SubmissionAuthorInputViewModel> Authors { get; set; } = new();

        [Display(Name = "Dosyamı kör hakemliğe uygun şekilde hazırladım ve telif/etik koşullarını kabul ediyorum")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Devam etmek için onay vermelisiniz.")]
        public bool AuthorChecklistAccepted { get; set; }
    }
}