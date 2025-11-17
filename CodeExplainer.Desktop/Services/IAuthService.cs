using CodeExplainer.Desktop.Models;
using CodeExplainer.Desktop.Models.Auth;

namespace CodeExplainer.Desktop.Services;

public interface IAuthService
{
    Task<ApiResult<UserProfile>> LoginAsync(LoginRequest request);
    Task<ApiResult> RegisterAsync(RegisterRequest request);
    Task<ApiResult> ForgotPasswordAsync(string email);
    Task LogoutAsync();
}
