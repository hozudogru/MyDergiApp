using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Helpers;
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

            // Dergi kimliği / karşılama / hakkında / ISSN / alt bilgi — formda vardı ama daha önce hiç kaydedilmiyordu
            // JournalTitle DB'de NOT NULL; bos gonderilirse mevcut deger korunur
            settings.JournalTitle = Clean(model.JournalTitle) ?? settings.JournalTitle ?? "MyDergiApp";
            settings.JournalSubtitle = Clean(model.JournalSubtitle);
            settings.HeroTitle = Clean(model.HeroTitle);
            settings.HeroDescription = Clean(model.HeroDescription);
            settings.AboutTitle = Clean(model.AboutTitle);
            settings.AboutContent = Clean(model.AboutContent);
            settings.PrintIssn = Clean(model.PrintIssn);
            settings.OnlineIssn = Clean(model.OnlineIssn);
            settings.FooterText = Clean(model.FooterText);

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
            // Kaldirma bayraklari: eski dosya diskten de silinir (yetim dosya birikmesin)
            if (removeHeaderLogo)
            {
                UploadHelper.TryDeleteWebFile(_env, settings.HeaderLogoPath);
                UploadHelper.TryDeleteWebFile(_env, settings.LogoPath);
                settings.HeaderLogoPath = null;
                settings.LogoPath = null; // eski alan; ana sayfada fallback olarak gorunmeye devam ediyordu
            }

            if (removeHeaderBackgroundImage)
            {
                UploadHelper.TryDeleteWebFile(_env, settings.HeaderBackgroundImagePath);
                settings.HeaderBackgroundImagePath = null;
            }

            if (removeBannerImage)
            {
                UploadHelper.TryDeleteWebFile(_env, settings.BannerImagePath);
                settings.BannerImagePath = null;
            }

            // Gorsel yuklemeleri: uzanti + boyut dogrulamasi, eski dosya silinir, yeni dosya kaydedilir
            var imageUploads = new (IFormFile? File, string Field, string Label, string Prefix, Func<string?> GetOld, Action<string> SetNew)[]
            {
                (headerLogo, "HeaderLogoPath", "Header logosu", "header-logo-", () => settings.HeaderLogoPath, p => settings.HeaderLogoPath = p),
                (headerBackgroundImage, "HeaderBackgroundImagePath", "Header arka plan görseli", "header-bg-", () => settings.HeaderBackgroundImagePath, p => settings.HeaderBackgroundImagePath = p),
                (heroImage, "LogoPath", "Logo görseli", "logo-", () => settings.LogoPath, p => settings.LogoPath = p),
                (bannerImage, "BannerImagePath", "Banner görseli", "banner-", () => settings.BannerImagePath, p => settings.BannerImagePath = p),
            };

            foreach (var upload in imageUploads)
            {
                if (upload.File == null || upload.File.Length == 0)
                    continue;

                var error = UploadHelper.Validate(upload.File, UploadHelper.ImageExtensions, UploadHelper.MaxImageBytes, upload.Label);

                if (error != null)
                {
                    ModelState.AddModelError(upload.Field, error);
                    return View(model);
                }
            }

            foreach (var upload in imageUploads)
            {
                if (upload.File == null || upload.File.Length == 0)
                    continue;

                UploadHelper.TryDeleteWebFile(_env, upload.GetOld());
                upload.SetNew(await UploadHelper.SaveAsync(_env, upload.File, "homepage", upload.Prefix));
            }

            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Ana sayfa ayarları güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        private static string? Clean(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
