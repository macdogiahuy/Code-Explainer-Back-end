using CodeExplainer.Desktop.Services;
using CodeExplainer.Desktop.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CodeExplainer.Desktop.ViewModels.Auth;

public partial class ForgotPasswordViewModel : ViewModelBase
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _requestSent;

    [ObservableProperty]
    private string? _message;

    public ForgotPasswordViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            Message = "Vui lòng nhập email.";
            return;
        }

        IsBusy = true;
        Message = null;

        try
        {
            var result = await _authService.ForgotPasswordAsync(Email);
            Message = result.Message ?? (result.Success
                ? "Nếu email tồn tại, liên kết đặt lại mật khẩu đã được gửi."
                : "Có lỗi xảy ra.");
            RequestSent = result.Success;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
