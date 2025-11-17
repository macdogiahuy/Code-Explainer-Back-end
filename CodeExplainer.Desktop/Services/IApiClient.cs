using System.Net;
using CodeExplainer.Desktop.Models;

namespace CodeExplainer.Desktop.Services;

public interface IApiClient
{
    Task<ApiResult<TResponse>> GetAsync<TResponse>(string uri, CancellationToken cancellationToken = default);
    Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string uri, TRequest payload, CancellationToken cancellationToken = default);
    Task<ApiResult<TResponse>> PutAsync<TRequest, TResponse>(string uri, TRequest payload, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteAsync(string uri, CancellationToken cancellationToken = default);
    CookieContainer CookieContainer { get; }
}
