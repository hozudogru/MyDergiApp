namespace MyDergiApp.ViewModels
{
    public class UserListViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}