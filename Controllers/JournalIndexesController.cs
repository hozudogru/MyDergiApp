using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Helpers;
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

                UploadHelper.TryDeleteWebFile(_env, item.LogoPath); // eski logo yetim kalmasin
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

            UploadHelper.TryDeleteWebFile(_env, item.LogoPath);

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
            var error = UploadHelper.Validate(logoFile, UploadHelper.ImageExtensions, UploadHelper.MaxImageBytes, "İndeks logosu");

            if (error != null)
                return (false, null, error);

            var path = await UploadHelper.SaveAsync(_env, logoFile, "indexes", string.Empty);

            return (true, path, null);
        }
    }
}
