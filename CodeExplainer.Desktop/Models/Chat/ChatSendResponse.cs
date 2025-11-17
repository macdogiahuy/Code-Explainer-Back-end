namespace CodeExplainer.Desktop.Models.Chat;

public class ChatSendResponse
{
    public Guid ChatSessionId { get; set; }
    public string UserMessage { get; set; } = string.Empty;
    public string AIResponse { get; set; } = string.Empty;
}
