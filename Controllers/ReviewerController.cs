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

            var submissionIds = await _context.SubmissionReviewers
                .Where(sr => sr.ReviewerId == user.Id)
                .Select(sr => sr.SubmissionId)
                .ToListAsync();

            var submissions = await _context.Submissions
                .Include(s => s.Reviews)
                .Where(s => submissionIds.Contains(s.Id))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(submissions);
        }

        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var isAssigned = await _context.SubmissionReviewers
                .AnyAsync(sr => sr.SubmissionId == id && sr.ReviewerId == user.Id);

            if (!isAssigned)
                return Forbid();

            var submission = await _context.Submissions
                .Include(s => s.Reviews)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
                return NotFound();

            var existingReview = submission.Reviews?
                .FirstOrDefault(r => r.ReviewerId == user.Id);

            var assignment = await _context.SubmissionReviewers
                .FirstOrDefaultAsync(sr => sr.SubmissionId == id && sr.ReviewerId == user.Id);

            if (assignment != null && assignment.Status == ReviewStatus.Pending)
            {
                assignment.Status = ReviewStatus.InReview;
                await _context.SaveChangesAsync();
            }

            ViewBag.ExistingReview = existingReview;
            ViewBag.Assignment = assignment;

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
            string submitType)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var isAssigned = await _context.SubmissionReviewers
                .AnyAsync(sr => sr.SubmissionId == submissionId && sr.ReviewerId == user.Id);

            if (!isAssigned)
                return Forbid();

            var submission = await _context.Submissions
                .Include(s => s.Reviews)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
                return NotFound();

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.SubmissionId == submissionId && r.ReviewerId == user.Id);

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

            var assignment = await _context.SubmissionReviewers
                .FirstOrDefaultAsync(x => x.SubmissionId == submissionId && x.ReviewerId == user.Id);

            if (submitType == "draft")
            {
                review.IsDraft = true;

                if (assignment != null)
                {
                    assignment.Status = ReviewStatus.InReview;
                    assignment.ReviewNote = review.Comments ?? string.Empty;
                }

                TempData["Success"] = "Taslak değerlendirme kaydedildi.";
            }
            else if (submitType == "submit")
            {
                review.IsDraft = false;
                review.SubmittedAt = DateTime.UtcNow;

                if (assignment != null)
                {
                    assignment.Status = ReviewStatus.Completed;
                    assignment.ReviewNote = review.Comments ?? string.Empty;
                    assignment.CompletedAt = DateTime.UtcNow;
                }

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
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}