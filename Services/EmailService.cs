using System.Net;
using System.Net.Mail;
using MyDergiApp.Services;

public class EmailService
{
    private const int TimeoutMilliseconds = 20000;

    private readonly SmtpSettingsService _settingsService;
    private readonly ILogger<EmailService> _logger;

    public EmailService(SmtpSettingsService settingsService, ILogger<EmailService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Bildirim e-postasi gonderir. SMTP hatasi istegi COKERTMEZ: hata loglanir ve false doner.
    /// (Onceden makale kaydedildikten sonra atilan e-posta patlayinca yazar 500 goruyor ve makaleyi
    /// tekrar gonderiyordu.) Baglanti sorununu gormek icin Admin > SMTP Ayarlari > Deneme Gonder kullanilir.
    /// </summary>
    public Task<bool> SendEmailAsync(string to, string subject, string body)
        => SendEmailWithAttachmentsAsync(to, subject, body, null);

    public async Task<bool> SendEmailWithAttachmentsAsync(
        string to,
        string subject,
        string body,
        List<string>? attachmentPaths = null)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("E-posta gönderilmedi: alıcı adresi boş. Konu: {Subject}", subject);
            return false;
        }

        try
        {
            var (settings, _) = await _settingsService.GetEffectiveAsync();

            using var mail = BuildMessage(settings, to, subject, body);

            if (attachmentPaths != null)
            {
                foreach (var relativePath in attachmentPaths.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    var cleanRelative = relativePath!.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", cleanRelative);

                    if (System.IO.File.Exists(fullPath))
                    {
                        mail.Attachments.Add(new Attachment(fullPath));
                    }
                }
            }

            await SendCoreAsync(settings, mail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-posta gönderilemedi. Alıcı: {To}, Konu: {Subject}", to, subject);
            return false;
        }
    }

    /// <summary>
    /// Verilen ayarlarla (kaydedilmemis form degerleri olabilir) bir deneme e-postasi gonderir.
    /// Hata durumunda istisna firlatir; cagiran taraf mesaji kullaniciya gosterir.
    /// </summary>
    public async Task SendTestEmailAsync(SmtpSettings settings, string to)
    {
        var body = $@"
            <p>Bu, <strong>DergiASP</strong> yönetim panelinden gönderilen bir SMTP deneme e-postasıdır.</p>
            <ul>
                <li>Sunucu: {WebUtility.HtmlEncode(settings.Host)}:{settings.Port}</li>
                <li>Gönderen: {WebUtility.HtmlEncode(settings.FromEmail)}</li>
                <li>TLS: {(settings.EnableSsl ? "Açık" : "Kapalı")}</li>
                <li>Zaman (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</li>
            </ul>
            <p>Bu e-postayı aldıysanız SMTP ayarları çalışıyor.</p>";

        using var mail = BuildMessage(settings, to, "DergiASP SMTP deneme e-postası", body);
        await SendCoreAsync(settings, mail);
    }

    private static MailMessage BuildMessage(SmtpSettings settings, string to, string subject, string body)
    {
        var mail = new MailMessage
        {
            From = string.IsNullOrWhiteSpace(settings.FromName)
                ? new MailAddress(settings.FromEmail)
                : new MailAddress(settings.FromEmail, settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mail.To.Add(to);
        return mail;
    }

    private static async Task SendCoreAsync(SmtpSettings settings, MailMessage mail)
    {
        if (string.IsNullOrWhiteSpace(settings.Host))
        {
            throw new InvalidOperationException("SMTP sunucusu tanımlı değil. Admin > SMTP Ayarları sayfasından ayarlayın.");
        }

        using var smtpClient = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            Timeout = TimeoutMilliseconds,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(settings.UserName))
        {
            smtpClient.Credentials = new NetworkCredential(settings.UserName, settings.Password);
        }

        // SmtpClient.Timeout yalnizca senkron Send icin gecerli; async gonderimde iptal belirteci kullanilir.
        using var cts = new CancellationTokenSource(TimeoutMilliseconds);

        try
        {
            await smtpClient.SendMailAsync(mail, cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"SMTP sunucusu {settings.Host}:{settings.Port} {TimeoutMilliseconds / 1000} saniye içinde yanıt vermedi.");
        }
    }
}
