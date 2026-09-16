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
                var ext = Path.GetExtension(coverImage.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!allowedExtensions.Contains(ext))
                {
                    TempData["Error"] = "Kapak görseli yalnızca jpg, jpeg, png veya webp olabilir.";
                    return RedirectToAction("ManageArticles", new { id });
                }

                var uploadRoot = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "covers"
                );

                Directory.CreateDirectory(uploadRoot);

                var fileName = $"issue_cover_{issue.Id}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadRoot, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await coverImage.CopyToAsync(stream);
                }

                issue.CoverImagePath = "/uploads/covers/" + fileName;
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

            var ext = Path.GetExtension(fullIssuePdf.FileName).ToLowerInvariant();

            if (ext != ".pdf")
            {
                TempData["Error"] = "Tam sayı dosyası yalnızca PDF formatında olmalıdır.";
                return RedirectToAction("ManageArticles", new { id });
            }

            var uploadRoot = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "issues"
            );

            Directory.CreateDirectory(uploadRoot);

            var fileName = $"issue_full_{issue.Id}_{Guid.NewGuid()}.pdf";
            var filePath = Path.Combine(uploadRoot, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fullIssuePdf.CopyToAsync(stream);
            }

            issue.FullIssuePdfPath = "/uploads/issues/" + fileName;
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

            var uploadRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "published");
            Directory.CreateDirectory(uploadRoot);

            if (pdfFile != null && pdfFile.Length > 0)
            {
                var ext = Path.GetExtension(pdfFile.FileName).ToLowerInvariant();

                if (ext != ".pdf")
                {
                    TempData["Error"] = "PDF dosyası yalnızca .pdf formatında olmalıdır.";
                    return RedirectToAction("EditPublishedArticleFile", new { id });
                }

                var fileName = $"published_{article.Id}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadRoot, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await pdfFile.CopyToAsync(stream);
                }

                article.PdfFilePath = "/uploads/published/" + fileName;
            }

            if (originalFile != null && originalFile.Length > 0)
            {
                var ext = Path.GetExtension(originalFile.FileName).ToLowerInvariant();

                var allowed = new[] { ".doc", ".docx", ".pdf" };

                if (!allowed.Contains(ext))
                {
                    TempData["Error"] = "Makale dosyası yalnızca .doc, .docx veya .pdf olabilir.";
                    return RedirectToAction("EditPublishedArticleFile", new { id });
                }

                var fileName = $"source_{article.Id}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadRoot, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await originalFile.CopyToAsync(stream);
                }

                article.OriginalFilePath = "/uploads/published/" + fileName;
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