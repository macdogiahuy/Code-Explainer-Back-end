using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CodeExplainer.Desktop.Models;
using CodeExplainer.Desktop.ViewModels.Profile;

namespace CodeExplainer.Desktop.Services;

public class UserProfileApiClient : IUserProfileApiClient
{
    private readonly HttpClient _httpClient;

    public UserProfileApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResult<UserProfileUpdateResponse>> UpdateProfileAsync(UserProfileUpdateRequest request)
    {
        using var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent(request.UserId.ToString()), "UserId");
        formContent.Add(new StringContent(request.UserName), "UserName");
        formContent.Add(new StringContent(request.Email), "Email");
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            formContent.Add(new StringContent(request.Password), "PasswordHash");
        }
        if (!string.IsNullOrWhiteSpace(request.AvatarFilePath) && File.Exists(request.AvatarFilePath))
        {
            var bytes = await File.ReadAllBytesAsync(request.AvatarFilePath);
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            formContent.Add(content, "ProfilePictureUrl", Path.GetFileName(request.AvatarFilePath));
        }

        var response = await _httpClient.PostAsync("api/user/update-profile", formContent);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<UserProfileUpdateResponse>.Fail(response.ReasonPhrase);
        }

        var payload = await response.Content.ReadFromJsonAsync<BaseResultResponse<UserProfileUpdateResponse>>();
        if (payload == null)
        {
            return ApiResult<UserProfileUpdateResponse>.Fail("Không nhận được dữ liệu từ máy chủ.");
        }

        return payload.Success
            ? ApiResult<UserProfileUpdateResponse>.Ok(payload.Data, payload.Message)
            : ApiResult<UserProfileUpdateResponse>.Fail(payload.Message, payload.Errors);
    }
}
