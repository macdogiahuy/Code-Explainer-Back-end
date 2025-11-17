using CommunityToolkit.Mvvm.ComponentModel;

namespace CodeExplainer.Desktop.Services;

public interface INavigationService
{
    ObservableObject CurrentViewModel { get; }
    event EventHandler<ObservableObject> CurrentViewModelChanged;
    void NavigateTo(ObservableObject viewModel);
}
