using Microsoft.AspNetCore.Identity;

namespace MyDergiApp.Entities
{
    public class AppUser : IdentityUser
    {
        public string? FullName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}