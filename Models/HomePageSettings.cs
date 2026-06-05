using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.Models
{
    public class HomePageSettings
    {
        [StringLength(200)]
        public string SiteTitle { get; set; } = "MyDergiApp Journal";

        [StringLength(300)]
        public string? Subtitle { get; set; } = "Peer-reviewed international academic journal";
        [StringLength(300)]
        public string? HeaderLogoPath { get; set; }

        [StringLength(300)]
        public string? HeaderTitle { get; set; } = "MyDergiApp Journal";

        [StringLength(500)]
        public string? HeaderSubtitle { get; set; } = "Peer-reviewed international academic journal";

        [StringLength(300)]
        public string? HeaderRightText { get; set; } = "Akademik Dergi Platformu";

        [StringLength(300)]
        public string? HeaderBackgroundImagePath { get; set; }

        public bool ShowHeaderLogo { get; set; } = true;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(200)]
        public string? ContactEmail { get; set; }

        [StringLength(200)]
        public string? ContactPhone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(300)]
        public string? LogoPath { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(30)]
        public string ThemeName { get; set; } = "classic";

        [StringLength(20)]
        public string PrimaryColor { get; set; } = "#0d6efd";

        [StringLength(20)]
        public string SecondaryColor { get; set; } = "#198754";

        [StringLength(20)]
        public string HeaderBgColor { get; set; } = "#ffffff";

        [StringLength(20)]
        public string NavBgColor { get; set; } = "#ffffff";

        [StringLength(20)]
        public string BodyBgColor { get; set; } = "#f8fafc";

        [StringLength(20)]
        public string TextColor { get; set; } = "#111827";
        public int Id { get; set; }

        [Required(ErrorMessage = "Dergi adı zorunludur.")]
        [StringLength(250)]
        public string? JournalTitle { get; set; } = "MyDergiApp";

        [StringLength(500)]
        public string? JournalSubtitle { get; set; } = "Journal Management Panel";

        [StringLength(500)]
        public string? HeroTitle { get; set; } = "Akademik Dergi Yönetim Sistemi";
        [StringLength(300)]
        public string? BannerTitle { get; set; } = "Professional Academic Publishing Platform";

        [StringLength(1000)]
        public string? BannerDescription { get; set; } =
            "Submit, review, manage and publish academic articles through a modern journal system.";

        [StringLength(200)]
        public string? BannerLabel { get; set; } = "Hakemli • Açık Erişim • Akademik Yayıncılık";

        [StringLength(200)]
        public string? BannerPrimaryButtonText { get; set; } = "Makale Gönder";

        [StringLength(300)]
        public string? BannerPrimaryButtonUrl { get; set; } = "/Submission/YeniMakale";

        [StringLength(200)]
        public string? BannerSecondaryButtonText { get; set; } = "Makaleleri İncele";

        [StringLength(300)]
        public string? BannerSecondaryButtonUrl { get; set; } = "/Issues/Published";

        [StringLength(300)]
        public string? BannerImagePath { get; set; }

        public bool ShowBanner { get; set; } = true;

        public string? HeroDescription { get; set; }

        [StringLength(250)]
        public string? AboutTitle { get; set; } = "Dergi Hakkında";

        public string? AboutContent { get; set; }

        [StringLength(50)]
        public string? PrintIssn { get; set; }

        [StringLength(50)]
        public string? OnlineIssn { get; set; }

        
        [StringLength(1000)]
        public string? FooterText { get; set; }
        
    }
}
