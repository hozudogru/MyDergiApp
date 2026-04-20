using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Models;

namespace MyDergiApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Editor")]
    public class SubmissionManagementController : Controller
    {
        private readonly AppDbContext _context;

        public SubmissionManagementController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSubmissions()
        {
            var data = await _context.Submissions
                .Include(s => s.Author)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    Author = s.Author != null ? s.Author.FullName : s.AuthorId,
                    Status = s.Status.ToString(),
                    s.CreatedAt
                })
                .ToListAsync();

            return Json(new { data });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id)
        {
            var submission = await _context.Submissions.FindAsync(id);

            if (submission == null)
                return NotFound(new { success = false, message = "Makale bulunamadı." });

            submission.Status = submission.Status switch
            {
                SubmissionStatus.Submitted => SubmissionStatus.InReview,
                SubmissionStatus.InReview => SubmissionStatus.Accepted,
                SubmissionStatus.Accepted => SubmissionStatus.Rejected,
                _ => SubmissionStatus.Submitted
            };

            submission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Durum güncellendi.",
                status = submission.Status.ToString()
            });
        }
    }
}