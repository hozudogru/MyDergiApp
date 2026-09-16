using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyDergiApp.Data;
using MyDergiApp.Models;
using MyDergiApp.ViewModels;

namespace MyDergiApp.Services
{
    /// <summary>
    /// SMTP ayarlarinin tek kaynagi. Oncelik: veritabanindaki aktif kayit; yoksa appsettings "SMTP" bolumu.
    /// Parola veritabaninda Data Protection ile sifreli tutulur.
    /// </summary>
    public class SmtpSettingsService
    {
        public const string SourceDatabase = "Veritabanı";
        public const string SourceConfig = "appsettings";

        private const string ProtectorPurpose = "MyDergiApp.SmtpSetting.Password.v1";

        private readonly AppDbContext _context;
        private readonly SmtpSettings _configSettings;
        private readonly IDataProtector _protector;

        public SmtpSettingsService(
            AppDbContext context,
            IOptions<SmtpSettings> configSettings,
            IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;
            _configSettings = configSettings.Value;
            _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        }

        public Task<SmtpSetting?> GetStoredAsync()
            => _context.SmtpSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();

        /// <summary>E-posta gonderiminde fiilen kullanilacak ayarlar ve kaynagi.</summary>
        public async Task<(SmtpSettings Settings, string Source)> GetEffectiveAsync()
        {
            var stored = await GetStoredAsync();

            if (stored != null && stored.IsActive && !string.IsNullOrWhiteSpace(stored.Host))
            {
                return (ToRuntime(stored, TryUnprotect(stored.ProtectedPassword) ?? ""), SourceDatabase);
            }

            return (_configSettings, SourceConfig);
        }

        public bool IsConfigured(SmtpSettings s)
            => !string.IsNullOrWhiteSpace(s.Host)
               && s.Port > 0
               && !string.IsNullOrWhiteSpace(s.FromEmail);

        public async Task<SmtpSettingsViewModel> BuildViewModelAsync()
        {
            var stored = await GetStoredAsync();
            var (effective, source) = await GetEffectiveAsync();

            var vm = new SmtpSettingsViewModel
            {
                EffectiveSource = source,
                EffectiveSummary = IsConfigured(effective)
                    ? $"{effective.Host}:{effective.Port} — {effective.FromEmail}"
                    : "Tanımlı SMTP ayarı yok",
                HasStoredSettings = stored != null
            };

            if (stored != null)
            {
                vm.Host = stored.Host;
                vm.Port = stored.Port;
                vm.UserName = stored.UserName;
                vm.FromEmail = stored.FromEmail;
                vm.FromName = stored.FromName;
                vm.EnableSsl = stored.EnableSsl;
                vm.IsActive = stored.IsActive;
                vm.UpdatedAt = stored.UpdatedAt;
                vm.UpdatedBy = stored.UpdatedBy;
                vm.LastTestAt = stored.LastTestAt;
                vm.LastTestSucceeded = stored.LastTestSucceeded;
                vm.LastTestMessage = stored.LastTestMessage;

                if (!string.IsNullOrEmpty(stored.ProtectedPassword))
                {
                    var plain = TryUnprotect(stored.ProtectedPassword);
                    vm.HasStoredPassword = plain != null;
                    vm.PasswordDecryptFailed = plain == null;
                }
            }
            else
            {
                // Ilk acilista formu appsettings'teki degerlerle doldur (parola haric).
                vm.Host = _configSettings.Host;
                vm.Port = _configSettings.Port > 0 ? _configSettings.Port : 587;
                vm.UserName = _configSettings.UserName;
                vm.FromEmail = _configSettings.FromEmail;
                vm.FromName = _configSettings.FromName;
                vm.EnableSsl = _configSettings.EnableSsl;
            }

            return vm;
        }

        /// <summary>
        /// Formdaki degerlerden calisma zamani ayarlarini uretir. Parola bos birakildiysa
        /// kayitli (veya kayit yoksa appsettings'teki) parola kullanilir.
        /// </summary>
        public async Task<SmtpSettings> FromViewModelAsync(SmtpSettingsViewModel vm)
        {
            var password = vm.Password;

            if (string.IsNullOrEmpty(password))
            {
                var stored = await GetStoredAsync();
                password = stored != null
                    ? TryUnprotect(stored.ProtectedPassword) ?? ""
                    : _configSettings.Password;
            }

            return new SmtpSettings
            {
                Host = vm.Host.Trim(),
                Port = vm.Port,
                UserName = vm.UserName?.Trim() ?? "",
                Password = password ?? "",
                FromEmail = vm.FromEmail.Trim(),
                FromName = vm.FromName?.Trim() ?? "",
                EnableSsl = vm.EnableSsl
            };
        }

        public async Task SaveAsync(SmtpSettingsViewModel vm, string? updatedBy)
        {
            var stored = await GetStoredAsync();

            if (stored == null)
            {
                stored = new SmtpSetting();
                _context.SmtpSettings.Add(stored);
            }

            stored.Host = vm.Host.Trim();
            stored.Port = vm.Port;
            stored.UserName = vm.UserName?.Trim() ?? "";
            stored.FromEmail = vm.FromEmail.Trim();
            stored.FromName = vm.FromName?.Trim() ?? "";
            stored.EnableSsl = vm.EnableSsl;
            stored.IsActive = vm.IsActive;
            stored.UpdatedAt = DateTime.UtcNow;
            stored.UpdatedBy = updatedBy;

            if (!string.IsNullOrEmpty(vm.Password))
            {
                stored.ProtectedPassword = _protector.Protect(vm.Password);
            }
            else if (stored.Id == 0 && !string.IsNullOrEmpty(_configSettings.Password))
            {
                // Ilk kayitta parola girilmediyse appsettings'tekini devral.
                stored.ProtectedPassword = _protector.Protect(_configSettings.Password);
            }

            await _context.SaveChangesAsync();
        }

        public async Task RecordTestResultAsync(bool succeeded, string message)
        {
            var stored = await GetStoredAsync();
            if (stored == null) return;

            stored.LastTestAt = DateTime.UtcNow;
            stored.LastTestSucceeded = succeeded;
            stored.LastTestMessage = message.Length > 1000 ? message[..1000] : message;

            await _context.SaveChangesAsync();
        }

        private string? TryUnprotect(string protectedValue)
        {
            if (string.IsNullOrEmpty(protectedValue)) return "";

            try
            {
                return _protector.Unprotect(protectedValue);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        private static SmtpSettings ToRuntime(SmtpSetting s, string password) => new()
        {
            Host = s.Host,
            Port = s.Port,
            UserName = s.UserName,
            Password = password,
            FromEmail = s.FromEmail,
            FromName = s.FromName,
            EnableSsl = s.EnableSsl
        };
    }
}
