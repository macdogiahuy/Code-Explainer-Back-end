using CodeExplainer.Desktop.Models;
using CodeExplainer.Desktop.Models.Notifications;

namespace CodeExplainer.Desktop.Services;

public interface INotificationApiClient
{
    Task<ApiResult<IReadOnlyList<NotificationModel>>> GetNotificationsAsync();
    Task<ApiResult> MarkAsReadAsync(Guid notificationId);
    Task<ApiResult> DeleteNotificationAsync(Guid notificationId);
}
