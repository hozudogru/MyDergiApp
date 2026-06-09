using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Entities;
using MyDergiApp.Models;
using MyDergiApp.ViewModels;

namespace MyDergiApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public HomeController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var settings = await _context.HomePageSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive);

            settings ??= new HomePageSettings
            {
                JournalTitle = "MyDergiApp",
                JournalSubtitle = "Journal Management Panel",
                HeroTitle = "Akademik Dergi Yönetim Sistemi",
                HeroDescription = "Makale gönderim, hakemlik ve editörlük süreçlerini çevrimiçi yönetin.",
                AboutTitle = "Dergi Hakkında",
                AboutContent = "Bu alanı yönetim panelinden düzenleyebilirsiniz.",
                FooterText = "MyDergiApp Journal Editorial System"
            };

            

            var announcements = await _context.Announcements
                 .AsNoTracking()
                 .Where(x => x.IsActive)
                 .OrderByDescending(x => x.ShowAsPopup)
                 .ThenByDescending(x => x.CreatedAt)
                 .Take(5)
                 .ToListAsync();

            var indexes = await _context.JournalIndexes
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            var currentIssue = await _context.Issues
                .AsNoTracking()
                .Include(x => x.Articles)
                    .ThenInclude(a => a.Submission)
                .Where(x => x.IsPublished)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Volume)
                .ThenByDescending(x => x.Number)
                .FirstOrDefaultAsync();
            var latestArticles = currentIssue == null
            ? new List<LatestArticleViewModel>()
            : await _context.PublishedArticles
                .AsNoTracking()
                .Include(x => x.Submission)
                .Where(x => x.IssueId == currentIssue.Id)
                .OrderBy(x => x.SortOrder)
                .Select(x => new LatestArticleViewModel
                {
                    Title = !string.IsNullOrWhiteSpace(x.TitleOverride)
                        ? x.TitleOverride
                        : x.Submission != null
                            ? x.Submission.Title
                            : "Başlık belirtilmemiş",

                    Authors = !string.IsNullOrWhiteSpace(x.AuthorsText)
                        ? x.AuthorsText
                        : "Yazar bilgisi belirtilmemiş",

                    Abstract = !string.IsNullOrWhiteSpace(x.AbstractOverride)
                        ? x.AbstractOverride
                        : x.Submission != null
                            ? x.Submission.Abstract
                            : "",

                    PdfFilePath = x.PdfFilePath,
                    IssueId = x.IssueId
                })
                .ToListAsync();




            var reviewerUsers = await _userManager.GetUsersInRoleAsync("Reviewer");

            var totalPublishedArticles = await _context.PublishedArticles
                .AsNoTracking()
                .Include(x => x.Issue)
                .CountAsync(x => x.Issue != null && x.Issue.IsPublished);

            var totalPublishedIssues = await _context.Issues
                .AsNoTracking()
                .CountAsync(x => x.IsPublished);

            var model = new HomePageViewModel
            {
                JournalTitle = settings.JournalTitle,
                JournalSubtitle = settings.JournalSubtitle,
                HeroTitle = settings.HeroTitle,
                HeroDescription = settings.HeroDescription,
                AboutTitle = settings.AboutTitle,
                AboutContent = settings.AboutContent,
                PrintIssn = settings.PrintIssn,
                OnlineIssn = settings.OnlineIssn,
                ContactEmail = settings.ContactEmail,
                ContactPhone = settings.ContactPhone,
                Address = settings.Address,
                LogoPath = settings.LogoPath,
                FooterText = settings.FooterText,

                ThemeName = settings.ThemeName ?? "classic",
                PrimaryColor = settings.PrimaryColor ?? "#0d6efd",
                SecondaryColor = settings.SecondaryColor ?? "#198754",
                HeaderBgColor = settings.HeaderBgColor ?? "#ffffff",
                NavBgColor = settings.NavBgColor ?? "#ffffff",
                BodyBgColor = settings.BodyBgColor ?? "#f8fafc",
                TextColor = settings.TextColor ?? "#111827",

                BannerTitle = settings.BannerTitle ?? "Professional Academic Publishing Platform",
                BannerDescription = settings.BannerDescription ?? "Submit, review, manage and publish academic articles through a modern journal system.",
                BannerLabel = settings.BannerLabel ?? "Hakemli • Açık Erişim • Akademik Yayıncılık",

                BannerPrimaryButtonText = settings.BannerPrimaryButtonText ?? "Makale Gönder",
                BannerPrimaryButtonUrl = settings.BannerPrimaryButtonUrl ?? "/Submission/YeniMakale",

                BannerSecondaryButtonText = settings.BannerSecondaryButtonText ?? "Makaleleri İncele",
                BannerSecondaryButtonUrl = settings.BannerSecondaryButtonUrl ?? "/Issues/Published",

                BannerImagePath = settings.BannerImagePath,
                ShowBanner = settings.ShowBanner,

                HeaderLogoPath = settings.HeaderLogoPath ?? settings.LogoPath,
                HeaderTitle = settings.HeaderTitle ?? settings.SiteTitle ?? "MyDergiApp Journal",
                HeaderSubtitle = settings.HeaderSubtitle ?? settings.Subtitle,
                HeaderRightText = settings.HeaderRightText ?? "Akademik Dergi Platformu",
                HeaderBackgroundImagePath = settings.HeaderBackgroundImagePath,
                ShowHeaderLogo = settings.ShowHeaderLogo,

                LatestArticles = latestArticles,
                Announcements = announcements,
                Indexes = indexes,

                TotalArticles = totalPublishedArticles,
                TotalIssues = totalPublishedIssues,
                TotalReviewers = reviewerUsers.Count,
                TotalAuthors = await _context.SubmissionAuthors
                    .AsNoTracking()
                    .Select(a => a.Email)
                    .Distinct()
                    .CountAsync(),

                CurrentIssue = currentIssue == null
                    ? null
                    : new CurrentIssueViewModel
                    {
                        Id = currentIssue.Id,
                        Title = currentIssue.Title,
                        Volume = currentIssue.Volume,
                        Number = currentIssue.Number,
                        Year = currentIssue.Year,
                        PublishedDate = currentIssue.UpdatedAt,
                        CoverImagePath = currentIssue.CoverImagePath,
                        FullIssuePdfPath = currentIssue.FullIssuePdfPath
                    }
            };

            return View(model);
        }
    }
}
