using BlazorWebGame.Shared.DTOs;
using System.Net.Http.Json;

namespace BlazorWebGame.Client.Services.Api
{
    /// <summary>
    /// 客户端服务用于与服务端玩家服务API通信
    /// </summary>
    public class ServerPlayerApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ServerPlayerApiService> _logger;

        public ServerPlayerApiService(HttpClient httpClient, ILogger<ServerPlayerApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// 获取角色的总属性值
        /// </summary>
        public async Task<AttributeSetDto?> GetPlayerAttributesAsync(string characterId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<AttributeSetDto>>($"api/player/{characterId}/attributes");
                return response?.Success == true ? response.Data : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting player attributes for character {CharacterId}", characterId);
                return null;
            }
        }

        /// <summary>
        /// 获取角色的攻击力
        /// </summary>
        public async Task<int> GetPlayerAttackPowerAsync(string characterId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<int>>($"api/player/{characterId}/attack-power");
                return response?.Success == true ? response.Data : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attack power for character {CharacterId}", characterId);
                return 0;
            }
        }

        /// <summary>
        /// 获取角色的最大生命值
        /// </summary>
        public async Task<int> GetPlayerMaxHealthAsync(string characterId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<int>>($"api/player/{characterId}/max-health");
                return response?.Success == true ? response.Data : 100;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting max health for character {CharacterId}", characterId);
                return 100;
            }
        }

        /// <summary>
        /// 获取角色专业等级
        /// </summary>
        public async Task<int> GetProfessionLevelAsync(string characterId, string professionType, string profession)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<int>>($"api/player/{characterId}/profession/{professionType}/{profession}/level");
                return response?.Success == true ? response.Data : 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profession level for character {CharacterId}, profession {Profession}", characterId, profession);
                return 1;
            }
        }

        /// <summary>
        /// 获取角色专业进度
        /// </summary>
        public async Task<double> GetProfessionProgressAsync(string characterId, string professionType, string profession)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<double>>($"api/player/{characterId}/profession/{professionType}/{profession}/progress");
                return response?.Success == true ? response.Data : 0.0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profession progress for character {CharacterId}, profession {Profession}", characterId, profession);
                return 0.0;
            }
        }

        /// <summary>
        /// 获取角色的采集速度加成
        /// </summary>
        public async Task<double> GetGatheringSpeedBonusAsync(string characterId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<double>>($"api/player/{characterId}/gathering-speed-bonus");
                return response?.Success == true ? response.Data : 0.0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gathering speed bonus for character {CharacterId}", characterId);
                return 0.0;
            }
        }

        /// <summary>
        /// 获取角色的额外战利品概率
        /// </summary>
        public async Task<double> GetExtraLootChanceAsync(string characterId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<double>>($"api/player/{characterId}/extra-loot-chance");
                return response?.Success == true ? response.Data : 0.0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting extra loot chance for character {CharacterId}", characterId);
                return 0.0;
            }
        }

        /// <summary>
        /// 检查角色背包中是否有指定物品
        /// </summary>
        public async Task<bool> HasItemInInventoryAsync(string characterId, string itemId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<bool>>($"api/player/{characterId}/inventory/has-item/{itemId}");
                return response?.Success == true && response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking inventory for character {CharacterId}, item {ItemId}", characterId, itemId);
                return false;
            }
        }

        /// <summary>
        /// 获取角色背包中指定物品的数量
        /// </summary>
        public async Task<int> GetItemQuantityAsync(string characterId, string itemId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<int>>($"api/player/{characterId}/inventory/item-quantity/{itemId}");
                return response?.Success == true ? response.Data : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item quantity for character {CharacterId}, item {ItemId}", characterId, itemId);
                return 0;
            }
        }

        /// <summary>
        /// 检查角色是否满足等级要求
        /// </summary>
        public async Task<bool> MeetsLevelRequirementAsync(string characterId, string profession, int requiredLevel)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<bool>>($"api/player/{characterId}/meets-level-requirement/{profession}/{requiredLevel}");
                return response?.Success == true && response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking level requirement for character {CharacterId}, profession {Profession}, level {Level}", characterId, profession, requiredLevel);
                return false;
            }
        }

        /// <summary>
        /// 初始化角色数据一致性
        /// </summary>
        public async Task<bool> EnsureDataConsistencyAsync(string characterId)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/player/{characterId}/ensure-data-consistency", new { });
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                    return result?.Success == true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring data consistency for character {CharacterId}", characterId);
                return false;
            }
        }

        /// <summary>
        /// 重新初始化角色属性
        /// </summary>
        public async Task<bool> ReinitializeAttributesAsync(string characterId)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/player/{characterId}/reinitialize-attributes", new { });
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                    return result?.Success == true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reinitializing attributes for character {CharacterId}", characterId);
                return false;
            }
        }
    }

    /// <summary>
    /// API响应的通用包装器
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
}