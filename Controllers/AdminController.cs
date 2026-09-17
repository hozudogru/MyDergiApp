using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDergiApp.Data;
using MyDergiApp.Models;
using MyDergiApp.Services;
using MyDergiApp.ViewModels;
using Microsoft.Extensions.Configuration;


namespace MyDergiApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly SmtpSettingsService _smtpSettings;
        private readonly EmailService _emailService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            AppDbContext context,
            IConfiguration configuration,
            SmtpSettingsService smtpSettings,
            EmailService emailService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _configuration = configuration;
            _smtpSettings = smtpSettings;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel
            {
                TotalSubmissions = await _context.Submissions.CountAsync(),

                PendingSubmissions = await _context.Submissions.CountAsync(x =>
                    x.Status == SubmissionStatus.OnKontrolBekliyor ||
                    x.Status == SubmissionStatus.Gonderildi),

                InReviewSubmissions = await _context.Submissions.CountAsync(x =>
                    x.Status == SubmissionStatus.HakemAtamasiBekliyor ||
                    x.Status == SubmissionStatus.HakemDegerlendirmesinde ||
                    x.Status == SubmissionStatus.RevizyonIstendi ||
                    x.Status == SubmissionStatus.RevizyonYuklendi),

                AcceptedSubmissions = await _context.Submissions.CountAsync(x =>
                    x.Status == SubmissionStatus.KabulEdildi),

                RejectedSubmissions = await _context.Submissions.CountAsync(x =>
                    x.Status == SubmissionStatus.Reddedildi),

                TotalUsers = await _context.Users.CountAsync(),
                ActiveUsers = await _context.Users.CountAsync(x => x.IsActive),
                PassiveUsers = await _context.Users.CountAsync(x => !x.IsActive),

                TotalAnnouncements = await _context.Announcements.CountAsync(),
                ActiveAnnouncements = await _context.Announcements.CountAsync(x => x.IsActive),

                TotalIssues = await _context.Issues.CountAsync(),
                PublishedIssues = await _context.Issues.CountAsync(x => x.IsPublished),

                // Ana sayfa sayaciyla ayni tanim: yalnizca yayindaki sayilardaki makaleler
                TotalPublishedArticles = await _context.PublishedArticles.CountAsync(x => x.Issue != null && x.Issue.IsPublished),
                TotalIndexes = await _context.JournalIndexes.CountAsync(x => x.IsActive),
                HasHomePageSettings = await _context.HomePageSettings.AnyAsync(x => x.IsActive),

                HasActiveAnnouncement = await _context.Announcements.AnyAsync(x => x.IsActive),

                HasPublishedIssue = await _context.Issues.AnyAsync(x => x.IsPublished),

                HasPublishedArticle = await _context.PublishedArticles
                    .Include(x => x.Issue)
                    .AnyAsync(x => x.Issue != null && x.Issue.IsPublished),

                HasActiveIndexes = await _context.JournalIndexes.AnyAsync(x => x.IsActive),

                HasPassiveUsers = await _context.Users.AnyAsync(x => !x.IsActive),

                HasSmtpSettings = false
            };

            var (effectiveSmtp, _) = await _smtpSettings.GetEffectiveAsync();
            model.HasSmtpSettings = _smtpSettings.IsConfigured(effectiveSmtp);

            return View(model);
        }

        // ---------------- SMTP Ayarları ----------------

        [HttpGet]
        public async Task<IActionResult> Smtp()
        {
            var vm = await _smtpSettings.BuildViewModelAsync();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Smtp(SmtpSettingsViewModel vm)
        {
            // Deneme alıcısı kaydetme sırasında zorunlu değil
            ModelState.Remove(nameof(SmtpSettingsViewModel.TestRecipient));

            if (!ModelState.IsValid)
            {
                return View(await MergeReadOnlyAsync(vm));
            }

            await _smtpSettings.SaveAsync(vm, User.Identity?.Name);

            TempData["Success"] = "SMTP ayarları kaydedildi.";
            return RedirectToAction(nameof(Smtp));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SmtpTest(SmtpSettingsViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.TestRecipient))
            {
                ModelState.AddModelError(nameof(vm.TestRecipient), "Deneme e-postasının gönderileceği adresi girin.");
            }

            if (!ModelState.IsValid)
            {
                return View(nameof(Smtp), await MergeReadOnlyAsync(vm));
            }

            var settings = await _smtpSettings.FromViewModelAsync(vm);
            var recipient = vm.TestRecipient!.Trim();

            try
            {
                await _emailService.SendTestEmailAsync(settings, recipient);

                var okMessage = $"{recipient} adresine deneme e-postası gönderildi ({settings.Host}:{settings.Port}).";
                await _smtpSettings.RecordTestResultAsync(true, okMessage);

                TempData["Success"] = okMessage + " Gelen kutusunu (ve spam klasörünü) kontrol edin.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SMTP deneme e-postası gönderilemedi ({Host}:{Port})", settings.Host, settings.Port);

                var reason = DescribeSmtpError(ex);
                await _smtpSettings.RecordTestResultAsync(false, reason);

                TempData["Error"] = "Deneme e-postası gönderilemedi: " + reason;
            }

            // Form değerleri kaydedilmedi; kullanıcının girdikleri kaybolmasın diye aynı görünüme dönülür.
            return View(nameof(Smtp), await MergeReadOnlyAsync(vm));
        }

        private async Task<SmtpSettingsViewModel> MergeReadOnlyAsync(SmtpSettingsViewModel vm)
        {
            var stored = await _smtpSettings.BuildViewModelAsync();

            vm.HasStoredSettings = stored.HasStoredSettings;
            vm.HasStoredPassword = stored.HasStoredPassword;
            vm.PasswordDecryptFailed = stored.PasswordDecryptFailed;
            vm.EffectiveSource = stored.EffectiveSource;
            vm.EffectiveSummary = stored.EffectiveSummary;
            vm.UpdatedAt = stored.UpdatedAt;
            vm.UpdatedBy = stored.UpdatedBy;
            vm.LastTestAt = stored.LastTestAt;
            vm.LastTestSucceeded = stored.LastTestSucceeded;
            vm.LastTestMessage = stored.LastTestMessage;

            return vm;
        }

        private static string DescribeSmtpError(Exception ex)
        {
            // En içteki anlamlı mesajı bul
            var inner = ex;
            while (inner.InnerException != null) inner = inner.InnerException;

            // SmtpException çoğu zaman asıl ağ hatasını (bağlantı reddi, DNS) içine sarar
            if (inner is System.Net.Sockets.SocketException socket)
            {
                return $"Sunucuya bağlanılamadı: {socket.Message} Sunucu adını ve portu kontrol edin.";
            }

            return ex switch
            {
                System.Net.Mail.SmtpFailedRecipientException r =>
                    $"Sunucu alıcıyı reddetti ({r.FailedRecipient}): {r.Message}",
                System.Net.Mail.SmtpException s when s.StatusCode == System.Net.Mail.SmtpStatusCode.MustIssueStartTlsFirst =>
                    "Sunucu TLS istiyor. 'TLS (STARTTLS) kullan' seçeneğini açın.",
                System.Net.Mail.SmtpException s when s.Message.Contains("5.7", StringComparison.Ordinal)
                                                    || s.Message.Contains("authent", StringComparison.OrdinalIgnoreCase) =>
                    "Kimlik doğrulama başarısız. Kullanıcı adı ve parolayı kontrol edin. Sunucu yanıtı: " + s.Message,
                System.Net.Mail.SmtpException s =>
                    $"SMTP hatası ({s.StatusCode}): {inner.Message}",
                TimeoutException t => t.Message,
                System.Net.Sockets.SocketException so =>
                    $"Sunucuya bağlanılamadı: {so.Message}. Sunucu adı ve portu kontrol edin.",
                _ => inner.Message
            };
        }
    }
}