namespace CodeExplainer.Desktop.Models;

public class UserProfileUpdateRequest
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? AvatarFilePath { get; set; }
}
