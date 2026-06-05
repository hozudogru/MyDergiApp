using MyDergiApp.Models;

namespace MyDergiApp.Models
{
    public class SubmissionAuthor
    {
        public int Id { get; set; }

        public int SubmissionId { get; set; }
        public Submission? Submission { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Institution { get; set; }
        public string? Orcid { get; set; }
        public string Role { get; set; } = "Yazar";
        public bool IsCorrespondingAuthor { get; set; }
        public int SortOrder { get; set; }
    }
}