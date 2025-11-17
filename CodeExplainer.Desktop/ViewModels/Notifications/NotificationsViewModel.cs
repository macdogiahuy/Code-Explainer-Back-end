using System.Collections.ObjectModel;
using System.Windows;
using CodeExplainer.Desktop.Models;
using CodeExplainer.Desktop.Models.Notifications;
using CodeExplainer.Desktop.Services;
using CodeExplainer.Desktop.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CodeExplainer.Desktop.ViewModels.Notifications;

public partial class NotificationsViewModel : ViewModelBase
{
    private readonly INotificationApiClient _notificationApiClient;
    private readonly INotificationHubClient _notificationHubClient;

    [ObservableProperty]
    private ObservableCollection<NotificationModel> _notifications = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _error;

    [ObservableProperty]
    private int _unreadCount;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionStatus = "Đang kết nối...";

    private UserProfile _currentUser = new();

    public NotificationsViewModel(
        INotificationApiClient notificationApiClient,
        INotificationHubClient notificationHubClient)
    {
        _notificationApiClient = notificationApiClient;
        _notificationHubClient = notificationHubClient;

        _notificationHubClient.OnNotificationReceived += HandleNotificationReceived;
        _notificationHubClient.OnConnectionError += HandleConnectionError;
    }

    public async void InitializeFor(UserProfile profile)
    {
        _currentUser = profile;
        try
        {
            await _notificationHubClient.StartConnection();
            IsConnected = _notificationHubClient.IsConnected;
            ConnectionStatus = IsConnected ? "Đã kết nối" : "Chưa kết nối";
            await LoadNotificationsAsync();
        }
        catch (Exception ex)
        {
            HandleConnectionError(ex.Message);
        }
    }

    private void HandleNotificationReceived(string message)
    {
        Application.Current.Dispatcher.Invoke(async () =>
        {
            await LoadNotificationsAsync();
            UpdateUnreadCount();
        });
    }

    private void HandleConnectionError(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Error = message;
            IsConnected = _notificationHubClient.IsConnected;
            ConnectionStatus = message;
        });
    }

    private void UpdateUnreadCount()
    {
        UnreadCount = Notifications.Count(n => !n.IsRead);
    }

    [RelayCommand]
    private async Task LoadNotificationsAsync()
    {
        if (!IsConnected)
        {
            Error = "Không thể tải thông báo khi chưa kết nối.";
            return;
        }

        IsBusy = true;
        Error = null;
        try
        {
            var result = await _notificationApiClient.GetNotificationsAsync();
            if (result.Success && result.Data != null)
            {
                Notifications = new ObservableCollection<NotificationModel>(result.Data.OrderByDescending(n => n.CreatedAt));
                UpdateUnreadCount();
            }
            else
            {
                Error = result.Message;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task MarkAsReadAsync(NotificationModel notification)
    {
        if (!IsConnected)
        {
            Error = "Không thể cập nhật thông báo khi chưa kết nối.";
            return;
        }

        var result = await _notificationApiClient.MarkAsReadAsync(notification.NotificationId);
        if (result.Success)
        {
            notification.IsRead = true;
            UpdateUnreadCount();
        }
        else
        {
            Error = result.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(NotificationModel notification)
    {
        if (!IsConnected)
        {
            Error = "Không thể xóa thông báo khi chưa kết nối.";
            return;
        }

        var result = await _notificationApiClient.DeleteNotificationAsync(notification.NotificationId);
        if (result.Success)
        {
            Notifications.Remove(notification);
            UpdateUnreadCount();
        }
        else
        {
            Error = result.Message;
        }
    }

    [RelayCommand]
    private async Task MarkAllAsReadAsync()
    {
        if (!IsConnected)
        {
            Error = "Không thể cập nhật thông báo khi chưa kết nối.";
            return;
        }

        var unreadNotifications = Notifications.Where(n => !n.IsRead).ToList();
        foreach (var notification in unreadNotifications)
        {
            await MarkAsReadAsync(notification);
        }
    }

    public void Cleanup()
    {
        _notificationHubClient.OnNotificationReceived -= HandleNotificationReceived;
        _notificationHubClient.OnConnectionError -= HandleConnectionError;
        _ = _notificationHubClient.StopConnection();
    }
}
