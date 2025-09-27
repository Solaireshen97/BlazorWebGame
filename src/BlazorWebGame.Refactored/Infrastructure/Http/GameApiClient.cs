using BlazorWebGame.Refactored.Application.Interfaces;
using BlazorWebGame.Refactored.Application.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace BlazorWebGame.Refactored.Infrastructure.Http;

public class GameApiClient : IHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GameApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public GameApiClient(HttpClient httpClient, ILogger<GameApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            _logger.LogInformation("GET request to: {Endpoint}", endpoint);
            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }
            
            _logger.LogWarning("GET request failed: {StatusCode} - {Endpoint}", response.StatusCode, endpoint);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GET request to: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            _logger.LogInformation("POST request to: {Endpoint}", endpoint);
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(endpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
            }
            
            _logger.LogWarning("POST request failed: {StatusCode} - {Endpoint}", response.StatusCode, endpoint);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in POST request to: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            _logger.LogInformation("PUT request to: {Endpoint}", endpoint);
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync(endpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
            }
            
            _logger.LogWarning("PUT request failed: {StatusCode} - {Endpoint}", response.StatusCode, endpoint);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PUT request to: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            _logger.LogInformation("DELETE request to: {Endpoint}", endpoint);
            var response = await _httpClient.DeleteAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            
            _logger.LogWarning("DELETE request failed: {StatusCode} - {Endpoint}", response.StatusCode, endpoint);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DELETE request to: {Endpoint}", endpoint);
            throw;
        }
    }

    // 角色相关API
    public async Task<List<CharacterDto>> GetCharactersAsync(string userId)
    {
        var result = await GetAsync<List<CharacterDto>>($"api/characters/user/{userId}");
        return result ?? new List<CharacterDto>();
    }

    public async Task<CharacterDto?> GetCharacterAsync(Guid characterId)
    {
        return await GetAsync<CharacterDto>($"api/characters/{characterId}");
    }

    public async Task<CharacterDto?> CreateCharacterAsync(CreateCharacterRequest request)
    {
        return await PostAsync<CreateCharacterRequest, CharacterDto>("api/characters", request);
    }

    public async Task<bool> DeleteCharacterAsync(Guid characterId)
    {
        return await DeleteAsync($"api/characters/{characterId}");
    }

    // 活动相关API
    public async Task<List<ActivityDto>> GetCharacterActivitiesAsync(Guid characterId)
    {
        var result = await GetAsync<List<ActivityDto>>($"api/activities/character/{characterId}");
        return result ?? new List<ActivityDto>();
    }

    public async Task<ActivityDto?> StartActivityAsync(StartActivityRequest request)
    {
        return await PostAsync<StartActivityRequest, ActivityDto>("api/activities/start", request);
    }

    public async Task<bool> CancelActivityAsync(Guid activityId)
    {
        return await DeleteAsync($"api/activities/{activityId}");
    }

    public async Task<ActivityDto?> GetActivityAsync(Guid activityId)
    {
        return await GetAsync<ActivityDto>($"api/activities/{activityId}");
    }
}

// Request DTOs
public class CreateCharacterRequest
{
    public string Name { get; set; } = string.Empty;
    public string CharacterClass { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

public class StartActivityRequest
{
    public Guid CharacterId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}