using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.Models
{
    public class JournalIndex
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "İndeks adı zorunludur.")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Url { get; set; }

        [StringLength(500)]
        public string? LogoPath { get; set; }

        public int SortOrder { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
