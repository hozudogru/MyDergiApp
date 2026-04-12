using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.ViewModels
{
    public class UserEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Display(Name = "Ad Soyad")]
        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "E-posta")]
        public string? Email { get; set; }

        [Display(Name = "Aktif mi?")]
        public bool IsActive { get; set; }
    }
}