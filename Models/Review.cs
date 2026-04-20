using System;
using MyDergiApp.Entities;

namespace MyDergiApp.Models
{
    public class Review
    {
        public int Id { get; set; }

        public int SubmissionId { get; set; }
        public Submission? Submission { get; set; }

        public string ReviewerId { get; set; } = string.Empty;
        public AppUser? Reviewer { get; set; }

        public string? Comments { get; set; }
        public string? CommentToAuthor { get; set; }
        public string? CommentToEditor { get; set; }

        public string? Strengths { get; set; }
        public string? Weaknesses { get; set; }

        public int? OriginalityScore { get; set; }
        public int? MethodologyScore { get; set; }
        public int? LiteratureScore { get; set; }
        public int? WritingQualityScore { get; set; }
        public int? OverallScore { get; set; }

        public string? ScopeFit { get; set; }

        public bool HasEthicalIssue { get; set; } = false;
        public string? EthicalConcerns { get; set; }

        public string? Decision { get; set; }

        public bool IsDraft { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }
}