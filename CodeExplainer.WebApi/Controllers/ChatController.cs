using System.Collections.Generic;
using System.Security.Claims;
using CodeExplainer.BusinessObject.Request;
using CodeExplainer.BusinessObject.Response;
using CodeExplainer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using CodeExplainer.WebApi.Hubs;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace CodeExplainer.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatServices _chatService;
    private readonly INotificationServices _notificationService;
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatServices chatService,
        INotificationServices notificationService,
        IHubContext<NotificationHub> notificationHub,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _notificationService = notificationService;
        _notificationHub = notificationHub;
        _logger = logger;
    }
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] ChatSendRequest request)
    {
        var userId = GetUserId();
        try
        {
            var result = await _chatService.SendMessageAsync(userId, request);

            // Notify the user that the AI response is ready
            await PublishNotificationAsync(
                userId,
                "Phản hồi mới từ AI",
                result.AIResponse);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process chat request for user {UserId}", userId);

            await PublishNotificationAsync(
                userId,
                "Phân tích thất bại",
                ex.Message);

            var failureResponse = new BaseResultResponse<ChatSendResponse>
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Success = false,
                Message = "Không thể xử lý yêu cầu. Vui lòng thử lại sau.",
                Errors = new List<string> { ex.Message },
                Data = null
            };

            return StatusCode(StatusCodes.Status500InternalServerError, failureResponse);
        }
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var userId = GetUserId();
        var sessions = await _chatService.GetUserSessionsAsync(userId);
        return Ok(sessions);
    }

    [HttpGet("messages/{sessionId:guid}")]
    public async Task<IActionResult> GetMessages(Guid sessionId)
    {
        var messages = await _chatService.GetMessagesAsync(sessionId);
        return Ok(messages);
    }

    private async Task PublishNotificationAsync(Guid userId, string title, string? message)
    {
        try
        {
            var trimmed = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
            if (!string.IsNullOrEmpty(trimmed) && trimmed.Length > 200)
            {
                trimmed = trimmed[..200] + "...";
            }

            var notification = await _notificationService.CreateNotificationAsync(userId, title, trimmed);
            await _notificationHub.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification.NotificationId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to publish notification '{Title}' for user {UserId}", title, userId);
        }
    }
}