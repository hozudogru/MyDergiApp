using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Entities;

namespace MyDergiApp.Controllers;

[Authorize(Roles = "Admin")]
public class JournalController : Controller
{
    private readonly AppDbContext _context;

    public JournalController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var journals = await _context.Journals
            .OrderBy(x => x.Id)
            .ToListAsync();

        return View(journals);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Journal journal)
    {
        if (!ModelState.IsValid)
            return View(journal);

        _context.Journals.Add(journal);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Dergi başarıyla eklendi.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var journal = await _context.Journals.FindAsync(id);
        if (journal == null)
            return NotFound();

        return View(journal);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Journal journal)
    {
        if (id != journal.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(journal);

        _context.Update(journal);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Dergi başarıyla güncellendi.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var journal = await _context.Journals.FirstOrDefaultAsync(x => x.Id == id);
        if (journal == null)
            return NotFound();

        return View(journal);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var journal = await _context.Journals.FindAsync(id);
        if (journal != null)
        {
            _context.Journals.Remove(journal);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Dergi silindi.";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var journal = await _context.Journals.FirstOrDefaultAsync(x => x.Id == id);
        if (journal == null)
            return NotFound();

        return View(journal);
    }
}
