using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.Models
{
    public class Issue
    {
        public int Id { get; set; }

        [Required]
        public int Volume { get; set; }

        [Required]
        public int Number { get; set; }

        [Required]
        public int Year { get; set; }

        [StringLength(250)]
        public string? Title { get; set; }

        [StringLength(500)]
        public string? CoverImagePath { get; set; }

        public DateTime? PublishedDate { get; set; }

        public string? Description { get; set; }

        public bool IsCurrent { get; set; } = false;
        public bool IsPublished { get; set; } = true;
    }
}