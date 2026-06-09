using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Entities;
using MyDergiApp.Helpers;
using MyDergiApp.Models;
using MyDergiApp.ViewModels;
using MyDergiApp.ViewModels.Submissions;
using System.IO;
using System.Net.Mail;
using System.Security.Claims;

namespace MyDergiApp.Controllers
{
    [Authorize]
    public class SubmissionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly EmailService _emailService;
        private readonly IWebHostEnvironment _env;

        public SubmissionController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            EmailService emailService,
             IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _env = env;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        }

        private bool IsAdminOrEditor()
        {
            return User.IsInRole("Admin") || User.IsInRole("Editor") || User.IsInRole("ChiefEditor");
        }

        private async Task SaveSubmissionFileAsync(
            int submissionId,
            IFormFile? file,
            string fileType,
            string folderPath,
            string? uploadedByUserId,
            int reviewRound = 1)
        {
            if (file == null || file.Length == 0)
                return;

            Directory.CreateDirectory(folderPath);

            var ext = Path.GetExtension(file.FileName);
            var generatedFileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(folderPath, generatedFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativeFolder = folderPath
                .Replace(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "")
                .Replace("\\", "/");

            var relativePath = $"{relativeFolder}/{generatedFileName}";

            _context.SubmissionFiles.Add(new SubmissionFile
            {
                SubmissionId = submissionId,
                FileType = fileType,
                OriginalFileName = file.FileName,
                StoredFilePath = relativePath,
                FileSize = file.Length,
                UploadedByUserId = uploadedByUserId,
                UploadedAt = DateTime.UtcNow,
                ReviewRound = reviewRound
            });
        }

        private async Task NotifyChiefEditorsForNewSubmissionAsync(
     Submission submission,
     List<SubmissionAuthor> authors)
        {
            var chiefEditors = await _userManager.GetUsersInRoleAsync("ChiefEditor");

            if (chiefEditors == null || chiefEditors.Count == 0)
                return;

            var correspondingAuthor = authors.FirstOrDefault(a => a.IsCorrespondingAuthor)
                                      ?? authors.FirstOrDefault();

            string H(string? value)
            {
                return System.Net.WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(value) ? "-" : value.Trim()
                );
            }

            var correspondingText = correspondingAuthor == null
                ? "-"
                : $"{H(correspondingAuthor.FullName)} ({H(correspondingAuthor.Email)})";

            var detailUrl = Url.Action(
                "Detail",
                "Submission",
                new { id = submission.Id },
                protocol: Request.Scheme);

            var reviewListUrl = Url.Action(
                "OnKontrolListesi",
                "Submission",
                null,
                protocol: Request.Scheme);

            var statusText = StatusDisplayHelper.GetSubmissionStatusText(submission.Status);

            foreach (var editor in chiefEditors)
            {
                if (string.IsNullOrWhiteSpace(editor.Email))
                    continue;

                var body = $"""
        <div style="font-family:Arial,Helvetica,sans-serif; background:#f8fafc; padding:24px;">
            <div style="max-width:760px; margin:0 auto; background:#ffffff; border:1px solid #e5e7eb; border-radius:14px; overflow:hidden;">
                
                <div style="background:#0d6efd; color:#ffffff; padding:22px 26px;">
                    <h2 style="margin:0; font-size:22px;">Yeni Makale Gönderimi</h2>
                    <p style="margin:8px 0 0 0; font-size:14px;">
                        Ön kontrol bekleyen yeni bir makale bulunmaktadır.
                    </p>
                </div>

                <div style="padding:26px;">
                    <p style="margin-top:0;">Sayın Baş Editör,</p>

                    <p>
                        Sisteme yeni bir makale gönderildi ve ön kontrol sürecine alınmayı beklemektedir.
                    </p>

                    <div style="background:#f9fafb; border:1px solid #e5e7eb; border-radius:12px; padding:16px 18px; margin:20px 0;">
                        <p><strong>Makale No:</strong> #{submission.Id}</p>
                        <p><strong>Başlık:</strong> {H(submission.Title)}</p>
                        <p><strong>Alt Başlık:</strong> {H(submission.Subtitle)}</p>
                        <p><strong>Sorumlu Yazar:</strong> {correspondingText}</p>
                        <p><strong>Gönderim Tarihi:</strong> {submission.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}</p>
                        <p><strong>Durum:</strong> {H(statusText)}</p>
                    </div>

                    <p style="margin-bottom:18px;">
                        Makale detayını veya ön kontrol listesini görüntülemek için aşağıdaki bağlantıları kullanabilirsiniz.
                    </p>

                    <p>
                        <a href="{detailUrl}" style="display:inline-block; background:#0d6efd; color:#ffffff; text-decoration:none; padding:10px 16px; border-radius:999px; font-weight:bold;">
                            Makale Detayını Görüntüle
                        </a>
                    </p>

                    <p>
                        <a href="{reviewListUrl}" style="display:inline-block; background:#f3f4f6; color:#111827; text-decoration:none; padding:10px 16px; border-radius:999px; font-weight:bold; border:1px solid #d1d5db;">
                            Ön Kontrol Listesine Git
                        </a>
                    </p>

                    <p style="margin-bottom:0;">İyi çalışmalar.</p>
                </div>

                <div style="padding:14px 22px; font-size:12px; color:#6b7280; border-top:1px solid #e5e7eb; background:#fafafa;">
                    MyDergiApp Journal Editorial System · Bu e-posta otomatik olarak oluşturulmuştur.
                </div>
            </div>
        </div>
        """;

                await _emailService.SendEmailAsync(
                    editor.Email,
                    $"Yeni Makale Gönderimi #{submission.Id}",
                    body);
            }
        }


        [Authorize(Roles = "Editor")]
        [HttpGet]
        public async Task<IActionResult> EditorDashboard(string? tab = "active")
        {
            var currentUserId = _userManager.GetUserId(User);
            tab = (tab ?? "active").ToLower();

            var baseQuery = _context.Submissions
                .Include(s => s.Authors)
                .Include(s => s.Files)
                .Where(s => s.AssignedSectionEditorId == currentUserId)
                .AsQueryable();

            var activeStatuses = new[]
            {
        SubmissionStatus.AlanEditorunde,
        SubmissionStatus.HakemAtamasiBekliyor,
        SubmissionStatus.HakemDegerlendirmesinde,
        SubmissionStatus.RevizyonYuklendi
    };

            var revisionWaitingStatuses = new[]
            {
        SubmissionStatus.RevizyonIstendi
    };

            var archiveStatuses = new[]
            {
        SubmissionStatus.KabulEdildi,
        SubmissionStatus.Reddedildi,
        SubmissionStatus.GeriCekildi
    };

            var activeCount = await baseQuery
                .Where(s => activeStatuses.Contains(s.Status))
                .CountAsync();

            var revisionWaitingCount = await baseQuery
                .Where(s => revisionWaitingStatuses.Contains(s.Status))
                .CountAsync();

            var unassignedCount = await baseQuery
                .Where(s =>
                    activeStatuses.Contains(s.Status) &&
                    !_context.SubmissionReviewers.Any(sr =>
                        sr.SubmissionId == s.Id &&
                        sr.ReviewRound == s.CurrentReviewRound &&
                        sr.Status != ReviewerAssignmentStatus.Cancelled &&
                        sr.Status != ReviewerAssignmentStatus.Declined))
                .CountAsync();

            var archiveCount = await baseQuery
                .Where(s => archiveStatuses.Contains(s.Status))
                .CountAsync();

            var allCount = await baseQuery.CountAsync();

            var filteredQuery = tab switch
            {
                "unassigned" => baseQuery.Where(s =>
                    activeStatuses.Contains(s.Status) &&
                    !_context.SubmissionReviewers.Any(sr =>
                        sr.SubmissionId == s.Id &&
                        sr.ReviewRound == s.CurrentReviewRound &&
                        sr.Status != ReviewerAssignmentStatus.Cancelled &&
                        sr.Status != ReviewerAssignmentStatus.Declined)),

                "revision" => baseQuery.Where(s =>
                    revisionWaitingStatuses.Contains(s.Status)),

                "archive" => baseQuery.Where(s =>
                    archiveStatuses.Contains(s.Status)),

                "all" => baseQuery,

                _ => baseQuery.Where(s =>
                    activeStatuses.Contains(s.Status))
            };

            var submissions = await filteredQuery
                .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
                .Select(s => new MyDergiApp.ViewModels.Submissions.EditorSubmissionListItemViewModel
                {
                    Id = s.Id,
                    Title = s.Title,
                    Subtitle = s.Subtitle,
                    Status = s.Status.ToString(),
                    RawStatus = s.Status,
                    CreatedAt = s.CreatedAt,
                    CurrentReviewRound = s.CurrentReviewRound,

                    CorrespondingAuthorName = s.Authors
                        .OrderByDescending(a => a.IsCorrespondingAuthor)
                        .ThenBy(a => a.SortOrder)
                        .Select(a => a.FullName)
                        .FirstOrDefault(),

                    CorrespondingAuthorEmail = s.Authors
                        .OrderByDescending(a => a.IsCorrespondingAuthor)
                        .ThenBy(a => a.SortOrder)
                        .Select(a => a.Email)
                        .FirstOrDefault(),

                    AuthorCount = s.Authors.Count,
                    FileCount = s.Files.Count,

                    AssignedReviewerCount = _context.SubmissionReviewers
                        .Count(sr =>
                            sr.SubmissionId == s.Id &&
                            sr.ReviewRound == s.CurrentReviewRound &&
                            sr.Status != ReviewerAssignmentStatus.Cancelled &&
                            sr.Status != ReviewerAssignmentStatus.Declined),
                    CompletedReviewerCount = _context.SubmissionReviewers
                            .Count(sr =>
                                sr.SubmissionId == s.Id &&
                                sr.ReviewRound == s.CurrentReviewRound &&
                                sr.Status == ReviewerAssignmentStatus.Completed)
                                        })
                                        .ToListAsync();

            ViewBag.ActiveTab = tab;
            ViewBag.ActiveCount = activeCount;
            ViewBag.RevisionWaitingCount = revisionWaitingCount;
            ViewBag.UnassignedCount = unassignedCount;
            ViewBag.ArchiveCount = archiveCount;
            ViewBag.AllCount = allCount;

            return View(submissions);
        }
        [Authorize(Roles = "Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignReviewerToCurrentRound(int submissionId, string reviewerId)
        {
            var currentUserId = _userManager.GetUserId(User);

            var submission = await _context.Submissions
                .FirstOrDefaultAsync(s =>
                    s.Id == submissionId &&
                    s.AssignedSectionEditorId == currentUserId);

            if (submission == null)
            {
                TempData["Error"] = "Bu makale size atanmadığı için hakem ataması yapamazsınız.";
                return RedirectToAction(nameof(EditorDashboard));
            }

            if (submission.Status == SubmissionStatus.KabulEdildi ||
                submission.Status == SubmissionStatus.Reddedildi ||
                submission.Status == SubmissionStatus.GeriCekildi)
            {
                TempData["Error"] = "Nihai karar verilmiş makaleye hakem atanamaz.";
                return RedirectToAction(nameof(EditorDashboard), new { tab = "archive" });
            }

            if (submission.Status != SubmissionStatus.AlanEditorunde &&
                submission.Status != SubmissionStatus.HakemAtamasiBekliyor &&
                submission.Status != SubmissionStatus.HakemDegerlendirmesinde &&
                submission.Status != SubmissionStatus.RevizyonYuklendi)
            {
                TempData["Error"] = "Bu makalenin mevcut durumunda hakem ataması yapılamaz.";
                return RedirectToAction(nameof(EditorDecision), new { id = submissionId });
            }

            if (submission.CurrentReviewRound <= 0)
            {
                submission.CurrentReviewRound = 1;
            }

            if (string.IsNullOrWhiteSpace(reviewerId))
            {
                TempData["Error"] = "Lütfen bir hakem seçiniz.";
                return RedirectToAction(nameof(EditorDecision), new { id = submissionId });
            }

            var reviewer = await _userManager.FindByIdAsync(reviewerId);

            if (reviewer == null)
            {
                TempData["Error"] = "Seçilen hakem bulunamadı.";
                return RedirectToAction(nameof(EditorDecision), new { id = submissionId });
            }

            var isReviewer = await _userManager.IsInRoleAsync(reviewer, "Reviewer");

            if (!isReviewer)
            {
                TempData["Error"] = "Seçilen kullanıcı hakem rolünde değildir.";
                return RedirectToAction(nameof(EditorDecision), new { id = submissionId });
            }

            var alreadyAssignedThisRound = await _context.SubmissionReviewers
                .AnyAsync(sr =>
                    sr.SubmissionId == submissionId &&
                    sr.ReviewerId == reviewerId &&
                    sr.ReviewRound == submission.CurrentReviewRound &&
                    sr.Status != ReviewerAssignmentStatus.Cancelled &&
                    sr.Status != ReviewerAssignmentStatus.Declined);

            if (alreadyAssignedThisRound)
            {
                TempData["Error"] = $"{submission.CurrentReviewRound}. tur için bu hakem zaten atanmış.";
                return RedirectToAction(nameof(EditorDecision), new { id = submissionId });
            }

            _context.SubmissionReviewers.Add(new SubmissionReviewer
            {
                SubmissionId = submission.Id,
                ReviewerId = reviewerId,
                ReviewRound = submission.CurrentReviewRound,
                Status = ReviewerAssignmentStatus.Assigned,

                AssignedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(15),

                CompletedAt = null,
                ReviewNote = null,

                ReminderCount = 0,
                ReminderSentAt = null,
                CancelReason = null,
                CancelledAt = null,
                CancelledByUserId = null
            });

            submission.Status = SubmissionStatus.HakemDegerlendirmesinde;
            submission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"{submission.CurrentReviewRound}. tur için hakem ataması yapıldı.";

            return RedirectToAction(nameof(EditorDecision), new { id = submissionId });
        }
        [Authorize(Roles = "Editor")]
        [HttpGet]
        public async Task<IActionResult> AssignReviewer(int id)
        {
            var currentUserId = _userManager.GetUserId(User);

            var submissionExists = await _context.Submissions
                .AnyAsync(s =>
                    s.Id == id &&
                    s.AssignedSectionEditorId == currentUserId);

            if (!submissionExists)
                return Forbid();

            return RedirectToAction(nameof(EditorDecision), new { id });
        }
        [Authorize(Roles = "Editor")]
        [HttpGet]
        public async Task<IActionResult> EditorDecision(int id)
        {
            var currentUserId = _userManager.GetUserId(User);

            var submission = await _context.Submissions
                .Include(s => s.Author)
                .Include(s => s.Authors)
                .Include(s => s.Files)
                .Include(s => s.Reviews)
                    .ThenInclude(r => r.Reviewer)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.AssignedSectionEditorId == currentUserId);

            if (submission == null)
                return NotFound();

            var revisionFiles = await _context.SubmissionFiles
                .Where(x =>
                    x.SubmissionId == id &&
                    x.FileType == "RevizyonDosyasi")
                .OrderByDescending(x => x.UploadedAt)
                .ToListAsync();

            var roundAssignments = await _context.SubmissionReviewers
                .Include(sr => sr.Reviewer)
                .Where(sr => sr.SubmissionId == id)
                .OrderBy(sr => sr.ReviewRound)
                .ThenByDescending(sr => sr.AssignedAt)
                .ToListAsync();

            var currentRoundAssignments = roundAssignments
                .Where(sr =>
                    sr.ReviewRound == submission.CurrentReviewRound &&
                    sr.Status != ReviewerAssignmentStatus.Cancelled &&
                    sr.Status != ReviewerAssignmentStatus.Declined)
                .ToList();

            var completedReviewerCount = currentRoundAssignments
                .Count(sr => sr.Status == ReviewerAssignmentStatus.Completed);

            var totalReviewerCount = currentRoundAssignments.Count;
            var completedReviews = submission.Reviews?
    .Where(r =>
        r.ReviewRound == submission.CurrentReviewRound &&
        !r.IsDraft &&
        !string.IsNullOrWhiteSpace(r.Decision))
    .ToList() ?? new List<Review>();

            bool hasPositiveDecision = completedReviews.Any(r =>
                r.Decision == "Accept" ||
                r.Decision == "Minor Revision");

            bool hasNegativeDecision = completedReviews.Any(r =>
                r.Decision == "Reject");

            bool hasMajorRevision = completedReviews.Any(r =>
                r.Decision == "Major Revision");

            bool hasConflictingReviews =
                completedReviews.Count >= 2 &&
                (
                    hasPositiveDecision && hasNegativeDecision
                    ||
                    hasNegativeDecision && hasMajorRevision
                );
            bool canAssignAdditionalReviewer =
                    hasConflictingReviews &&
                    totalReviewerCount < 3;

            ViewBag.HasConflictingReviews = hasConflictingReviews;

            var reviewers = await _userManager.GetUsersInRoleAsync("Reviewer");

            ViewBag.RevisionFiles = revisionFiles;
            ViewBag.CanAssignAdditionalReviewer = canAssignAdditionalReviewer;
            ViewBag.Reviewers = reviewers
                .Where(x => x.IsActive)
                .OrderBy(x => x.FullName ?? x.UserName ?? x.Email)
                .ToList();

            ViewBag.RoundAssignments = roundAssignments;

            ViewBag.TotalReviewerCount = totalReviewerCount;
            ViewBag.CompletedReviewerCount = completedReviewerCount;
            ViewBag.AllReviewsCompleted =
                totalReviewerCount > 0 &&
                completedReviewerCount == totalReviewerCount;

            return View(submission);
        }

        [Authorize(Roles = "Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditorDecision(int id, string decision, string? decisionNote)
        {
            var currentUserId = _userManager.GetUserId(User);

            var submission = await _context.Submissions
                .Include(s => s.Authors)
                .Include(s => s.Reviews)
                    .ThenInclude(r => r.Reviewer)
                .Include(s => s.Files)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.AssignedSectionEditorId == currentUserId);

            if (submission == null)
            {
                TempData["Error"] = "Bu makale size atanmadığı için karar veremezsiniz.";
                return RedirectToAction(nameof(EditorDashboard));
            }

            if (submission.Status == SubmissionStatus.KabulEdildi ||
                submission.Status == SubmissionStatus.Reddedildi ||
                submission.Status == SubmissionStatus.GeriCekildi)
            {
                TempData["Error"] = "Bu makale için nihai karar daha önce verilmiştir. Tekrar karar verilemez.";
                return RedirectToAction(nameof(EditorDashboard), new { tab = "archive" });
            }

            if (string.IsNullOrWhiteSpace(decision))
            {
                TempData["Error"] = "Lütfen karar seçiniz.";
                return RedirectToAction(nameof(EditorDecision), new { id });
            }

            submission.DecisionNote = string.IsNullOrWhiteSpace(decisionNote)
                ? ""
                : decisionNote.Trim();

            submission.DecisionByUserId = currentUserId;
            submission.DecisionDate = DateTime.UtcNow;
            submission.UpdatedAt = DateTime.UtcNow;

            string decisionText;

            switch (decision.ToLower())
            {
                case "revision":
                case "minor_revision":
                case "major_revision":
                case "minorrevision":
                case "majorrevision":
                case "minor":
                case "major":
                    submission.Status = SubmissionStatus.RevizyonIstendi;
                    decisionText = "Revizyon İstendi";
                    break;

                case "accept":
                case "accepted":
                case "kabul":
                    submission.Status = SubmissionStatus.KabulEdildi;
                    decisionText = "Kabul";
                    break;

                case "reject":
                case "rejected":
                case "red":
                    submission.Status = SubmissionStatus.Reddedildi;
                    decisionText = "Red";
                    break;

                default:
                    TempData["Error"] = $"Geçersiz karar: {decision}";
                    return RedirectToAction(nameof(EditorDecision), new { id });
            }

            await _context.SaveChangesAsync();

            var correspondingAuthor = submission.Authors
                .OrderByDescending(a => a.IsCorrespondingAuthor)
                .ThenBy(a => a.SortOrder)
                .FirstOrDefault();

            if (correspondingAuthor != null && !string.IsNullOrWhiteSpace(correspondingAuthor.Email))
            {
                var templatePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Templates",
                    "EditorDecisionEmailTemplate.html");

                string html;

                if (System.IO.File.Exists(templatePath))
                {
                    html = await System.IO.File.ReadAllTextAsync(templatePath);
                }
                else
                {
                    html = """
            <html>
            <body style="font-family:Arial,Helvetica,sans-serif; background:#f8f9fa; padding:20px;">
                <div style="max-width:760px; margin:auto; background:#fff; border:1px solid #e5e7eb; border-radius:12px; overflow:hidden;">
                    <div style="background:#0d6efd; color:#fff; padding:24px;">
                        <h2 style="margin:0;">Makale Kararı</h2>
                    </div>
                    <div style="padding:24px;">
                        <p>Sayın <strong>{{AuthorName}}</strong>,</p>
                        <p><strong>"{{SubmissionTitle}}"</strong> başlıklı makaleniz için editör kararı verilmiştir.</p>
                        <p><strong>Karar:</strong> {{Decision}}</p>
                        <p><strong>Değerlendirme Turu:</strong> {{ReviewRound}}</p>
                        <p><strong>Editör Notu:</strong> {{DecisionNote}}</p>
                        <h3>Hakem Görüşleri</h3>
                        {{ReviewerReportsHtml}}
                        <h3>Ek Hakem Dosyaları</h3>
                        {{AttachmentsHtml}}
                        {{RevisionInstructionHtml}}
                    </div>
                </div>
            </body>
            </html>
            """;
                }

                var completedReviews = submission.Reviews
                    .Where(r =>
                        !r.IsDraft &&
                        (
                            r.SubmittedAt != null ||
                            !string.IsNullOrWhiteSpace(r.Decision) ||
                            !string.IsNullOrWhiteSpace(r.CommentToAuthor) ||
                            !string.IsNullOrWhiteSpace(r.Strengths) ||
                            !string.IsNullOrWhiteSpace(r.Weaknesses)
                        ))
                    .OrderBy(r => r.ReviewRound <= 0 ? 1 : r.ReviewRound)
                    .ThenBy(r => r.SubmittedAt ?? r.UpdatedAt ?? r.CreatedAt)
                    .ToList();

                string HtmlEncode(string? value)
                {
                    return System.Net.WebUtility.HtmlEncode(
                        string.IsNullOrWhiteSpace(value) ? "-" : value.Trim()
                    );
                }

                var reviewerNumberByRound = new Dictionary<int, int>();
                var reviewerReportsBuilder = new System.Text.StringBuilder();

                foreach (var review in completedReviews)
                {
                    var reviewRound = review.ReviewRound <= 0 ? 1 : review.ReviewRound;

                    if (!reviewerNumberByRound.ContainsKey(reviewRound))
                        reviewerNumberByRound[reviewRound] = 0;

                    reviewerNumberByRound[reviewRound]++;

                    var anonymousReviewerName =
                        $"{reviewRound}. Tur Hakem {reviewerNumberByRound[reviewRound]}";

                    reviewerReportsBuilder.Append($"""
            <div style="background:#ffffff; border:1px solid #e5e7eb; border-radius:12px; padding:18px 20px; margin-bottom:16px;">
                <div style="margin:0 0 12px 0; font-size:16px; font-weight:700; color:#111827;">
                    {HtmlEncode(anonymousReviewerName)}
                </div>

                <div style="margin-bottom:14px;">
                    <div style="font-weight:700; margin-bottom:8px;">Yazara Yorum</div>
                    <div style="background:#f9fafb; border:1px solid #e5e7eb; border-radius:10px; padding:12px 14px; line-height:1.7;">
                        {HtmlEncode(review.CommentToAuthor)}
                    </div>
                </div>

                <div style="margin-bottom:14px;">
                    <div style="font-weight:700; margin-bottom:8px;">Güçlü Yönler</div>
                    <div style="background:#f9fafb; border:1px solid #e5e7eb; border-radius:10px; padding:12px 14px; line-height:1.7;">
                        {HtmlEncode(review.Strengths)}
                    </div>
                </div>

                <div>
                    <div style="font-weight:700; margin-bottom:8px;">Geliştirilmesi Gereken Yönler</div>
                    <div style="background:#f9fafb; border:1px solid #e5e7eb; border-radius:10px; padding:12px 14px; line-height:1.7;">
                        {HtmlEncode(review.Weaknesses)}
                    </div>
                </div>
            </div>
            """);
                }

                var reviewerReportsHtml = reviewerReportsBuilder.Length > 0
                    ? reviewerReportsBuilder.ToString()
                    : """
              <div style="background:#f9fafb; border:1px solid #e5e7eb; border-radius:12px; padding:18px 20px;">
                  Hakem görüşü bulunmamaktadır.
              </div>
              """;

                var attachmentReviews = completedReviews
                    .Where(r =>
                        r.SendAttachmentToAuthor &&
                        !string.IsNullOrWhiteSpace(r.ReviewerAttachmentPath))
                    .OrderBy(r => r.ReviewRound <= 0 ? 1 : r.ReviewRound)
                    .ThenBy(r => r.SubmittedAt ?? r.UpdatedAt ?? r.CreatedAt)
                    .ToList();

                var attachmentNumberByRound = new Dictionary<int, int>();
                var attachmentsBuilder = new System.Text.StringBuilder();

                foreach (var review in attachmentReviews)
                {
                    var reviewRound = review.ReviewRound <= 0 ? 1 : review.ReviewRound;

                    if (!attachmentNumberByRound.ContainsKey(reviewRound))
                        attachmentNumberByRound[reviewRound] = 0;

                    attachmentNumberByRound[reviewRound]++;

                    var anonymousReviewerName =
                        $"{reviewRound}. Tur Hakem {attachmentNumberByRound[reviewRound]}";

                    var reviewerAttachmentName = !string.IsNullOrWhiteSpace(review.ReviewerAttachmentOriginalFileName)
                        ? review.ReviewerAttachmentOriginalFileName
                        : Path.GetFileName(review.ReviewerAttachmentPath ?? "");

                    var reviewerAttachmentNote = !string.IsNullOrWhiteSpace(review.ReviewerAttachmentNote)
                        ? review.ReviewerAttachmentNote
                        : "Açıklama bulunmamaktadır.";

                    attachmentsBuilder.Append($"""
            <div style="background:#f9fafb; border:1px solid #e5e7eb; border-radius:12px; padding:18px 20px; margin-bottom:12px;">
                <div style="margin:0 0 8px 0; font-size:15px;">
                    <strong>{HtmlEncode(anonymousReviewerName)} Ek Dosyası:</strong> {HtmlEncode(reviewerAttachmentName)}
                </div>

                <div style="margin:0 0 8px 0; font-size:15px;">
                    <strong>Açıklama:</strong> {HtmlEncode(reviewerAttachmentNote)}
                </div>

                <div style="margin:0; font-size:15px; line-height:1.7;">
                    Hakem tarafından yazara iletilmesine izin verilen ek dosya sistemde mevcuttur.
                </div>
            </div>
            """);
                }

                var attachmentsHtml = attachmentsBuilder.Length > 0
                    ? attachmentsBuilder.ToString()
                    : """
              <div style="background:#f9fafb; border:1px solid #e5e7eb; border-radius:12px; padding:18px 20px;">
                  Yazara iletilecek hakem ek dosyası bulunmamaktadır.
              </div>
              """;

                var revisionInstructionHtml = submission.Status == SubmissionStatus.RevizyonIstendi
                    ? """
              <div style="background:#fffbea; border:1px solid #f6e7b0; border-radius:12px; padding:16px 18px; margin:0 0 24px 0; line-height:1.7;">
                  Lütfen editör notu ve hakem görüşleri doğrultusunda gerekli düzenlemeleri yaparak sistem üzerinden güncel dosyalarınızı yükleyiniz.
              </div>
              """
                    : "";

                html = html.Replace("{{AuthorName}}",
                    System.Net.WebUtility.HtmlEncode(correspondingAuthor.FullName ?? "-"));

                html = html.Replace("{{SubmissionTitle}}",
                    System.Net.WebUtility.HtmlEncode(submission.Title ?? "-"));

                html = html.Replace("{{Decision}}",
                    System.Net.WebUtility.HtmlEncode(decisionText));

                html = html.Replace("{{ReviewRound}}",
                    System.Net.WebUtility.HtmlEncode($"{submission.CurrentReviewRound}. Tur"));

                html = html.Replace("{{DecisionNote}}",
                    string.IsNullOrWhiteSpace(submission.DecisionNote)
                        ? "Editör notu eklenmemiştir."
                        : System.Net.WebUtility.HtmlEncode(submission.DecisionNote));

                html = html.Replace("{{ReviewerReportsHtml}}", reviewerReportsHtml);
                html = html.Replace("{{AttachmentsHtml}}", attachmentsHtml);
                html = html.Replace("{{RevisionInstructionHtml}}", revisionInstructionHtml);

                var roundText = $"{submission.CurrentReviewRound}. Tur";

                var subject = submission.Status switch
                {
                    SubmissionStatus.RevizyonIstendi => $"Makaleniz İçin Revizyon Kararı - {roundText} #{submission.Id}",
                    SubmissionStatus.KabulEdildi => $"Makaleniz Kabul Edildi - {roundText} #{submission.Id}",
                    SubmissionStatus.Reddedildi => $"Makaleniz Reddedildi - {roundText} #{submission.Id}",
                    _ => $"Makale Kararı - {roundText} #{submission.Id}"
                };

                await _emailService.SendEmailAsync(
                    correspondingAuthor.Email,
                    subject,
                    html);
            }

            TempData["Success"] = "Editör kararı kaydedildi ve yazara bildirildi.";
            return RedirectToAction(nameof(EditorDashboard));
        }
        [Authorize(Roles = "Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendReviewerReminder(int assignmentId)
        {
            var currentUserId = _userManager.GetUserId(User);

            var assignment = await _context.SubmissionReviewers
                .Include(sr => sr.Submission)
                .Include(sr => sr.Reviewer)
                .FirstOrDefaultAsync(sr => sr.Id == assignmentId);

            if (assignment == null)
                return NotFound();

            if (assignment.Submission == null)
                return NotFound();

            if (assignment.Submission.AssignedSectionEditorId != currentUserId)
                return Forbid();

            if (assignment.Status == ReviewerAssignmentStatus.Completed ||
                assignment.Status == ReviewerAssignmentStatus.Cancelled ||
                assignment.Status == ReviewerAssignmentStatus.Declined)
            {
                TempData["Error"] = "Bu hakem ataması için hatırlatma gönderilemez.";
                return RedirectToAction(nameof(EditorDecision), new { id = assignment.SubmissionId });
            }

            if (assignment.Reviewer == null || string.IsNullOrWhiteSpace(assignment.Reviewer.Email))
            {
                TempData["Error"] = "Hakemin e-posta adresi bulunamadı.";
                return RedirectToAction(nameof(EditorDecision), new { id = assignment.SubmissionId });
            }

            var reviewUrl = Url.Action(
                "Review",
                "Reviewer",
                new { id = assignment.SubmissionId },
                protocol: Request.Scheme);

            var dueDateText = assignment.DueDate.HasValue
                ? assignment.DueDate.Value.ToLocalTime().ToString("dd.MM.yyyy")
                : "Belirtilmemiş";

            var subject = $"Hakem Değerlendirme Hatırlatması - {assignment.ReviewRound}. Tur #{assignment.SubmissionId}";

            var body = $@"
        <p>Sayın {assignment.Reviewer.FullName ?? assignment.Reviewer.UserName},</p>

        <p>
            Size atanmış olan <strong>#{assignment.Submission.Id}</strong> numaralı 
            <strong>{assignment.Submission.Title}</strong> başlıklı makale için değerlendirme süreci devam etmektedir.
        </p>

        <p>
            <strong>Değerlendirme Turu:</strong> {assignment.ReviewRound}. Tur<br>
            <strong>Son Tarih:</strong> {dueDateText}
        </p>

        <p>
            Değerlendirme ekranına ulaşmak için aşağıdaki bağlantıyı kullanabilirsiniz:
        </p>

        <p>
            <a href='{reviewUrl}'>{reviewUrl}</a>
        </p>

        <p>İyi çalışmalar.</p>
    ";

            try
            {
                await _emailService.SendEmailAsync(
                    assignment.Reviewer.Email,
                    subject,
                    body);

                assignment.ReminderSentAt = DateTime.UtcNow;
                assignment.ReminderCount += 1;

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Hatırlatma e-postası gönderildi: {assignment.Reviewer.Email}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Mail gönderilemedi: {ex.Message}";
            }

            return RedirectToAction(nameof(EditorDecision), new { id = assignment.SubmissionId });
        }
        [Authorize(Roles = "Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReviewerAssignment(int assignmentId, string? reason)
        {
            var currentUserId = _userManager.GetUserId(User);

            var assignment = await _context.SubmissionReviewers
                .Include(sr => sr.Submission)
                .FirstOrDefaultAsync(sr => sr.Id == assignmentId);

            if (assignment == null)
                return NotFound();

            if (assignment.Submission == null)
                return NotFound();

            if (assignment.Submission.AssignedSectionEditorId != currentUserId)
                return Forbid();

            if (assignment.Status == ReviewerAssignmentStatus.Completed)
            {
                TempData["Error"] = "Tamamlanmış hakem değerlendirmesi iptal edilemez.";
                return RedirectToAction(nameof(EditorDecision), new { id = assignment.SubmissionId });
            }

            assignment.Status = ReviewerAssignmentStatus.Cancelled;
            assignment.CancelledAt = DateTime.UtcNow;
            assignment.CancelledByUserId = currentUserId;
            assignment.CancelReason = string.IsNullOrWhiteSpace(reason)
                ? "Editör tarafından iptal edildi."
                : reason.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Hakem ataması iptal edildi. Yeni hakem atayabilirsiniz.";

            return RedirectToAction(nameof(EditorDecision), new { id = assignment.SubmissionId });
        }
        [Authorize(Roles = "Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartReviewRound(int id)
        {
            var currentUserId = _userManager.GetUserId(User);

            var submission = await _context.Submissions
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.AssignedSectionEditorId == currentUserId);

            if (submission == null)
                return NotFound();

            if (submission.Status != SubmissionStatus.RevizyonYuklendi)
            {
                TempData["Error"] = "Yeni değerlendirme turu yalnızca yazar revizyon dosyasını yükledikten sonra başlatılabilir.";
                return RedirectToAction(nameof(EditorDecision), new { id });
            }

            submission.Status = SubmissionStatus.HakemAtamasiBekliyor;
            submission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"{submission.CurrentReviewRound}. tur değerlendirme süreci başlatıldı. Hakem ataması yapabilirsiniz.";

            return RedirectToAction(nameof(EditorDecision), new { id });
        }

        [Authorize(Roles = "Reviewer")]
        [HttpGet]
        public async Task<IActionResult> MyReviews()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var activeAssignments = await _context.SubmissionReviewers
                .Include(sr => sr.Submission)
                .Where(sr =>
                    sr.ReviewerId == user.Id &&
                    sr.Submission != null &&
                    sr.ReviewRound == sr.Submission.CurrentReviewRound &&
                    sr.Status != ReviewerAssignmentStatus.Cancelled &&
                    sr.Status != ReviewerAssignmentStatus.Declined)
                .OrderByDescending(sr => sr.AssignedAt)
                .ToListAsync();

            var submissionIds = activeAssignments
                .Select(sr => sr.SubmissionId)
                .Distinct()
                .ToList();

            var submissions = await _context.Submissions
                .Include(s => s.Reviews)
                .Where(s => submissionIds.Contains(s.Id))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            var historyAssignments = await _context.SubmissionReviewers
                .Include(sr => sr.Submission)
                .Where(sr =>
                    sr.ReviewerId == user.Id &&
                    sr.Submission != null &&
                    sr.ReviewRound < sr.Submission.CurrentReviewRound &&
                    sr.Status == ReviewerAssignmentStatus.Completed)
                .OrderByDescending(sr => sr.CompletedAt)
                .ToListAsync();

            ViewBag.HistoryAssignments = historyAssignments;
            ViewBag.TotalCount = activeAssignments.Count;

            ViewBag.CompletedCount = activeAssignments
                .Count(x => x.Status == ReviewerAssignmentStatus.Completed);

            ViewBag.DraftCount = activeAssignments
                .Count(x => x.Status == ReviewerAssignmentStatus.InReview);

            ViewBag.PendingCount = activeAssignments
                .Count(x => x.Status == ReviewerAssignmentStatus.Assigned);

            return View(submissions);
        }

        [Authorize(Roles = "Author")]
        [HttpGet]
        public async Task<IActionResult> Makalelerim()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var submissions = await _context.Submissions
                .Include(x => x.Authors)
                .Where(x =>
                    x.AuthorId == user.Id ||
                    x.Authors.Any(a => a.Email == user.Email))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(submissions);
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpGet]
        public async Task<IActionResult> AdminList()
        {
            var submissions = await _context.Submissions
                .Include(s => s.Author)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(submissions);
        }

        [Authorize(Roles = "Author")]
        [HttpGet]
        public IActionResult YeniMakale()
        {
            return View(new CreateSubmissionViewModel
            {
                Authors = new List<SubmissionAuthorInputViewModel>
                {
                    new SubmissionAuthorInputViewModel
                    {
                        Role = "Yazar",
                        IsCorrespondingAuthor = true
                    }
                }
            });
        }

        [Authorize(Roles = "Author")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YeniMakale(CreateSubmissionViewModel model)
        {
            if (model.Authors == null || !model.Authors.Any())
            {
                model.Authors = new List<SubmissionAuthorInputViewModel>
                {
                    new SubmissionAuthorInputViewModel()
                };
            }

            var validAuthors = model.Authors
                .Where(a => !string.IsNullOrWhiteSpace(a.FullName) && !string.IsNullOrWhiteSpace(a.Email))
                .ToList();

            if (!validAuthors.Any())
            {
                ModelState.AddModelError(string.Empty, "En az bir yazar bilgisi girilmelidir.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Formda eksik veya hatalı alanlar var. Lütfen kontrol ediniz.";
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var submission = new Submission
            {
                Prefix = string.IsNullOrWhiteSpace(model.Prefix) ? null : model.Prefix.Trim(),
                Title = model.Title.Trim(),
                Subtitle = string.IsNullOrWhiteSpace(model.Subtitle) ? null : model.Subtitle.Trim(),
                Abstract = model.Abstract.Trim(),
                Keywords = model.Keywords?.Trim() ?? string.Empty,
                ReferencesText = string.IsNullOrWhiteSpace(model.ReferencesText) ? null : model.ReferencesText.Trim(),
                CoverLetter = string.IsNullOrWhiteSpace(model.CoverLetter) ? null : model.CoverLetter.Trim(),
                AuthorId = user.Id,
                Status = SubmissionStatus.OnKontrolBekliyor,
                CurrentReviewRound = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            for (int i = 0; i < validAuthors.Count; i++)
            {
                var item = validAuthors[i];

                _context.SubmissionAuthors.Add(new SubmissionAuthor
                {
                    SubmissionId = submission.Id,
                    FullName = item.FullName.Trim(),
                    Email = item.Email.Trim(),
                    Institution = string.IsNullOrWhiteSpace(item.Institution) ? null : item.Institution.Trim(),
                    Orcid = string.IsNullOrWhiteSpace(item.Orcid) ? null : item.Orcid.Trim(),
                    Role = string.IsNullOrWhiteSpace(item.Role) ? "Yazar" : item.Role.Trim(),
                    IsCorrespondingAuthor = item.IsCorrespondingAuthor,
                    SortOrder = i + 1
                });
            }

            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "submissions");
            var newRound = submission.CurrentReviewRound + 1;

            await SaveSubmissionFileAsync(submission.Id, model.MainManuscriptFile, "MakaleDosyasi", Path.Combine(root, "main"), user.Id);
            await SaveSubmissionFileAsync(submission.Id, model.CoverLetterFile, "KapakYazisi", Path.Combine(root, "cover-letter"), user.Id);
            await SaveSubmissionFileAsync(submission.Id, model.EthicsApprovalFile, "EtikKurulBelgesi", Path.Combine(root, "ethics"), user.Id);
            await SaveSubmissionFileAsync(submission.Id, model.CopyrightTransferFile, "TelifDevirFormu", Path.Combine(root, "copyright"), user.Id);
            await SaveSubmissionFileAsync(submission.Id, model.SimilarityReportFile, "BenzerlikRaporu", Path.Combine(root, "similarity"), user.Id);

            if (model.SupplementaryFiles != null)
            {
                foreach (var file in model.SupplementaryFiles.Where(f => f != null && f.Length > 0))
                {
                    await SaveSubmissionFileAsync(submission.Id, file, "EkDosya", Path.Combine(root, "supplementary"), user.Id);
                }
            }

            await _context.SaveChangesAsync();

            var mainFile = await _context.SubmissionFiles
                .Where(x => x.SubmissionId == submission.Id && x.FileType == "MakaleDosyasi")
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (mainFile != null)
            {
                submission.FilePath = mainFile.StoredFilePath;
            }

            await _context.SaveChangesAsync();

            var savedAuthors = await _context.SubmissionAuthors
                .Where(x => x.SubmissionId == submission.Id)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();

            await NotifyChiefEditorsForNewSubmissionAsync(submission, savedAuthors);

            TempData["Success"] = "Makaleniz başarıyla gönderildi ve ön kontrole alındı.";
            return RedirectToAction(nameof(Makalelerim));
        }

        [Authorize(Roles = "Author")]
        [HttpGet]
        public async Task<IActionResult> UploadRevision(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var submission = await _context.Submissions
                .Include(x => x.Authors)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    (
                        x.AuthorId == user.Id ||
                        x.Authors.Any(a => a.Email == user.Email)
                    ));

            if (submission == null)
                return Forbid();

            if (submission.Status != SubmissionStatus.RevizyonIstendi)
            {
                TempData["Error"] = "Bu makale için şu anda revizyon yüklenemez.";
                return RedirectToAction(nameof(Makalelerim));
            }

            var model = new UploadRevisionViewModel
            {
                SubmissionId = submission.Id,
                SubmissionTitle = submission.Title
            };

            return View(model);
        }

            

        [Authorize(Roles = "Author")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadRevision(UploadRevisionViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

                var submission = await _context.Submissions
            .Include(x => x.Authors)
            .FirstOrDefaultAsync(x =>
            x.Id == model.SubmissionId &&
            (
                x.AuthorId == user.Id ||
                x.Authors.Any(a => a.Email == user.Email)
            ));

                if (submission == null)
                            return NotFound();

                        if (submission.Status != SubmissionStatus.RevizyonIstendi)
                        {
                            TempData["Error"] = "Bu makale için şu anda revizyon yüklenemez.";
                            return RedirectToAction(nameof(Makalelerim));
                        }

                        if (!ModelState.IsValid)
                        {
                            model.SubmissionTitle = submission.Title;
                            return View(model);
                        }

            if (model.RevisionFile == null || model.RevisionFile.Length == 0)
            {
                ModelState.AddModelError("RevisionFile", "Lütfen geçerli bir dosya yükleyiniz.");
                model.SubmissionTitle = submission.Title;
                return View(model);
            }

            var root = Path.Combine(
                  Directory.GetCurrentDirectory(),
                  "wwwroot",
                  "uploads",
                  "submissions"
              );

            var newRound = submission.CurrentReviewRound + 1;

            await SaveSubmissionFileAsync(
                submission.Id,
                model.RevisionFile,
                "RevizyonDosyasi",
                Path.Combine(root, "revisions"),
                user.Id,
                newRound
            );

            await _context.SaveChangesAsync();

            var latestRevisionFile = await _context.SubmissionFiles
                .Where(x => x.SubmissionId == submission.Id && x.FileType == "RevizyonDosyasi")
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (latestRevisionFile != null)
            {
                submission.FilePath = latestRevisionFile.StoredFilePath;
            }

            submission.CurrentReviewRound = newRound;
            submission.Status = SubmissionStatus.RevizyonYuklendi;
            submission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Revizyon dosyanız başarıyla yüklendi.";

            return RedirectToAction(nameof(Makalelerim));
        }
        [Authorize(Roles = "ChiefEditor")]
        [HttpGet]
        public async Task<IActionResult> OnKontrolListesi()
        {
            var allowedStatuses = new[]
            {
        SubmissionStatus.Gonderildi,
        SubmissionStatus.OnKontrolBekliyor
    };

            var submissionsRaw = await _context.Submissions
                .Include(s => s.Authors)
                .Include(s => s.Files)
                .Where(s => allowedStatuses.Contains(s.Status))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var submissions = submissionsRaw
                .Select(s => new OnKontrolListeItemViewModel
                {
                    Id = s.Id,
                    Title = s.Title,
                    Subtitle = s.Subtitle,

                    Status = StatusDisplayHelper.GetSubmissionStatusText(s.Status),

                    CreatedAt = s.CreatedAt,

                    CorrespondingAuthorName = s.Authors
                        .OrderByDescending(a => a.IsCorrespondingAuthor)
                        .ThenBy(a => a.SortOrder)
                        .Select(a => a.FullName)
                        .FirstOrDefault(),

                    CorrespondingAuthorEmail = s.Authors
                        .OrderByDescending(a => a.IsCorrespondingAuthor)
                        .ThenBy(a => a.SortOrder)
                        .Select(a => a.Email)
                        .FirstOrDefault(),

                    AuthorCount = s.Authors.Count,
                    FileCount = s.Files.Count
                })
                .ToList();

            return View(submissions);
        }
        [Authorize(Roles = "ChiefEditor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YazaraIadeEt(int id, string? reason)
        {
            var submission = await _context.Submissions
                .Include(s => s.Authors)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
                return NotFound();

            if (submission.Status != SubmissionStatus.Gonderildi &&
                submission.Status != SubmissionStatus.OnKontrolBekliyor)
            {
                TempData["Error"] = "Bu makale ön kontrol aşamasında değildir. Yazara iade edilemez.";
                return RedirectToAction(nameof(OnKontrolListesi));
            }

            submission.Status = SubmissionStatus.YazaraIadeEdildi;
            submission.DecisionNote = string.IsNullOrWhiteSpace(reason)
                ? "Ön kontrol sonrası yazara iade edildi."
                : reason.Trim();

            submission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var targetEmail = submission.Authors
                .OrderByDescending(a => a.IsCorrespondingAuthor)
                .ThenBy(a => a.SortOrder)
                .Select(a => a.Email)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(targetEmail))
            {
                var safeTitle = System.Net.WebUtility.HtmlEncode(submission.Title ?? "-");
                var safeNote = System.Net.WebUtility.HtmlEncode(submission.DecisionNote ?? "-");

                var body = $"""
        <div style="font-family:Arial,Helvetica,sans-serif; line-height:1.7;">
            <p>Sayın Yazar,</p>

            <p>
                <strong>#{submission.Id}</strong> numaralı
                <strong>"{safeTitle}"</strong> başlıklı makaleniz ön kontrol aşamasında tarafınıza iade edilmiştir.
            </p>

            <p><strong>Açıklama:</strong></p>
            <div style="background:#f8fafc; border:1px solid #e5e7eb; border-radius:10px; padding:12px 14px;">
                {safeNote}
            </div>

            <p>Lütfen gerekli düzenlemeleri yaparak sistemi tekrar kullanınız.</p>

            <p>İyi çalışmalar.</p>
        </div>
        """;

                await _emailService.SendEmailAsync(
                    targetEmail,
                    $"Makaleniz Yazara İade Edildi #{submission.Id}",
                    body);
            }

            TempData["Success"] = "Makale yazara iade edildi.";
            return RedirectToAction(nameof(OnKontrolListesi));
        }

        [Authorize(Roles = "ChiefEditor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlanEditoruneYonlendir(int id, string sectionEditorId)
        {
            var submission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
                return NotFound();

            if (submission.Status != SubmissionStatus.Gonderildi &&
                submission.Status != SubmissionStatus.OnKontrolBekliyor)
            {
                TempData["Error"] = "Bu makale ön kontrol aşamasında değildir. Alan editörüne tekrar yönlendirilemez.";
                return RedirectToAction(nameof(OnKontrolListesi));
            }

            if (string.IsNullOrWhiteSpace(sectionEditorId))
            {
                TempData["Error"] = "Alan editörü seçmelisiniz.";
                return RedirectToAction(nameof(OnKontrolListesi));
            }

            var editor = await _userManager.FindByIdAsync(sectionEditorId);

            if (editor == null)
            {
                TempData["Error"] = "Seçilen alan editörü bulunamadı.";
                return RedirectToAction(nameof(OnKontrolListesi));
            }

            var isEditor = await _userManager.IsInRoleAsync(editor, "Editor");

            if (!isEditor)
            {
                TempData["Error"] = "Seçilen kullanıcı alan editörü rolünde değildir.";
                return RedirectToAction(nameof(OnKontrolListesi));
            }

            submission.AssignedSectionEditorId = sectionEditorId;
            submission.Status = SubmissionStatus.HakemAtamasiBekliyor;

            if (submission.CurrentReviewRound <= 0)
            {
                submission.CurrentReviewRound = 1;
            }

            submission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(editor.Email))
            {
                var detailUrl = Url.Action(
                    "EditorDecision",
                    "Submission",
                    new { id = submission.Id },
                    protocol: Request.Scheme);

                var safeTitle = System.Net.WebUtility.HtmlEncode(submission.Title ?? "-");

                var body = $"""
        <div style="font-family:Arial,Helvetica,sans-serif; line-height:1.7;">
            <p>Sayın Alan Editörü,</p>

            <p>
                <strong>#{submission.Id}</strong> numaralı
                <strong>"{safeTitle}"</strong> başlıklı makale tarafınıza yönlendirilmiştir.
            </p>

            <p>
                Makale sürecini yönetmek için aşağıdaki bağlantıyı kullanabilirsiniz:
            </p>

            <p>
                <a href="{detailUrl}">{detailUrl}</a>
            </p>

            <p>İyi çalışmalar.</p>
        </div>
        """;

                await _emailService.SendEmailAsync(
                    editor.Email,
                    $"Yeni Makale Ataması #{submission.Id}",
                    body);
            }

            TempData["Success"] = "Makale alan editörüne yönlendirildi.";
            return RedirectToAction(nameof(OnKontrolListesi));
        }

        [Authorize(Roles = "ChiefEditor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reddet(int id, string? reason)
        {
            var submission = await _context.Submissions
                .Include(s => s.Authors)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
                return NotFound();

            if (submission.Status != SubmissionStatus.Gonderildi &&
                submission.Status != SubmissionStatus.OnKontrolBekliyor)
            {
                TempData["Error"] = "Bu makale ön kontrol aşamasında değildir. Reddedilemez.";
                return RedirectToAction(nameof(OnKontrolListesi));
            }

            submission.Status = SubmissionStatus.Reddedildi;
            submission.DecisionNote = string.IsNullOrWhiteSpace(reason)
                ? "Ön kontrol aşamasında reddedildi."
                : reason.Trim();

            submission.DecisionDate = DateTime.UtcNow;
            submission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var targetEmail = submission.Authors
                .OrderByDescending(a => a.IsCorrespondingAuthor)
                .ThenBy(a => a.SortOrder)
                .Select(a => a.Email)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(targetEmail))
            {
                var safeTitle = System.Net.WebUtility.HtmlEncode(submission.Title ?? "-");
                var safeNote = System.Net.WebUtility.HtmlEncode(submission.DecisionNote ?? "-");

                var body = $"""
        <div style="font-family:Arial,Helvetica,sans-serif; line-height:1.7;">
            <p>Sayın Yazar,</p>

            <p>
                <strong>#{submission.Id}</strong> numaralı
                <strong>"{safeTitle}"</strong> başlıklı makaleniz ön kontrol aşamasında reddedilmiştir.
            </p>

            <p><strong>Açıklama:</strong></p>
            <div style="background:#f8fafc; border:1px solid #e5e7eb; border-radius:10px; padding:12px 14px;">
                {safeNote}
            </div>

            <p>İyi çalışmalar.</p>
        </div>
        """;

                await _emailService.SendEmailAsync(
                    targetEmail,
                    $"Makaleniz Reddedildi #{submission.Id}",
                    body);
            }

            TempData["Success"] = "Makale reddedildi.";
            return RedirectToAction(nameof(OnKontrolListesi));
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            var currentUserEmail = User.Identity?.Name;

            var file = await _context.SubmissionFiles
                .Include(f => f.Submission)
                    .ThenInclude(s => s!.Authors)
                .Include(f => f.Submission)
                    .ThenInclude(s => s!.SubmissionReviewers)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (file == null)
                return NotFound();
            
            if (file.Submission == null)
                return NotFound();

            var submission = file.Submission;

            var isAdmin = User.IsInRole("Admin");

            var isChiefEditor = User.IsInRole("ChiefEditor");

            var isAssignedEditor =
                User.IsInRole("Editor") &&
                submission.AssignedSectionEditorId == currentUserId;

            var isAuthor =
                User.IsInRole("Author") &&
                (
                    submission.AuthorId == currentUserId ||
                    submission.Authors.Any(a => a.Email == currentUserEmail)
                );

            var isAssignedReviewer =
                User.IsInRole("Reviewer") &&
                submission.SubmissionReviewers.Any(sr =>
                    sr.ReviewerId == currentUserId &&
                    sr.Status != ReviewerAssignmentStatus.Cancelled &&
                    sr.Status != ReviewerAssignmentStatus.Declined);

            var canDownload = false;

            if (isAdmin)
            {
                canDownload = true;
            }
            else if (isChiefEditor)
            {
                canDownload = true;
            }
            else if (isAssignedEditor)
            {
                canDownload = true;
            }
            else if (isAssignedReviewer)
            {
                // Hakem sadece değerlendireceği makale dosyalarını görebilsin.
                // Hakem ek dosyalarını veya başka hakem dosyalarını görmesin.
                canDownload =
                    file.FileType != "HakemEkDosyasi" &&
                    file.FileType != "ReviewerAttachment";
            }
            else if (isAuthor)
            {
                // Yazar hakem ek dosyasını normalde göremez.
                // Ancak yazara iletilebilir işaretli hakem ek dosyasıysa görebilir.
                if (file.FileType == "HakemEkDosyasi" || file.FileType == "ReviewerAttachment")
                {
                    canDownload = false;
                }
                else
                {
                    canDownload = true;
                }
            }

            if (!canDownload)
                return Forbid();

            if (string.IsNullOrWhiteSpace(file.StoredFilePath))
                return NotFound();

            var relativePath = file.StoredFilePath
                .Replace("\\", "/")
                .TrimStart('/');

            var physicalPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                relativePath);

            if (!System.IO.File.Exists(physicalPath))
                return NotFound();

            var fileName = string.IsNullOrWhiteSpace(file.OriginalFileName)
                ? Path.GetFileName(physicalPath)
                : file.OriginalFileName;

            var contentType = GetContentType(fileName);

            return PhysicalFile(physicalPath, contentType, fileName);
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> DownloadPublishedFile(int id)
        {
            var file = await _context.SubmissionFiles
                .Include(f => f.Submission)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (file == null)
                return NotFound();

            var submission = file.Submission;

            if (submission == null)
                return NotFound();

            var isPublished = await _context.PublishedArticles
                .Include(pa => pa.Issue)
                .AnyAsync(pa =>
                    pa.SubmissionId == submission.Id &&
                    pa.Issue != null &&
                    pa.Issue.IsPublished);

            if (!isPublished)
                return Forbid();

            if (file.FileType == "HakemEkDosyasi" ||
                file.FileType == "ReviewerAttachment")
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(file.StoredFilePath))
                return NotFound();

            var relativePath = file.StoredFilePath
                .Replace("\\", "/")
                .TrimStart('/');

            var physicalPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                relativePath);

            if (!System.IO.File.Exists(physicalPath))
                return NotFound();

            var fileName = string.IsNullOrWhiteSpace(file.OriginalFileName)
                ? Path.GetFileName(physicalPath)
                : file.OriginalFileName;

            var contentType = GetContentType(fileName);

            return PhysicalFile(physicalPath, contentType, fileName);
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Detail(int id, string? viewMode = null)
        {
            var currentUserId = _userManager.GetUserId(User);
            var currentUserEmail = User.Identity?.Name;

            var submission = await _context.Submissions
                .Include(s => s.Authors)
                .Include(s => s.Files)
                .Include(s => s.Reviews)
                    .ThenInclude(r => r.Reviewer)
                .Include(s => s.SubmissionReviewers)
                    .ThenInclude(sr => sr.Reviewer)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
                return NotFound();

            var renderAsAuthor = viewMode == "author";

            var isAdmin = User.IsInRole("Admin");
            var isChiefEditor = User.IsInRole("ChiefEditor");

            var isAssignedEditor =
                User.IsInRole("Editor") &&
                submission.AssignedSectionEditorId == currentUserId;

            var isOwnAuthorSubmission =
                submission.AuthorId == currentUserId ||
                submission.Authors.Any(a => a.Email == currentUserEmail);

            var isAssignedReviewer =
                User.IsInRole("Reviewer") &&
                submission.SubmissionReviewers.Any(sr =>
                    sr.ReviewerId == currentUserId &&
                    sr.Status != ReviewerAssignmentStatus.Cancelled &&
                    sr.Status != ReviewerAssignmentStatus.Declined);

            if (renderAsAuthor)
            {
                if (!isOwnAuthorSubmission)
                    return Forbid();

                ViewBag.RenderAsAuthor = true;
            }
            else
            {
                var canView =
                    isAdmin ||
                    isChiefEditor ||
                    isAssignedEditor ||
                    isOwnAuthorSubmission ||
                    isAssignedReviewer;

                if (!canView)
                    return Forbid();

                ViewBag.RenderAsAuthor = false;
            }

            var model = new SubmissionDetailViewModel
            {
                Id = submission.Id,
                Prefix = submission.Prefix,
                Title = submission.Title,
                Subtitle = submission.Subtitle,
                Abstract = submission.Abstract,
                Keywords = submission.Keywords,
                ReferencesText = submission.ReferencesText,
                CoverLetter = submission.CoverLetter,
                Status = submission.Status,
                CreatedAt = submission.CreatedAt,
                UpdatedAt = submission.UpdatedAt,

                AuthorName = submission.Authors
                    .OrderByDescending(a => a.IsCorrespondingAuthor)
                    .ThenBy(a => a.SortOrder)
                    .Select(a => a.FullName)
                    .FirstOrDefault(),

                AuthorEmail = submission.Authors
                    .OrderByDescending(a => a.IsCorrespondingAuthor)
                    .ThenBy(a => a.SortOrder)
                    .Select(a => a.Email)
                    .FirstOrDefault(),

                Authors = submission.Authors
                    .OrderBy(a => a.SortOrder)
                    .ToList(),

                Files = submission.Files
                    .OrderByDescending(f => f.UploadedAt)
                    .ToList(),

                Reviews = submission.Reviews
                    .OrderByDescending(r => r.CreatedAt)
                    .ToList(),

                Reviewers = submission.SubmissionReviewers
                    .OrderByDescending(sr => sr.AssignedAt)
                    .Select(sr => new ReviewerAssignmentListItemViewModel
                    {
                        AssignmentId = sr.Id,

                        ReviewerId = sr.ReviewerId,

                        ReviewerName = sr.Reviewer != null
                            ? (sr.Reviewer.FullName ?? sr.Reviewer.UserName ?? sr.Reviewer.Email ?? "Hakem")
                            : "Hakem",

                        ReviewerEmail = sr.Reviewer != null
                            ? (sr.Reviewer.Email ?? "-")
                            : "-",

                        Status = sr.Status.ToString(),

                        AssignedAt = sr.AssignedAt,

                        CompletedAt = sr.CompletedAt,

                        ReviewNote = sr.ReviewNote
                    })
                    .ToList()
            };

            return View(model);
        }
        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".zip" => "application/zip",
                ".rar" => "application/vnd.rar",
                _ => "application/octet-stream"
            };
        }
    }
}