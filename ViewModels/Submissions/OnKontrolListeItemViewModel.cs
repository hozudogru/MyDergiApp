namespace MyDergiApp.ViewModels.Submissions
{
    public class OnKontrolListeItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string? CorrespondingAuthorName { get; set; }
        public string? CorrespondingAuthorEmail { get; set; }

        public int AuthorCount { get; set; }
        public int FileCount { get; set; }
    }
}