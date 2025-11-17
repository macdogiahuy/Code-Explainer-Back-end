using System;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace CodeExplainer.Desktop.Services;

public interface IChatHubClient
{
 Task StartConnection();
 Task StopConnection();
 Task JoinSession(string sessionId);
 Task LeaveSession(string sessionId);
 event Action<string, string> OnMessageReceived;
 event Action<string> OnSystemMessage;
 event Action<string> OnConnectionError;
 bool IsConnected { get; }
}

public class ChatHubClient : IChatHubClient
{
 private readonly HubConnection _hubConnection;

 public event Action<string, string>? OnMessageReceived;
 public event Action<string>? OnSystemMessage;
 public event Action<string>? OnConnectionError;

 public bool IsConnected => _hubConnection.State == HubConnectionState.Connected;

 public ChatHubClient(IConfiguration configuration)
 {
 // Support both "Api:BaseUrl" and legacy/single value "ApiBaseUrl" config keys, fallback to localhost.
 var baseUrl = configuration["Api:BaseUrl"] ?? configuration.GetValue<string>("ApiBaseUrl") ?? "https://localhost:5001";

 if (string.IsNullOrWhiteSpace(baseUrl))
 {
 throw new ArgumentException("Configuration value for 'Api:BaseUrl' is missing or empty.");
 }

 // Ensure we build an absolute URI for the hub endpoint
 if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
 {
 // Attempt to prepend scheme if user provided host without scheme
 if (Uri.TryCreate("https://" + baseUrl, UriKind.Absolute, out baseUri) == false)
 {
 throw new ArgumentException($"Configuration value for 'Api:BaseUrl' is not a valid URI: '{baseUrl}'");
 }
 }

 var hubUri = new Uri(baseUri, "hubs/chat");

 _hubConnection = new HubConnectionBuilder()
 .WithUrl(hubUri)
 .WithAutomaticReconnect()
 .Build();

 _hubConnection.On<string, string>("ReceiveMessage", (user, message) =>
 {
 OnMessageReceived?.Invoke(user, message);
 });

 _hubConnection.On<string>("SystemMessage", (message) =>
 {
 OnSystemMessage?.Invoke(message);
 });

 _hubConnection.Reconnecting += error =>
 {
 OnConnectionError?.Invoke("Đang kết nối lại...");
 return Task.CompletedTask;
 };

 _hubConnection.Reconnected += connectionId =>
 {
 OnSystemMessage?.Invoke("Đã kết nối lại thành công.");
 return Task.CompletedTask;
 };

 _hubConnection.Closed += error =>
 {
 if (error != null)
 {
 OnConnectionError?.Invoke($"Mất kết nối: {error.Message}");
 }
 return Task.CompletedTask;
 };
 }

 public async Task StartConnection()
 {
 try
 {
 if (_hubConnection.State == HubConnectionState.Disconnected)
 {
 await _hubConnection.StartAsync();
 OnSystemMessage?.Invoke("Đã kết nối thành công.");
 }
 }
 catch (Exception ex)
 {
 OnConnectionError?.Invoke($"Không thể kết nối: {ex.Message}");
 throw;
 }
 }

 public async Task StopConnection()
 {
 try
 {
 if (_hubConnection.State != HubConnectionState.Disconnected)
 {
 await _hubConnection.StopAsync();
 }
 }
 catch (Exception ex)
 {
 OnConnectionError?.Invoke($"Lỗi khi ngắt kết nối: {ex.Message}");
 throw;
 }
 }

 public async Task JoinSession(string sessionId)
 {
 try
 {
 await _hubConnection.InvokeAsync("JoinSession", sessionId);
 }
 catch (Exception ex)
 {
 OnConnectionError?.Invoke($"Không thể tham gia phiên chat: {ex.Message}");
 throw;
 }
 }

 public async Task LeaveSession(string sessionId)
 {
 try
 {
 await _hubConnection.InvokeAsync("LeaveSession", sessionId);
 }
 catch (Exception ex)
 {
 OnConnectionError?.Invoke($"Lỗi khi rời phiên chat: {ex.Message}");
 throw;
 }
 }
}