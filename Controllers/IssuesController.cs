using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Helpers;
using MyDergiApp.Models;

namespace MyDergiApp.Controllers
{
    public class IssuesController : Controller
    {
        private readonly AppDbContext _context;

        public IssuesController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var issues = await _context.Issues
                .Include(i => i.Articles)
                .OrderByDescending(i => i.Year)
                .ThenByDescending(i => i.Id)
                .ToListAsync();

            return View(issues);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Issue
            {
                Year = DateTime.UtcNow.Year,
                IsPublished = false
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Issue model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Volume = model.Volume.Trim();
            model.Number = model.Number.Trim();
            model.Title = string.IsNullOrWhiteSpace(model.Title) ? null : model.Title.Trim();
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;
            model.PublishedAt = model.IsPublished ? DateTime.UtcNow : null;

            _context.Issues.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Sayı oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var issue = await _context.Issues.FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null)
                return NotFound();

            return View(issue);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Issue model)
        {
            var issue = await _context.Issues.FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            issue.Volume = model.Volume.Trim();
            issue.Number = model.Number.Trim();
            issue.Title = string.IsNullOrWhiteSpace(model.Title) ? null : model.Title.Trim();
            issue.Year = model.Year;
            issue.IsPublished = model.IsPublished;
            issue.UpdatedAt = DateTime.UtcNow;

            if (issue.IsPublished && issue.PublishedAt == null)
                issue.PublishedAt = DateTime.UtcNow;

            if (!issue.IsPublished)
                issue.PublishedAt = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Sayı güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageArticles(int id)
        {
            var issue = await _context.Issues
                .Include(i => i.Articles)
                    .ThenInclude(a => a.Submission)
                        .ThenInclude(s => s.Authors)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null)
                return NotFound();

            var alreadyAddedSubmissionIds = issue.Articles
                .Select(a => a.SubmissionId)
                .ToList();

            ViewBag.AcceptedSubmissions = await _context.Submissions
                .Include(s => s.Authors)
                .Where(s =>
                    s.Status == SubmissionStatus.KabulEdildi &&
                    !alreadyAddedSubmissionIds.Contains(s.Id))
                .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
                .ToListAsync();

            return View(issue);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddArticle(int issueId, int submissionId, string? pages)
        {
            var issue = await _context.Issues.FirstOrDefaultAsync(i => i.Id == issueId);

            if (issue == null)
                return NotFound();

            var submission = await _context.Submissions
                .Include(s => s.Authors)
                .FirstOrDefaultAsync(s =>
                    s.Id == submissionId &&
                    s.Status == SubmissionStatus.KabulEdildi);

            if (submission == null)
            {
                TempData["Error"] = "Sadece kabul edilmiş makaleler sayıya eklenebilir.";
                return RedirectToAction(nameof(ManageArticles), new { id = issueId });
            }

            var exists = await _context.PublishedArticles
                .AnyAsync(a => a.IssueId == issueId && a.SubmissionId == submissionId);

            if (exists)
            {
                TempData["Error"] = "Bu makale zaten bu sayıya eklenmiş.";
                return RedirectToAction(nameof(ManageArticles), new { id = issueId });
            }

            var maxSortOrder = await _context.PublishedArticles
                .Where(a => a.IssueId == issueId)
                .Select(a => (int?)a.SortOrder)
                .MaxAsync() ?? 0;

            var authorsText = string.Join(", ", submission.Authors
                .OrderBy(a => a.SortOrder)
                .Select(a => a.FullName));

            _context.PublishedArticles.Add(new PublishedArticle
            {
                IssueId = issueId,
                SubmissionId = submissionId,
                TitleOverride = null,
                AuthorsText = authorsText,
                Pages = string.IsNullOrWhiteSpace(pages) ? null : pages.Trim(),
                SortOrder = maxSortOrder + 1,
                AddedAt = DateTime.UtcNow
            });

            issue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Makale sayıya eklendi.";
            return RedirectToAction(nameof(ManageArticles), new { id = issueId });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveArticle(int id)
        {
            var article = await _context.PublishedArticles
                .Include(a => a.Issue)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
                return NotFound();

            var issueId = article.IssueId;

            _context.PublishedArticles.Remove(article);

            if (article.Issue != null)
                article.Issue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Makale sayıdan çıkarıldı.";
            return RedirectToAction(nameof(ManageArticles), new { id = issueId });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var issue = await _context.Issues.FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null)
                return NotFound();

            issue.IsPublished = !issue.IsPublished;
            issue.PublishedAt = issue.IsPublished ? DateTime.UtcNow : null;
            issue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = issue.IsPublished
                ? "Sayı yayına alındı."
                : "Sayı yayından kaldırıldı.";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var issue = await _context.Issues
                .Include(i => i.Articles)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null)
                return NotFound();

            if (issue.Articles.Any())
            {
                TempData["Error"] = "İçinde makale bulunan sayı silinemez. Önce makaleleri çıkarınız.";
                return RedirectToAction(nameof(Index));
            }

            _context.Issues.Remove(issue);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Sayı silindi.";
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Published()
        {
            var issues = await _context.Issues
                .Include(i => i.Articles)
                    .ThenInclude(a => a.Submission)
                        .ThenInclude(s => s.Authors)
                .Where(i => i.IsPublished)
                .OrderByDescending(i => i.Year)
                .ThenByDescending(i => i.Id)
                .ToListAsync();

            return View(issues);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var issue = await _context.Issues
                .Include(i => i.Articles)
                    .ThenInclude(a => a.Submission)
                        .ThenInclude(s => s.Files)
                .FirstOrDefaultAsync(i => i.Id == id && i.IsPublished);
            
            if (issue == null)
                return NotFound();

            return View(issue);
        }
    }
}
