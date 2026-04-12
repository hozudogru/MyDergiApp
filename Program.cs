using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Entities;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// Cookie paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
});

// MVC + Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Seed roles + default admin
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    string[] roles = { "Admin", "Editor", "Hakem", "Yazar", "Okuyucu" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var adminEmail = "admin@dergi.com";
    var adminPassword = "Admin123!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Sistem Yöneticisi",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(" | ", createResult.Errors.Select(x => x.Description));
            throw new Exception($"Varsayılan admin oluşturulamadı: {errors}");
        }
    }
    else
    {
        bool updated = false;

        if (!adminUser.EmailConfirmed)
        {
            adminUser.EmailConfirmed = true;
            updated = true;
        }

        if (!adminUser.IsActive)
        {
            adminUser.IsActive = true;
            updated = true;
        }

        if (string.IsNullOrWhiteSpace(adminUser.FullName))
        {
            adminUser.FullName = "Sistem Yöneticisi";
            updated = true;
        }

        if (updated)
        {
            var updateResult = await userManager.UpdateAsync(adminUser);

            if (!updateResult.Succeeded)
            {
                var errors = string.Join(" | ", updateResult.Errors.Select(x => x.Description));
                throw new Exception($"Varsayılan admin güncellenemedi: {errors}");
            }
        }
    }

    var adminRoles = await userManager.GetRolesAsync(adminUser);
    if (!adminRoles.Contains("Admin"))
    {
        var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");

        if (!roleResult.Succeeded)
        {
            var errors = string.Join(" | ", roleResult.Errors.Select(x => x.Description));
            throw new Exception($"Admin rolü atanamadı: {errors}");
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();

// Oturum açmış ama rolü olmayan kullanıcıya otomatik Author rolü ver
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

    var skipCheck =
        path.StartsWith("/identity/account/login") ||
        path.StartsWith("/identity/account/logout") ||
        path.StartsWith("/identity/account/register") ||
        path.StartsWith("/identity/account/accessdenied") ||
        path.StartsWith("/home/accessdenied");

    if (context.User.Identity?.IsAuthenticated == true && !skipCheck)
    {
        using var scope = context.RequestServices.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.GetUserAsync(context.User);

        if (user != null)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Any())
            {
                await userManager.AddToRoleAsync(user, "Author");
            }
        }
    }

    await next();
});

// Pasif kullanıcı kontrolü
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

    var skipCheck =
        path.StartsWith("/identity/account/login") ||
        path.StartsWith("/identity/account/logout") ||
        path.StartsWith("/identity/account/register") ||
        path.StartsWith("/identity/account/accessdenied") ||
        path.StartsWith("/home/accessdenied");

    if (context.User.Identity?.IsAuthenticated == true && !skipCheck)
    {
        using var scope = context.RequestServices.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.GetUserAsync(context.User);

        if (user != null && !user.IsActive)
        {
            await context.SignOutAsync();
            context.Response.Redirect("/Home/AccessDenied");
            return;
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();