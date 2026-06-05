using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.ViewModels
{
    public class UploadRevisionViewModel
    {
        public int SubmissionId { get; set; }

        public string SubmissionTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lütfen revizyon dosyasını seçiniz.")]
        public IFormFile? RevisionFile { get; set; }

        public string Note { get; set; } = string.Empty;
    }
}