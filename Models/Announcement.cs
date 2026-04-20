using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public bool ShowAsPopup { get; set; } = false;
    }
}