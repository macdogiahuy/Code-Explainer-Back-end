using CodeExplainer.Desktop.Models;
using CodeExplainer.Desktop.Services;
using CodeExplainer.Desktop.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace CodeExplainer.Desktop.ViewModels.Profile;

public partial class ProfileViewModel : ViewModelBase
{
    private readonly IUserProfileApiClient _profileApiClient;

    [ObservableProperty]
    private Guid _userId;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string? _password;

    [ObservableProperty]
    private string? _confirmPassword;

    [ObservableProperty]
    private string? _profilePictureUrl;

    [ObservableProperty]
    private string? _localAvatarPath;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _message;

    public ProfileViewModel(IUserProfileApiClient profileApiClient)
    {
        _profileApiClient = profileApiClient;
    }

    public void InitializeFor(UserProfile profile)
    {
        UserId = profile.UserId;
        UserName = profile.UserName;
        Email = profile.Email;
        ProfilePictureUrl = profile.ProfilePictureUrl;
        LocalAvatarPath = null;
        Message = null;
    }

    [RelayCommand]
    private void SelectAvatar()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.gif;*.webp"
        };
        if (dialog.ShowDialog() == true)
        {
            LocalAvatarPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_password != _confirmPassword)
        {
            Message = "Mật khẩu xác nhận không khớp.";
            return;
        }

        IsBusy = true;
        Message = null;

        try
        {
            var request = new UserProfileUpdateRequest
            {
                UserId = UserId,
                UserName = UserName,
                Email = Email,
                Password = string.IsNullOrWhiteSpace(_password) ? null : _password,
                AvatarFilePath = LocalAvatarPath
            };

            var result = await _profileApiClient.UpdateProfileAsync(request);
            if (result.Success && result.Data != null)
            {
                UserName = result.Data.UserName;
                Email = result.Data.Email;
                ProfilePictureUrl = result.Data.ProfilePictureUrl;
                LocalAvatarPath = null;
                Message = "Cập nhật thông tin thành công.";
            }
            else
            {
                Message = result.Message ?? string.Join("\n", result.Errors ?? Array.Empty<string>());
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
