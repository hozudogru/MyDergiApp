using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Helpers;

namespace MyDergiApp.Areas.Admin.Controllers
{
    /// <summary>
    /// Admin icin salt okunur "tum makaleler" listesi. Durum degisiklikleri yalnizca
    /// SubmissionController'daki is akisi action'lari uzerinden yapilir; buradaki eski
    /// UpdateStatus (CSRF korumasiz, is akisini atlayan, Editor'e acik) kaldirildi.
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
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
            var rows = await _context.Submissions
                .AsNoTracking()
                .Include(s => s.Author)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    Author = s.Author != null ? s.Author.FullName : s.AuthorId,
                    s.Status,
                    s.CreatedAt
                })
                .ToListAsync();

            var data = rows.Select(s => new
            {
                id = s.Id,
                title = s.Title,
                author = s.Author,
                status = s.Status.ToString(),
                statusText = StatusDisplayHelper.GetSubmissionStatusText(s.Status),
                statusBadgeClass = StatusDisplayHelper.GetSubmissionStatusBadgeClass(s.Status),
                createdAt = s.CreatedAt
            });

            return Json(new { data });
        }
    }
}
