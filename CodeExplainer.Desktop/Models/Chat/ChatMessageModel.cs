namespace CodeExplainer.Desktop.Models.Chat;

public class ChatMessageModel
{
    public Guid ChatMessageId { get; set; }
    public Guid ChatSessionId { get; set; }
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsFromUser => Role.Equals("user", StringComparison.OrdinalIgnoreCase);

    // Optional temporary id used by the client to identify placeholder messages
    public string? TemporaryId { get; set; }

    public bool IsPlaceholder => !string.IsNullOrWhiteSpace(TemporaryId);
}
