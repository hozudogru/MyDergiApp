using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Entities;
using MyDergiApp.ViewModels;
using MyDergiApp.ViewModels.Users;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;


[Authorize(Roles = "Admin")]
public class UserManagementController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _context;
    private readonly EmailService _emailService;
    public UserManagementController(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole> roleManager,
    AppDbContext context,
    EmailService emailService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _emailService = emailService;
    }

    public async Task<IActionResult> Index()
    {
        var users = new List<UserListViewModel>();

        foreach (var user in _userManager.Users.ToList())
        {
            var roles = await _userManager.GetRolesAsync(user);

            users.Add(new UserListViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive,
                RoleName = string.Join(", ", roles),
                Roles = roles.ToList(),
                CreatedAt = user.CreatedAt,
                HasSubmissions = await _context.Submissions.AnyAsync(x => x.AuthorId == user.Id)
            });


        }
        return View(users);
    }
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var roles = await _roleManager.Roles
            .OrderBy(r => r.Name)
            .Select(r => new SelectListItem
            {
                Value = r.Name!,
                Text = r.Name!
            })
            .ToListAsync();

        var vm = new CreateUserViewModel
        {
            AvailableRoles = roles
        };

        return View(vm);
    }
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var currentUserId = _userManager.GetUserId(User);

        if (user.Id == currentUserId)
        {
            TempData["Error"] = "Kendi hesabınızı silemezsiniz.";
            return RedirectToAction(nameof(Index));
        }

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains("Admin"))
        {
            TempData["Error"] = "Sistem yöneticisi kullanıcı silinemez.";
            return RedirectToAction(nameof(Index));
        }

        var hasSubmissions = await _context.Submissions
            .AnyAsync(x => x.AuthorId == user.Id);

        if (hasSubmissions)
        {
            TempData["Error"] = "Bu kullanıcıya bağlı makaleler olduğu için silinemez. Kullanıcıyı pasif yapın.";
            return RedirectToAction(nameof(Index));
        }

        var reviewerAssignments = await _context.SubmissionReviewers
            .Where(x => x.ReviewerId == user.Id)
            .ToListAsync();

        if (reviewerAssignments.Any())
        {
            _context.SubmissionReviewers.RemoveRange(reviewerAssignments);
            await _context.SaveChangesAsync();
        }

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" | ", result.Errors.Select(x => x.Description));
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Kullanıcı başarıyla silindi.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> TestMail()
    {
        try
        {
            await _emailService.SendEmailAsync(
                "hozudogru@gmail.com",
                "SMTP Test Mail",
                "<h3>Mail sistemi çalışıyor 👍</h3><p>Her şey yolunda.</p>"
            );

            return Content("✅ Mail başarıyla gönderildi.");
        }
        catch (Exception ex)
        {
            return Content("❌ Mail hatası: " + ex.Message);
        }
    }
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var model = new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName ?? "",
            Email = user.Email ?? "",
            IsActive = user.IsActive
        };

        return View(model);
    }
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        model.AvailableRoles = await _roleManager.Roles
            .OrderBy(r => r.Name)
            .Select(r => new SelectListItem
            {
                Value = r.Name!,
                Text = r.Name!
            })
            .ToListAsync();

        if (!ModelState.IsValid)
            return View(model);

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "Bu e-posta ile kayıtlı kullanıcı zaten var.");
            return View(model);
        }

        var user = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        if (model.SelectedRoles != null && model.SelectedRoles.Any())
        {
            var roleResult = await _userManager.AddToRolesAsync(user, model.SelectedRoles);

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await _userManager.DeleteAsync(user);
                return View(model);
            }
        }
        else
        {
            await _userManager.AddToRoleAsync(user, "Author");
        }

        TempData["Success"] = "Yeni kullanıcı başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.IsActive = model.IsActive;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditRoles(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var userRoles = await _userManager.GetRolesAsync(user);
        var allRoles = _roleManager.Roles.ToList();

        var model = new EditUserRolesViewModel
        {
            UserId = user.Id,
            FullName = user.FullName ?? "",
            Email = user.Email ?? "",
            Roles = allRoles.Select(r => new RoleCheckboxViewModel
            {
                RoleName = r.Name ?? "",
                Selected = r.Name != null && userRoles.Contains(r.Name)
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRoles(EditUserRolesViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        var selectedRoles = model.Roles
            .Where(r => r.Selected)
            .Select(r => r.RoleName)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .ToList();

        if (selectedRoles.Any())
            await _userManager.AddToRolesAsync(user, selectedRoles);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (currentUserId == id)
        {
            return Json(new
            {
                success = false,
                message = "Kendi hesabınızı pasif yapamazsınız!"
            });
        }

        // 👇 senin mevcut kodun
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return Json(new { success = false });

        user.IsActive = !user.IsActive;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return Json(new { success = false });

        return Json(new
        {
            success = true,
            isActive = user.IsActive
        });
    }


}