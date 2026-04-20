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
        public IActionResult AccessDenied()
        {
            return View();
        }
        public async Task<IActionResult> Index()
        {
            var settings = await _context.HomePageSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive);

            var indexes = await _context.JournalIndexes
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .Select(x => new JournalIndexItemViewModel
                {
                    Name = x.Name,
                    LogoPath = x.LogoPath,
                    Url = x.Url
                })
                .ToListAsync();

            var latestArticles = await _context.Submissions
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Take(6)
                .Select(x => new ArticleCardViewModel
                {
                    Id = x.Id,
                    Title = x.Title ?? "",
                    Authors = "Author",
                    Abstract = x.Abstract,
                    PdfPath = null,
                    Pages = null
                })
                .ToListAsync();
            var announcements = await _context.Announcements
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new AnnouncementItemViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Content = x.Content,
                    CreatedAt = x.CreatedAt,
                    ShowAsPopup = x.ShowAsPopup
                })
                .ToListAsync();
            var currentIssue = await _context.Issues
                .AsNoTracking()
                .Where(x => x.IsCurrent && x.IsPublished)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Volume)
                .ThenByDescending(x => x.Number)
                .FirstOrDefaultAsync();
            var reviewers = await _userManager.GetUsersInRoleAsync("Reviewer");
            var authors = await _userManager.GetUsersInRoleAsync("Author");

            var vm = new HomePageViewModel
            {
                JournalTitle = settings?.JournalTitle ?? "MyDergiApp Journal",
                JournalSubtitle = settings?.JournalSubtitle ?? "Peer-reviewed international academic journal",
                HeroTitle = settings?.HeroTitle ?? "Professional Academic Publishing Platform",
                HeroDescription = settings?.HeroDescription ?? "Submit, review, manage and publish academic articles through a modern journal system.",
                AboutTitle = settings?.AboutTitle ?? "About the Journal",
                AboutContent = settings?.AboutContent ?? "MyDergiApp is a modern academic journal platform for submission, review, editorial workflow and publication management.",
                PrintIssn = settings?.PrintIssn,
                OnlineIssn = settings?.OnlineIssn,
                ContactEmail = settings?.ContactEmail,
                ContactPhone = settings?.ContactPhone,
                Address = settings?.Address,
                FooterText = settings?.FooterText ?? "MyDergiApp is designed for professional scholarly publishing.",
                LogoPath = settings?.LogoPath,
                LatestArticles = latestArticles,
                Announcements = announcements,
                Indexes = indexes,
                TotalArticles = await _context.Submissions.CountAsync(x => x.Status == SubmissionStatus.Accepted),
                CurrentIssue = currentIssue == null ? null : new CurrentIssueViewModel
                {
                    Id = currentIssue.Id,
                    Title = currentIssue.Title ?? "",
                    Volume = currentIssue.Volume,
                    Number = currentIssue.Number,
                    Year = currentIssue.Year,
                    CoverImagePath = currentIssue.CoverImagePath,
                    PublishedDate = currentIssue.PublishedDate,
                    Description = currentIssue.Description
                },

                TotalIssues = await _context.Issues.CountAsync(x => x.IsPublished),
                TotalReviewers = reviewers.Count,
                TotalAuthors = authors.Count
            };

            return View(vm);
        }
    }
}