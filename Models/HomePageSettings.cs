using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.Models
{
    public class HomePageSettings
    {
        public int Id { get; set; }

        [StringLength(250)]
        public string? JournalTitle { get; set; }

        [StringLength(500)]
        public string? JournalSubtitle { get; set; }

        [StringLength(500)]
        public string? HeroTitle { get; set; }

        public string? HeroDescription { get; set; }

        [StringLength(250)]
        public string? AboutTitle { get; set; }

        public string? AboutContent { get; set; }

        [StringLength(50)]
        public string? PrintIssn { get; set; }

        [StringLength(50)]
        public string? OnlineIssn { get; set; }

        [StringLength(250)]
        public string? ContactEmail { get; set; }

        [StringLength(50)]
        public string? ContactPhone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? LogoPath { get; set; }

        [StringLength(1000)]
        public string? FooterText { get; set; }

        public bool IsActive { get; set; } = true;
    }
}