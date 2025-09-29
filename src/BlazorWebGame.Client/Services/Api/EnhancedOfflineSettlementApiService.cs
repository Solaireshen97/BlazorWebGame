using BlazorWebGame.Shared.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 增强离线结算API服务 - 客户端实现
/// </summary>
public class EnhancedOfflineSettlementApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EnhancedOfflineSettlementApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public EnhancedOfflineSettlementApiService(
        HttpClient httpClient, 
        ILogger<EnhancedOfflineSettlementApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    /// <summary>
    /// 执行增强的离线结算
    /// </summary>
    public async Task<ApiResponse<EnhancedOfflineSettlementResultDto>> ProcessEnhancedOfflineSettlementAsync(
        string playerId, 
        bool useEventDriven = true,
        bool enableTimeDecay = true,
        TimeSpan? customOfflineTime = null)
    {
        try
        {
            var request = new EnhancedOfflineSettlementRequestDto
            {
                PlayerId = playerId,
                UseEventDriven = useEventDriven,
                EnableTimeDecay = enableTimeDecay,
                CustomOfflineTime = customOfflineTime
            };

            _logger.LogInformation("发送增强离线结算请求：玩家 {PlayerId}，事件驱动 {EventDriven}，时间衰减 {TimeDecay}",
                playerId, useEventDriven, enableTimeDecay);

            var response = await _httpClient.PostAsJsonAsync(
                $"api/offlineSettlement/enhanced/player/{playerId}", 
                request, 
                _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<EnhancedOfflineSettlementResultDto>>(_jsonOptions);
                
                if (result?.Success == true && result.Data != null)
                {
                    _logger.LogInformation("增强离线结算成功：玩家 {PlayerId}，获得经验 {Experience}，金币 {Gold}，处理时间 {ProcessTime}ms",
                        playerId, result.Data.TotalExperience, result.Data.TotalGold, result.Data.ProcessingTimeMs);
                }

                return result ?? new ApiResponse<EnhancedOfflineSettlementResultDto>
                {
                    Success = false,
                    Message = "响应数据为空"
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("增强离线结算请求失败：{StatusCode} - {Error}", response.StatusCode, errorContent);
                
                return new ApiResponse<EnhancedOfflineSettlementResultDto>
                {
                    Success = false,
                    Message = $"请求失败: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "增强离线结算异常：玩家 {PlayerId}", playerId);
            return new ApiResponse<EnhancedOfflineSettlementResultDto>
            {
                Success = false,
                Message = $"请求异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 智能批量离线结算
    /// </summary>
    public async Task<BatchOperationResponseDto<EnhancedOfflineSettlementResultDto>> ProcessSmartBatchOfflineSettlementAsync(
        List<string> playerIds,
        int maxConcurrency = 10,
        bool useEventDriven = true,
        bool enableTimeDecay = true)
    {
        try
        {
            var request = new SmartBatchOfflineSettlementRequestDto
            {
                PlayerIds = playerIds,
                MaxConcurrency = maxConcurrency,
                UseEventDriven = useEventDriven,
                EnableTimeDecay = enableTimeDecay,
                PrioritizeByActivity = true,
                BatchDelayMs = 100
            };

            _logger.LogInformation("发送智能批量离线结算请求：{PlayerCount} 个玩家，并发度 {Concurrency}",
                playerIds.Count, maxConcurrency);

            var response = await _httpClient.PostAsJsonAsync(
                "api/offlineSettlement/enhanced/batch", 
                request, 
                _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BatchOperationResponseDto<EnhancedOfflineSettlementResultDto>>(_jsonOptions);
                
                if (result != null)
                {
                    _logger.LogInformation("智能批量离线结算完成：成功 {SuccessCount}，失败 {ErrorCount}",
                        result.SuccessCount, result.ErrorCount);
                }

                return result ?? new BatchOperationResponseDto<EnhancedOfflineSettlementResultDto>
                {
                    TotalProcessed = playerIds.Count,
                    SuccessCount = 0,
                    ErrorCount = playerIds.Count,
                    Errors = new List<string> { "响应数据为空" }
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("智能批量离线结算请求失败：{StatusCode} - {Error}", response.StatusCode, errorContent);
                
                return new BatchOperationResponseDto<EnhancedOfflineSettlementResultDto>
                {
                    TotalProcessed = playerIds.Count,
                    SuccessCount = 0,
                    ErrorCount = playerIds.Count,
                    Errors = new List<string> { $"请求失败: {response.StatusCode}" }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "智能批量离线结算异常");
            return new BatchOperationResponseDto<EnhancedOfflineSettlementResultDto>
            {
                TotalProcessed = playerIds.Count,
                SuccessCount = 0,
                ErrorCount = playerIds.Count,
                Errors = new List<string> { $"请求异常: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// 预估离线收益
    /// </summary>
    public async Task<ApiResponse<OfflineRevenueEstimateDto>> EstimateOfflineRevenueAsync(
        string playerId, 
        double? customHours = null)
    {
        try
        {
            var url = $"api/offlineSettlement/estimate/{playerId}";
            if (customHours.HasValue)
            {
                url += $"?customHours={customHours.Value}";
            }

            _logger.LogInformation("请求离线收益预估：玩家 {PlayerId}，自定义时间 {CustomHours} 小时",
                playerId, customHours);

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<OfflineRevenueEstimateDto>>(_jsonOptions);
                
                if (result?.Success == true && result.Data != null)
                {
                    _logger.LogInformation("离线收益预估成功：玩家 {PlayerId}，预估经验 {Experience}，预估金币 {Gold}",
                        playerId, result.Data.EstimatedExperience, result.Data.EstimatedGold);
                }

                return result ?? new ApiResponse<OfflineRevenueEstimateDto>
                {
                    Success = false,
                    Message = "响应数据为空"
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("离线收益预估请求失败：{StatusCode} - {Error}", response.StatusCode, errorContent);
                
                return new ApiResponse<OfflineRevenueEstimateDto>
                {
                    Success = false,
                    Message = $"请求失败: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "离线收益预估异常：玩家 {PlayerId}", playerId);
            return new ApiResponse<OfflineRevenueEstimateDto>
            {
                Success = false,
                Message = $"请求异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 获取离线活动进度
    /// </summary>
    public async Task<ApiResponse<OfflineActivityProgressDto>> GetOfflineActivityProgressAsync(string playerId)
    {
        try
        {
            _logger.LogInformation("获取离线活动进度：玩家 {PlayerId}", playerId);

            var response = await _httpClient.GetAsync($"api/offlineSettlement/progress/{playerId}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<OfflineActivityProgressDto>>(_jsonOptions);
                
                if (result?.Success == true && result.Data != null)
                {
                    _logger.LogInformation("获取离线活动进度成功：玩家 {PlayerId}，活动类型 {ActivityType}，进度 {Progress:P2}",
                        playerId, result.Data.ActivityType, result.Data.ProgressPercentage);
                }

                return result ?? new ApiResponse<OfflineActivityProgressDto>
                {
                    Success = false,
                    Message = "响应数据为空"
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("获取离线活动进度失败：{StatusCode} - {Error}", response.StatusCode, errorContent);
                
                return new ApiResponse<OfflineActivityProgressDto>
                {
                    Success = false,
                    Message = $"请求失败: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取离线活动进度异常：玩家 {PlayerId}", playerId);
            return new ApiResponse<OfflineActivityProgressDto>
            {
                Success = false,
                Message = $"请求异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 获取离线结算性能指标
    /// </summary>
    public async Task<ApiResponse<OfflineSettlementMetricsDto>> GetOfflineSettlementMetricsAsync()
    {
        try
        {
            _logger.LogInformation("获取离线结算性能指标");

            var response = await _httpClient.GetAsync("api/offlineSettlement/metrics");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<OfflineSettlementMetricsDto>>(_jsonOptions);
                
                if (result?.Success == true && result.Data != null)
                {
                    _logger.LogInformation("获取性能指标成功：平均处理时间 {ProcessTime}ms，成功率 {SuccessRate:P2}，总处理数 {TotalProcessed}",
                        result.Data.AverageProcessingTimeMs, result.Data.SuccessRate, result.Data.TotalPlayersProcessed);
                }

                return result ?? new ApiResponse<OfflineSettlementMetricsDto>
                {
                    Success = false,
                    Message = "响应数据为空"
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("获取离线结算性能指标失败：{StatusCode} - {Error}", response.StatusCode, errorContent);
                
                return new ApiResponse<OfflineSettlementMetricsDto>
                {
                    Success = false,
                    Message = $"请求失败: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取离线结算性能指标异常");
            return new ApiResponse<OfflineSettlementMetricsDto>
            {
                Success = false,
                Message = $"请求异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 传统离线结算（向后兼容）
    /// </summary>
    public async Task<ApiResponse<OfflineSettlementResultDto>> ProcessLegacyOfflineSettlementAsync(string playerId)
    {
        try
        {
            _logger.LogInformation("发送传统离线结算请求：玩家 {PlayerId}", playerId);

            var response = await _httpClient.PostAsync($"api/offlineSettlement/player/{playerId}", null);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<OfflineSettlementResultDto>>(_jsonOptions);
                
                if (result?.Success == true && result.Data != null)
                {
                    _logger.LogInformation("传统离线结算成功：玩家 {PlayerId}，获得经验 {Experience}，金币 {Gold}",
                        playerId, result.Data.TotalExperience, result.Data.TotalGold);
                }

                return result ?? new ApiResponse<OfflineSettlementResultDto>
                {
                    Success = false,
                    Message = "响应数据为空"
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("传统离线结算请求失败：{StatusCode} - {Error}", response.StatusCode, errorContent);
                
                return new ApiResponse<OfflineSettlementResultDto>
                {
                    Success = false,
                    Message = $"请求失败: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "传统离线结算异常：玩家 {PlayerId}", playerId);
            return new ApiResponse<OfflineSettlementResultDto>
            {
                Success = false,
                Message = $"请求异常: {ex.Message}"
            };
        }
    }
}