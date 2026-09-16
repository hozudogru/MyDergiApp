using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.Models
{
    /// <summary>
    /// Veritabaninda tutulan SMTP ayarlari (tek satir). Kayit yoksa veya pasifse
    /// uygulama appsettings'teki "SMTP" bolumune duser (bkz. SmtpSettingsService).
    /// </summary>
    public class SmtpSetting
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Host { get; set; } = "";

        public int Port { get; set; } = 587;

        [MaxLength(200)]
        public string UserName { get; set; } = "";

        /// <summary>Data Protection ile sifrelenmis parola. Duz metin tutulmaz.</summary>
        public string ProtectedPassword { get; set; } = "";

        [Required, MaxLength(200)]
        public string FromEmail { get; set; } = "";

        [MaxLength(200)]
        public string FromName { get; set; } = "";

        /// <summary>System.Net.Mail icin STARTTLS (587). 465/SslOnConnect desteklenmez.</summary>
        public bool EnableSsl { get; set; } = true;

        /// <summary>Pasifse appsettings'teki ayarlar kullanilir.</summary>
        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; }

        [MaxLength(256)]
        public string? UpdatedBy { get; set; }

        public DateTime? LastTestAt { get; set; }
        public bool? LastTestSucceeded { get; set; }

        [MaxLength(1000)]
        public string? LastTestMessage { get; set; }
    }
}
