using System.Collections.ObjectModel;
using System.Windows;
using CodeExplainer.Desktop.Models;
using CodeExplainer.Desktop.Models.Chat;
using CodeExplainer.Desktop.Services;
using CodeExplainer.Desktop.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CodeExplainer.Desktop.ViewModels.Chat;

public partial class ChatViewModel : ViewModelBase
{
    private readonly IChatApiClient _chatApiClient;
    private readonly IChatHubClient _chatHubClient;

    [ObservableProperty]
    private ObservableCollection<ChatSessionModel> _sessions = new();

    [ObservableProperty]
    private ObservableCollection<ChatMessageModel> _messages = new();

    [ObservableProperty]
    private ChatSessionModel? _selectedSession;

    [ObservableProperty]
    private string _messageText = string.Empty;

    [ObservableProperty]
    private string _language = "csharp";

    [ObservableProperty]
    private string _sourceCode = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _error;

    [ObservableProperty]
    private string? _systemMessage;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionStatus = "Đang kết nối...";

    private UserProfile _currentUser = new();

    public ChatViewModel(IChatApiClient chatApiClient, IChatHubClient chatHubClient)
    {
        _chatApiClient = chatApiClient;
        _chatHubClient = chatHubClient;
        
        _chatHubClient.OnMessageReceived += HandleMessageReceived;
        _chatHubClient.OnSystemMessage += HandleSystemMessage;
        _chatHubClient.OnConnectionError += HandleConnectionError;
    }

    public async void InitializeFor(UserProfile profile)
    {
        _currentUser = profile;
        try
        {
            await _chatHubClient.StartConnection();
            IsConnected = _chatHubClient.IsConnected;
            ConnectionStatus = IsConnected ? "Đã kết nối" : "Chưa kết nối";
            await LoadSessionsAsync();
        }
        catch (Exception ex)
        {
            HandleConnectionError(ex.Message);
        }
    }

    private void HandleMessageReceived(string user, string message)
    {
        if (SelectedSession != null)
        {
            var chatMessage = new ChatMessageModel
            {
                ChatSessionId = SelectedSession.ChatSessionId,
                Role = user,
                Content = message,
                CreatedAt = DateTime.Now
            };
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(chatMessage);
            });
        }
    }

    private void HandleSystemMessage(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            SystemMessage = message;
        });
    }

    private void HandleConnectionError(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Error = message;
            IsConnected = _chatHubClient.IsConnected;
            ConnectionStatus = message;
        });
    }

    [RelayCommand]
    private async Task LoadSessionsAsync()
    {
        IsBusy = true;
        Error = null;
        try
        {
            var result = await _chatApiClient.GetSessionsAsync();
            if (result.Success && result.Data != null)
            {
                Sessions = new ObservableCollection<ChatSessionModel>(result.Data.OrderByDescending(s => s.UpdatedAt));
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

    partial void OnSelectedSessionChanged(ChatSessionModel? oldValue, ChatSessionModel? newValue)
    {
        if (oldValue?.ChatSessionId != null)
        {
            _ = _chatHubClient.LeaveSession(oldValue.ChatSessionId.ToString());
        }

        if (newValue?.ChatSessionId != null)
        {
            _ = _chatHubClient.JoinSession(newValue.ChatSessionId.ToString());
            _ = LoadMessagesAsync(newValue.ChatSessionId);
        }
    }

    private async Task LoadMessagesAsync(Guid sessionId)
    {
        IsBusy = true;
        Error = null;
        try
        {
            var result = await _chatApiClient.GetMessagesAsync(sessionId);
            if (result.Success && result.Data != null)
            {
                Messages = new ObservableCollection<ChatMessageModel>(result.Data.OrderBy(m => m.CreatedAt));
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
    private async Task SendMessageAsync()
    {
        if (!IsConnected)
        {
            Error = "Không thể gửi tin nhắn khi chưa kết nối.";
            return;
        }

        if (string.IsNullOrWhiteSpace(MessageText) || string.IsNullOrWhiteSpace(SourceCode))
        {
            Error = "Vui lòng nhập mô tả và đoạn mã.";
            return;
        }

        IsBusy = true;
        Error = null;

        try
        {
            var existingSessionId = SelectedSession?.ChatSessionId ?? Guid.Empty;

            // Add user's message immediately for better UX
            var userMsg = new ChatMessageModel
            {
                ChatMessageId = Guid.NewGuid(),
                ChatSessionId = existingSessionId,
                Role = "user",
                Content = MessageText,
                CreatedAt = DateTime.Now
            };
            Messages.Add(userMsg);

            // Add assistant placeholder immediately so the UI shows that the AI is working
            var placeholderId = Guid.NewGuid().ToString();
            var assistantPlaceholder = new ChatMessageModel
            {
                ChatMessageId = Guid.NewGuid(),
                ChatSessionId = existingSessionId,
                Role = "assistant",
                Content = "Đang phân tích...",
                CreatedAt = DateTime.Now,
                TemporaryId = placeholderId
            };
            Messages.Add(assistantPlaceholder);

            var response = await _chatApiClient.SendMessageAsync(existingSessionId, MessageText, Language, SourceCode);
            if (response.Success && response.Data != null)
            {
                // Clear inputs
                MessageText = string.Empty;
                SourceCode = string.Empty;

                // Refresh sessions and select the returned session if available
                await LoadSessionsAsync();
                SelectedSession = Sessions.FirstOrDefault(s => s.ChatSessionId == response.Data.ChatSessionId) ?? Sessions.FirstOrDefault();

                // Update the placeholder: find by TemporaryId and replace content
                var placeholder = Messages.FirstOrDefault(m => m.TemporaryId == placeholderId);
                if (placeholder != null)
                {
                    placeholder.Content = string.IsNullOrWhiteSpace(response.Data.AIResponse) ? "(No response)" : response.Data.AIResponse;
                    placeholder.CreatedAt = DateTime.Now;
                    placeholder.TemporaryId = null; // mark as real
                }
                else
                {
                    // As a fallback, append assistant message
                    if (!string.IsNullOrWhiteSpace(response.Data.AIResponse))
                    {
                        var assistantMsg = new ChatMessageModel
                        {
                            ChatMessageId = Guid.NewGuid(),
                            ChatSessionId = response.Data.ChatSessionId,
                            Role = "assistant",
                            Content = response.Data.AIResponse,
                            CreatedAt = DateTime.Now
                        };
                        Messages.Add(assistantMsg);
                    }
                }
            }
            else
            {
                Error = response.Message;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExplainCodeAsync()
    {
        MessageText = "Giải thích code này";
        await SendMessageAsync();
    }

    [RelayCommand]
    private async Task AnalyzeErrorsAsync()
    {
        MessageText = "Phân tích lỗi trong code này";
        await SendMessageAsync();
    }

    [RelayCommand]
    private async Task OptimizeCodeAsync()
    {
        MessageText = "Tối ưu code này";
        await SendMessageAsync();
    }
}
