using CodeExplainer.Desktop.Models;
using CodeExplainer.Desktop.Models.Notifications;

namespace CodeExplainer.Desktop.Services;

public class NotificationApiClient : INotificationApiClient
{
    private readonly IApiClient _apiClient;

    public NotificationApiClient(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<ApiResult<IReadOnlyList<NotificationModel>>> GetNotificationsAsync()
        => _apiClient.GetAsync<IReadOnlyList<NotificationModel>>("api/notification");

    public Task<ApiResult> MarkAsReadAsync(Guid notificationId)
        => _apiClient.PutAsync<object, BaseResultResponse<object?>>("api/notification/" + notificationId + "/read", new { })
            .ContinueWith(task =>
            {
                var result = task.Result;
                return result.Success
                    ? ApiResult.Ok(result.Message)
                    : ApiResult.Fail(result.Message, result.Errors);
            });

    public Task<ApiResult> DeleteNotificationAsync(Guid notificationId)
        => _apiClient.DeleteAsync("api/notification/" + notificationId);
}
