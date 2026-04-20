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
        var mail = new MailMessage();
        mail.From = new MailAddress(_smtp.FromEmail, _smtp.FromName);
        mail.To.Add(to);
        mail.Subject = subject;
        mail.Body = body;
        mail.IsBodyHtml = true;

        var smtpClient = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            Credentials = new NetworkCredential(_smtp.UserName, _smtp.Password),
            EnableSsl = _smtp.EnableSsl
        };

        await smtpClient.SendMailAsync(mail);
    }
}