using Microsoft.AspNetCore.Authentication;
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
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<EmailTemplateService>();

// DbContext
// DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));// Identity
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

// Cookie paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
});

// MVC + Razor
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// DB migrate + roles + test data
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

    var adminUser = await EnsureUserAsync(
        "admin@dergi.com",
        "admin",
        "Admin123!",
        "Admin",
        "Sistem Yöneticisi");

    var editorUser = await EnsureUserAsync(
        "editor@dergi.com",
        "editor",
        "Editor123!",
        "Editor",
        "Test Editör");

    var reviewerUser = await EnsureUserAsync(
        "reviewer@dergi.com",
        "reviewer",
        "Reviewer123!",
        "Reviewer",
        "Test Hakem");

    var authorUser = await EnsureUserAsync(
        "author@dergi.com",
        "author",
        "Author123!",
        "Author",
        "Test Yazar");

    var existingSubmission = await dbContext.Submissions
        .Include(s => s.Reviews)
        .FirstOrDefaultAsync(s => s.Title == "Test Article");

    if (existingSubmission == null)
    {
        var submission = new Submission
        {
            Title = "Test Article",
            Abstract = "Bu, hakem modülünü test etmek için oluşturulmuş örnek makaledir.",
            Keywords = "test, review, journal",
            AuthorId = authorUser.Id,
            FilePath = "/uploads/test-article.pdf",
            Status = SubmissionStatus.Gonderildi,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            NoteToEditor = "Bu makale test amaçlı oluşturulmuştur.",
            DecisionNote = ""
        };

        dbContext.Submissions.Add(submission);
        await dbContext.SaveChangesAsync();

        var assignmentExists = await dbContext.SubmissionReviewers
            .AnyAsync(x => x.SubmissionId == submission.Id && x.ReviewerId == reviewerUser.Id);

        if (!assignmentExists)
        {
            dbContext.SubmissionReviewers.Add(new SubmissionReviewer
            {
                SubmissionId = submission.Id,
                ReviewerId = reviewerUser.Id,
                Status = ReviewerAssignmentStatus.Assigned,
                ReviewRound = submission.CurrentReviewRound,
                AssignedAt = DateTime.UtcNow,
                CompletedAt = null,
                ReviewNote = null
            });
        }

        dbContext.Reviews.Add(new Review
        {
            SubmissionId = submission.Id,
            ReviewerId = reviewerUser.Id,
            ReviewRound = submission.CurrentReviewRound,
            Comments = "Genel olarak umut verici bir çalışma. Yöntem kısmı daha ayrıntılı yazılabilir.",
            CommentToAuthor = "Literatür bölümü genişletilmeli ve yöntem daha açık anlatılmalıdır.",
            CommentToEditor = "Makale yayın potansiyeli taşıyor ancak revizyon gerekli.",
            Strengths = "Konu güncel, problem tanımı açık.",
            Weaknesses = "Yöntem bölümü kısa, bazı kaynaklar eksik.",
            ScopeFit = "Evet",
            Decision = "Minor Revision",
            OriginalityScore = 4,
            MethodologyScore = 3,
            LiteratureScore = 3,
            WritingQualityScore = 4,
            OverallScore = 7,
            HasEthicalIssue = false,
            EthicalConcerns = null,
            IsDraft = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SubmittedAt = null
        });

        await dbContext.SaveChangesAsync();
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
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();