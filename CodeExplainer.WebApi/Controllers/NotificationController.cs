using System;
using System.Security.Claims;
using CodeExplainer.BusinessObject.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CodeExplainer.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using CodeExplainer.WebApi.Hubs;
using Microsoft.Extensions.Logging;
namespace CodeExplainer.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationServices _notificationService;
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly ILogger<NotificationController> _logger;
    
    public NotificationController(
        INotificationServices notificationService,
        IHubContext<NotificationHub> notificationHub,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _notificationHub = notificationHub;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetUserNotifications()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var notifications = await _notificationService.GetUserNotificationsAsync(userId);
        return Ok(notifications);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] NotificationCreateRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var notification = await _notificationService.CreateNotificationAsync(userId, request.Title, request.Message);
        await TryNotifyAsync(userId, notification.NotificationId.ToString());
        return Ok(notification);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await _notificationService.MarkAsReadAsync(id);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        await _notificationService.DeleteNotificationAsync(id);
        return Ok();
    }

    private async Task TryNotifyAsync(Guid userId, string message)
    {
        try
        {
            await _notificationHub.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to push notification hub message for user {UserId}", userId);
        }
    }
}