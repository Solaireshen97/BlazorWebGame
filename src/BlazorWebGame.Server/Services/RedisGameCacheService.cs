using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// Redis 游戏缓存服务 - 提供游戏数据的分布式缓存
/// </summary>
public class RedisGameCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisGameCacheService> _logger;
    
    // 缓存键前缀
    private const string PLAYER_PREFIX = "player:";
    private const string CHARACTER_PREFIX = "character:";
    private const string BATTLE_PREFIX = "battle:";
    private const string PARTY_PREFIX = "party:";
    private const string INVENTORY_PREFIX = "inventory:";
    private const string EQUIPMENT_PREFIX = "equipment:";
    
    // 缓存过期时间
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _battleExpiration = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _playerExpiration = TimeSpan.FromMinutes(15);

    public RedisGameCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer redis,
        ILogger<RedisGameCacheService> logger)
    {
        _distributedCache = distributedCache;
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;
    }

    #region 玩家数据缓存

    /// <summary>
    /// 获取玩家缓存数据
    /// </summary>
    public async Task<PlayerStorageDto?> GetPlayerAsync(string playerId)
    {
        try
        {
            var key = PLAYER_PREFIX + playerId;
            var cached = await _distributedCache.GetStringAsync(key);
            
            if (cached != null)
            {
                var player = JsonSerializer.Deserialize<PlayerStorageDto>(cached);
                _logger.LogDebug("Player {PlayerId} loaded from cache", playerId);
                return player;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player {PlayerId} from cache", playerId);
            return null;
        }
    }

    /// <summary>
    /// 缓存玩家数据
    /// </summary>
    public async Task SetPlayerAsync(string playerId, PlayerStorageDto player)
    {
        try
        {
            var key = PLAYER_PREFIX + playerId;
            var serialized = JsonSerializer.Serialize(player);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _playerExpiration
            };
            
            await _distributedCache.SetStringAsync(key, serialized, options);
            _logger.LogDebug("Player {PlayerId} cached", playerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching player {PlayerId}", playerId);
        }
    }

    /// <summary>
    /// 删除玩家缓存
    /// </summary>
    public async Task RemovePlayerAsync(string playerId)
    {
        try
        {
            var key = PLAYER_PREFIX + playerId;
            await _distributedCache.RemoveAsync(key);
            _logger.LogDebug("Player {PlayerId} removed from cache", playerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing player {PlayerId} from cache", playerId);
        }
    }

    #endregion

    #region 角色数据缓存

    /// <summary>
    /// 获取角色缓存数据
    /// </summary>
    public async Task<CharacterDto?> GetCharacterAsync(string characterId)
    {
        try
        {
            var key = CHARACTER_PREFIX + characterId;
            var cached = await _distributedCache.GetStringAsync(key);
            
            if (cached != null)
            {
                var character = JsonSerializer.Deserialize<CharacterDto>(cached);
                _logger.LogDebug("Character {CharacterId} loaded from cache", characterId);
                return character;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character {CharacterId} from cache", characterId);
            return null;
        }
    }

    /// <summary>
    /// 缓存角色数据
    /// </summary>
    public async Task SetCharacterAsync(string characterId, CharacterDto character)
    {
        try
        {
            var key = CHARACTER_PREFIX + characterId;
            var serialized = JsonSerializer.Serialize(character);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _playerExpiration
            };
            
            await _distributedCache.SetStringAsync(key, serialized, options);
            _logger.LogDebug("Character {CharacterId} cached", characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching character {CharacterId}", characterId);
        }
    }

    /// <summary>
    /// 删除角色缓存
    /// </summary>
    public async Task RemoveCharacterAsync(string characterId)
    {
        try
        {
            var key = CHARACTER_PREFIX + characterId;
            await _distributedCache.RemoveAsync(key);
            _logger.LogDebug("Character {CharacterId} removed from cache", characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing character {CharacterId} from cache", characterId);
        }
    }

    #endregion

    #region 战斗状态缓存

    /// <summary>
    /// 获取战斗状态缓存
    /// </summary>
    public async Task<BattleStateDto?> GetBattleStateAsync(Guid battleId)
    {
        try
        {
            var key = BATTLE_PREFIX + battleId.ToString();
            var cached = await _distributedCache.GetStringAsync(key);
            
            if (cached != null)
            {
                var battleState = JsonSerializer.Deserialize<BattleStateDto>(cached);
                _logger.LogDebug("Battle {BattleId} loaded from cache", battleId);
                return battleState;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting battle {BattleId} from cache", battleId);
            return null;
        }
    }

    /// <summary>
    /// 缓存战斗状态
    /// </summary>
    public async Task SetBattleStateAsync(Guid battleId, BattleStateDto battleState)
    {
        try
        {
            var key = BATTLE_PREFIX + battleId.ToString();
            var serialized = JsonSerializer.Serialize(battleState);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _battleExpiration
            };
            
            await _distributedCache.SetStringAsync(key, serialized, options);
            _logger.LogDebug("Battle {BattleId} cached", battleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching battle {BattleId}", battleId);
        }
    }

    /// <summary>
    /// 删除战斗状态缓存
    /// </summary>
    public async Task RemoveBattleStateAsync(Guid battleId)
    {
        try
        {
            var key = BATTLE_PREFIX + battleId.ToString();
            await _distributedCache.RemoveAsync(key);
            _logger.LogDebug("Battle {BattleId} removed from cache", battleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing battle {BattleId} from cache", battleId);
        }
    }

    #endregion

    #region 队伍数据缓存

    /// <summary>
    /// 获取队伍缓存数据
    /// </summary>
    public async Task<PartyDto?> GetPartyAsync(string partyId)
    {
        try
        {
            var key = PARTY_PREFIX + partyId;
            var cached = await _distributedCache.GetStringAsync(key);
            
            if (cached != null)
            {
                var party = JsonSerializer.Deserialize<PartyDto>(cached);
                _logger.LogDebug("Party {PartyId} loaded from cache", partyId);
                return party;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting party {PartyId} from cache", partyId);
            return null;
        }
    }

    /// <summary>
    /// 缓存队伍数据
    /// </summary>
    public async Task SetPartyAsync(string partyId, PartyDto party)
    {
        try
        {
            var key = PARTY_PREFIX + partyId;
            var serialized = JsonSerializer.Serialize(party);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _defaultExpiration
            };
            
            await _distributedCache.SetStringAsync(key, serialized, options);
            _logger.LogDebug("Party {PartyId} cached", partyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching party {PartyId}", partyId);
        }
    }

    /// <summary>
    /// 删除队伍缓存
    /// </summary>
    public async Task RemovePartyAsync(string partyId)
    {
        try
        {
            var key = PARTY_PREFIX + partyId;
            await _distributedCache.RemoveAsync(key);
            _logger.LogDebug("Party {PartyId} removed from cache", partyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing party {PartyId} from cache", partyId);
        }
    }

    #endregion

    #region 背包数据缓存

    /// <summary>
    /// 获取背包缓存数据
    /// </summary>
    public async Task<InventoryDto?> GetInventoryAsync(string characterId)
    {
        try
        {
            var key = INVENTORY_PREFIX + characterId;
            var cached = await _distributedCache.GetStringAsync(key);
            
            if (cached != null)
            {
                var inventory = JsonSerializer.Deserialize<InventoryDto>(cached);
                _logger.LogDebug("Inventory for character {CharacterId} loaded from cache", characterId);
                return inventory;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory for character {CharacterId} from cache", characterId);
            return null;
        }
    }

    /// <summary>
    /// 缓存背包数据
    /// </summary>
    public async Task SetInventoryAsync(string characterId, InventoryDto inventory)
    {
        try
        {
            var key = INVENTORY_PREFIX + characterId;
            var serialized = JsonSerializer.Serialize(inventory);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _defaultExpiration
            };
            
            await _distributedCache.SetStringAsync(key, serialized, options);
            _logger.LogDebug("Inventory for character {CharacterId} cached", characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching inventory for character {CharacterId}", characterId);
        }
    }

    /// <summary>
    /// 删除背包缓存
    /// </summary>
    public async Task RemoveInventoryAsync(string characterId)
    {
        try
        {
            var key = INVENTORY_PREFIX + characterId;
            await _distributedCache.RemoveAsync(key);
            _logger.LogDebug("Inventory for character {CharacterId} removed from cache", characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing inventory for character {CharacterId} from cache", characterId);
        }
    }

    #endregion

    #region 装备数据缓存

    /// <summary>
    /// 获取装备缓存数据
    /// </summary>
    public async Task<Dictionary<string, object>?> GetEquipmentAsync(string characterId)
    {
        try
        {
            var key = EQUIPMENT_PREFIX + characterId;
            var cached = await _distributedCache.GetStringAsync(key);
            
            if (cached != null)
            {
                var equipment = JsonSerializer.Deserialize<Dictionary<string, object>>(cached);
                _logger.LogDebug("Equipment for character {CharacterId} loaded from cache", characterId);
                return equipment;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment for character {CharacterId} from cache", characterId);
            return null;
        }
    }

    /// <summary>
    /// 缓存装备数据
    /// </summary>
    public async Task SetEquipmentAsync(string characterId, Dictionary<string, object> equipment)
    {
        try
        {
            var key = EQUIPMENT_PREFIX + characterId;
            var serialized = JsonSerializer.Serialize(equipment);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _defaultExpiration
            };
            
            await _distributedCache.SetStringAsync(key, serialized, options);
            _logger.LogDebug("Equipment for character {CharacterId} cached", characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching equipment for character {CharacterId}", characterId);
        }
    }

    /// <summary>
    /// 删除装备缓存
    /// </summary>
    public async Task RemoveEquipmentAsync(string characterId)
    {
        try
        {
            var key = EQUIPMENT_PREFIX + characterId;
            await _distributedCache.RemoveAsync(key);
            _logger.LogDebug("Equipment for character {CharacterId} removed from cache", characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing equipment for character {CharacterId} from cache", characterId);
        }
    }

    #endregion

    #region 批量操作

    /// <summary>
    /// 批量删除缓存键
    /// </summary>
    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern + "*").ToArray();
            
            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
                _logger.LogDebug("Removed {Count} cache keys matching pattern: {Pattern}", keys.Length, pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
        }
    }

    /// <summary>
    /// 清理角色相关的所有缓存
    /// </summary>
    public async Task ClearCharacterCacheAsync(string characterId)
    {
        await Task.WhenAll(
            RemoveCharacterAsync(characterId),
            RemoveInventoryAsync(characterId),
            RemoveEquipmentAsync(characterId)
        );
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public async Task<CacheStatistics> GetCacheStatisticsAsync()
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var info = await server.InfoAsync("memory");
            
            var stats = new CacheStatistics
            {
                Timestamp = DateTime.UtcNow,
                TotalKeys = await _database.ExecuteAsync("DBSIZE"),
                MemoryUsage = ParseMemoryInfo(info)
            };

            // 按类型统计键数量
            stats.KeyCounts = new Dictionary<string, int>
            {
                ["players"] = await CountKeysByPatternAsync(PLAYER_PREFIX + "*"),
                ["characters"] = await CountKeysByPatternAsync(CHARACTER_PREFIX + "*"),
                ["battles"] = await CountKeysByPatternAsync(BATTLE_PREFIX + "*"),
                ["parties"] = await CountKeysByPatternAsync(PARTY_PREFIX + "*"),
                ["inventories"] = await CountKeysByPatternAsync(INVENTORY_PREFIX + "*"),
                ["equipment"] = await CountKeysByPatternAsync(EQUIPMENT_PREFIX + "*")
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return new CacheStatistics { Timestamp = DateTime.UtcNow };
        }
    }

    #endregion

    #region 辅助方法

    private async Task<int> CountKeysByPatternAsync(string pattern)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);
            return keys.Count();
        }
        catch
        {
            return 0;
        }
    }

    private long ParseMemoryInfo(IGrouping<string, HashEntry>[] info)
    {
        try
        {
            var memorySection = info.FirstOrDefault(g => g.Key == "memory");
            if (memorySection != null)
            {
                var usedMemory = memorySection.FirstOrDefault(h => h.Name == "used_memory");
                if (usedMemory.HasValue && long.TryParse(usedMemory.Value, out var bytes))
                {
                    return bytes;
                }
            }
        }
        catch
        {
            // 忽略解析错误
        }
        return 0;
    }

    #endregion
}

/// <summary>
/// 缓存统计信息
/// </summary>
public class CacheStatistics
{
    public DateTime Timestamp { get; set; }
    public long TotalKeys { get; set; }
    public long MemoryUsage { get; set; }
    public Dictionary<string, int> KeyCounts { get; set; } = new();
}