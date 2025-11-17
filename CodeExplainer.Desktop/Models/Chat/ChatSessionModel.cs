namespace CodeExplainer.Desktop.Models.Chat;

public class ChatSessionModel
{
    public Guid ChatSessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
