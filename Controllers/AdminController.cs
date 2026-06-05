using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Models;
using MyDergiApp.ViewModels;
using Microsoft.Extensions.Configuration;


namespace MyDergiApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel
            {
                TotalSubmissions = await _context.Submissions.CountAsync(),

                PendingSubmissions = await _context.Submissions.CountAsync(x =>
                    x.Status == SubmissionStatus.OnKontrolBekliyor ||
                    x.Status == SubmissionStatus.Gonderildi),

                InReviewSubmissions = await _context.Submissions.CountAsync(x =>
                    x.Status == SubmissionStatus.HakemAtamasiBekliyor ||
                    x.Status == SubmissionStatus.HakemDegerlendirmesinde ||
                    x.Status == SubmissionStatus.RevizyonIstendi ||
                    x.Status == SubmissionStatus.RevizyonYuklendi),

                AcceptedSubmissions = await _context.Submissions.CountAsync(x =>
                    x.Status == SubmissionStatus.KabulEdildi),

                RejectedSubmissions = await _context.Submissions.CountAsync(x =>
                    x.Status == SubmissionStatus.Reddedildi),

                TotalUsers = await _context.Users.CountAsync(),
                ActiveUsers = await _context.Users.CountAsync(x => x.IsActive),
                PassiveUsers = await _context.Users.CountAsync(x => !x.IsActive),

                TotalAnnouncements = await _context.Announcements.CountAsync(),
                ActiveAnnouncements = await _context.Announcements.CountAsync(x => x.IsActive),

                TotalIssues = await _context.Issues.CountAsync(),
                PublishedIssues = await _context.Issues.CountAsync(x => x.IsPublished),

                TotalPublishedArticles = await _context.PublishedArticles.CountAsync(),
                TotalIndexes = await _context.JournalIndexes.CountAsync(x => x.IsActive),
                HasHomePageSettings = await _context.HomePageSettings.AnyAsync(x => x.IsActive),

                HasActiveAnnouncement = await _context.Announcements.AnyAsync(x => x.IsActive),

                HasPublishedIssue = await _context.Issues.AnyAsync(x => x.IsPublished),

                HasPublishedArticle = await _context.PublishedArticles
                    .Include(x => x.Issue)
                    .AnyAsync(x => x.Issue != null && x.Issue.IsPublished),

                HasActiveIndexes = await _context.JournalIndexes.AnyAsync(x => x.IsActive),

                HasPassiveUsers = await _context.Users.AnyAsync(x => !x.IsActive),

                HasSmtpSettings =
                    !string.IsNullOrWhiteSpace(_configuration["SMTP:Host"]) &&
                    !string.IsNullOrWhiteSpace(_configuration["SMTP:UserName"]) &&
                    !string.IsNullOrWhiteSpace(_configuration["SMTP:Password"]) &&
                    !string.IsNullOrWhiteSpace(_configuration["SMTP:FromEmail"])
            };

            return View(model);
        }
    }
}