using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace BlazorIdleGame.Client.Services.Core
{
    public interface IGameCommunicationService
    {
        Task<T?> GetAsync<T>(string endpoint);
        Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data);
        Task<bool> PostAsync(string endpoint, object data);
        void SetAuthToken(string token);
        void ClearAuthToken();
    }

    public class GameCommunicationService : IGameCommunicationService
    {
        private readonly HttpClient _http;
        private readonly ILogger<GameCommunicationService> _logger;

        public GameCommunicationService(HttpClient http, ILogger<GameCommunicationService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public void SetAuthToken(string token)
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public void ClearAuthToken()
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _http.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<T>();
                }
                else
                {
                    _logger.LogWarning("API请求失败: {Endpoint}, 状态码: {StatusCode}",
                        endpoint, response.StatusCode);
                    return default;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET 请求失败: {Endpoint}", endpoint);
                return default;
            }
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(endpoint, data);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<TResponse>();
                }
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "POST 请求失败: {Endpoint}", endpoint);
                return default;
            }
        }

        public async Task<bool> PostAsync(string endpoint, object data)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(endpoint, data);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "POST 请求失败: {Endpoint}", endpoint);
                return false;
            }
        }
    }
}