using System;
using System.Collections.Generic;

namespace MyDergiApp.ViewModels
{
    public class HomePageViewModel
    {
        public string JournalTitle { get; set; } = string.Empty;
        public string JournalSubtitle { get; set; } = string.Empty;
        public string HeroTitle { get; set; } = string.Empty;
        public string HeroDescription { get; set; } = string.Empty;
        public string AboutTitle { get; set; } = string.Empty;
        public string AboutContent { get; set; } = string.Empty;

        public string? PrintIssn { get; set; }
        public string? OnlineIssn { get; set; }

        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string? FooterText { get; set; }
        public string? LogoPath { get; set; }

        public CurrentIssueViewModel? CurrentIssue { get; set; }

        public List<ArticleCardViewModel> LatestArticles { get; set; } = new();
        public List<AnnouncementItemViewModel> Announcements { get; set; } = new();
        public List<JournalIndexItemViewModel> Indexes { get; set; } = new();

        public int TotalArticles { get; set; }
        public int TotalIssues { get; set; }
        public int TotalReviewers { get; set; }
        public int TotalAuthors { get; set; }
    }

    public class CurrentIssueViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Volume { get; set; }
        public int Number { get; set; }
        public int Year { get; set; }
        public string? CoverImagePath { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string? Description { get; set; }
    }

    public class ArticleCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Authors { get; set; } = string.Empty;
        public string? Abstract { get; set; }
        public string? PdfPath { get; set; }
        public string? Pages { get; set; }
    }

    public class AnnouncementItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool ShowAsPopup { get; set; }
    }

    public class JournalIndexItemViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? LogoPath { get; set; }
        public string? Url { get; set; }
    }
}