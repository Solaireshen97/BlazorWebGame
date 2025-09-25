using BlazorWebGame.Shared.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 客户端生产系统 API 服务 - 连接到服务端生产 API
/// </summary>
public class ProductionApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductionApiService> _logger;

    public ProductionApiService(HttpClient httpClient, ILogger<ProductionApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有可用的采集节点
    /// </summary>
    public async Task<List<GatheringNodeDto>> GetAvailableNodesAsync(string profession = "")
    {
        try
        {
            var query = string.IsNullOrEmpty(profession) ? "" : $"?profession={profession}";
            var response = await _httpClient.GetAsync($"api/production/nodes{query}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<GatheringNodeDto>>>();
                return result?.Data ?? new List<GatheringNodeDto>();
            }
            else
            {
                _logger.LogWarning("Failed to get available nodes: {StatusCode}", response.StatusCode);
                return new List<GatheringNodeDto>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available nodes");
            return new List<GatheringNodeDto>();
        }
    }

    /// <summary>
    /// 根据ID获取特定采集节点
    /// </summary>
    public async Task<GatheringNodeDto?> GetNodeByIdAsync(string nodeId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/production/nodes/{nodeId}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<GatheringNodeDto>>();
                return result?.Data;
            }
            else
            {
                _logger.LogWarning("Failed to get node {NodeId}: {StatusCode}", nodeId, response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting node {NodeId}", nodeId);
            return null;
        }
    }

    /// <summary>
    /// 开始采集
    /// </summary>
    public async Task<(bool Success, string Message, GatheringStateDto? State)> StartGatheringAsync(string characterId, string nodeId)
    {
        try
        {
            var request = new StartGatheringRequest
            {
                CharacterId = characterId,
                NodeId = nodeId
            };

            var response = await _httpClient.PostAsJsonAsync("api/production/gathering/start", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<GatheringStateDto>>();

            if (result != null)
            {
                return (result.Success, result.Message, result.Data);
            }
            else
            {
                return (false, "请求失败", null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting gathering for character {CharacterId} at node {NodeId}", characterId, nodeId);
            return (false, "开始采集时发生错误", null);
        }
    }

    /// <summary>
    /// 停止采集
    /// </summary>
    public async Task<(bool Success, string Message)> StopGatheringAsync(string characterId)
    {
        try
        {
            var request = new StopGatheringRequest
            {
                CharacterId = characterId
            };

            var response = await _httpClient.PostAsJsonAsync("api/production/gathering/stop", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();

            if (result != null)
            {
                return (result.Success, result.Message);
            }
            else
            {
                return (false, "请求失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping gathering for character {CharacterId}", characterId);
            return (false, "停止采集时发生错误");
        }
    }

    /// <summary>
    /// 获取玩家当前的采集状态
    /// </summary>
    public async Task<GatheringStateDto?> GetGatheringStateAsync(string characterId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/production/gathering/state/{characterId}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<GatheringStateDto>>();
                return result?.Data;
            }
            else
            {
                _logger.LogWarning("Failed to get gathering state for {CharacterId}: {StatusCode}", characterId, response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gathering state for character {CharacterId}", characterId);
            return null;
        }
    }

    /// <summary>
    /// 获取所有活跃的采集状态（管理用）
    /// </summary>
    public async Task<List<GatheringStateDto>> GetActiveGatheringStatesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/production/gathering/active");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<GatheringStateDto>>>();
                return result?.Data ?? new List<GatheringStateDto>();
            }
            else
            {
                _logger.LogWarning("Failed to get active gathering states: {StatusCode}", response.StatusCode);
                return new List<GatheringStateDto>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active gathering states");
            return new List<GatheringStateDto>();
        }
    }
}