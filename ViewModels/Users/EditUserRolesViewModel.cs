namespace MyDergiApp.ViewModels.Users;
public class EditUserRolesViewModel
{
    public string UserId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public List<RoleCheckboxViewModel> Roles { get; set; } = new();
}