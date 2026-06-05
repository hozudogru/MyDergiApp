using System;
using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.Models
{
    public class SubmissionRevision
    {
        public int Id { get; set; }

        public int SubmissionId { get; set; }

        public Submission Submission { get; set; }

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public string OriginalFileName { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.Now;
        public int ReviewRound { get; set; } = 1;
    }
}