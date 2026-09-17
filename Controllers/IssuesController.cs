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
        private readonly IWebHostEnvironment _env;

        public IssuesController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ---------------- Sayı oluşturma / silme ----------------
        // Not: Bu action'lar d47f567 commit'indeki yeniden yazımda kaybolmuştu; Index/ManageArticles view'ları onlara post ediyor.

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

            var now = DateTime.UtcNow;

            var issue = new Issue
            {
                Volume = model.Volume.Trim(),
                Number = model.Number.Trim(),
                Year = model.Year,
                Title = string.IsNullOrWhiteSpace(model.Title) ? null : model.Title.Trim(),
                IsPublished = model.IsPublished,
                PublishedAt = model.IsPublished ? now : null,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Issues.Add(issue);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Sayı oluşturuldu. Şimdi kapak, tam sayı PDF'i ve makaleleri ekleyebilirsiniz.";
            return RedirectToAction(nameof(ManageArticles), new { id = issue.Id });
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
                TempData["Error"] = "İçinde makale bulunan sayı silinemez. Önce makaleleri sayıdan çıkarınız.";
                return RedirectToAction(nameof(Index));
            }

            if (issue.IsPublished)
            {
                TempData["Error"] = "Yayındaki sayı silinemez. Önce yayından kaldırınız.";
                return RedirectToAction(nameof(Index));
            }

            _context.Issues.Remove(issue);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Sayı silindi.";
            return RedirectToAction(nameof(Index));
        }

        // ---------------- Sayıya makale ekleme / çıkarma ----------------

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
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null || submission.Status != SubmissionStatus.KabulEdildi)
            {
                TempData["Error"] = "Sadece kabul edilmiş makaleler sayıya eklenebilir.";
                return RedirectToAction(nameof(ManageArticles), new { id = issueId });
            }

            // Bir makale yalnızca tek bir sayıda yayınlanabilir
            var alreadyPublished = await _context.PublishedArticles
                .Include(a => a.Issue)
                .FirstOrDefaultAsync(a => a.SubmissionId == submissionId);

            if (alreadyPublished != null)
            {
                TempData["Error"] = alreadyPublished.IssueId == issueId
                    ? "Bu makale zaten bu sayıya eklenmiş."
                    : $"Bu makale zaten başka bir sayıda (Cilt {alreadyPublished.Issue?.Volume}, Sayı {alreadyPublished.Issue?.Number}) yer alıyor.";
                return RedirectToAction(nameof(ManageArticles), new { id = issueId });
            }

            var maxSortOrder = await _context.PublishedArticles
                .Where(a => a.IssueId == issueId)
                .Select(a => (int?)a.SortOrder)
                .MaxAsync() ?? 0;

            var authorsText = string.Join(", ", submission.Authors
                .OrderBy(a => a.SortOrder)
                .Select(a => a.FullName)
                .Where(n => !string.IsNullOrWhiteSpace(n)));

            _context.PublishedArticles.Add(new PublishedArticle
            {
                IssueId = issueId,
                SubmissionId = submissionId,
                AuthorsText = string.IsNullOrWhiteSpace(authorsText) ? null : authorsText,
                Pages = string.IsNullOrWhiteSpace(pages) ? null : pages.Trim(),
                SortOrder = maxSortOrder + 1,
                AddedAt = DateTime.UtcNow
            });

            issue.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Makale sayıya eklendi. Yayın PDF'ini makale satırındaki dosya düzenleme bağlantısından yükleyebilirsiniz.";
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
        [HttpGet]
        public IActionResult Edit(int id)
        {
            return RedirectToAction("ManageArticles", new { id });
        }
        [HttpGet]
        public async Task<IActionResult> Published()
        {
            var issues = await _context.Issues
                .AsNoTracking()
                .Include(i => i.Articles)
                    .ThenInclude(a => a.Submission)
                .Where(i => i.IsPublished)
                .ToListAsync();

            // Cilt/Sayi metin; dogal (sayisal) siralama bellek tarafinda (IssueOrdering)
            return View(issues.NewestFirst().ToList());
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var issue = await _context.Issues
                .Include(i => i.Articles)
                    .ThenInclude(a => a.Submission)
                .FirstOrDefaultAsync(i => i.Id == id && i.IsPublished);

            if (issue == null)
                return NotFound();

            return View(issue);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var issue = await _context.Issues
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null)
                return NotFound();

            issue.IsPublished = !issue.IsPublished;
            issue.UpdatedAt = DateTime.UtcNow;

            // Yayin tarihi ilk yayina alista yazilir; UpdatedAt her ust veri degisikliginde degistigi icin yayin tarihi olarak kullanilamaz
            if (issue.IsPublished && issue.PublishedAt == null)
                issue.PublishedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = issue.IsPublished
                ? "Sayı yayına alındı."
                : "Sayı yayından kaldırıldı.";

            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateIssueData(int id, Issue model, IFormFile? coverImage)
        {
            var issue = await _context.Issues.FirstOrDefaultAsync(x => x.Id == id);

            if (issue == null)
                return NotFound();

            // Dogrulama: Volume/Number DB'de NOT NULL, bos gonderilirse 500 veriyordu
            if (string.IsNullOrWhiteSpace(model.Volume) || string.IsNullOrWhiteSpace(model.Number))
            {
                TempData["Error"] = "Cilt ve Sayı alanları boş bırakılamaz.";
                return RedirectToAction("ManageArticles", new { id });
            }

            if (model.Volume.Length > 50 || model.Number.Length > 50 || (model.Title?.Length ?? 0) > 250)
            {
                TempData["Error"] = "Cilt ve Sayı en fazla 50, Başlık en fazla 250 karakter olabilir.";
                return RedirectToAction("ManageArticles", new { id });
            }

            if (model.Year < 1900 || model.Year > DateTime.UtcNow.Year + 5)
            {
                TempData["Error"] = "Yıl geçerli bir değer olmalıdır.";
                return RedirectToAction("ManageArticles", new { id });
            }

            issue.Volume = model.Volume.Trim();
            issue.Number = model.Number.Trim();
            issue.Year = model.Year;
            issue.Title = string.IsNullOrWhiteSpace(model.Title) ? null : model.Title.Trim();

            if (model.IsPublished && !issue.IsPublished)
                issue.PublishedAt = DateTime.UtcNow;

            issue.IsPublished = model.IsPublished;
            issue.UpdatedAt = DateTime.UtcNow;

            if (coverImage != null && coverImage.Length > 0)
            {
                var error = UploadHelper.Validate(coverImage, UploadHelper.ImageExtensions, UploadHelper.MaxImageBytes, "Kapak görseli");

                if (error != null)
                {
                    TempData["Error"] = error;
                    return RedirectToAction("ManageArticles", new { id });
                }

                UploadHelper.TryDeleteWebFile(_env, issue.CoverImagePath); // eski kapak yetim kalmasin
                issue.CoverImagePath = await UploadHelper.SaveAsync(_env, coverImage, "covers", $"issue_cover_{issue.Id}_");
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Sayı bilgileri güncellendi.";
            return RedirectToAction("ManageArticles", new { id = issue.Id });
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateArticleOrder([FromBody] List<int> articleIds)
        {
            if (articleIds == null || !articleIds.Any())
                return BadRequest();

            var articles = await _context.PublishedArticles
                .Where(x => articleIds.Contains(x.Id))
                .ToListAsync();

            // Tek istekte yalnizca ayni sayinin makaleleri yeniden siralanabilir
            if (articles.Count == 0 || articles.Select(x => x.IssueId).Distinct().Count() != 1)
                return BadRequest("Sıralanan makaleler aynı sayıya ait olmalıdır.");

            for (int i = 0; i < articleIds.Count; i++)
            {
                var article = articles.FirstOrDefault(x => x.Id == articleIds[i]);

                if (article != null)
                {
                    article.SortOrder = i + 1;
                }
            }

            var issueId = articles.FirstOrDefault()?.IssueId;

            if (issueId != null)
            {
                var issue = await _context.Issues.FirstOrDefaultAsync(x => x.Id == issueId.Value);
                if (issue != null)
                {
                    issue.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return Ok();
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateIssueGalley(int id, IFormFile? fullIssuePdf)
        {
            var issue = await _context.Issues.FirstOrDefaultAsync(x => x.Id == id);

            if (issue == null)
                return NotFound();

            if (fullIssuePdf == null || fullIssuePdf.Length == 0)
            {
                TempData["Error"] = "Lütfen tam sayı PDF dosyası seçiniz.";
                return RedirectToAction("ManageArticles", new { id });
            }

            var error = UploadHelper.Validate(fullIssuePdf, UploadHelper.PdfExtensions, UploadHelper.MaxPdfBytes, "Tam sayı dosyası");

            if (error != null)
            {
                TempData["Error"] = error;
                return RedirectToAction("ManageArticles", new { id });
            }

            UploadHelper.TryDeleteWebFile(_env, issue.FullIssuePdfPath); // eski PDF yetim kalmasin
            issue.FullIssuePdfPath = await UploadHelper.SaveAsync(_env, fullIssuePdf, "issues", $"issue_full_{issue.Id}_");
            issue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Tam sayı PDF dosyası güncellendi.";
            return RedirectToAction("ManageArticles", new { id = issue.Id });
        }
        // Mevcut actionlar burada...
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditPublishedArticleFile(int id)
        {
            var article = await _context.PublishedArticles
                .Include(a => a.Submission)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
                return NotFound();

            return View(article);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPublishedArticleFile(int id, IFormFile? pdfFile, IFormFile? originalFile)
        {
            var article = await _context.PublishedArticles
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
                return NotFound();

            // Once iki dosyayi da dogrula, sonra kaydet (biri hataliysa digeri yarim kalmasin)
            string? pdfError = pdfFile != null && pdfFile.Length > 0
                ? UploadHelper.Validate(pdfFile, UploadHelper.PdfExtensions, UploadHelper.MaxPdfBytes, "Yayın PDF dosyası")
                : null;

            string? originalError = originalFile != null && originalFile.Length > 0
                ? UploadHelper.Validate(originalFile, UploadHelper.DocumentExtensions, UploadHelper.MaxDocumentBytes, "Makale kaynak dosyası")
                : null;

            if (pdfError != null || originalError != null)
            {
                TempData["Error"] = pdfError ?? originalError;
                return RedirectToAction("EditPublishedArticleFile", new { id });
            }

            if (pdfFile != null && pdfFile.Length > 0)
            {
                UploadHelper.TryDeleteWebFile(_env, article.PdfFilePath);
                article.PdfFilePath = await UploadHelper.SaveAsync(_env, pdfFile, "published", $"published_{article.Id}_");
            }

            if (originalFile != null && originalFile.Length > 0)
            {
                UploadHelper.TryDeleteWebFile(_env, article.OriginalFilePath);
                article.OriginalFilePath = await UploadHelper.SaveAsync(_env, originalFile, "published", $"source_{article.Id}_");
            }

            var issue = await _context.Issues.FirstOrDefaultAsync(i => i.Id == article.IssueId);
            if (issue != null)
                issue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Makale dosyaları güncellendi.";
            return RedirectToAction("ManageArticles", new { id = article.IssueId });
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var issues = await _context.Issues
                .Include(i => i.Articles)
                .ToListAsync();

            return View(issues.NewestFirst().ToList());
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditPublishedArticle(int id)
        {
            var article = await _context.PublishedArticles
                .Include(a => a.Submission)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
                return NotFound();

            return View(article);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPublishedArticle(int id, PublishedArticle model)
        {
            var article = await _context.PublishedArticles
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
                return NotFound();

            // Uzunluk sinirlari (StringLength) DB'de character varying(n); asilirsa Npgsql 500 veriyordu
            ModelState.Remove(nameof(PublishedArticle.Issue));
            ModelState.Remove(nameof(PublishedArticle.Submission));

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Distinct();

                TempData["Error"] = "Makale üst verisi kaydedilmedi: " + string.Join(" ", errors);
                return RedirectToAction(nameof(EditPublishedArticle), new { id });
            }

            article.TitleOverride = string.IsNullOrWhiteSpace(model.TitleOverride)
                ? null
                : model.TitleOverride.Trim();

            article.AuthorsText = string.IsNullOrWhiteSpace(model.AuthorsText)
                ? null
                : model.AuthorsText.Trim();

            article.Pages = string.IsNullOrWhiteSpace(model.Pages)
                ? null
                : model.Pages.Trim();

            article.Doi = string.IsNullOrWhiteSpace(model.Doi)
                ? null
                : model.Doi.Trim();

            article.AbstractOverride = string.IsNullOrWhiteSpace(model.AbstractOverride)
                ? null
                : model.AbstractOverride.Trim();

            article.Keywords = string.IsNullOrWhiteSpace(model.Keywords)
                ? null
                : model.Keywords.Trim();

            article.SortOrder = model.SortOrder;

            var issue = await _context.Issues.FirstOrDefaultAsync(i => i.Id == article.IssueId);
            if (issue != null)
                issue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Makale üst verisi güncellendi.";
            return RedirectToAction("ManageArticles", new { id = article.IssueId });
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageArticles(int id)
        {
            var issue = await _context.Issues
                .Include(i => i.Articles)
                    .ThenInclude(a => a.Submission)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null)
                return NotFound();

            var acceptedSubmissions = await _context.Submissions
                .Where(s => s.Status == SubmissionStatus.KabulEdildi)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            ViewBag.AcceptedSubmissions = acceptedSubmissions;

            return View(issue);
        }
    }
}