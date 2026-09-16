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
    /// E-posta onay baglantisini yeniden gonderir (onay, varsayilan Identity UI'in
    /// /Identity/Account/ConfirmEmail sayfasinda tamamlanir). Hesap var/yok bilgisi sizdirilmaz.
    /// </summary>
    [AllowAnonymous]
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly EmailService _emailService;

        public ResendEmailConfirmationModel(UserManager<AppUser> userManager, EmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
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

            if (user != null && !user.EmailConfirmed)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = user.Id, code },
                    protocol: Request.Scheme);

                var displayName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName);
                var link = HtmlEncoder.Default.Encode(callbackUrl ?? string.Empty);

                var body = $@"
                    <p>Sayın {displayName},</p>
                    <p>E-posta adresinizi doğrulamak için aşağıdaki bağlantıya tıklayın:</p>
                    <p><a href=""{link}"">E-postamı doğrula</a></p>";

                var sent = await _emailService.SendEmailAsync(user.Email!, "E-posta doğrulama", body);

                if (!sent)
                {
                    ModelState.AddModelError(string.Empty,
                        "E-posta gönderilemedi. Lütfen daha sonra tekrar deneyin veya dergi yönetimiyle iletişime geçin.");
                    return Page();
                }
            }

            TempData["Success"] = "Kayıtlı ve doğrulanmamış bir hesap varsa onay e-postası gönderildi.";
            return RedirectToPage("./Login");
        }
    }
}
