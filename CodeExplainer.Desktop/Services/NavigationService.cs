using CodeExplainer.Desktop.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CodeExplainer.Desktop.Services;

public class NavigationService : INavigationService
{
    private ObservableObject? _current;
    private readonly object _lock = new();

    public ObservableObject CurrentViewModel
    {
        get
        {
            lock (_lock)
            {
                return _current ?? throw new InvalidOperationException("Navigation target not initialized");
            }
        }
    }

    public event EventHandler<ObservableObject>? CurrentViewModelChanged;

    public void NavigateTo(ObservableObject viewModel)
    {
        if (viewModel == null)
            throw new ArgumentNullException(nameof(viewModel));

        if (System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            PerformNavigation(viewModel);
        }
        else
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() => PerformNavigation(viewModel));
        }
    }

    private void PerformNavigation(ObservableObject viewModel)
    {
        lock (_lock)
        {
            if (_current == viewModel)
                return;

            var oldViewModel = _current;
            _current = viewModel;

            // Notify on UI thread
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    CurrentViewModelChanged?.Invoke(this, viewModel);

                    if (System.Windows.Application.Current.MainWindow != null)
                    {
                        System.Windows.Application.Current.MainWindow.UpdateLayout();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Navigation error: {ex}");
                    // Revert on failure
                    lock (_lock)
                    {
                        _current = oldViewModel;
                    }
                }
            });
        }
    }
}
