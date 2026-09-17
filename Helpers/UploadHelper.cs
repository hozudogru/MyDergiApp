namespace MyDergiApp.Helpers
{
    /// <summary>
    /// wwwroot/uploads altindaki yonetim dosyalari (kapak, sayi PDF'i, yayin PDF'i, logo, banner, indeks logosu)
    /// icin ortak dogrulama, kaydetme ve eski dosyayi silme yardimcilari.
    /// </summary>
    public static class UploadHelper
    {
        public const long MaxImageBytes = 5L * 1024 * 1024;   // 5 MB
        public const long MaxPdfBytes = 100L * 1024 * 1024;   // 100 MB (tam sayi PDF'leri buyuk olabilir)
        public const long MaxDocumentBytes = 50L * 1024 * 1024; // 50 MB

        public static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        public static readonly string[] PdfExtensions = { ".pdf" };
        public static readonly string[] DocumentExtensions = { ".pdf", ".doc", ".docx" };

        /// <summary>Uzanti ve boyut kontrolu. Hata varsa Turkce mesaj doner, yoksa null.</summary>
        public static string? Validate(IFormFile file, string[] allowedExtensions, long maxBytes, string label)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(ext))
            {
                var list = string.Join(", ", allowedExtensions.Select(e => e.TrimStart('.')));
                return $"{label} yalnızca {list} formatında olabilir.";
            }

            if (file.Length > maxBytes)
            {
                return $"{label} en fazla {maxBytes / (1024 * 1024)} MB olabilir.";
            }

            return null;
        }

        /// <summary>Dosyayi wwwroot/uploads/{subFolder} altina benzersiz adla kaydeder ve "/uploads/..." yolunu dondurur.</summary>
        public static async Task<string> SaveAsync(IWebHostEnvironment env, IFormFile file, string subFolder, string fileNamePrefix)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var folder = Path.Combine(env.WebRootPath, "uploads", subFolder);
            Directory.CreateDirectory(folder);

            var fileName = $"{fileNamePrefix}{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/{subFolder}/{fileName}";
        }

        /// <summary>
        /// "/uploads/..." ile baslayan bir web yolunun fiziksel dosyasini siler. Yol wwwroot/uploads disina cikamaz;
        /// dosya yoksa veya silinemezse sessizce gecer (eski kayit yeni dosyayi engellemesin).
        /// </summary>
        public static void TryDeleteWebFile(IWebHostEnvironment env, string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return;

            var clean = relativePath.Replace("\\", "/").TrimStart('/');

            if (!clean.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase) || clean.Contains(".."))
                return;

            try
            {
                var uploadsRoot = Path.GetFullPath(Path.Combine(env.WebRootPath, "uploads"));
                var fullPath = Path.GetFullPath(Path.Combine(env.WebRootPath, clean.Replace('/', Path.DirectorySeparatorChar)));

                if (fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (IOException)
            {
                // Silinemeyen eski dosya islemi durdurmasin
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
