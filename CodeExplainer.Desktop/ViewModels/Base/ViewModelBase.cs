using CommunityToolkit.Mvvm.ComponentModel;
using CodeExplainer.Desktop.Models;
using CodeExplainer.Desktop.Services;

namespace CodeExplainer.Desktop.ViewModels.Base;

public abstract partial class ViewModelBase : ObservableObject
{
    private readonly IChatHubClient? _chatHubClient;

    // Update handler signatures to match IChatHubClient event delegates
    protected virtual void HandleMessageReceived(string user, string message)
    {
        // Handler logic or leave empty if not needed
    }

    protected virtual void HandleSystemMessage(string message)
    {
        // Handler logic or leave empty if not needed
    }

    protected virtual void HandleConnectionError(string error)
    {
        // Handler logic or leave empty if not needed
    }

    public virtual void Cleanup()   
    {
        if (_chatHubClient != null)
        {
            _chatHubClient.OnMessageReceived -= HandleMessageReceived;
            _chatHubClient.OnSystemMessage -= HandleSystemMessage;
            _chatHubClient.OnConnectionError -= HandleConnectionError;
            _ = _chatHubClient.StopConnection();
        }
    }
}