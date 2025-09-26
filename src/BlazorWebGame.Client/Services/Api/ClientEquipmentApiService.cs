using BlazorWebGame.Shared.DTOs;
using System.Text.Json;
using System.Text;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 客户端装备API服务 - 与服务端装备系统通信
/// </summary>
public class ClientEquipmentApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClientEquipmentApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ClientEquipmentApiService(HttpClient httpClient, ILogger<ClientEquipmentApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// 生成装备
    /// </summary>
    public async Task<ApiResponse<EquipmentDto>> GenerateEquipmentAsync(EquipmentGenerationRequest request)
    {
        try
        {
            _logger.LogDebug("请求生成装备: {Name}", request.Name);

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/Equipment/generate", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<EquipmentDto>>(responseContent, _jsonOptions);
                _logger.LogDebug("装备生成成功: {EquipmentName}", result?.Data?.Name);
                return result ?? new ApiResponse<EquipmentDto> { Success = false, Message = "反序列化失败" };
            }
            else
            {
                _logger.LogError("装备生成失败: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new ApiResponse<EquipmentDto>
                {
                    Success = false,
                    Message = $"服务器错误: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成装备时发生异常: {RequestName}", request.Name);
            return new ApiResponse<EquipmentDto>
            {
                Success = false,
                Message = "网络连接错误"
            };
        }
    }

    /// <summary>
    /// 批量生成装备
    /// </summary>
    public async Task<ApiResponse<List<EquipmentDto>>> GenerateBatchEquipmentAsync(List<EquipmentGenerationRequest> requests)
    {
        try
        {
            _logger.LogDebug("请求批量生成装备，数量: {Count}", requests.Count);

            var json = JsonSerializer.Serialize(requests, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/Equipment/generate-batch", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<List<EquipmentDto>>>(responseContent, _jsonOptions);
                _logger.LogDebug("批量装备生成成功，数量: {Count}", result?.Data?.Count ?? 0);
                return result ?? new ApiResponse<List<EquipmentDto>> { Success = false, Message = "反序列化失败" };
            }
            else
            {
                _logger.LogError("批量装备生成失败: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new ApiResponse<List<EquipmentDto>>
                {
                    Success = false,
                    Message = $"服务器错误: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量生成装备时发生异常");
            return new ApiResponse<List<EquipmentDto>>
            {
                Success = false,
                Message = "网络连接错误"
            };
        }
    }

    /// <summary>
    /// 获取装备示例
    /// </summary>
    public async Task<ApiResponse<List<EquipmentDto>>> GetEquipmentExamplesAsync()
    {
        try
        {
            _logger.LogDebug("请求获取装备示例");

            var response = await _httpClient.GetAsync("api/Equipment/examples");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<List<EquipmentDto>>>(responseContent, _jsonOptions);
                _logger.LogDebug("获取装备示例成功，数量: {Count}", result?.Data?.Count ?? 0);
                return result ?? new ApiResponse<List<EquipmentDto>> { Success = false, Message = "反序列化失败" };
            }
            else
            {
                _logger.LogError("获取装备示例失败: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new ApiResponse<List<EquipmentDto>>
                {
                    Success = false,
                    Message = $"服务器错误: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取装备示例时发生异常");
            return new ApiResponse<List<EquipmentDto>>
            {
                Success = false,
                Message = "网络连接错误"
            };
        }
    }

    /// <summary>
    /// 计算装备价值
    /// </summary>
    public async Task<ApiResponse<int>> CalculateEquipmentValueAsync(EquipmentDto equipment)
    {
        try
        {
            _logger.LogDebug("请求计算装备价值: {EquipmentName}", equipment.Name);

            var json = JsonSerializer.Serialize(equipment, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/Equipment/calculate-value", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<int>>(responseContent, _jsonOptions);
                _logger.LogDebug("装备价值计算成功: {Value}", result?.Data ?? 0);
                return result ?? new ApiResponse<int> { Success = false, Message = "反序列化失败" };
            }
            else
            {
                _logger.LogError("计算装备价值失败: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new ApiResponse<int>
                {
                    Success = false,
                    Message = $"服务器错误: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算装备价值时发生异常: {EquipmentName}", equipment.Name);
            return new ApiResponse<int>
            {
                Success = false,
                Message = "网络连接错误"
            };
        }
    }

    /// <summary>
    /// 根据名称猜测武器类型
    /// </summary>
    public async Task<ApiResponse<string>> GuessWeaponTypeAsync(string name)
    {
        try
        {
            _logger.LogDebug("请求猜测武器类型: {WeaponName}", name);

            var response = await _httpClient.GetAsync($"api/Equipment/guess-weapon-type/{Uri.EscapeDataString(name)}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<string>>(responseContent, _jsonOptions);
                _logger.LogDebug("武器类型猜测成功: {WeaponType}", result?.Data);
                return result ?? new ApiResponse<string> { Success = false, Message = "反序列化失败" };
            }
            else
            {
                _logger.LogError("猜测武器类型失败: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"服务器错误: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "猜测武器类型时发生异常: {WeaponName}", name);
            return new ApiResponse<string>
            {
                Success = false,
                Message = "网络连接错误"
            };
        }
    }

    /// <summary>
    /// 根据名称猜测护甲类型
    /// </summary>
    public async Task<ApiResponse<string>> GuessArmorTypeAsync(string name)
    {
        try
        {
            _logger.LogDebug("请求猜测护甲类型: {ArmorName}", name);

            var response = await _httpClient.GetAsync($"api/Equipment/guess-armor-type/{Uri.EscapeDataString(name)}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<string>>(responseContent, _jsonOptions);
                _logger.LogDebug("护甲类型猜测成功: {ArmorType}", result?.Data);
                return result ?? new ApiResponse<string> { Success = false, Message = "反序列化失败" };
            }
            else
            {
                _logger.LogError("猜测护甲类型失败: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"服务器错误: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "猜测护甲类型时发生异常: {ArmorName}", name);
            return new ApiResponse<string>
            {
                Success = false,
                Message = "网络连接错误"
            };
        }
    }
}