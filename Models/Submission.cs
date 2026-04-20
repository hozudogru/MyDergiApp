using MyDergiApp.Entities;
using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.Models
{
    public class Submission
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Abstract { get; set; } = string.Empty;

        public string Keywords { get; set; } = string.Empty;

        [Required]
        public string AuthorId { get; set; } = string.Empty;

        public AppUser? Author { get; set; }

        public string FilePath { get; set; } = string.Empty;

        public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public string NoteToEditor { get; set; } = string.Empty;

        public string DecisionNote { get; set; } = string.Empty;
        public DateTime? DecisionDate { get; set; }
        public string? DecisionByUserId { get; set; }
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}