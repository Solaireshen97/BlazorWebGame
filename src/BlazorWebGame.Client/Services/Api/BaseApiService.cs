using BlazorWebGame.Shared.DTOs;
using System.Net.Http.Json;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// API服务基类，提供通用的HTTP请求功能
/// </summary>
public abstract class BaseApiService
{
    protected readonly ConfigurableHttpClientFactory _httpClientFactory;
    protected readonly ILogger _logger;

    protected BaseApiService(ConfigurableHttpClientFactory httpClientFactory, ILogger logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 获取配置好的HttpClient
    /// </summary>
    protected HttpClient GetHttpClient() => _httpClientFactory.GetHttpClient();

    /// <summary>
    /// 执行GET请求
    /// </summary>
    protected async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.GetFromJsonAsync<ApiResponse<T>>(endpoint);
            return response ?? new ApiResponse<T> { Success = false, Message = "No response received" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GET request to {Endpoint}", endpoint);
            return new ApiResponse<T>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 执行POST请求
    /// </summary>
    protected async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? request = null)
    {
        try
        {
            var httpClient = GetHttpClient();
            HttpResponseMessage response;

            if (request == null)
            {
                response = await httpClient.PostAsync(endpoint, null);
            }
            else
            {
                response = await httpClient.PostAsJsonAsync(endpoint, request);
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
                return result ?? new ApiResponse<T> { Success = false, Message = "No response received" };
            }

            return new ApiResponse<T>
            {
                Success = false,
                Message = $"Server returned {response.StatusCode}: {response.ReasonPhrase}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during POST request to {Endpoint}", endpoint);
            return new ApiResponse<T>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 执行PUT请求
    /// </summary>
    protected async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object request)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.PutAsJsonAsync(endpoint, request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
                return result ?? new ApiResponse<T> { Success = false, Message = "No response received" };
            }

            return new ApiResponse<T>
            {
                Success = false,
                Message = $"Server returned {response.StatusCode}: {response.ReasonPhrase}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during PUT request to {Endpoint}", endpoint);
            return new ApiResponse<T>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 执行DELETE请求
    /// </summary>
    protected async Task<ApiResponse<T>> DeleteAsync<T>(string endpoint)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.DeleteAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
                return result ?? new ApiResponse<T> { Success = false, Message = "No response received" };
            }

            return new ApiResponse<T>
            {
                Success = false,
                Message = $"Server returned {response.StatusCode}: {response.ReasonPhrase}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during DELETE request to {Endpoint}", endpoint);
            return new ApiResponse<T>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }
}