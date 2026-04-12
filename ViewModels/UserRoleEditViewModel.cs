using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.ViewModels
{
    public class UserRoleEditViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }

        [Required]
        public string SelectedRole { get; set; } = string.Empty;

        public List<string> AvailableRoles { get; set; } = new();
    }
}