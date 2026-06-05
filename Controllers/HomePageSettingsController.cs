using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Models;
using System.Linq;

namespace MyDergiApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HomePageSettingsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public HomePageSettingsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var settings = await _context.HomePageSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new HomePageSettings
                {
                    JournalTitle = "MyDergiApp",
                    JournalSubtitle = "Journal Management Panel",
                    HeroTitle = "Akademik Dergi Yönetim Sistemi",
                    AboutTitle = "Dergi Hakkında",
                    IsActive = true,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.HomePageSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            HomePageSettings model,
            IFormFile? heroImage,
            IFormFile? bannerImage,
            IFormFile? headerLogo,
            IFormFile? headerBackgroundImage,
            bool removeHeaderLogo = false,
            bool removeHeaderBackgroundImage = false,
            bool removeBannerImage = false)
        {
            var settings = await _context.HomePageSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new HomePageSettings();
                _context.HomePageSettings.Add(settings);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Temel alanlar
            settings.SiteTitle = string.IsNullOrWhiteSpace(model.SiteTitle)
                ? "MyDergiApp"
                : model.SiteTitle.Trim();

            settings.Subtitle = string.IsNullOrWhiteSpace(model.Subtitle)
                ? null
                : model.Subtitle.Trim();

            settings.Description = string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim();

            settings.ContactEmail = string.IsNullOrWhiteSpace(model.ContactEmail)
                ? null
                : model.ContactEmail.Trim();

            settings.ContactPhone = string.IsNullOrWhiteSpace(model.ContactPhone)
                ? null
                : model.ContactPhone.Trim();

            settings.Address = string.IsNullOrWhiteSpace(model.Address)
                ? null
                : model.Address.Trim();

            settings.IsActive = model.IsActive;

            // Tema alanları
            settings.ThemeName = string.IsNullOrWhiteSpace(model.ThemeName)
                ? "classic"
                : model.ThemeName.Trim();

            settings.PrimaryColor = string.IsNullOrWhiteSpace(model.PrimaryColor)
                ? "#0d6efd"
                : model.PrimaryColor.Trim();

            settings.SecondaryColor = string.IsNullOrWhiteSpace(model.SecondaryColor)
                ? "#198754"
                : model.SecondaryColor.Trim();

            settings.HeaderBgColor = string.IsNullOrWhiteSpace(model.HeaderBgColor)
                ? "#ffffff"
                : model.HeaderBgColor.Trim();

            settings.NavBgColor = string.IsNullOrWhiteSpace(model.NavBgColor)
                ? "#ffffff"
                : model.NavBgColor.Trim();

            settings.BodyBgColor = string.IsNullOrWhiteSpace(model.BodyBgColor)
                ? "#f8fafc"
                : model.BodyBgColor.Trim();

            settings.TextColor = string.IsNullOrWhiteSpace(model.TextColor)
                ? "#111827"
                : model.TextColor.Trim();

            // Banner alanları
            settings.ShowBanner = model.ShowBanner;

            settings.BannerTitle = string.IsNullOrWhiteSpace(model.BannerTitle)
                ? null
                : model.BannerTitle.Trim();

            settings.BannerDescription = string.IsNullOrWhiteSpace(model.BannerDescription)
                ? null
                : model.BannerDescription.Trim();

            settings.BannerLabel = string.IsNullOrWhiteSpace(model.BannerLabel)
                ? null
                : model.BannerLabel.Trim();

            settings.BannerPrimaryButtonText = string.IsNullOrWhiteSpace(model.BannerPrimaryButtonText)
                ? null
                : model.BannerPrimaryButtonText.Trim();

            settings.BannerPrimaryButtonUrl = string.IsNullOrWhiteSpace(model.BannerPrimaryButtonUrl)
                ? null
                : model.BannerPrimaryButtonUrl.Trim();

            settings.BannerSecondaryButtonText = string.IsNullOrWhiteSpace(model.BannerSecondaryButtonText)
                ? null
                : model.BannerSecondaryButtonText.Trim();

            settings.BannerSecondaryButtonUrl = string.IsNullOrWhiteSpace(model.BannerSecondaryButtonUrl)
                ? null
                : model.BannerSecondaryButtonUrl.Trim();
            settings.HeaderTitle = string.IsNullOrWhiteSpace(model.HeaderTitle)
                ? null
                : model.HeaderTitle.Trim();

            settings.HeaderSubtitle = string.IsNullOrWhiteSpace(model.HeaderSubtitle)
                ? null
                : model.HeaderSubtitle.Trim();

            settings.HeaderRightText = string.IsNullOrWhiteSpace(model.HeaderRightText)
                ? null
                : model.HeaderRightText.Trim();
            if (removeHeaderLogo)
{
    settings.HeaderLogoPath = null;
}

if (removeHeaderBackgroundImage)
{
    settings.HeaderBackgroundImagePath = null;
}

if (removeBannerImage)
{
    settings.BannerImagePath = null;
}
            settings.ShowHeaderLogo = model.ShowHeaderLogo;
            if (removeHeaderLogo)
            {
                settings.HeaderLogoPath = null;
            }

            if (removeHeaderBackgroundImage)
            {
                settings.HeaderBackgroundImagePath = null;
            }

            if (removeBannerImage)
            {
                settings.BannerImagePath = null;
            }
            if (headerLogo != null && headerLogo.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(headerLogo.FileName).ToLowerInvariant();

                if (!allowedExtensions.Any(x => x == ext))
                {
                    ModelState.AddModelError("HeaderLogoPath", "Sadece jpg, jpeg, png veya webp logo yüklenebilir.");
                    return View(model);
                }

                var folder = Path.Combine(_env.WebRootPath, "uploads", "homepage");
                Directory.CreateDirectory(folder);

                var fileName = $"header-logo-{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(folder, fileName);

                await using var headerLogoStream = new FileStream(fullPath, FileMode.Create);
                await headerLogo.CopyToAsync(headerLogoStream);

                settings.HeaderLogoPath = $"/uploads/homepage/{fileName}";
            }

            if (headerBackgroundImage != null && headerBackgroundImage.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(headerBackgroundImage.FileName).ToLowerInvariant();

                if (!allowedExtensions.Any(x => x == ext))
                {
                    ModelState.AddModelError("HeaderBackgroundImagePath", "Sadece jpg, jpeg, png veya webp arka plan görseli yüklenebilir.");
                    return View(model);
                }

                var folder = Path.Combine(_env.WebRootPath, "uploads", "homepage");
                Directory.CreateDirectory(folder);

                var fileName = $"header-bg-{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(folder, fileName);

                await using var headerBgStream = new FileStream(fullPath, FileMode.Create);
                await headerBackgroundImage.CopyToAsync(headerBgStream);

                settings.HeaderBackgroundImagePath = $"/uploads/homepage/{fileName}";
            }
            // Logo / ana görsel yükleme
            if (heroImage != null && heroImage.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(heroImage.FileName).ToLowerInvariant();

                if (!allowedExtensions.Any(x => x == ext))
                {
                    ModelState.AddModelError("LogoPath", "Sadece jpg, jpeg, png veya webp görsel yüklenebilir.");
                    return View(model);
                }

                var folder = Path.Combine(_env.WebRootPath, "uploads", "homepage");
                Directory.CreateDirectory(folder);

                var fileName = $"logo-{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(folder, fileName);

                await using var logoStream = new FileStream(fullPath, FileMode.Create);
                await heroImage.CopyToAsync(logoStream);

                settings.LogoPath = $"/uploads/homepage/{fileName}";
            }

            // Banner görseli yükleme
            if (bannerImage != null && bannerImage.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(bannerImage.FileName).ToLowerInvariant();

                if (!allowedExtensions.Any(x => x == ext))
                {
                    ModelState.AddModelError("BannerImagePath", "Sadece jpg, jpeg, png veya webp banner görseli yüklenebilir.");
                    return View(model);
                }

                var folder = Path.Combine(_env.WebRootPath, "uploads", "homepage");
                Directory.CreateDirectory(folder);

                var fileName = $"banner-{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(folder, fileName);

                await using var bannerStream = new FileStream(fullPath, FileMode.Create);
                await bannerImage.CopyToAsync(bannerStream);

                settings.BannerImagePath = $"/uploads/homepage/{fileName}";
            }

            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Ana sayfa ayarları güncellendi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
