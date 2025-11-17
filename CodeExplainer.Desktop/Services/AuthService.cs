using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CodeExplainer.Desktop.Models;
using CodeExplainer.Desktop.Models.Auth;
using Microsoft.Extensions.Configuration;

namespace CodeExplainer.Desktop.Services;

public class AuthService : IAuthService
{
    private readonly IApiClient _apiClient;
    private readonly IConfiguration _configuration;

    public AuthService(IApiClient apiClient, IConfiguration configuration)
    {
        _apiClient = apiClient;
        _configuration = configuration;
    }

    public async Task<ApiResult<UserProfile>> LoginAsync(LoginRequest request)
    {
        var result = await _apiClient.PostAsync<LoginRequest, BaseResultResponse<LoginResponse>>("api/auth/login", request);

        // If the call failed, propagate the failure
        if (!result.Success)
        {
            return ApiResult<UserProfile>.Fail(result.Message, result.Errors);
        }

        // result.Data is expected to be a BaseResultResponse<LoginResponse>
        var wrapped = result.Data;
        var loginData = wrapped?.Data;

        // If the server returned the login payload, use it
        if (loginData != null)
        {
            var userProfile = new UserProfile
            {
                UserName = loginData.UserName,
                Email = loginData.Email
            };
            return ApiResult<UserProfile>.Ok(userProfile, result.Message);
        }

        // Fallback: sometimes the API may indicate success without including the nested Data payload
        // In that case, create a minimal UserProfile from the request so the UI can proceed.
        var fallbackProfile = new UserProfile
        {
            Email = request.Email ?? string.Empty,
            UserName = string.IsNullOrWhiteSpace(request.Email) ? string.Empty : request.Email.Split('@')[0]
        };

        return ApiResult<UserProfile>.Ok(fallbackProfile, result.Message ?? "Login successful");
    }

    public async Task<ApiResult> RegisterAsync(RegisterRequest request)
    {
        var result = await _apiClient.PostAsync<RegisterRequest, BaseResultResponse<object?>>("api/auth/register", request);
        return result.Success
            ? ApiResult.Ok(result.Message)
            : ApiResult.Fail(result.Message, result.Errors);
    }

    public async Task<ApiResult> ForgotPasswordAsync(string email)
    {
        var result = await _apiClient.PostAsync<object, BaseResultResponse<string>>("api/auth/forgot-password?email=" + Uri.EscapeDataString(email), new { });
        return result.Success
            ? ApiResult.Ok(result.Message)
            : ApiResult.Fail(result.Message, result.Errors);
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _apiClient.PostAsync<object, BaseResultResponse<string>>("api/auth/logout", new { });
        }
        catch
        {
            // Ignore network errors; still clear local cookies/state so the UI can log out locally.
        }

        // Clear cookies for the API base URL so any session cookie is removed locally.
        try
        {
            var baseUrl = _configuration["Api:BaseUrl"] ?? Environment.GetEnvironmentVariable("CODEEXPLAINER_DESKTOP_Api__BaseUrl") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                if (!baseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    baseUrl = "http://" + baseUrl;
                }

                var uri = new Uri(baseUrl);
                var cookies = _apiClient.CookieContainer.GetCookies(uri);
                foreach (System.Net.Cookie c in cookies)
                {
                    try
                    {
                        c.Expired = true;
                    }
                    catch { }
                }
            }
        }
        catch { }
    }
}
