using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Models;

namespace MyDergiApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AnnouncementsController : Controller
    {
        private readonly AppDbContext _context;

        public AnnouncementsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var items = await _context.Announcements
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Announcement
            {
                IsActive = true,
                ShowAsPopup = false
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Announcement model)
        {
            model.Title = model.Title?.Trim() ?? string.Empty;
            model.Content = model.Content?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
                return View(model);

            model.CreatedAt = DateTime.UtcNow;

            if (model.ShowAsPopup)
            {
                model.IsActive = true;
                await ClearOtherPopupAnnouncementsAsync();
            }

            _context.Announcements.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Duyuru eklendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.Announcements.FindAsync(id);
            if (item == null)
                return NotFound();

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Announcement model)
        {
            if (id != model.Id)
                return BadRequest();

            model.Title = model.Title?.Trim() ?? string.Empty;
            model.Content = model.Content?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
                return View(model);

            var item = await _context.Announcements.FindAsync(id);
            if (item == null)
                return NotFound();

            item.Title = model.Title;
            item.Content = model.Content;
            item.IsActive = model.IsActive;
            item.ShowAsPopup = model.ShowAsPopup;

            if (item.ShowAsPopup)
            {
                item.IsActive = true;
                await ClearOtherPopupAnnouncementsAsync(item.Id);
            }

            if (!item.IsActive)
            {
                item.ShowAsPopup = false;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Duyuru güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Announcements.FindAsync(id);
            if (item == null)
                return NotFound();

            _context.Announcements.Remove(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Duyuru silindi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePopup(int id)
        {
            var item = await _context.Announcements.FindAsync(id);
            if (item == null)
                return NotFound();

            if (!item.ShowAsPopup)
            {
                item.IsActive = true;
                item.ShowAsPopup = true;
                await ClearOtherPopupAnnouncementsAsync(item.Id);
            }
            else
            {
                item.ShowAsPopup = false;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Popup durumu güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var item = await _context.Announcements.FindAsync(id);
            if (item == null)
                return NotFound();

            item.IsActive = !item.IsActive;

            if (!item.IsActive)
            {
                item.ShowAsPopup = false;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Aktiflik durumu güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ClearOtherPopupAnnouncementsAsync(int? exceptId = null)
        {
            var query = _context.Announcements.Where(x => x.ShowAsPopup);

            if (exceptId.HasValue)
            {
                query = query.Where(x => x.Id != exceptId.Value);
            }

            var popupItems = await query.ToListAsync();

            foreach (var popup in popupItems)
            {
                popup.ShowAsPopup = false;
            }
        }
    }
}
