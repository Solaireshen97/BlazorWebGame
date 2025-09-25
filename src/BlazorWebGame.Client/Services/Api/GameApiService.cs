using BlazorWebGame.Shared.DTOs;
using System.Net.Http.Json;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 客户端API服务，负责与服务器通信
/// </summary>
public class GameApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GameApiService> _logger;
    private string _baseUrl = "https://localhost:7290"; // 默认服务器地址

    public string BaseUrl => _baseUrl;

    public GameApiService(HttpClient httpClient, ILogger<GameApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // 配置基础地址
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }
    }

    /// <summary>
    /// 设置服务器基础地址
    /// </summary>
    public void SetBaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl;
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    /// <summary>
    /// 开始战斗
    /// </summary>
    public async Task<ApiResponse<BattleStateDto>> StartBattleAsync(StartBattleRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/battle/start", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<BattleStateDto>>() 
                    ?? new ApiResponse<BattleStateDto> { Success = false };
            }

            return new ApiResponse<BattleStateDto>
            {
                Success = false,
                Message = $"Server returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting battle");
            return new ApiResponse<BattleStateDto>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 获取战斗状态
    /// </summary>
    public async Task<ApiResponse<BattleStateDto>> GetBattleStateAsync(Guid battleId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ApiResponse<BattleStateDto>>(
                $"api/battle/state/{battleId}") 
                ?? new ApiResponse<BattleStateDto> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting battle state for {BattleId}", battleId);
            return new ApiResponse<BattleStateDto>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 停止战斗
    /// </summary>
    public async Task<ApiResponse<bool>> StopBattleAsync(Guid battleId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/battle/stop/{battleId}", null);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>() 
                    ?? new ApiResponse<bool> { Success = false };
            }

            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Server returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping battle {BattleId}", battleId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Network error occurred"
            };
        }
    }

    /// <summary>
    /// 检查服务器连接状态
    /// </summary>
    public async Task<bool> IsServerAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/battle/state/00000000-0000-0000-0000-000000000000");
            // 我们不关心返回内容，只要能连通就行
            return true;
        }
        catch
        {
            return false;
        }
    }
}