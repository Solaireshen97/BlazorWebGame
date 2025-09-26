using BlazorWebGame.Shared.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlazorWebGame.Client.Services;

/// <summary>
/// 专门用于测试服务端API功能的服务
/// </summary>
public class ServerApiTestService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServerApiTestService> _logger;
    private string _baseUrl = "https://localhost:7000";

    public ServerApiTestService(HttpClient httpClient, ILogger<ServerApiTestService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public void SetBaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl;
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    // ====== 健康检查和系统信息 ======
    
    public async Task<string> TestHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health/simple");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return $"✅ 健康检查成功: {content}";
            }
            return $"❌ 健康检查失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ 健康检查异常: {ex.Message}";
        }
    }

    public async Task<string> TestApiInfoAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/info");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return $"✅ API信息获取成功: {content}";
            }
            return $"❌ API信息获取失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ API信息获取异常: {ex.Message}";
        }
    }

    // ====== 监控API测试 ======
    
    public async Task<string> TestSystemMetricsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/monitoring/system-metrics");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(content);
                var success = json.RootElement.GetProperty("success").GetBoolean();
                if (success)
                {
                    return $"✅ 系统指标获取成功: {content[..Math.Min(200, content.Length)]}...";
                }
                return $"❌ 系统指标获取失败: {content}";
            }
            return $"❌ 系统指标请求失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ 系统指标获取异常: {ex.Message}";
        }
    }

    public async Task<string> TestOperationMetricsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/monitoring/operation-metrics");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return $"✅ 操作指标获取成功: {content[..Math.Min(200, content.Length)]}...";
            }
            return $"❌ 操作指标请求失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ 操作指标获取异常: {ex.Message}";
        }
    }

    // ====== 角色API测试 ======
    
    public async Task<string> TestGetCharactersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/character");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<CharacterDto>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (apiResponse?.Success == true)
                {
                    return $"✅ 角色列表获取成功: 找到 {apiResponse.Data?.Count ?? 0} 个角色";
                }
                return $"❌ 角色列表获取失败: {apiResponse?.Message}";
            }
            return $"❌ 角色列表请求失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ 角色列表获取异常: {ex.Message}";
        }
    }

    public async Task<string> TestCreateCharacterAsync(string name)
    {
        try
        {
            var request = new CreateCharacterRequest { Name = name };
            var response = await _httpClient.PostAsJsonAsync("/api/character", request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<CharacterDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (apiResponse?.Success == true)
                {
                    return $"✅ 角色创建成功: {apiResponse.Data?.Name} (ID: {apiResponse.Data?.Id})";
                }
                return $"❌ 角色创建失败: {apiResponse?.Message}";
            }
            return $"❌ 角色创建请求失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ 角色创建异常: {ex.Message}";
        }
    }

    // ====== 战斗API测试 ======
    
    public async Task<string> TestStartBattleAsync(string characterId, string enemyId)
    {
        try
        {
            var request = new StartBattleRequest 
            { 
                CharacterId = characterId, 
                EnemyId = enemyId 
            };
            var response = await _httpClient.PostAsJsonAsync("/api/battle/start", request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<BattleStateDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (apiResponse?.Success == true)
                {
                    return $"✅ 战斗开始成功: 战斗ID {apiResponse.Data?.BattleId}";
                }
                return $"❌ 战斗开始失败: {apiResponse?.Message}";
            }
            return $"❌ 战斗开始请求失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ 战斗开始异常: {ex.Message}";
        }
    }

    public async Task<string> TestGetBattleStateAsync(string battleId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/battle/state/{battleId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<BattleStateDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (apiResponse?.Success == true)
                {
                    var battle = apiResponse.Data;
                    return $"✅ 战斗状态获取成功: {(battle?.IsActive == true ? "进行中" : "已结束")} - 玩家血量: {battle?.PlayerHealth}/{battle?.PlayerMaxHealth}";
                }
                return $"❌ 战斗状态获取失败: {apiResponse?.Message}";
            }
            return $"❌ 战斗状态请求失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ 战斗状态获取异常: {ex.Message}";
        }
    }

    // ====== 组队API测试 ======
    
    public async Task<string> TestCreatePartyAsync(string characterId)
    {
        try
        {
            var request = new CreatePartyRequest { CharacterId = characterId };
            var response = await _httpClient.PostAsJsonAsync("/api/party", request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<PartyDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (apiResponse?.Success == true)
                {
                    return $"✅ 组队创建成功: 队伍ID {apiResponse.Data?.Id}";
                }
                return $"❌ 组队创建失败: {apiResponse?.Message}";
            }
            return $"❌ 组队创建请求失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ 组队创建异常: {ex.Message}";
        }
    }

    // ====== 装备API测试 ======
    
    public async Task<string> TestGenerateEquipmentAsync()
    {
        try
        {
            var request = new EquipmentGenerationRequest
            {
                Name = "测试装备",
                Level = 1,
                Slot = "MainHand",
                Quality = EquipmentQuality.Common,
                WeaponType = "Sword"
            };
            var response = await _httpClient.PostAsJsonAsync("/api/equipment/generate", request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<EquipmentDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (apiResponse?.Success == true)
                {
                    return $"✅ 装备生成成功: {apiResponse.Data?.Name} (伤害: {apiResponse.Data?.WeaponDamage})";
                }
                return $"❌ 装备生成失败: {apiResponse?.Message}";
            }
            return $"❌ 装备生成请求失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ 装备生成异常: {ex.Message}";
        }
    }

    // ====== 物品和库存API测试 ======
    
    public async Task<string> TestGetInventoryAsync(string characterId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/inventory/{characterId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<InventoryDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (apiResponse?.Success == true)
                {
                    var inventory = apiResponse.Data;
                    var itemCount = inventory?.Slots?.Where(s => !s.IsEmpty).Count() ?? 0;
                    return $"✅ 库存获取成功: {itemCount} 个物品";
                }
                return $"❌ 库存获取失败: {apiResponse?.Message}";
            }
            return $"❌ 库存获取请求失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ 库存获取异常: {ex.Message}";
        }
    }

    // ====== 生产系统API测试 ======
    
    public async Task<string> TestGetGatheringNodesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/production/gathering-nodes");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<GatheringNodeDto>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (apiResponse?.Success == true)
                {
                    return $"✅ 采集节点获取成功: 找到 {apiResponse.Data?.Count ?? 0} 个节点";
                }
                return $"❌ 采集节点获取失败: {apiResponse?.Message}";
            }
            return $"❌ 采集节点请求失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ 采集节点获取异常: {ex.Message}";
        }
    }

    // ====== 任务系统API测试 ======
    
    public async Task<string> TestGetQuestsAsync(string characterId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/quest/{characterId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<CharacterQuestStatusDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (apiResponse?.Success == true)
                {
                    var quests = apiResponse.Data;
                    return $"✅ 任务状态获取成功: 活跃任务 {quests?.ActiveQuests?.Count ?? 0} 个, 可接受任务 {quests?.AvailableQuests?.Count ?? 0} 个";
                }
                return $"❌ 任务状态获取失败: {apiResponse?.Message}";
            }
            return $"❌ 任务状态请求失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ 任务状态获取异常: {ex.Message}";
        }
    }

    // ====== API文档测试 ======
    
    public async Task<string> TestApiOverviewAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/apidocumentation/overview");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(content);
                if (json.RootElement.GetProperty("success").GetBoolean())
                {
                    var data = json.RootElement.GetProperty("data");
                    var endpointCount = data.GetProperty("apiEndpoints").GetArrayLength();
                    var featureCount = data.GetProperty("featureStatus").EnumerateObject().Count();
                    return $"✅ API概述获取成功: {endpointCount} 个端点, {featureCount} 个功能模块";
                }
                return $"❌ API概述获取失败: {content}";
            }
            return $"❌ API概述请求失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ API概述获取异常: {ex.Message}";
        }
    }

    public async Task<string> TestServerInfoAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/apidocumentation/server-info");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(content);
                if (json.RootElement.GetProperty("success").GetBoolean())
                {
                    var data = json.RootElement.GetProperty("data");
                    var version = data.GetProperty("version").GetString();
                    var uptime = data.GetProperty("uptime").GetString();
                    return $"✅ 服务器信息获取成功: v{version}, 运行时间: {uptime}";
                }
                return $"❌ 服务器信息获取失败: {content}";
            }
            return $"❌ 服务器信息请求失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ 服务器信息获取异常: {ex.Message}";
        }
    }

    // ====== 数据存储API测试 ======
    
    public async Task<string> TestDataStorageAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/datastorage/stats");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return $"✅ 数据存储统计获取成功: {content[..Math.Min(100, content.Length)]}...";
            }
            return $"❌ 数据存储统计请求失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ 数据存储统计获取异常: {ex.Message}";
        }
    }

    // ====== 认证API测试 ======
    
    public async Task<string> TestDemoLoginAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/auth/demo-login", null);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);
                if (tokenResponse.TryGetProperty("token", out var tokenElement))
                {
                    var token = tokenElement.GetString();
                    // 设置认证头
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    return $"✅ 演示登录成功: 获得JWT令牌";
                }
                return $"❌ 演示登录失败: 未获得令牌";
            }
            return $"❌ 演示登录请求失败: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"❌ 演示登录异常: {ex.Message}";
        }
    }

    // ====== 批量测试 ======
    
    public async Task<List<string>> RunAllBasicTestsAsync()
    {
        var results = new List<string>();
        
        results.Add("=== 基础连接测试 ===");
        results.Add(await TestHealthAsync());
        results.Add(await TestApiInfoAsync());
        
        results.Add("\n=== 监控API测试 ===");
        results.Add(await TestSystemMetricsAsync());
        results.Add(await TestOperationMetricsAsync());
        
        results.Add("\n=== 认证测试 ===");
        results.Add(await TestDemoLoginAsync());
        
        results.Add("\n=== 角色API测试 ===");
        results.Add(await TestGetCharactersAsync());
        
        results.Add("\n=== 生产系统测试 ===");
        results.Add(await TestGetGatheringNodesAsync());
        
        results.Add("\n=== 装备系统测试 ===");
        results.Add(await TestGenerateEquipmentAsync());
        
        results.Add("\n=== API文档测试 ===");
        results.Add(await TestApiOverviewAsync());
        results.Add(await TestServerInfoAsync());
        
        results.Add("\n=== 数据存储测试 ===");
        results.Add(await TestDataStorageAsync());
        
        return results;
    }
}