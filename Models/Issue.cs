using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.Models
{
    public class Issue
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Cilt bilgisi zorunludur.")]
        [StringLength(50)]
        public string Volume { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sayı bilgisi zorunludur.")]
        [StringLength(50)]
        public string Number { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Title { get; set; }

        public int Year { get; set; } = DateTime.UtcNow.Year;

        public bool IsPublished { get; set; } = false;

        public DateTime? PublishedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PublishedArticle> Articles { get; set; } = new List<PublishedArticle>();
    }
}
