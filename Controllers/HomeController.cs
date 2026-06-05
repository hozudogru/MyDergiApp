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

            var latestArticles = await _context.Submissions
                .Include(s => s.Authors)
                .Where(s => s.Status == SubmissionStatus.KabulEdildi)
                .OrderByDescending(s => s.DecisionDate ?? s.UpdatedAt ?? s.CreatedAt)
                .Take(5)
                .Select(s => new LatestArticleViewModel
                {
                    Id = s.Id,
                    Title = s.Title,
                    Abstract = s.Abstract,
                    Authors = string.Join(", ", s.Authors
                        .OrderBy(a => a.SortOrder)
                        .Select(a => a.FullName))
                })
                .ToListAsync();

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

            var reviewerUsers = await _userManager.GetUsersInRoleAsync("Reviewer");

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
                ThemeName = settings?.ThemeName ?? "classic",
                PrimaryColor = settings?.PrimaryColor ?? "#0d6efd",
                SecondaryColor = settings?.SecondaryColor ?? "#198754",
                HeaderBgColor = settings?.HeaderBgColor ?? "#ffffff",
                NavBgColor = settings?.NavBgColor ?? "#ffffff",
                BodyBgColor = settings?.BodyBgColor ?? "#f8fafc",
                TextColor = settings?.TextColor ?? "#111827",
                BannerTitle = settings?.BannerTitle ?? "Professional Academic Publishing Platform",
                BannerDescription = settings?.BannerDescription ?? "Submit, review, manage and publish academic articles through a modern journal system.",
                BannerLabel = settings?.BannerLabel ?? "Hakemli • Açık Erişim • Akademik Yayıncılık",

                BannerPrimaryButtonText = settings?.BannerPrimaryButtonText ?? "Makale Gönder",
                BannerPrimaryButtonUrl = settings?.BannerPrimaryButtonUrl ?? "/Submission/YeniMakale",

                BannerSecondaryButtonText = settings?.BannerSecondaryButtonText ?? "Makaleleri İncele",
                BannerSecondaryButtonUrl = settings?.BannerSecondaryButtonUrl ?? "/Issues/Published",

                BannerImagePath = settings?.BannerImagePath,
                ShowBanner = settings?.ShowBanner ?? true,
                HeaderLogoPath = settings?.HeaderLogoPath ?? settings?.LogoPath,
                HeaderTitle = settings?.HeaderTitle ?? settings?.SiteTitle ?? "MyDergiApp Journal",
                HeaderSubtitle = settings?.HeaderSubtitle ?? settings?.Subtitle,
                HeaderRightText = settings?.HeaderRightText ?? "Akademik Dergi Platformu",
                HeaderBackgroundImagePath = settings?.HeaderBackgroundImagePath,
                ShowHeaderLogo = settings?.ShowHeaderLogo ?? true,
                LatestArticles = latestArticles,
                Announcements = announcements,
                Indexes = indexes,
                TotalArticles = await _context.Submissions.CountAsync(s => s.Status == SubmissionStatus.KabulEdildi),
                TotalIssues = 0,
                TotalReviewers = reviewerUsers.Count,
                TotalAuthors = await _context.SubmissionAuthors.Select(a => a.Email).Distinct().CountAsync(),
                CurrentIssue = null

            };

            return View(model);
        }
    }
}
