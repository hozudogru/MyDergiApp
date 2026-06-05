using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Models;

namespace MyDergiApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class JournalIndexesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public JournalIndexesController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var items = await _context.JournalIndexes
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new JournalIndex { IsActive = true, SortOrder = 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JournalIndex model, IFormFile? logoFile)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Name = model.Name.Trim();
            model.Url = model.Url?.Trim();
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            if (logoFile != null && logoFile.Length > 0)
            {
                var uploadResult = await SaveLogoAsync(logoFile);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError("LogoPath", uploadResult.ErrorMessage ?? "Logo yüklenemedi.");
                    return View(model);
                }
                model.LogoPath = uploadResult.Path;
            }

            _context.JournalIndexes.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "İndeks eklendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.JournalIndexes.FindAsync(id);
            if (item == null)
                return NotFound();

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, JournalIndex model, IFormFile? logoFile)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            var item = await _context.JournalIndexes.FindAsync(id);
            if (item == null)
                return NotFound();

            item.Name = model.Name.Trim();
            item.Url = model.Url?.Trim();
            item.SortOrder = model.SortOrder;
            item.IsActive = model.IsActive;
            item.UpdatedAt = DateTime.UtcNow;

            if (logoFile != null && logoFile.Length > 0)
            {
                var uploadResult = await SaveLogoAsync(logoFile);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError("LogoPath", uploadResult.ErrorMessage ?? "Logo yüklenemedi.");
                    return View(model);
                }
                item.LogoPath = uploadResult.Path;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "İndeks güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.JournalIndexes.FindAsync(id);
            if (item == null)
                return NotFound();

            _context.JournalIndexes.Remove(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = "İndeks silindi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var item = await _context.JournalIndexes.FindAsync(id);
            if (item == null)
                return NotFound();

            item.IsActive = !item.IsActive;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Aktiflik durumu güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<(bool Success, string? Path, string? ErrorMessage)> SaveLogoAsync(IFormFile logoFile)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(logoFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(ext))
                return (false, null, "Sadece jpg, jpeg, png veya webp görsel yüklenebilir.");

            var folder = Path.Combine(_env.WebRootPath, "uploads", "indexes");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await logoFile.CopyToAsync(stream);

            return (true, $"/uploads/indexes/{fileName}", null);
        }
    }
}
