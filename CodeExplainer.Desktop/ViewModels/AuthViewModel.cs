using CodeExplainer.Desktop.Models;
using CodeExplainer.Desktop.Models.Auth;
using CodeExplainer.Desktop.Services;
using CodeExplainer.Desktop.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows; // Add this at the top of the file

namespace CodeExplainer.Desktop.ViewModels;

public partial class AuthViewModel : ViewModelBase
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private LoginRequest _loginRequest = new();

    [ObservableProperty]
    private RegisterRequest _registerRequest = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isRegisterMode;

    [ObservableProperty]
    private bool _isForgotPasswordMode;

    [ObservableProperty]
    private string _forgotPasswordEmail = string.Empty;

    [ObservableProperty]
    private string? _forgotPasswordMessage;

    public event Action<UserProfile>? AuthenticationSucceeded;

    public bool IsLoginMode => !IsRegisterMode && !IsForgotPasswordMode;

    public AuthViewModel(IAuthService authService)
    {
        _authService = authService;
        
        // Initialize default state
        LoginRequest = new LoginRequest();
        RegisterRequest = new RegisterRequest();
        IsBusy = false;
        IsRegisterMode = false;
        IsForgotPasswordMode = false;
    }

    [RelayCommand]
    private void ToggleMode()
    {
        IsForgotPasswordMode = false;
        ForgotPasswordMessage = null;
        IsRegisterMode = !IsRegisterMode;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = null;
        IsBusy = true;

        try
        {
            var result = await _authService.LoginAsync(LoginRequest);
            if (result.Success && result.Data != null)
            {
                ErrorMessage = "Đăng nhập thành công. Đang chuyển hướng...";
                
                // Allow error message to be shown
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(200);
                    AuthenticationSucceeded?.Invoke(result.Data);
                });
            }
            else
            {
                ErrorMessage = result.Message ?? string.Join("\n", result.Errors ?? Array.Empty<string>());
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        ErrorMessage = null;
        IsBusy = true;

        try
        {
            if (RegisterRequest.Password != RegisterRequest.ConfirmPassword)
            {
                ErrorMessage = "Mật khẩu xác nhận không khớp.";
                return;
            }

            var result = await _authService.RegisterAsync(RegisterRequest);
            if (!result.Success)
            {
                ErrorMessage = result.Message ?? string.Join("\n", result.Errors ?? Array.Empty<string>());
                return;
            }

            IsRegisterMode = false;
            LoginRequest = new LoginRequest
            {
                Email = RegisterRequest.Email,
                Password = RegisterRequest.Password
            };
            ErrorMessage = "Đăng ký thành công. Vui lòng đăng nhập.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ShowForgotPassword()
    {
        ForgotPasswordEmail = LoginRequest.Email;
        ForgotPasswordMessage = null;
        IsRegisterMode = false;
        IsForgotPasswordMode = true;
    }

    [RelayCommand]
    private void BackToLogin()
    {
        IsForgotPasswordMode = false;
        IsRegisterMode = false;
        ForgotPasswordMessage = null;
    }

    [RelayCommand]
    private async Task SubmitForgotPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(ForgotPasswordEmail))
        {
            ForgotPasswordMessage = "Vui lòng nhập email.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _authService.ForgotPasswordAsync(ForgotPasswordEmail);
            ForgotPasswordMessage = result.Message ?? (result.Success
                ? "Nếu email tồn tại, liên kết đặt lại mật khẩu đã được gửi."
                : string.Join("\n", result.Errors ?? Array.Empty<string>()));
            if (result.Success)
            {
                IsForgotPasswordMode = false;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Reset()
    {
        LoginRequest = new LoginRequest();
        RegisterRequest = new RegisterRequest();
        ErrorMessage = null;
        IsRegisterMode = false;
        IsForgotPasswordMode = false;
        ForgotPasswordEmail = string.Empty;
        ForgotPasswordMessage = null;
    }

    partial void OnIsRegisterModeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsLoginMode));
    }

    partial void OnIsForgotPasswordModeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsLoginMode));
    }
}
