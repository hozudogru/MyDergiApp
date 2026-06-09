using MyDergiApp.Models;

namespace MyDergiApp.ViewModels
{
    public class HomePageViewModel
    {
        public string? HeaderLogoPath { get; set; }

        public string? HeaderTitle { get; set; }

        public string? HeaderSubtitle { get; set; }

        public string? HeaderRightText { get; set; }

        public string? HeaderBackgroundImagePath { get; set; }

        public bool ShowHeaderLogo { get; set; } = true;
        public string? BannerTitle { get; set; }
        public string? BannerDescription { get; set; }
        public string? BannerLabel { get; set; }

        public string? BannerPrimaryButtonText { get; set; }
        public string? BannerPrimaryButtonUrl { get; set; }

        public string? BannerSecondaryButtonText { get; set; }
        public string? BannerSecondaryButtonUrl { get; set; }

        public string? BannerImagePath { get; set; }

        public bool ShowBanner { get; set; } = true;
        public string? JournalTitle { get; set; }
        public string? JournalSubtitle { get; set; }
        public string? HeroTitle { get; set; }
        public string? HeroDescription { get; set; }
        public string? AboutTitle { get; set; }
        public string? AboutContent { get; set; }
        public string? PrintIssn { get; set; }
        public string? OnlineIssn { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string? LogoPath { get; set; }
        public string? FooterText { get; set; }

        public int TotalArticles { get; set; }
        public int TotalIssues { get; set; }
        public int TotalReviewers { get; set; }
        public int TotalAuthors { get; set; }
        public string ThemeName { get; set; } = "classic";
        public string PrimaryColor { get; set; } = "#0d6efd";
        public string SecondaryColor { get; set; } = "#198754";
        public string HeaderBgColor { get; set; } = "#ffffff";
        public string NavBgColor { get; set; } = "#ffffff";
        public string BodyBgColor { get; set; } = "#f8fafc";
        public string TextColor { get; set; } = "#111827";
        public CurrentIssueViewModel? CurrentIssue { get; set; }
        public List<LatestArticleViewModel> LatestArticles { get; set; } = new();
        public List<Announcement> Announcements { get; set; } = new();
        public List<JournalIndex> Indexes { get; set; } = new();
        public string? PdfFilePath { get; set; }

        public int IssueId { get; set; }
    }

    public class CurrentIssueViewModel
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Volume { get; set; }
        public string? Number { get; set; }
        public int Year { get; set; }
        public string? Description { get; set; }
        public string? CoverImagePath { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string? FullIssuePdfPath { get; set; }

    }

    public class LatestArticleViewModel
    {
        public string? Title { get; set; }

        public string? Authors { get; set; }

        public string? Abstract { get; set; }

        public string? PdfFilePath { get; set; }

        public int IssueId { get; set; }
    }
}
