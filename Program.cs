using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Entities;
using MyDergiApp.Models;
using MyDergiApp.Services;

var builder = WebApplication.CreateBuilder(args);

// SMTP
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("SMTP"));

// Custom services
builder.Services.AddScoped<SmtpSettingsService>(); // DB'deki SMTP ayarlari, yoksa appsettings
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<EmailTemplateService>();

// DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Identity
builder.Services
    .AddIdentity<AppUser, IdentityRole>(options =>
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

// Rol/aktiflik degisiklikleri cookie'ye en gec 1 dk icinde yansisin (varsayilan 30 dk):
// SecurityStampValidator bu aralikla DB'deki security stamp'i kontrol edip principal'i yeniden kurar.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(1);
});

// Cookie paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
});

// MVC + Razor
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Reverse proxy (canlida Plesk nginx -> 127.0.0.1:5002). X-Forwarded-For / X-Forwarded-Proto
// basliklarini yalnizca ayni makinedeki proxy'den (loopback) kabul et; boylece Request.Scheme
// "https" olur, yonlendirmeler ve HSTS dogru calisir, loglarda gercek istemci IP'si gorunur.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Clear();
    options.KnownNetworks.Clear();
    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);
});

var app = builder.Build();

// Pipeline'in en basinda olmali: sonraki middleware'ler (HTTPS yonlendirme, HSTS, auth) dogru scheme'i gorsun
app.UseForwardedHeaders();

// DB migrate + roles + default admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var dbContext = services.GetRequiredService<AppDbContext>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();

    await dbContext.Database.MigrateAsync();

    string[] roles = { "Admin", "Editor", "ChiefEditor", "Reviewer", "Author", "Reader" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    async Task<AppUser> EnsureUserAsync(
        string email,
        string userName,
        string password,
        string role,
        string? fullName = null,
        bool isActive = true)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new AppUser
            {
                UserName = userName,
                Email = email,
                FullName = fullName ?? userName,
                EmailConfirmed = true,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(" | ", createResult.Errors.Select(e => e.Description));
                throw new Exception($"Kullanıcı oluşturulamadı: {email} - {errors}");
            }
        }
        else
        {
            var updated = false;

            // Eski seed "admin" kullanici adi vermisti; giris e-posta ile yapildigi icin esitle.
            if (!string.Equals(user.UserName, email, StringComparison.OrdinalIgnoreCase))
            {
                user.UserName = email;
                updated = true;
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                updated = true;
            }

            if (user.IsActive != isActive)
            {
                user.IsActive = isActive;
                updated = true;
            }

            if (string.IsNullOrWhiteSpace(user.FullName) && !string.IsNullOrWhiteSpace(fullName))
            {
                user.FullName = fullName;
                updated = true;
            }

            if (updated)
            {
                var updateResult = await userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(" | ", updateResult.Errors.Select(e => e.Description));
                    throw new Exception($"Kullanıcı güncellenemedi: {email} - {errors}");
                }
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var addRoleResult = await userManager.AddToRoleAsync(user, role);

            if (!addRoleResult.Succeeded)
            {
                var errors = string.Join(" | ", addRoleResult.Errors.Select(e => e.Description));
                throw new Exception($"Rol atanamadı: {email} - {errors}");
            }
        }

        return user;
    }

    // Kullanici adi = e-posta: giris sayfasi e-posta ile arar, Register de UserName=Email yazar.
    // (Onceden "admin" idi ve seed edilen admin login formundan giris yapamiyordu.)
    await EnsureUserAsync(
        "admin@dergi.com",
        "admin@dergi.com",
        "Admin123!",
        "Admin",
        "Sistem Yöneticisi");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Makale dosyalari (/uploads/submissions/**: ana metin, revizyon, hakem ekleri) statik olarak servis EDILMEZ;
// bu yol SubmissionController.ServeSubmissionFile'a duser ve orada yetki kontrolu yapilir.
// Kapak, sayi PDF'i, yayinlanan makale PDF'i, logo vb. (/uploads/covers, /uploads/published ...) herkese aciktir.
app.UseWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/uploads/submissions"),
    branch => branch.UseStaticFiles());

// Varsayilan Identity UI'in istenmeyen sayfalari: hesap silme (FK'lar yuzunden 500 verir, admin pasif/silme
// kontrollerini atlar) ve e-posta degistirme (UserName ile e-posta ayrisir, kullanici yeni e-postayla giremez).
app.Use(async (context, next) =>
{
    var path = context.Request.Path;

    if (path.StartsWithSegments("/Identity/Account/Manage/DeletePersonalData", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/Identity/Account/Manage/PersonalData", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/Identity/Account/Manage/DownloadPersonalData", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/Identity/Account/Manage/Email", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

app.UseRouting();

app.UseAuthentication();

// Rolü olmayan kullanıcıya otomatik Author rolü ver
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
                var addResult = await userManager.AddToRoleAsync(user, "Author");

                // Rol claim'i cookie'de tasinir; yenilenmezse ayni istekte ve 30 dk boyunca kullanici rolsuz gorunur
                // (menude "Makalelerim" cikmaz, sayfa 403 verir). Oturumu hemen yenile.
                if (addResult.Succeeded && !context.Response.HasStarted)
                {
                    var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<AppUser>>();
                    await signInManager.RefreshSignInAsync(user);

                    // Bu istegin yetkilendirmesi de yeni rolu gorsun
                    context.User = await signInManager.CreateUserPrincipalAsync(user);
                }
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
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();