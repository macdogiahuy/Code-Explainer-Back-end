using CodeExplainer.Desktop.Models;
using CodeExplainer.Desktop.Models.Chat;

namespace CodeExplainer.Desktop.Services;

public interface IChatApiClient
{
    Task<ApiResult<IReadOnlyList<ChatSessionModel>>> GetSessionsAsync();
    Task<ApiResult<IReadOnlyList<ChatMessageModel>>> GetMessagesAsync(Guid sessionId);
    Task<ApiResult<ChatSendResponse>> SendMessageAsync(Guid sessionId, string message, string language, string sourceCode);
}
