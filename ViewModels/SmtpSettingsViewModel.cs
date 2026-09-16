using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.ViewModels
{
    public class SmtpSettingsViewModel
    {
        [Required(ErrorMessage = "SMTP sunucusu zorunludur.")]
        [MaxLength(200)]
        [Display(Name = "SMTP Sunucusu")]
        public string Host { get; set; } = "";

        [Range(1, 65535, ErrorMessage = "Port 1-65535 arasında olmalıdır.")]
        [Display(Name = "Port")]
        public int Port { get; set; } = 587;

        [MaxLength(200)]
        [Display(Name = "Kullanıcı Adı")]
        public string UserName { get; set; } = "";

        /// <summary>Bos bırakılırsa kayıtlı parola korunur.</summary>
        [DataType(DataType.Password)]
        [Display(Name = "Parola")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Gönderen e-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        [MaxLength(200)]
        [Display(Name = "Gönderen E-posta")]
        public string FromEmail { get; set; } = "";

        [MaxLength(200)]
        [Display(Name = "Gönderen Adı")]
        public string FromName { get; set; } = "";

        [Display(Name = "TLS (STARTTLS) kullan")]
        public bool EnableSsl { get; set; } = true;

        [Display(Name = "Bu ayarlar kullanılsın")]
        public bool IsActive { get; set; } = true;

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        [Display(Name = "Deneme e-postası alıcısı")]
        public string? TestRecipient { get; set; }

        // --- Salt okunur bilgi alanları ---

        /// <summary>Veritabanında kayıt var mı.</summary>
        public bool HasStoredSettings { get; set; }

        /// <summary>Veritabanında çözülebilir bir parola var mı.</summary>
        public bool HasStoredPassword { get; set; }

        /// <summary>Kayıtlı parola çözülemedi (Data Protection anahtarı değişmiş olabilir).</summary>
        public bool PasswordDecryptFailed { get; set; }

        /// <summary>Şu an e-posta gönderiminde fiilen kullanılan kaynak.</summary>
        public string EffectiveSource { get; set; } = "";

        public string EffectiveSummary { get; set; } = "";

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        public DateTime? LastTestAt { get; set; }
        public bool? LastTestSucceeded { get; set; }
        public string? LastTestMessage { get; set; }
    }
}
