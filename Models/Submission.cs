using MyDergiApp.Entities;
using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.Models
{
    public class Submission
    {
        public int Id { get; set; }

        public string? Prefix { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Subtitle { get; set; }

        [Required]
        public string Abstract { get; set; } = string.Empty;

        public string Keywords { get; set; } = string.Empty;

        public string? ReferencesText { get; set; }
        public string? CoverLetter { get; set; }

        public string? FilePath { get; set; }

        [Required]
        public string AuthorId { get; set; } = string.Empty;
        public AppUser? Author { get; set; }

        public string? AssignedChiefEditorId { get; set; }
        public AppUser? AssignedChiefEditor { get; set; }

        public string? AssignedSectionEditorId { get; set; }
        public AppUser? AssignedSectionEditor { get; set; }

        public SubmissionStatus Status { get; set; } = SubmissionStatus.Gonderildi;

        public string NoteToEditor { get; set; } = string.Empty;
        public string DecisionNote { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DecisionDate { get; set; }
    

        public string? DecisionByUserId { get; set; }
        public AppUser? DecisionByUser { get; set; }

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<SubmissionFile> Files { get; set; } = new List<SubmissionFile>();
        public ICollection<SubmissionAuthor> Authors { get; set; } = new List<SubmissionAuthor>();
        public ICollection<SubmissionReviewer> SubmissionReviewers { get; set; } = new List<SubmissionReviewer>();
        public ICollection<SubmissionRevision> Revisions { get; set; } = new List<SubmissionRevision>();
        public int CurrentReviewRound { get; set; } = 1;
    }
}