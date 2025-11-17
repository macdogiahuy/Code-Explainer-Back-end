using CodeExplainer.BusinessObject;
using CodeExplainer.BusinessObject.Models;
using CodeExplainer.BusinessObject.Request;
using CodeExplainer.BusinessObject.Response;
using CodeExplainer.Services.Interfaces;
using MaIN.Core.Hub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeExplainer.Services.Implements;    

public class ChatServices : IChatServices
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ChatServices> _logger;
    private const int MaxSourceCodeLength = 10000; // prevent sending excessively large payloads to AI

    public ChatServices(ApplicationDbContext context, ILogger<ChatServices> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<ChatSendResponse> SendMessageAsync(Guid userId, ChatSendRequest request)
    {
        ChatSession chatSession;
        if (request.ChatSessionId == Guid.Empty)
        {
            chatSession = new ChatSession
            {
                ChatSessionId = Guid.NewGuid(),
                UserId = userId,
                Title = request.Message.Length > 50 ? request.Message[..50] + "..." : request.Message,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _context.ChatSessions.Add(chatSession);
        }
        else
        {
            chatSession = await _context.ChatSessions
                .FirstOrDefaultAsync(x => x.ChatSessionId == request.ChatSessionId && x.UserId == userId)
                ?? new ChatSession
                {
                    ChatSessionId = request.ChatSessionId,
                    UserId = userId,
                    Title = request.Message.Length > 50 ? request.Message[..50] + "..." : request.Message,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
            if (_context.Entry(chatSession).State == EntityState.Detached)
                _context.ChatSessions.Add(chatSession);
            chatSession.UpdatedAt = DateTime.Now;
        }

        // Ensure request properties are not null
        request.Message ??= string.Empty;
        request.Language ??= "text";
        request.SourceCode ??= string.Empty;

        // Truncate large source code to avoid sending massive payloads that may cause400 from the AI backend
        var sourceToSend = request.SourceCode;
        var truncatedNotice = string.Empty;
        if (sourceToSend.Length > MaxSourceCodeLength)
        {
            truncatedNotice = "\n\n[Source code truncated due to length]";
            sourceToSend = sourceToSend.Substring(0, MaxSourceCodeLength);
            _logger.LogWarning("Source code truncated from {OriginalLength} to {MaxLength} characters before sending to AI.", request.SourceCode.Length, MaxSourceCodeLength);
        }

        var message = new ChatMessage
        {
            ChatMessageId = Guid.NewGuid(),
            ChatSessionId = chatSession.ChatSessionId,
            Role = "user",
            Content = $"{request.Message}\n\n```{request.Language}\n{request.SourceCode}\n```",
            CreatedAt = DateTime.Now
        };
        _context.ChatMessages.Add(message);

        var prompt = $"Explain what this {request.Language} code does. Also suggest improvements if possible:\n\n{sourceToSend}{truncatedNotice}";

        // Call AI and handle errors gracefully
        string replyText;
        try
        {
            var aiResponse = await AIHub.Chat()
                .WithModel("gemini-2.0-flash")
                .WithMessage(prompt)
                .CompleteAsync();

            replyText = aiResponse?.Message?.Content ?? string.Empty;
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            _logger.LogError(ex, "AI request failed with HttpRequestException.");
            replyText = "AI service request failed: " + ex.Message;

            // Persist reply message with error content so client gets feedback
            var errorReplyMessage = new ChatMessage
            {
                ChatMessageId = Guid.NewGuid(),
                ChatSessionId = chatSession.ChatSessionId,
                Role = "assistant",
                Content = replyText,
                CreatedAt = DateTime.Now
            };
            _context.ChatMessages.Add(errorReplyMessage);

            // Save both user message and error reply
            await _context.SaveChangesAsync();

            return new ChatSendResponse
            {
                ChatSessionId = chatSession.ChatSessionId,
                UserMessage = message.Content,
                AIResponse = errorReplyMessage.Content
            };
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling AI.");
            replyText = "Unexpected error while calling AI: " + ex.Message;

            var errorReplyMessage = new ChatMessage
            {
                ChatMessageId = Guid.NewGuid(),
                ChatSessionId = chatSession.ChatSessionId,
                Role = "assistant",
                Content = replyText,
                CreatedAt = DateTime.Now
            };
            _context.ChatMessages.Add(errorReplyMessage);
            await _context.SaveChangesAsync();

            return new ChatSendResponse
            {
                ChatSessionId = chatSession.ChatSessionId,
                UserMessage = message.Content,
                AIResponse = errorReplyMessage.Content
            };
        }

        var replyMessage = new ChatMessage
        {
            ChatMessageId = Guid.NewGuid(),
            ChatSessionId = chatSession.ChatSessionId,
            Role = "assistant",
            Content = replyText,
            CreatedAt = DateTime.Now
        };
        _context.ChatMessages.Add(replyMessage);

        await _context.SaveChangesAsync();

        return new ChatSendResponse
        {
            ChatSessionId = chatSession.ChatSessionId,
            UserMessage = message.Content,
            AIResponse = replyMessage.Content
        };
    }

    public async Task<List<ChatSession>> GetUserSessionsAsync(Guid userId)
        => await _context.ChatSessions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

    public async Task<List<ChatMessage>> GetMessagesAsync(Guid chatSessionId)
        => await _context.ChatMessages
            .Where(x => x.ChatSessionId == chatSessionId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
}