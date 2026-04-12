using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyDergiApp.Entities;
using MyDergiApp.ViewModels;
using MyDergiApp.ViewModels.Users;

[Authorize(Roles = "Admin")]
public class UserManagementController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserManagementController(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.ToList();
        var model = new List<UserListViewModel>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);

            model.Add(new UserListViewModel
            {
                Id = u.Id,
                FullName = u.FullName ?? "",
                Email = u.Email ?? "",
                IsActive = u.IsActive,
                RoleName = roles.Any() ? string.Join(", ", roles) : "",
                CreatedAt = u.CreatedAt
            });
        }

        return View(model);
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        return RedirectToAction(nameof(Index));
    }
}