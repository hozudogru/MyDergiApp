using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Entities;
using MyDergiApp.Models;

namespace MyDergiApp.Controllers
{
    [Authorize(Roles = "Reviewer")]
    public class ReviewerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly EmailService _emailService;

        public ReviewerController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
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

            ViewBag.TotalCount = activeAssignments.Count;

            ViewBag.CompletedCount = activeAssignments
                .Count(x => x.Status == ReviewerAssignmentStatus.Completed);

            ViewBag.DraftCount = activeAssignments
                .Count(x => x.Status == ReviewerAssignmentStatus.InReview);

            ViewBag.PendingCount = activeAssignments
                .Count(x => x.Status == ReviewerAssignmentStatus.Assigned);

            return View(submissions);
        }

        [HttpGet]
        public async Task<IActionResult> Review(int id, int? round = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var submission = await _context.Submissions
                .Include(s => s.Reviews)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
                return NotFound();

            var targetRound = round ?? submission.CurrentReviewRound;

            var assignment = await _context.SubmissionReviewers
                .FirstOrDefaultAsync(x =>
                    x.SubmissionId == id &&
                    x.ReviewerId == user.Id &&
                    x.ReviewRound == targetRound);

            if (assignment == null)
            {
                TempData["Error"] = "Bu makale için ilgili turda hakem atamanız bulunmamaktadır.";
                return RedirectToAction(nameof(Index));
            }

            if (assignment.Status == ReviewerAssignmentStatus.Cancelled)
            {
                TempData["Error"] = "Bu hakem ataması editör tarafından iptal edilmiştir.";
                return RedirectToAction(nameof(Index));
            }

            if (assignment.Status == ReviewerAssignmentStatus.Declined)
            {
                TempData["Error"] = "Bu hakem ataması reddedilmiştir.";
                return RedirectToAction(nameof(Index));
            }

            var isPastRound = targetRound < submission.CurrentReviewRound;
            var isReadOnly = assignment.Status == ReviewerAssignmentStatus.Completed || isPastRound;

            if (!isReadOnly && assignment.Status == ReviewerAssignmentStatus.Assigned)
            {
                assignment.Status = ReviewerAssignmentStatus.InReview;
                await _context.SaveChangesAsync();
            }

            var existingReview = submission.Reviews?
                .FirstOrDefault(r =>
                    r.ReviewerId == user.Id &&
                    r.ReviewRound == targetRound);
            var reviewerAttachmentFile = await _context.SubmissionFiles
                .Where(f =>
                    f.SubmissionId == id &&
                    f.FileType == "HakemEkDosyasi" &&
                    f.ReviewRound == targetRound &&
                    f.UploadedByUserId == user.Id)
                .OrderByDescending(f => f.UploadedAt)
                .FirstOrDefaultAsync();

            ViewBag.ReviewerAttachmentFile = reviewerAttachmentFile;
            ViewBag.ExistingReview = existingReview;
            ViewBag.Assignment = assignment;
            ViewBag.CurrentReviewRound = targetRound;
            ViewBag.IsReadOnly = isReadOnly;

            return View(submission);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(
            int submissionId,
            string? comments,
            string? commentToAuthor,
            string? commentToEditor,
            string? strengths,
            string? weaknesses,
            int? originalityScore,
            int? methodologyScore,
            int? literatureScore,
            int? writingQualityScore,
            int? overallScore,
            bool hasEthicalIssue,
            string? ethicalConcerns,
            string? decision,
            string? scopeFit,
            string submitType,
            IFormFile? reviewerAttachment,
            string? reviewerAttachmentNote,
            bool sendAttachmentToAuthor)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var submission = await _context.Submissions
                .Include(s => s.Reviews)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
                return NotFound();

            var assignment = await _context.SubmissionReviewers
                .FirstOrDefaultAsync(x =>
                    x.SubmissionId == submissionId &&
                    x.ReviewerId == user.Id &&
                    x.ReviewRound == submission.CurrentReviewRound);

            if (assignment == null)
            {
                TempData["Error"] = "Bu makale için aktif hakem atamanız bulunmamaktadır.";
                return RedirectToAction(nameof(Index));
            }

            if (assignment.Status == ReviewerAssignmentStatus.Cancelled)
            {
                TempData["Error"] = "Bu hakem ataması editör tarafından iptal edilmiştir. Değerlendirme gönderilemez.";
                return RedirectToAction(nameof(Index));
            }

            if (assignment.Status == ReviewerAssignmentStatus.Declined)
            {
                TempData["Error"] = "Bu hakem ataması reddedilmiştir. Değerlendirme gönderilemez.";
                return RedirectToAction(nameof(Index));
            }

            if (assignment.Status == ReviewerAssignmentStatus.Completed)
            {
                TempData["Error"] = "Bu değerlendirme nihai olarak gönderilmiştir. Artık düzenleme yapılamaz.";
                return RedirectToAction(nameof(Index));
            }

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r =>
                    r.SubmissionId == submissionId &&
                    r.ReviewerId == user.Id &&
                    r.ReviewRound == submission.CurrentReviewRound);

            if (submitType == "submit")
            {
                if (string.IsNullOrWhiteSpace(decision))
                {
                    TempData["Error"] = "Nihai gönderim için karar seçmeniz gerekir.";
                    return RedirectToAction(nameof(Review), new { id = submissionId });
                }

                if (!overallScore.HasValue)
                {
                    TempData["Error"] = "Nihai gönderim için genel değerlendirme puanı girmeniz gerekir.";
                    return RedirectToAction(nameof(Review), new { id = submissionId });
                }

                if (string.IsNullOrWhiteSpace(commentToAuthor))
                {
                    TempData["Error"] = "Nihai gönderim için yazara yorum alanı doldurulmalıdır.";
                    return RedirectToAction(nameof(Review), new { id = submissionId });
                }
            }

            if (review == null)
            {
                review = new Review
                {
                    SubmissionId = submissionId,
                    ReviewerId = user.Id,
                    ReviewRound = submission.CurrentReviewRound,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reviews.Add(review);
            }

            review.Comments = comments?.Trim();
            review.CommentToAuthor = commentToAuthor?.Trim();
            review.CommentToEditor = commentToEditor?.Trim();
            review.Strengths = strengths?.Trim();
            review.Weaknesses = weaknesses?.Trim();

            review.OriginalityScore = originalityScore;
            review.MethodologyScore = methodologyScore;
            review.LiteratureScore = literatureScore;
            review.WritingQualityScore = writingQualityScore;
            review.OverallScore = overallScore;

            review.HasEthicalIssue = hasEthicalIssue;
            review.EthicalConcerns = ethicalConcerns?.Trim();
            review.ScopeFit = scopeFit?.Trim();
            review.Decision = string.IsNullOrWhiteSpace(decision) ? null : decision.Trim();
            review.UpdatedAt = DateTime.UtcNow;

            review.ReviewerAttachmentNote = reviewerAttachmentNote?.Trim();
            review.SendAttachmentToAuthor = sendAttachmentToAuthor;

            if (submitType == "draft")
            {
                review.IsDraft = true;

                assignment.Status = ReviewerAssignmentStatus.InReview;
                assignment.ReviewNote = review.Comments ?? string.Empty;

                TempData["Success"] = "Taslak değerlendirme kaydedildi.";
            }
            else if (submitType == "submit")
            {
                review.IsDraft = false;
                review.SubmittedAt = DateTime.UtcNow;

                assignment.Status = ReviewerAssignmentStatus.Completed;
                assignment.ReviewNote = review.Comments ?? string.Empty;
                assignment.CompletedAt = DateTime.UtcNow;

                TempData["Success"] = "Hakem değerlendirmesi başarıyla gönderildi.";

                try
                {
                    await _emailService.SendEmailAsync(
                        "editor@mail.com",
                        "Yeni Hakem Değerlendirmesi",
                        $"""
                        Makale için hakem değerlendirmesi gönderildi.<br><br>
                        <strong>Makale:</strong> {submission.Title}<br>
                        <strong>Karar:</strong> {review.Decision ?? "-"}<br>
                        <strong>Genel Puan:</strong> {(review.OverallScore.HasValue ? review.OverallScore + "/10" : "-")}<br>
                        <strong>Etik Uyarı:</strong> {(review.HasEthicalIssue ? "Var" : "Yok")}
                        """
                    );
                }
                catch
                {
                    // Mail gönderilemese bile değerlendirme kaydı bozulmasın.
                }
            }

            if (reviewerAttachment != null && reviewerAttachment.Length > 0)
            {
                var allowedExtensions = new[]
                {
                    ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"
                };

                var extension = Path.GetExtension(reviewerAttachment.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Error"] = "Sadece PDF, DOC, DOCX, JPG, JPEG veya PNG dosyası yükleyebilirsiniz.";
                    return RedirectToAction(nameof(Review), new { id = submissionId });
                }

                var root = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "submissions",
                    "reviewer-files"
                );

                if (!Directory.Exists(root))
                    Directory.CreateDirectory(root);

                var uniqueFileName =
                    $"reviewer_{submissionId}_{submission.CurrentReviewRound}_{Guid.NewGuid()}{extension}";

                var fullPath = Path.Combine(root, uniqueFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await reviewerAttachment.CopyToAsync(stream);
                }

                var dbPath = $"/uploads/submissions/reviewer-files/{uniqueFileName}";

                review.ReviewerAttachmentPath = dbPath;
                review.ReviewerAttachmentOriginalFileName = reviewerAttachment.FileName;

                _context.SubmissionFiles.Add(new SubmissionFile
                {
                    SubmissionId = submissionId,
                    FileType = "HakemEkDosyasi",
                    OriginalFileName = reviewerAttachment.FileName,
                    StoredFilePath = dbPath,
                    FileSize = reviewerAttachment.Length,
                    UploadedByUserId = user.Id,
                    UploadedAt = DateTime.UtcNow,
                    ReviewRound = submission.CurrentReviewRound
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}