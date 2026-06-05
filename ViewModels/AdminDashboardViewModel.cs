namespace MyDergiApp.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalSubmissions { get; set; }
        public int PendingSubmissions { get; set; }
        public int InReviewSubmissions { get; set; }
        public int AcceptedSubmissions { get; set; }
        public int RejectedSubmissions { get; set; }

        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int PassiveUsers { get; set; }

        public int TotalAnnouncements { get; set; }
        public int ActiveAnnouncements { get; set; }

        public int TotalIssues { get; set; }
        public int PublishedIssues { get; set; }

        public int TotalPublishedArticles { get; set; }
        public int TotalIndexes { get; set; }
        public bool HasHomePageSettings { get; set; }
        public bool HasActiveAnnouncement { get; set; }
        public bool HasPublishedIssue { get; set; }
        public bool HasPublishedArticle { get; set; }
        public bool HasActiveIndexes { get; set; }
        public bool HasPassiveUsers { get; set; }
        public bool HasSmtpSettings { get; set; }
    }
}