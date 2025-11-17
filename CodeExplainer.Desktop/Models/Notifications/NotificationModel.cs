using CommunityToolkit.Mvvm.ComponentModel;

namespace CodeExplainer.Desktop.Models.Notifications;

public class NotificationModel : ObservableObject
{
    public Guid NotificationId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    private bool _isRead;

    public bool IsRead
    {
        get => _isRead;
        set => SetProperty(ref _isRead, value);
    }
}
