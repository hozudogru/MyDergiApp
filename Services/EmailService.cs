using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

public class EmailService
{
    private readonly SmtpSettings _smtp;

    public EmailService(IOptions<SmtpSettings> smtp)
    {
        _smtp = smtp.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        using var mail = new MailMessage();
        mail.From = new MailAddress(_smtp.FromEmail, _smtp.FromName);
        mail.To.Add(to);
        mail.Subject = subject;
        mail.Body = body;
        mail.IsBodyHtml = true;

        using var smtpClient = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            Credentials = new NetworkCredential(_smtp.UserName, _smtp.Password),
            EnableSsl = _smtp.EnableSsl
        };

        await smtpClient.SendMailAsync(mail);
    }

    public async Task SendEmailWithAttachmentsAsync(
        string to,
        string subject,
        string body,
        List<string>? attachmentPaths = null)
    {
        using var mail = new MailMessage();
        mail.From = new MailAddress(_smtp.FromEmail, _smtp.FromName);
        mail.To.Add(to);
        mail.Subject = subject;
        mail.Body = body;
        mail.IsBodyHtml = true;

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

        using var smtpClient = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            Credentials = new NetworkCredential(_smtp.UserName, _smtp.Password),
            EnableSsl = _smtp.EnableSsl
        };

        await smtpClient.SendMailAsync(mail);
    }
}