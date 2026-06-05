using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Duyuru başlığı zorunludur.")]
        [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir.")]
        [Display(Name = "Başlık")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Duyuru içeriği zorunludur.")]
        [Display(Name = "İçerik")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Popup Olarak Göster")]
        public bool ShowAsPopup { get; set; } = false;
    }
}
