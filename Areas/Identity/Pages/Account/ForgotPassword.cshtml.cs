using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using MyDergiApp.Entities;

namespace MyDergiApp.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Sifremi unuttum: sifirlama baglantisini e-posta ile gonderir. Kullanici bulunamazsa da ayni
    /// onay sayfasina yonlendirir (hesap var/yok bilgisi sizdirilmaz). Sifirlama formu, varsayilan
    /// Identity UI'in /Identity/Account/ResetPassword sayfasidir.
    /// </summary>
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly EmailService _emailService;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<AppUser> userManager,
            EmailService emailService,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "E-posta zorunludur.")]
            [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email.Trim());

            if (user != null && user.IsActive)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                var displayName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName);
                var link = HtmlEncoder.Default.Encode(callbackUrl ?? string.Empty);

                var body = $@"
                    <p>Sayın {displayName},</p>
                    <p>Hesabınız için şifre sıfırlama talebi aldık. Yeni şifre belirlemek için aşağıdaki bağlantıya tıklayın:</p>
                    <p><a href=""{link}"">Şifremi sıfırla</a></p>
                    <p>Bağlantı kısa süre için geçerlidir. Bu talebi siz yapmadıysanız bu e-postayı yok sayabilirsiniz; şifreniz değişmez.</p>";

                var sent = await _emailService.SendEmailAsync(user.Email!, "Şifre sıfırlama", body);

                if (!sent)
                {
                    _logger.LogWarning("Şifre sıfırlama e-postası gönderilemedi: {Email}", user.Email);
                    ModelState.AddModelError(string.Empty,
                        "E-posta gönderilemedi. Lütfen daha sonra tekrar deneyin veya dergi yönetimiyle iletişime geçin.");
                    return Page();
                }
            }

            return RedirectToPage("./ForgotPasswordConfirmation");
        }
    }
}
