using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Entities;
using MyDergiApp.Models;
using MyDergiApp.Services;
using MyDergiApp.ViewModels.Submissions;
using System.Security.Claims;


namespace MyDergiApp.Controllers
{
    [Authorize]
    public class SubmissionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly EmailService _emailService;
        private readonly EmailTemplateService _templateService;

        public SubmissionController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            EmailService emailService,
            EmailTemplateService templateService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _templateService = templateService;
        }
        [Authorize(Roles = "Reviewer")]

        public async Task<IActionResult> MyReviews()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var submissions = await _context.SubmissionReviewers
                 .Where(sr => sr.ReviewerId == user.Id)
                 .Include(sr => sr.Submission)
                     .ThenInclude(s => s!.Reviews)
                 .Select(sr => sr.Submission!)
                 .ToListAsync();

            return View(submissions);
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        }

        private bool IsAdminOrEditor()
        {
            return User.IsInRole("Admin") || User.IsInRole("Editor");
        }

        [Authorize(Roles = "Author,Admin,Editor")]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            IQueryable<Submission> query = _context.Submissions
                .Include(s => s.Author);

            if (!IsAdminOrEditor())
            {
                query = query.Where(s => s.AuthorId == userId);
            }

            var submissions = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(submissions);
        }

        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> AdminList()
        {
            var submissions = await _context.Submissions
                .Include(s => s.Author)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(submissions);
        }

        [Authorize(Roles = "Author,Admin,Editor")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new SubmissionCreateEditViewModel());
        }

        [Authorize(Roles = "Author,Admin,Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubmissionCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var submission = new Submission
            {
                Title = model.Title ?? string.Empty,
                Abstract = model.Abstract ?? string.Empty,
                Keywords = model.Keywords ?? string.Empty,
                AuthorId = GetCurrentUserId(),
                CreatedAt = DateTime.UtcNow,
                Status = SubmissionStatus.Submitted
            };

            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Makale başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = "Author,Admin,Editor")]
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var submission = await _context.Submissions
                .Include(s => s.Author)
                .Include(s => s.Reviews)
                    .ThenInclude(r => r.Reviewer)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
                return NotFound();

            var currentUserId = GetCurrentUserId();
            var isPrivileged = IsAdminOrEditor();
            var isOwner = submission.AuthorId == currentUserId;

            if (!isPrivileged && !isOwner)
                return Forbid();

            var reviewerAssignments = await _context.SubmissionReviewers
                .Include(x => x.Reviewer)
                .Where(x => x.SubmissionId == submission.Id)
                .OrderByDescending(x => x.AssignedAt)
                .ToListAsync();

            var vm = new SubmissionDetailViewModel
            {
                Id = submission.Id,
                Title = submission.Title,
                Abstract = submission.Abstract,
                Keywords = submission.Keywords,
                FilePath = submission.FilePath,
                CreatedAt = submission.CreatedAt,
                UpdatedAt = submission.UpdatedAt,
                Status = submission.Status,
                NoteToEditor = submission.NoteToEditor,
                AuthorId = submission.AuthorId,
                AuthorName = submission.Author?.FullName ?? "",
                AuthorEmail = submission.Author != null ? submission.Author.Email : null,
                CanEdit = isOwner && submission.Status == SubmissionStatus.Submitted,
                CanManageStatus = isPrivileged,
                Reviewers = reviewerAssignments.Select(x => new ReviewerAssignmentListItemViewModel
                {
                    AssignmentId = x.Id,
                    SubmissionId = x.SubmissionId,
                    SubmissionTitle = submission.Title ?? "",
                    AuthorName = submission.Author != null ? (submission.Author.FullName ?? "") : "",
                    ReviewerName = x.Reviewer != null ? (x.Reviewer.FullName ?? "") : "",
                    ReviewerEmail = x.Reviewer != null ? (x.Reviewer.Email ?? "") : "",
                    Status = x.Status.ToString(),
                    AssignedAt = x.AssignedAt,
                    CompletedAt = x.CompletedAt,
                    ReviewNote = x.ReviewNote ?? ""
                }).ToList()
            };

            return View(vm);
        }

        [Authorize(Roles = "Author,Admin,Editor")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var submission = await _context.Submissions.FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
                return NotFound();

            var currentUserId = GetCurrentUserId();
            var isPrivileged = IsAdminOrEditor();
            var isOwner = submission.AuthorId == currentUserId;

            if (!isPrivileged && !isOwner)
                return Forbid();

            if (!isPrivileged && submission.Status != SubmissionStatus.Submitted)
            {
                TempData["Error"] = "Bu makale artık düzenlenemez.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var vm = new SubmissionCreateEditViewModel
            {
                Id = submission.Id,
                Title = submission.Title,
                Abstract = submission.Abstract,
                Keywords = submission.Keywords
            };

            return View(vm);
        }
        [Authorize(Roles = "Admin,Editor")]
        [HttpGet]
        public async Task<IActionResult> UpdateStatus(int id)
        {
            var submission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
                return NotFound();

            var vm = new SubmissionStatusUpdateViewModel
            {
                SubmissionId = submission.Id,
                Status = submission.Status,
                NoteToEditor = submission.NoteToEditor,
                DecisionNote = submission.DecisionNote
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(SubmissionStatusUpdateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var submission = await _context.Submissions.FindAsync(model.SubmissionId);

            if (submission == null)
                return NotFound();

            submission.Status = model.Status;
            submission.NoteToEditor = model.NoteToEditor ?? string.Empty;
            submission.DecisionNote = model.DecisionNote ?? string.Empty;
            submission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Makale durumu güncellendi.";
            return RedirectToAction(nameof(Detail), new { id = submission.Id });
        }
        [Authorize(Roles = "Author,Admin,Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SubmissionCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.Id == null)
                return BadRequest();

            var submission = await _context.Submissions.FirstOrDefaultAsync(s => s.Id == model.Id.Value);

            if (submission == null)
                return NotFound();

            var currentUserId = GetCurrentUserId();
            var isPrivileged = IsAdminOrEditor();
            var isOwner = submission.AuthorId == currentUserId;

            if (!isPrivileged && !isOwner)
                return Forbid();

            if (!isPrivileged && submission.Status != SubmissionStatus.Submitted)
            {
                TempData["Error"] = "Bu makale artık düzenlenemez.";
                return RedirectToAction(nameof(Detail), new { id = submission.Id });
            }

            submission.Title = model.Title ?? string.Empty;
            submission.Abstract = model.Abstract ?? string.Empty;
            submission.Keywords = model.Keywords ?? string.Empty;
            submission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Makale başarıyla güncellendi.";
            return RedirectToAction(nameof(Detail), new { id = submission.Id });
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpGet]
        public async Task<IActionResult> AssignReviewer(int id)
        {
            var submission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
                return NotFound();

            var assignedReviewerIds = await _context.SubmissionReviewers
                .Where(x => x.SubmissionId == id)
                .Select(x => x.ReviewerId)
                .ToListAsync();

            var reviewers = await _userManager.GetUsersInRoleAsync("Reviewer");

            var availableReviewers = reviewers
                .Where(r => !assignedReviewerIds.Contains(r.Id))
                .Select(r => new SelectListItem
                {
                    Value = r.Id,
                    Text = $"{r.FullName} ({r.Email})"
                })
                .ToList();

            var vm = new AssignReviewerViewModel
            {
                SubmissionId = submission.Id,
                SubmissionTitle = submission.Title,
                AvailableReviewers = availableReviewers
            };

            return View(vm);
        }
        [Authorize(Roles = "Admin,Editor")]
        [HttpGet]
        public async Task<IActionResult> EditorDecision(int id)
        {
            var submission = await _context.Submissions
                .Include(s => s.Author)
                .Include(s => s.Reviews)
                    .ThenInclude(r => r.Reviewer)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
                return NotFound();

            var assignments = await _context.SubmissionReviewers
                .Include(x => x.Reviewer)
                .Where(x => x.SubmissionId == id)
                .ToListAsync();

            var completedCount = assignments.Count(x => x.CompletedAt.HasValue);
            var totalCount = assignments.Count;

            if (totalCount < 2 || completedCount < 2)
            {
                TempData["Error"] = "Nihai karar için en az 2 hakem değerlendirmesi tamamlanmış olmalıdır.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var reviewList = submission.Reviews.ToList();
            var hasConflict = HasReviewerConflict(reviewList);
            var suggestedDecision = GetSuggestedDecision(reviewList);

            var vm = new EditorDecisionViewModel
            {
                SubmissionId = submission.Id,
                Title = submission.Title ?? string.Empty,
                Status = submission.Status,
                DecisionNote = submission.DecisionNote ?? string.Empty,
                SuggestedDecision = suggestedDecision,
                TotalReviewerCount = totalCount,
                CompletedReviewerCount = completedCount,
                HasConflict = hasConflict,
                ConflictMessage = hasConflict
                    ? "Hakem kararları arasında çelişki var. Nihai karar dikkatle verilmelidir."
                    : "",
                Reviews = assignments.Select(a =>
                {
                    var review = submission.Reviews.FirstOrDefault(r => r.ReviewerId == a.ReviewerId);

                    return new ReviewerDecisionItemViewModel
                    {
                        ReviewerName = a.Reviewer?.FullName ?? "",
                        ReviewerEmail = a.Reviewer?.Email ?? "",
                        Decision = review?.Decision ?? "",
                        Comments = review?.Comments ?? a.ReviewNote ?? "",
                        CompletedAt = a.CompletedAt,
                        IsCompleted = a.CompletedAt.HasValue
                    };
                }).ToList()
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditorDecision(EditorDecisionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var submissionReload = await _context.Submissions
                    .Include(s => s.Reviews)
                        .ThenInclude(r => r.Reviewer)
                    .FirstOrDefaultAsync(s => s.Id == model.SubmissionId);

                var assignmentsReload = await _context.SubmissionReviewers
                    .Include(x => x.Reviewer)
                    .Where(x => x.SubmissionId == model.SubmissionId)
                    .ToListAsync();

                var reviewList = submissionReload?.Reviews.ToList() ?? new List<Review>();
                var hasConflict = HasReviewerConflict(reviewList);

                model.TotalReviewerCount = assignmentsReload.Count;
                model.CompletedReviewerCount = assignmentsReload.Count(x => x.CompletedAt.HasValue);
                model.SuggestedDecision = GetSuggestedDecision(reviewList);
                model.HasConflict = hasConflict;
                model.ConflictMessage = hasConflict
                    ? "Hakem kararları arasında çelişki var. Nihai karar dikkatle verilmelidir."
                    : "";

                model.Reviews = assignmentsReload.Select(a =>
                {
                    var review = submissionReload?.Reviews.FirstOrDefault(r => r.ReviewerId == a.ReviewerId);

                    return new ReviewerDecisionItemViewModel
                    {
                        ReviewerName = a.Reviewer?.FullName ?? "",
                        ReviewerEmail = a.Reviewer?.Email ?? "",
                        Decision = review?.Decision ?? "",
                        Comments = review?.Comments ?? a.ReviewNote ?? "",
                        CompletedAt = a.CompletedAt,
                        IsCompleted = a.CompletedAt.HasValue
                    };
                }).ToList();

                return View(model);
            }

            var assignments = await _context.SubmissionReviewers
                .Where(x => x.SubmissionId == model.SubmissionId)
                .ToListAsync();

            var completedCount = assignments.Count(x => x.CompletedAt.HasValue);
            var totalCount = assignments.Count;

            if (totalCount < 2 || completedCount < 2)
            {
                TempData["Error"] = "En az 2 hakem değerlendirmesi tamamlanmadan nihai karar verilemez.";
                return RedirectToAction(nameof(Detail), new { id = model.SubmissionId });
            }

            var submission = await _context.Submissions
                .Include(s => s.Author)
                .FirstOrDefaultAsync(s => s.Id == model.SubmissionId);

            if (submission == null)
                return NotFound();

            submission.Status = model.Status;
            submission.DecisionNote = model.DecisionNote ?? string.Empty;
            submission.DecisionDate = DateTime.UtcNow;
            submission.DecisionByUserId = _userManager.GetUserId(User);
            submission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(submission.Author?.Email))
            {
                var body = await _templateService.BuildEditorDecisionAsync(
                    submission.Author?.FullName ?? "Yazar",
                    submission.Title ?? "",
                    model.Status.ToString(),
                    model.DecisionNote ?? ""
                );

                var authorEmail = submission?.Author?.Email;

                if (string.IsNullOrWhiteSpace(authorEmail))
                {
                    TempData["Error"] = "Yazar e-posta adresi bulunamadı.";
                    return RedirectToAction("Detail", new { id = model.SubmissionId });
                }

                await _emailService.SendEmailAsync(
                    authorEmail,
                    "Makale Kararı",
                    body
                );

                if (string.IsNullOrWhiteSpace(authorEmail))
                {
                    TempData["Error"] = "Yazar e-posta adresi bulunamadı.";
                    return RedirectToAction("Detail", new { id = model.SubmissionId });
                }

                await _emailService.SendEmailAsync(
                    authorEmail,
                    "Makale Kararı",
                    body
                );
            }

            TempData["Success"] = "Editör kararı başarıyla kaydedildi.";
            return RedirectToAction(nameof(Detail), new { id = model.SubmissionId });
        }
        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var submission = await _context.Submissions
                .Include(s => s.Reviews)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
                return NotFound();

            var assignments = await _context.SubmissionReviewers
                .Where(x => x.SubmissionId == id)
                .ToListAsync();

            if (assignments.Any())
            {
                _context.SubmissionReviewers.RemoveRange(assignments);
            }

            if (submission.Reviews != null && submission.Reviews.Any())
            {
                _context.Reviews.RemoveRange(submission.Reviews);
            }

            _context.Submissions.Remove(submission);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Makale başarıyla silindi.";
            return RedirectToAction(nameof(AdminList));
        }
        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveReviewer(int assignmentId, int submissionId)
        {
            var assignment = await _context.SubmissionReviewers
                .FirstOrDefaultAsync(x => x.Id == assignmentId && x.SubmissionId == submissionId);

            if (assignment == null)
            {
                TempData["Error"] = "Hakem ataması bulunamadı.";
                return RedirectToAction(nameof(Detail), new { id = submissionId });
            }

            if (assignment.CompletedAt.HasValue)
            {
                TempData["Error"] = "Değerlendirmesini tamamlayan hakem kaldırılamaz.";
                return RedirectToAction(nameof(Detail), new { id = submissionId });
            }

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.SubmissionId == submissionId && r.ReviewerId == assignment.ReviewerId);

            if (review != null)
            {
                _context.Reviews.Remove(review);
            }

            _context.SubmissionReviewers.Remove(assignment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Hakem ataması kaldırıldı.";
            return RedirectToAction(nameof(Detail), new { id = submissionId });
        }
        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignReviewer(AssignReviewerViewModel model)
        {
            var submission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.Id == model.SubmissionId);

            if (submission == null)
                return NotFound();

            var assignedReviewerIds = await _context.SubmissionReviewers
                .Where(x => x.SubmissionId == model.SubmissionId)
                .Select(x => x.ReviewerId)
                .ToListAsync();

            var reviewers = await _userManager.GetUsersInRoleAsync("Reviewer");

            model.AvailableReviewers = reviewers
                .Where(r => !assignedReviewerIds.Contains(r.Id))
                .Select(r => new SelectListItem
                {
                    Value = r.Id,
                    Text = $"{r.FullName} ({r.Email})"
                })
                .ToList();

            if (!ModelState.IsValid)
            {
                model.SubmissionTitle = submission.Title;
                return View(model);
            }

            var alreadyAssigned = await _context.SubmissionReviewers
                .AnyAsync(x => x.SubmissionId == model.SubmissionId && x.ReviewerId == model.ReviewerId);

            if (alreadyAssigned)
            {
                TempData["Error"] = "Bu hakem zaten atanmış.";
                return RedirectToAction(nameof(Detail), new { id = model.SubmissionId });
            }

            var assignment = new SubmissionReviewer
            {
                SubmissionId = model.SubmissionId,
                ReviewerId = model.ReviewerId,
                AssignedAt = DateTime.UtcNow
            };

            _context.SubmissionReviewers.Add(assignment);
            await _context.SaveChangesAsync();

            string? emailWarning = null;

            var reviewer = await _userManager.FindByIdAsync(model.ReviewerId);
            if (reviewer != null && !string.IsNullOrWhiteSpace(reviewer.Email))
            {
                try
                {
                    var body = await _templateService.BuildReviewerAssignmentAsync(
                        reviewer.FullName ?? "Hakem",
                        submission.Title ?? ""
                    );

                    await _emailService.SendEmailAsync(
                        reviewer.Email,
                        "Yeni Hakem Ataması",
                        body
                    );
                }
                catch
                {
                    emailWarning = "Hakem atandı fakat e-posta gönderilemedi.";
                }
            }

            TempData["Success"] = "Hakem başarıyla atandı.";

            if (!string.IsNullOrWhiteSpace(emailWarning))
            {
                TempData["Error"] = emailWarning;
            }

            return RedirectToAction(nameof(Detail), new { id = model.SubmissionId });
        }
        private string GetSuggestedDecision(List<Review> reviews)
        {
            if (reviews == null || !reviews.Any())
                return "Henüz öneri yok";

            var decisions = reviews
                .Where(r => !string.IsNullOrWhiteSpace(r.Decision))
                .Select(r => r.Decision!.Trim())
                .ToList();

            if (!decisions.Any())
                return "Henüz öneri yok";

            if (decisions.All(x => x == "Accept"))
                return "Kabul öneriliyor";

            if (decisions.Any(x => x == "Reject") && decisions.Count(x => x == "Reject") >= 2)
                return "Red öneriliyor";

            if (decisions.Any(x => x == "Major Revision"))
                return "Majör revizyon öneriliyor";

            if (decisions.Any(x => x == "Minor Revision"))
                return "Minör revizyon öneriliyor";

            if (decisions.Count(x => x == "Reject") > decisions.Count(x => x == "Accept"))
                return "Red eğilimi var";

            if (decisions.Count(x => x == "Accept") > decisions.Count(x => x == "Reject"))
                return "Kabul eğilimi var";

            return "Editör değerlendirmesi gerekli";
        }
        private bool HasReviewerConflict(List<Review> reviews)
        {
            if (reviews == null || reviews.Count < 2)
                return false;

            var decisions = reviews
                .Select(r => r.Decision)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => d!.Trim())
                .Distinct()
                .ToList();

            if (decisions.Count <= 1)
                return false;

            var hasAccept = decisions.Contains("Accept");
            var hasReject = decisions.Contains("Reject");
            var hasMajor = decisions.Contains("Major Revision");
            var hasMinor = decisions.Contains("Minor Revision");

            if (hasAccept && hasReject)
                return true;

            if (hasAccept && hasMajor)
                return true;

            if (hasReject && (hasMinor || hasMajor))
                return true;

            return false;
        }
    }
}
