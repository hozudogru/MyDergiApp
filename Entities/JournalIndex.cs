using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.Models
{
    public class JournalIndex
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? LogoPath { get; set; }

        [StringLength(1000)]
        public string? Url { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}