using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Entities;
using MyDergiApp.Models.Enums;
using MyDergiApp.ViewModels.Submissions;
using System.Security.Claims;

namespace MyDergiApp.Controllers
{
    [Authorize]
    public class SubmissionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public SubmissionController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

        [Authorize(Roles = "Author")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new SubmissionCreateEditViewModel());
        }

        [Authorize(Roles = "Author")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubmissionCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var submission = new Submission
            {
                Title = model.Title,
                Abstract = model.Abstract,
                Keywords = model.Keywords,
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
                EditorNote = submission.EditorNote,
                AuthorId = submission.AuthorId,
                AuthorName = submission.Author?.FullName ?? "",
                AuthorEmail = submission.Author?.Email ?? "",
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
                Status = submission.Status.ToString(),
                EditorNote = submission.EditorNote
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

            if (!Enum.TryParse<SubmissionStatus>(model.Status, out var parsedStatus))
            {
                ModelState.AddModelError("Status", "Geçersiz durum seçildi.");
                return View(model);
            }

            var submission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.Id == model.SubmissionId);

            if (submission == null)
                return NotFound();

            submission.Status = parsedStatus;
            submission.EditorNote = model.EditorNote;
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

            submission.Title = model.Title;
            submission.Abstract = model.Abstract;
            submission.Keywords = model.Keywords;
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

            var reviewerUsers = await _userManager.GetUsersInRoleAsync("Reviewer");

            var vm = new AssignReviewerViewModel
            {
                SubmissionId = submission.Id,
                SubmissionTitle = submission.Title,
                Reviewers = reviewerUsers
                    .Where(x => x.IsActive)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id,
                        Text = $"{x.FullName} ({x.Email})"
                    })
                    .ToList()
            };

            return View(vm);
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

            var reviewerUsers = await _userManager.GetUsersInRoleAsync("Reviewer");

            model.Reviewers = reviewerUsers
                .Where(x => x.IsActive)
                .Select(x => new SelectListItem
                {
                    Value = x.Id,
                    Text = $"{x.FullName} ({x.Email})"
                })
                .ToList();

            if (!ModelState.IsValid)
                return View(model);

            var reviewerExists = reviewerUsers.Any(x => x.Id == model.ReviewerId);
            if (!reviewerExists)
            {
                ModelState.AddModelError("ReviewerId", "Geçersiz reviewer seçildi.");
                return View(model);
            }

            var alreadyAssigned = await _context.SubmissionReviewers
                .AnyAsync(x => x.SubmissionId == model.SubmissionId && x.ReviewerId == model.ReviewerId);

            if (alreadyAssigned)
            {
                ModelState.AddModelError("ReviewerId", "Bu reviewer zaten atanmış.");
                return View(model);
            }

            var assignment = new SubmissionReviewer
            {
                SubmissionId = model.SubmissionId,
                ReviewerId = model.ReviewerId,
                Status = ReviewStatus.Assigned,
                AssignedAt = DateTime.UtcNow
            };

            if (submission.Status == SubmissionStatus.Submitted)
            {
                submission.Status = SubmissionStatus.InReview;
                submission.UpdatedAt = DateTime.UtcNow;
            }

            _context.SubmissionReviewers.Add(assignment);

            if (submission.Status == SubmissionStatus.Submitted)
                submission.Status = SubmissionStatus.InReview;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Reviewer başarıyla atandı.";
            return RedirectToAction(nameof(Detail), new { id = model.SubmissionId });
        }

        [Authorize(Roles = "Reviewer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(ReviewSubmitViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();

            var assignment = await _context.SubmissionReviewers
                .Include(x => x.Submission)
                .FirstOrDefaultAsync(x => x.Id == model.AssignmentId && x.ReviewerId == userId);

            if (assignment == null)
                return NotFound();

            assignment.ReviewNote = model.ReviewNote;
            assignment.Recommendation = model.Recommendation;
            assignment.Status = ReviewStatus.Completed;
            assignment.CompletedAt = DateTime.UtcNow;

            var submission = assignment.Submission;

            var reviews = await _context.SubmissionReviewers
                .Where(x => x.SubmissionId == submission.Id && x.Status == ReviewStatus.Completed)
                .ToListAsync();

            if (reviews.Count >= 2)
            {
                if (reviews.All(r => r.Recommendation == "Accept"))
                {
                    submission.FinalDecision = "Accept Suggestion";
                }
                else if (reviews.Any(r => r.Recommendation == "Reject"))
                {
                    submission.FinalDecision = "Reject Suggestion";
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Review başarıyla kaydedildi.";
            return RedirectToAction("MyReviews");
        }

      
       
    }
}