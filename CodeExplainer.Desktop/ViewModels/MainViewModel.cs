using CodeExplainer.Desktop.Models;
using CodeExplainer.Desktop.Services;
using CodeExplainer.Desktop.ViewModels.Base;
using CodeExplainer.Desktop.ViewModels.Chat;
using CodeExplainer.Desktop.ViewModels.Notifications;
using CodeExplainer.Desktop.ViewModels.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows; // Add this at the top with other using directives

namespace CodeExplainer.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;
    private readonly AuthViewModel _authViewModel;
    private readonly ChatViewModel _chatViewModel;
    private readonly NotificationsViewModel _notificationsViewModel;
    private readonly ProfileViewModel _profileViewModel;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private UserProfile _currentUser = new();

    [ObservableProperty]
    private ObservableObject _currentViewModel = null!;

    public MainViewModel(
        INavigationService navigationService,
        IAuthService authService,
        AuthViewModel authViewModel,
        ChatViewModel chatViewModel,
        NotificationsViewModel notificationsViewModel,
        ProfileViewModel profileViewModel)
    {
        _navigationService = navigationService;
        _authService = authService;
        _authViewModel = authViewModel;
        _chatViewModel = chatViewModel;
        _notificationsViewModel = notificationsViewModel;
        _profileViewModel = profileViewModel;

        // Initialize current view model
        CurrentViewModel = _authViewModel;

        // Set up event handlers
        _authViewModel.AuthenticationSucceeded += OnAuthenticationSucceeded;
        _navigationService.CurrentViewModelChanged += OnCurrentViewModelChanged;

        // Start with auth view
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            NavigateTo(_authViewModel);
        });
    }

    private void OnCurrentViewModelChanged(object? sender, ObservableObject viewModel)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            CurrentViewModel = viewModel;
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.UpdateLayout();
            }
        });
    }

    [RelayCommand]
    private void NavigateChat() => NavigateTo(_chatViewModel);

    [RelayCommand]
    private void NavigateNotifications() => NavigateTo(_notificationsViewModel);

    [RelayCommand]
    private void NavigateProfile() => NavigateTo(_profileViewModel);

    [RelayCommand]
    private async Task LogoutAsync()
    {
        // Immediately update UI so logout is responsive even when API is slow/unreachable
        try
        {
            IsAuthenticated = false;
            CurrentUser = new UserProfile();
            _authViewModel.Reset();
            NavigateTo(_authViewModel);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Logout UI update error: {ex.Message}");
        }

        // Fire-and-forget the server logout call; clear local state above regardless of network outcome.
        _ = Task.Run(async () =>
        {
            try
            {
                await _authService.LogoutAsync();
                System.Diagnostics.Debug.WriteLine("Logout API call completed.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout API call failed: {ex.Message}");
            }
        });
    }

    private void NavigateTo(ObservableObject viewModel)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _navigationService.NavigateTo(viewModel);
            CurrentViewModel = viewModel;
        });
    }

    private async void OnAuthenticationSucceeded(UserProfile profile)
    {
        try
        {
            // First, update authentication state
            IsAuthenticated = true;
            CurrentUser = profile;

            // Initialize all viewmodels
            _chatViewModel.InitializeFor(profile);
            _notificationsViewModel.InitializeFor(profile);
            _profileViewModel.InitializeFor(profile);

            // Force UI update
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                // Give UI time to process authentication state change
                await Task.Delay(200);
                
                // Navigate to chat view
                NavigateTo(_chatViewModel);
                
                // Force layout update
                if (Application.Current.MainWindow != null)
                {
                    Application.Current.MainWindow.UpdateLayout();
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Navigation error: {ex.Message}");
            NavigateTo(_authViewModel); // Fallback to auth view if navigation fails
        }
    }
}
