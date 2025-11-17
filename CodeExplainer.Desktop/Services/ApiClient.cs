using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;
using CodeExplainer.Desktop.Models;

namespace CodeExplainer.Desktop.Services;

public class ApiClient : IApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookieContainer;

    public ApiClient(HttpClient httpClient, CookieContainer cookieContainer)
    {
        _httpClient = httpClient;
        _cookieContainer = cookieContainer;
    }

    public CookieContainer CookieContainer => _cookieContainer;

    public async Task<ApiResult<TResponse>> GetAsync<TResponse>(string uri, CancellationToken cancellationToken = default)
    {
        try
        {
            Debug.WriteLine($"GET: {_httpClient.BaseAddress}{uri}");
            var response = await _httpClient.GetAsync(uri, cancellationToken);
            return await ParseResponse<TResponse>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"HttpRequestException GET {_httpClient.BaseAddress}{uri} -> {ex.Message}");
            return ApiResult<TResponse>.Fail($"Unable to connect to API at {_httpClient.BaseAddress}{uri}: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            Debug.WriteLine($"Timeout GET {_httpClient.BaseAddress}{uri} -> {ex.Message}");
            return ApiResult<TResponse>.Fail($"Request to {_httpClient.BaseAddress}{uri} timed out: {ex.Message}");
        }
    }

    public async Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string uri, TRequest payload, CancellationToken cancellationToken = default)
    {
        try
        {
            Debug.WriteLine($"POST: {_httpClient.BaseAddress}{uri}");
            using var response = await _httpClient.PostAsJsonAsync(uri, payload, SerializerOptions, cancellationToken);
            return await ParseResponse<TResponse>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"HttpRequestException POST {_httpClient.BaseAddress}{uri} -> {ex.Message}");
            return ApiResult<TResponse>.Fail($"Unable to connect to API at {_httpClient.BaseAddress}{uri}: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            Debug.WriteLine($"Timeout POST {_httpClient.BaseAddress}{uri} -> {ex.Message}");
            return ApiResult<TResponse>.Fail($"Request to {_httpClient.BaseAddress}{uri} timed out: {ex.Message}");
        }
    }

    public async Task<ApiResult<TResponse>> PutAsync<TRequest, TResponse>(string uri, TRequest payload, CancellationToken cancellationToken = default)
    {
        try
        {
            Debug.WriteLine($"PUT: {_httpClient.BaseAddress}{uri}");
            using var response = await _httpClient.PutAsJsonAsync(uri, payload, SerializerOptions, cancellationToken);
            return await ParseResponse<TResponse>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"HttpRequestException PUT {_httpClient.BaseAddress}{uri} -> {ex.Message}");
            return ApiResult<TResponse>.Fail($"Unable to connect to API at {_httpClient.BaseAddress}{uri}: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            Debug.WriteLine($"Timeout PUT {_httpClient.BaseAddress}{uri} -> {ex.Message}");
            return ApiResult<TResponse>.Fail($"Request to {_httpClient.BaseAddress}{uri} timed out: {ex.Message}");
        }
    }

    public async Task<ApiResult> DeleteAsync(string uri, CancellationToken cancellationToken = default)
    {
        try
        {
            Debug.WriteLine($"DELETE: {_httpClient.BaseAddress}{uri}");
            using var response = await _httpClient.DeleteAsync(uri, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return ApiResult.Ok();
            }

            var parsed = await TryParseBaseResult<string>(response, cancellationToken);
            return ApiResult.Fail(parsed?.Message ?? response.ReasonPhrase, parsed?.Errors);
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"HttpRequestException DELETE {_httpClient.BaseAddress}{uri} -> {ex.Message}");
            return ApiResult.Fail($"Unable to connect to API at {_httpClient.BaseAddress}{uri}: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            Debug.WriteLine($"Timeout DELETE {_httpClient.BaseAddress}{uri} -> {ex.Message}");
            return ApiResult.Fail($"Request to {_httpClient.BaseAddress}{uri} timed out: {ex.Message}");
        }
    }

    private static async Task<BaseResultResponse<T>?> TryParseBaseResult<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return TryDeserialize<BaseResultResponse<T>>(content);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<ApiResult<TResponse>> ParseResponse<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength == 0)
        {
            return response.IsSuccessStatusCode
        ? ApiResult<TResponse>.Ok(default(TResponse), response.ReasonPhrase)
                : ApiResult<TResponse>.Fail(response.ReasonPhrase);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(content))
        {
            var baseResult = TryDeserialize<BaseResultResponse<TResponse>>(content);
            if (baseResult != null)
            {
                if (baseResult.Success)
                {
                    return ApiResult<TResponse>.Ok(baseResult.Data, baseResult.Message);
                }

                return ApiResult<TResponse>.Fail(baseResult.Message, baseResult.Errors);
            }

            var payload = TryDeserialize<TResponse>(content);
            if (payload is not null && response.IsSuccessStatusCode)
            {
                return ApiResult<TResponse>.Ok(payload, response.ReasonPhrase);
            }
        }

        if (response.IsSuccessStatusCode)
        {
            return ApiResult<TResponse>.Ok(default(TResponse), response.ReasonPhrase);
        }

        return ApiResult<TResponse>.Fail(response.ReasonPhrase, string.IsNullOrWhiteSpace(content) ? null : new[] { content });
    }

    private static T? TryDeserialize<T>(string raw)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(raw, SerializerOptions);
        }
        catch
        {
            return default;
        }
    }
}
