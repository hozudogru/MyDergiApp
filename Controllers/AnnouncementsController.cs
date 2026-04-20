using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Models;

namespace MyDergiApp.Controllers
{
    [Authorize(Roles = "Admin,Editor")]
    public class AnnouncementsController : Controller
    {
        private readonly AppDbContext _context;

        public AnnouncementsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _context.Announcements
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(items);
        }

        public IActionResult Create()
        {
            return View(new Announcement());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Announcement model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.CreatedAt = DateTime.UtcNow;
            _context.Announcements.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Duyuru eklendi.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.Announcements.FindAsync(id);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Announcement model)
        {
            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            var item = await _context.Announcements.FindAsync(id);
            if (item == null) return NotFound();

            item.Title = model.Title;
            item.Content = model.Content;
            item.IsActive = model.IsActive;
            item.ShowAsPopup = model.ShowAsPopup;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Duyuru güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Announcements.FindAsync(id);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Announcements.FindAsync(id);
            if (item == null) return NotFound();

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
            if (item == null) return NotFound();

            if (!item.ShowAsPopup)
            {
                var popupItems = await _context.Announcements
                    .Where(x => x.ShowAsPopup)
                    .ToListAsync();

                foreach (var popup in popupItems)
                    popup.ShowAsPopup = false;

                item.ShowAsPopup = true;
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
            if (item == null) return NotFound();

            item.IsActive = !item.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Aktiflik durumu güncellendi.";
            return RedirectToAction(nameof(Index));
        }
    }
}