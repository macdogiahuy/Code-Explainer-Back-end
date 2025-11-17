using CodeExplainer.Desktop.Models;
using CodeExplainer.Desktop.ViewModels.Profile;

namespace CodeExplainer.Desktop.Services;

public interface IUserProfileApiClient
{
    Task<ApiResult<UserProfileUpdateResponse>> UpdateProfileAsync(UserProfileUpdateRequest request);
}
