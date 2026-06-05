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
using System.Linq;

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
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            TempData["Error"] = "Kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            TempData["Error"] = "Kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var currentUserId = _userManager.GetUserId(User);

        if (user.Id == currentUserId)
        {
            TempData["Error"] = "Kendi hesabınızı silemezsiniz.";
            return RedirectToAction(nameof(Index));
        }

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains("Admin"))
        {
            TempData["Error"] = "Admin kullanıcı silinemez.";
            return RedirectToAction(nameof(Index));
        }

        var hasSubmissionAsAuthor = await _context.Submissions
            .AnyAsync(s => s.AuthorId == user.Id);

        var hasSubmissionAsEditor = await _context.Submissions
            .AnyAsync(s => s.AssignedSectionEditorId == user.Id);


        var hasReviewerAssignment = await _context.SubmissionReviewers
            .AnyAsync(sr => sr.ReviewerId == user.Id);

        if (hasSubmissionAsAuthor ||
            hasSubmissionAsEditor ||
            hasReviewerAssignment)
        {
            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            TempData["Error"] = "Bu kullanıcı makale, editörlük, yazarlık veya hakemlik kayıtlarına bağlı olduğu için silinmedi; pasif yapıldı.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            TempData["Success"] = "Kullanıcı silindi.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
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
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var model = new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName ?? string.Empty,
            Email = user.Email ?? string.Empty,
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
        if (user == null)
            return NotFound();

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.NormalizedEmail = model.Email.ToUpperInvariant();
        user.NormalizedUserName = model.Email.ToUpperInvariant();
        user.IsActive = model.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (!resetResult.Succeeded)
            {
                foreach (var error in resetResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }
        }

        TempData["Success"] = "Kullanıcı başarıyla güncellendi.";
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

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRoles(EditUserRolesViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId);

        if (user == null)
            return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        var currentRoles = await _userManager.GetRolesAsync(user);

        var selectedRoles = model.Roles?
            .Where(r => r.Selected)
            .Select(r => r.RoleName)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct()
            .ToList() ?? new List<string>();

        foreach (var roleName in selectedRoles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                TempData["Error"] = $"Geçersiz rol seçildi: {roleName}";
                return RedirectToAction(nameof(EditRoles), new { id = model.UserId });
            }
        }

        if (user.Id == currentUserId && currentRoles.Contains("Admin") && !selectedRoles.Contains("Admin"))
        {
            TempData["Error"] = "Kendi Admin rolünüzü kaldıramazsınız.";
            return RedirectToAction(nameof(EditRoles), new { id = model.UserId });
        }

        if (!selectedRoles.Any())
        {
            selectedRoles.Add("Author");
        }

        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

        if (!removeResult.Succeeded)
        {
            TempData["Error"] = string.Join(" | ", removeResult.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(EditRoles), new { id = model.UserId });
        }

        var addResult = await _userManager.AddToRolesAsync(user, selectedRoles);

        if (!addResult.Succeeded)
        {
            TempData["Error"] = string.Join(" | ", addResult.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(EditRoles), new { id = model.UserId });
        }

        TempData["Success"] = "Kullanıcı rolleri güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Json(new
            {
                success = false,
                message = "Kullanıcı bilgisi alınamadı."
            });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (currentUserId == id)
        {
            return Json(new
            {
                success = false,
                message = "Kendi hesabınızı pasif yapamazsınız."
            });
        }

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return Json(new
            {
                success = false,
                message = "Kullanıcı bulunamadı."
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains("Admin"))
        {
            return Json(new
            {
                success = false,
                message = "Admin kullanıcı pasif yapılamaz."
            });
        }

        user.IsActive = !user.IsActive;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return Json(new
            {
                success = false,
                message = string.Join(" | ", result.Errors.Select(e => e.Description))
            });
        }

        return Json(new
        {
            success = true,
            isActive = user.IsActive,
            message = user.IsActive
                ? "Kullanıcı aktif yapıldı."
                : "Kullanıcı pasif yapıldı."
        });
    }


}