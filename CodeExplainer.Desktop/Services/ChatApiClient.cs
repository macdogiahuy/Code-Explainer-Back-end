using System.Net.Http.Json;
using CodeExplainer.Desktop.Models;
using CodeExplainer.Desktop.Models.Chat;

namespace CodeExplainer.Desktop.Services;

public class ChatApiClient : IChatApiClient
{
    private readonly IApiClient _apiClient;

    public ChatApiClient(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<ApiResult<IReadOnlyList<ChatSessionModel>>> GetSessionsAsync()
        => _apiClient.GetAsync<IReadOnlyList<ChatSessionModel>>("api/chat/sessions");

    public Task<ApiResult<IReadOnlyList<ChatMessageModel>>> GetMessagesAsync(Guid sessionId)
        => _apiClient.GetAsync<IReadOnlyList<ChatMessageModel>>($"api/chat/messages/{sessionId}");

    public Task<ApiResult<ChatSendResponse>> SendMessageAsync(Guid sessionId, string message, string language, string sourceCode)
    {
        var payload = new
        {
            ChatSessionId = sessionId,
            Message = message,
            Language = language,
            SourceCode = sourceCode
        };
        return _apiClient.PostAsync<object, ChatSendResponse>("api/chat/send", payload);
    }
}
