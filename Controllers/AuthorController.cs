using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;

namespace MyDergiApp.Controllers
{
    [Authorize(Roles = "Author")]
    public class AuthorController : Controller
    {
        private readonly AppDbContext _context;

        public AuthorController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> MyArticles()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var articles = await _context.Submissions
                .Where(x => x.AuthorId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(articles);
        }
    }
}