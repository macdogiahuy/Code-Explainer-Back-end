using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using CodeExplainer.Desktop.Services;
using CodeExplainer.Desktop.ViewModels;
using CodeExplainer.Desktop.ViewModels.Chat;
using CodeExplainer.Desktop.ViewModels.Notifications;
using CodeExplainer.Desktop.ViewModels.Profile;
using CodeExplainer.Desktop.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeExplainer.Desktop;

public partial class App : Application
{
	private IServiceProvider _serviceProvider = null!;

	public App()
	{
		ConfigureServices();
	}

	protected override async void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		// Check API reachability before showing main window
		try
		{
			var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
			var request = new HttpRequestMessage(HttpMethod.Head, string.Empty);
			await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
		}
		catch (TaskCanceledException)
		{
			MessageBox.Show($"Unable to connect to API at {_serviceProvider.GetRequiredService<HttpClient>().BaseAddress}\nRequest timed out. Ensure the API is running and the configured Api:BaseUrl is correct.", "API Unreachable", MessageBoxButton.OK, MessageBoxImage.Warning);
		}
		catch (HttpRequestException ex) when (ex.InnerException is SocketException se && se.SocketErrorCode == SocketError.ConnectionRefused)
		{
			MessageBox.Show($"Unable to connect to API at {_serviceProvider.GetRequiredService<HttpClient>().BaseAddress}\nConnection refused. Is the backend running on that port?\n{ex.Message}", "API Unreachable", MessageBoxButton.OK, MessageBoxImage.Warning);
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Unable to connect to API at {_serviceProvider.GetRequiredService<HttpClient>().BaseAddress}\n{ex.Message}", "API Unreachable", MessageBoxButton.OK, MessageBoxImage.Warning);
		}

		var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
		mainWindow.Show();
	}

	private void ConfigureServices()
	{
		var services = new ServiceCollection();

		var configuration = new ConfigurationBuilder()
			.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
			.AddEnvironmentVariables(prefix: "CODEEXPLAINER_DESKTOP_")
			.Build();

		services.AddSingleton<IConfiguration>(configuration);

		services.AddSingleton(_ => new CookieContainer());

		services.AddSingleton(provider =>
		{
			var cookieContainer = provider.GetRequiredService<CookieContainer>();

			var handler = new HttpClientHandler
			{
				UseCookies = true,
				CookieContainer = cookieContainer
			};

			var baseUrl = configuration["Api:BaseUrl"] ?? "https://localhost:5001";
			var httpClient = new HttpClient(handler)
			{
				BaseAddress = new Uri(baseUrl)
			};
			httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
			return httpClient;
		});

		services.AddSingleton<IApiClient, ApiClient>();
		services.AddSingleton<INavigationService, NavigationService>();
		services.AddSingleton<IAuthService, AuthService>();
		services.AddSingleton<IChatApiClient, ChatApiClient>();
		services.AddSingleton<INotificationApiClient, NotificationApiClient>();
		services.AddSingleton<IUserProfileApiClient, UserProfileApiClient>();

		// Register the chat hub client used by ChatViewModel
		services.AddSingleton<IChatHubClient, ChatHubClient>();

		// Register the notification hub client used by NotificationsViewModel
		services.AddSingleton<INotificationHubClient, NotificationHubClient>();

		services.AddSingleton<MainViewModel>();
		services.AddSingleton<AuthViewModel>();
		services.AddSingleton<ChatViewModel>();
		services.AddSingleton<NotificationsViewModel>();
		services.AddSingleton<ProfileViewModel>();

		services.AddSingleton<MainWindow>();

		_serviceProvider = services.BuildServiceProvider();
	}
}

