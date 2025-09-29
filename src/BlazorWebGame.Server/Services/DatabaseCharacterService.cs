using Microsoft.EntityFrameworkCore;
using BlazorWebGame.Server.Data;
using BlazorWebGame.Shared.Models;
using BlazorWebGame.Shared.DTOs;
using System.Text.Json;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 数据库驱动的角色服务 - 提供完整的用户-角色关联功能
/// </summary>
public interface IDatabaseCharacterService
{
    Task<List<CharacterDto>> GetCharactersByUserIdAsync(string userId);
    Task<CharacterDto?> GetCharacterByIdAsync(string characterId);
    Task<CharacterDetailsDto?> GetCharacterDetailsAsync(string characterId);
    Task<CharacterDto> CreateCharacterAsync(string userId, CreateCharacterRequest request);
    Task<bool> UpdateCharacterAsync(string characterId, CharacterUpdateDto updates);
    Task<bool> DeleteCharacterAsync(string characterId);
    Task<bool> IsCharacterOwnedByUserAsync(string characterId, string userId);
    Task<List<CharacterDto>> GetAllCharactersAsync();
}

/// <summary>
/// 数据库角色服务实现
/// </summary>
public class DatabaseCharacterService : IDatabaseCharacterService
{
    private readonly ConsolidatedGameDbContext _context;
    private readonly ILogger<DatabaseCharacterService> _logger;

    public DatabaseCharacterService(
        ConsolidatedGameDbContext context,
        ILogger<DatabaseCharacterService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户的所有角色
    /// </summary>
    public async Task<List<CharacterDto>> GetCharactersByUserIdAsync(string userId)
    {
        try
        {
            var players = await _context.Players
                .Where(p => p.UserId == userId && p.IsOnline)
                .Select(p => new CharacterDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Health = p.Health,
                    MaxHealth = p.MaxHealth,
                    Gold = p.Gold,
                    IsDead = p.Health <= 0,
                    RevivalTimeRemaining = 0,
                    CurrentAction = p.CurrentAction,
                    SelectedBattleProfession = p.SelectedBattleProfession,
                    LastUpdated = p.UpdatedAt
                })
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} characters for user {UserId}", players.Count, SafeLogId(userId));
            return players;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get characters for user {UserId}", SafeLogId(userId));
            return new List<CharacterDto>();
        }
    }

    /// <summary>
    /// 根据ID获取角色基本信息
    /// </summary>
    public async Task<CharacterDto?> GetCharacterByIdAsync(string characterId)
    {
        try
        {
            var player = await _context.Players
                .Where(p => p.Id == characterId)
                .Select(p => new CharacterDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Health = p.Health,
                    MaxHealth = p.MaxHealth,
                    Gold = p.Gold,
                    IsDead = p.Health <= 0,
                    RevivalTimeRemaining = 0,
                    CurrentAction = p.CurrentAction,
                    SelectedBattleProfession = p.SelectedBattleProfession,
                    LastUpdated = p.UpdatedAt
                })
                .FirstOrDefaultAsync();

            return player;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character {CharacterId}", SafeLogId(characterId));
            return null;
        }
    }

    /// <summary>
    /// 获取角色详细信息
    /// </summary>
    public async Task<CharacterDetailsDto?> GetCharacterDetailsAsync(string characterId)
    {
        try
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == characterId);

            if (player == null)
                return null;

            // 解析JSON数据
            var attributes = DeserializeJson<Dictionary<string, int>>(player.AttributesJson);
            var inventory = DeserializeJson<List<object>>(player.InventoryJson);
            var skills = DeserializeJson<List<string>>(player.SkillsJson);
            var equipment = DeserializeJson<Dictionary<string, object>>(player.EquipmentJson);

            var details = new CharacterDetailsDto
            {
                Id = player.Id,
                Name = player.Name,
                Health = player.Health,
                MaxHealth = player.MaxHealth,
                Gold = player.Gold,
                IsDead = player.Health <= 0,
                RevivalTimeRemaining = 0,
                CurrentAction = player.CurrentAction,
                SelectedBattleProfession = player.SelectedBattleProfession,
                LastUpdated = player.UpdatedAt,
                // 使用现有的DTO属性结构
                EquippedSkills = new Dictionary<string, List<string>>
                {
                    [player.SelectedBattleProfession] = skills ?? new List<string>()
                },
                EquippedItems = equipment?.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => kvp.Value?.ToString() ?? string.Empty
                ) ?? new Dictionary<string, string>()
            };

            return details;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character details for {CharacterId}", SafeLogId(characterId));
            return null;
        }
    }

    /// <summary>
    /// 创建新角色并关联到用户
    /// </summary>
    public async Task<CharacterDto> CreateCharacterAsync(string userId, CreateCharacterRequest request)
    {
        try
        {
            // 验证用户存在
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId && u.IsActive);
            if (!userExists)
            {
                throw new ArgumentException($"User {userId} not found or inactive");
            }

            // 检查角色名称是否已存在
            var nameExists = await _context.Players.AnyAsync(p => p.Name == request.Name);
            if (nameExists)
            {
                throw new ArgumentException($"Character name '{request.Name}' is already taken");
            }

            var characterId = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow;

            var player = new PlayerEntity
            {
                Id = characterId,
                UserId = userId,
                Name = request.Name,
                Level = 1,
                Experience = 0,
                Health = 100,
                MaxHealth = 100,
                Gold = 1000,
                SelectedBattleProfession = "Warrior",
                CurrentAction = "Idle",
                IsOnline = true,
                LastActiveAt = now,
                CreatedAt = now,
                UpdatedAt = now,
                // 初始化JSON数据
                AttributesJson = JsonSerializer.Serialize(new Dictionary<string, int>
                {
                    ["Strength"] = 10,
                    ["Agility"] = 10,
                    ["Intellect"] = 10,
                    ["Spirit"] = 10,
                    ["Stamina"] = 10
                }),
                InventoryJson = JsonSerializer.Serialize(new List<object>()),
                SkillsJson = JsonSerializer.Serialize(new List<string>()),
                EquipmentJson = JsonSerializer.Serialize(new Dictionary<string, object>())
            };

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created character {CharacterName} (ID: {CharacterId}) for user {UserId}", 
                request.Name, SafeLogId(characterId), SafeLogId(userId));

            return new CharacterDto
            {
                Id = player.Id,
                Name = player.Name,
                Health = player.Health,
                MaxHealth = player.MaxHealth,
                Gold = player.Gold,
                IsDead = false,
                RevivalTimeRemaining = 0,
                CurrentAction = player.CurrentAction,
                SelectedBattleProfession = player.SelectedBattleProfession,
                LastUpdated = player.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create character {CharacterName} for user {UserId}", 
                request.Name, SafeLogId(userId));
            throw;
        }
    }

    /// <summary>
    /// 更新角色信息
    /// </summary>
    public async Task<bool> UpdateCharacterAsync(string characterId, CharacterUpdateDto updates)
    {
        try
        {
            var player = await _context.Players.FindAsync(characterId);
            if (player == null)
                return false;

            // 更新基本属性
            if (updates.Health.HasValue)
                player.Health = updates.Health.Value;
            if (updates.MaxHealth.HasValue)
                player.MaxHealth = updates.MaxHealth.Value;
            if (updates.Gold.HasValue)
                player.Gold = updates.Gold.Value;
            if (!string.IsNullOrEmpty(updates.CurrentAction))
                player.CurrentAction = updates.CurrentAction;
            if (!string.IsNullOrEmpty(updates.SelectedBattleProfession))
                player.SelectedBattleProfession = updates.SelectedBattleProfession;

            // 更新JSON数据
            if (!string.IsNullOrEmpty(updates.AttributesJson))
                player.AttributesJson = updates.AttributesJson;
            if (!string.IsNullOrEmpty(updates.InventoryJson))
                player.InventoryJson = updates.InventoryJson;
            if (!string.IsNullOrEmpty(updates.SkillsJson))
                player.SkillsJson = updates.SkillsJson;
            if (!string.IsNullOrEmpty(updates.EquipmentJson))
                player.EquipmentJson = updates.EquipmentJson;

            player.UpdatedAt = DateTime.UtcNow;
            player.LastActiveAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Updated character {CharacterId}", SafeLogId(characterId));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update character {CharacterId}", SafeLogId(characterId));
            return false;
        }
    }

    /// <summary>
    /// 删除角色（软删除 - 设为离线状态）
    /// </summary>
    public async Task<bool> DeleteCharacterAsync(string characterId)
    {
        try
        {
            var player = await _context.Players.FindAsync(characterId);
            if (player == null)
                return false;

            player.IsOnline = false;
            player.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Soft deleted character {CharacterId}", SafeLogId(characterId));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete character {CharacterId}", SafeLogId(characterId));
            return false;
        }
    }

    /// <summary>
    /// 验证角色是否属于指定用户
    /// </summary>
    public async Task<bool> IsCharacterOwnedByUserAsync(string characterId, string userId)
    {
        try
        {
            return await _context.Players
                .AnyAsync(p => p.Id == characterId && p.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check character ownership for {CharacterId} and user {UserId}", 
                SafeLogId(characterId), SafeLogId(userId));
            return false;
        }
    }

    /// <summary>
    /// 获取所有角色（管理员功能）
    /// </summary>
    public async Task<List<CharacterDto>> GetAllCharactersAsync()
    {
        try
        {
            var players = await _context.Players
                .Where(p => p.IsOnline)
                .Select(p => new CharacterDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Health = p.Health,
                    MaxHealth = p.MaxHealth,
                    Gold = p.Gold,
                    IsDead = p.Health <= 0,
                    RevivalTimeRemaining = 0,
                    CurrentAction = p.CurrentAction,
                    SelectedBattleProfession = p.SelectedBattleProfession,
                    LastUpdated = p.UpdatedAt
                })
                .ToListAsync();

            return players;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all characters");
            return new List<CharacterDto>();
        }
    }

    /// <summary>
    /// 安全地记录ID用于日志
    /// </summary>
    private static string SafeLogId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "[empty]";
        
        var sanitized = new string(id.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return sanitized.Length > 8 ? sanitized.Substring(0, 8) + "..." : sanitized;
    }

    /// <summary>
    /// 安全地反序列化JSON数据
    /// </summary>
    private T? DeserializeJson<T>(string json) where T : class
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize JSON: {Json}", json.Length > 100 ? json.Substring(0, 100) + "..." : json);
            return null;
        }
    }
}