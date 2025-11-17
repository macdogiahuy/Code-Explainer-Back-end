using System;
using System.Net.Http;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace CodeExplainer.Desktop.Services;

public interface INotificationHubClient
{
 Task StartConnection();
 Task StopConnection();
 event Action<string> OnNotificationReceived;
 event Action<string> OnConnectionError;
 bool IsConnected { get; }
}

public class NotificationHubClient : INotificationHubClient
{
 private readonly HubConnection _hubConnection;

 public event Action<string>? OnNotificationReceived;
 public event Action<string>? OnConnectionError;

 public bool IsConnected => _hubConnection.State == HubConnectionState.Connected;

 // Accept HttpClient so we can reuse its configured BaseAddress (which has a fallback in DI)
 public NotificationHubClient(IConfiguration configuration, HttpClient? httpClient = null)
 {
 // Prefer HttpClient.BaseAddress (registered in DI). Fall back to configuration keys or a safe default.
 Uri? baseUri = httpClient?.BaseAddress;

 if (baseUri == null)
 {
 var baseUrl = configuration["Api:BaseUrl"] ?? configuration.GetValue<string>("ApiBaseUrl") ?? "https://localhost:5001";

 if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out baseUri))
 {
 throw new InvalidOperationException($"Invalid Api base URL: '{baseUrl}'");
 }
 }

 var hubUrl = new Uri(baseUri, "hubs/notification");

 _hubConnection = new HubConnectionBuilder()
 .WithUrl(hubUrl)
 .WithAutomaticReconnect()
 .Build();

 _hubConnection.On<string>("ReceiveNotification", (message) =>
 {
 OnNotificationReceived?.Invoke(message);
 });

 _hubConnection.Reconnecting += error =>
 {
 OnConnectionError?.Invoke("Đang kết nối lại đến máy chủ thông báo...");
 return Task.CompletedTask;
 };

 _hubConnection.Reconnected += connectionId =>
 {
 OnConnectionError?.Invoke("Đã kết nối lại thành công.");
 return Task.CompletedTask;
 };

 _hubConnection.Closed += error =>
 {
 if (error != null)
 {
 OnConnectionError?.Invoke($"Mất kết nối đến máy chủ thông báo: {error.Message}");
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
 }
 }
 catch (Exception ex)
 {
 OnConnectionError?.Invoke($"Không thể kết nối đến máy chủ thông báo: {ex.Message}");
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
 OnConnectionError?.Invoke($"Lỗi khi ngắt kết nối từ máy chủ thông báo: {ex.Message}");
 throw;
 }
 }
}