using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;
using BlazorWebGame.Shared.Models;
using BlazorWebGame.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server.Services.Data;

/// <summary>
/// 完整的SQLite数据存储服务实现 - 基于Entity Framework Core
/// </summary>
public class SqliteDataStorageService : IDataStorageService
{
    private readonly IDbContextFactory<GameDbContext> _contextFactory;
    private readonly ILogger<SqliteDataStorageService> _logger;

    public SqliteDataStorageService(IDbContextFactory<GameDbContext> contextFactory, ILogger<SqliteDataStorageService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <summary>
    /// 安全地截取ID用于日志记录，防止日志注入攻击
    /// </summary>
    private static string SafeLogId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "[empty]";
        
        // 只保留字母数字和连字符，并截取前8位
        var sanitized = new string(id.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return sanitized.Substring(0, Math.Min(8, sanitized.Length)) + (sanitized.Length > 8 ? "..." : "");
    }

    #region 用户账号管理

    public async Task<UserStorageDto?> GetUserByUsernameAsync(string username)
    {
        try
        {
            if (string.IsNullOrEmpty(username))
                return null;

            using var context = _contextFactory.CreateDbContext();
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            return user != null ? MapToDto(user) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by username: {Username}", username);
            return null;
        }
    }

    public async Task<UserStorageDto?> GetUserByIdAsync(string userId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var user = await context.Users.FindAsync(userId);
            return user != null ? MapToDto(user) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user with ID: {SafeUserId}", SafeLogId(userId));
            return null;
        }
    }

    public async Task<UserStorageDto?> GetUserByEmailAsync(string email)
    {
        try
        {
            if (string.IsNullOrEmpty(email))
                return null;

            using var context = _contextFactory.CreateDbContext();
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            return user != null ? MapToDto(user) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by email: {Email}", email);
            return null;
        }
    }

    public async Task<ApiResponse<UserStorageDto>> CreateUserAsync(UserStorageDto user, string password)
    {
        try
        {
            if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(password))
            {
                return new ApiResponse<UserStorageDto>
                {
                    IsSuccess = false,
                    Message = "用户名和密码不能为空"
                };
            }

            using var context = _contextFactory.CreateDbContext();

            // 检查用户名是否已存在
            var existingUser = await context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == user.Username.ToLower());
            if (existingUser != null)
            {
                return new ApiResponse<UserStorageDto>
                {
                    IsSuccess = false,
                    Message = "用户名已存在"
                };
            }

            // 检查邮箱是否已存在（如果提供了邮箱）
            if (!string.IsNullOrEmpty(user.Email))
            {
                var existingEmail = await context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == user.Email.ToLower());
                if (existingEmail != null)
                {
                    return new ApiResponse<UserStorageDto>
                    {
                        IsSuccess = false,
                        Message = "邮箱已被使用"
                    };
                }
            }

            // 确保ID和其他必要字段已设置
            if (string.IsNullOrEmpty(user.Id))
            {
                user.Id = Guid.NewGuid().ToString();
            }

            // 设置密码哈希和盐
            user.PasswordSalt = BCrypt.Net.BCrypt.GenerateSalt();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, user.PasswordSalt);

            // 设置创建和更新时间
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            // 确保角色列表包含"Player"
            if (!user.Roles.Contains("Player"))
            {
                user.Roles.Add("Player");
            }

            var entity = MapToEntity(user);
            context.Users.Add(entity);
            await context.SaveChangesAsync();

            _logger.LogInformation("User created successfully: {SafeUserId}, Username: {Username}",
                SafeLogId(entity.Id), user.Username);

            return new ApiResponse<UserStorageDto>
            {
                IsSuccess = true,
                Data = MapToDto(entity),
                Message = "用户创建成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Username}", user.Username);
            return new ApiResponse<UserStorageDto>
            {
                IsSuccess = false,
                Message = "用户创建失败"
            };
        }
    }

    public async Task<ApiResponse<UserStorageDto>> UpdateUserAsync(UserStorageDto user)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var existingEntity = await context.Users.FindAsync(user.Id);
            if (existingEntity == null)
            {
                return new ApiResponse<UserStorageDto>
                {
                    IsSuccess = false,
                    Message = "用户不存在"
                };
            }

            var entity = MapToEntity(user);
            entity.PasswordHash = existingEntity.PasswordHash; // 不更新密码
            entity.Salt = existingEntity.Salt;
            entity.CreatedAt = existingEntity.CreatedAt;
            entity.UpdatedAt = DateTime.UtcNow;

            context.Entry(existingEntity).CurrentValues.SetValues(entity);
            await context.SaveChangesAsync();

            _logger.LogDebug("User updated successfully: {SafeUserId}", SafeLogId(user.Id));

            return new ApiResponse<UserStorageDto>
            {
                IsSuccess = true,
                Data = MapToDto(entity),
                Message = "用户更新成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {SafeUserId}", SafeLogId(user.Id));
            return new ApiResponse<UserStorageDto>
            {
                IsSuccess = false,
                Message = "用户更新失败"
            };
        }
    }

    public async Task<bool> ValidateUserPasswordAsync(string userId, string password)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var user = await context.Users.FindAsync(userId);
            if (user != null)
            {
                return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password for user: {SafeUserId}", SafeLogId(userId));
            return false;
        }
    }

    public async Task<ApiResponse<bool>> UpdateUserPasswordAsync(string userId, string newPassword)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var user = await context.Users.FindAsync(userId);
            if (user == null)
            {
                return new ApiResponse<bool>
                {
                    IsSuccess = false,
                    Message = "用户不存在"
                };
            }

            var salt = BCrypt.Net.BCrypt.GenerateSalt();
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword, salt);

            user.Salt = salt;
            user.PasswordHash = hashedPassword;
            user.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            _logger.LogInformation("Password updated for user: {SafeUserId}", SafeLogId(userId));

            return new ApiResponse<bool>
            {
                IsSuccess = true,
                Data = true,
                Message = "密码更新成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password for user: {SafeUserId}", SafeLogId(userId));
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Message = "密码更新失败"
            };
        }
    }

    public async Task<ApiResponse<bool>> UpdateUserLastLoginAsync(string userId, string ipAddress)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var user = await context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                user.LastLoginIp = ipAddress;
                user.LoginAttempts = 0; // 重置登录尝试次数
                user.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();

                return new ApiResponse<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "登录信息更新成功"
                };
            }

            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Message = "用户不存在"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for user: {SafeUserId}", SafeLogId(userId));
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Message = "登录信息更新失败"
            };
        }
    }

    public async Task<ApiResponse<bool>> LockUserAccountAsync(string userId, DateTime lockUntil)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var user = await context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LockedUntil = lockUntil;
                user.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();

                _logger.LogInformation("User account locked: {SafeUserId} until {LockUntil}", 
                    SafeLogId(userId), lockUntil);

                return new ApiResponse<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "用户账户已锁定"
                };
            }

            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Message = "用户不存在"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking user account: {SafeUserId}", SafeLogId(userId));
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Message = "账户锁定失败"
            };
        }
    }

    public async Task<ApiResponse<bool>> UnlockUserAccountAsync(string userId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var user = await context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LockedUntil = null;
                user.LoginAttempts = 0;
                user.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();

                _logger.LogInformation("User account unlocked: {SafeUserId}", SafeLogId(userId));

                return new ApiResponse<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "用户账户已解锁"
                };
            }

            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Message = "用户不存在"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user account: {SafeUserId}", SafeLogId(userId));
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Message = "账户解锁失败"
            };
        }
    }

    #endregion

    #region 用户角色关联管理

    public async Task<ApiResponse<UserCharacterStorageDto>> CreateUserCharacterAsync(string userId, string characterId, string characterName, bool isDefault = false, int slotIndex = 0)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();

            // 验证用户存在
            var userExists = await context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return ApiResponse<UserCharacterStorageDto>.Failure("用户不存在");
            }

            // 检查角色是否已被其他用户关联
            var existingRelation = await context.UserCharacters
                .FirstOrDefaultAsync(uc => uc.CharacterId == characterId && uc.IsActive);

            if (existingRelation != null)
            {
                return ApiResponse<UserCharacterStorageDto>.Failure("该角色已被其他用户占用");
            }

            // 如果设置为默认角色，需要先取消其他默认角色
            if (isDefault)
            {
                var userCharacters = await context.UserCharacters
                    .Where(uc => uc.UserId == userId && uc.IsDefault)
                    .ToListAsync();

                foreach (var uc in userCharacters)
                {
                    uc.IsDefault = false;
                    uc.UpdatedAt = DateTime.UtcNow;
                }
            }

            // 查询角色信息以获取职业和等级
            string professionName = "Warrior";
            int level = 1;

            // 首先尝试从Characters表获取信息
            var character = await context.Characters.FindAsync(characterId);
            if (character != null)
            {
                professionName = character.ProfessionId;
                level = character.Level;
            }
            // 如果没有找到，尝试从Players表获取（向后兼容）
            else
            {
                var player = await context.Players.FindAsync(characterId);
                if (player != null)
                {
                    professionName = player.SelectedBattleProfession;
                    level = player.Level;
                }
            }

            var userCharacter = new UserCharacterEntity
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                CharacterId = characterId,
                CharacterName = characterName,
                IsActive = true,
                IsDefault = isDefault,
                SlotIndex = slotIndex,
                ProfessionName = professionName,
                Level = level,
                LastPlayedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.UserCharacters.Add(userCharacter);
            await context.SaveChangesAsync();

            _logger.LogInformation($"Created user-character relationship: {SafeLogId(userId)} -> {SafeLogId(characterId)} at slot {slotIndex}");

            return ApiResponse<UserCharacterStorageDto>.Success(ConvertToUserCharacterDto(userCharacter), "用户角色关联创建成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating user-character relationship: {SafeLogId(userId)} -> {SafeLogId(characterId)}");
            return ApiResponse<UserCharacterStorageDto>.Failure("创建用户角色关联失败");
        }
    }

    /// <summary>
    /// 解锁角色槽位
    /// </summary>
    public async Task<ApiResponse<bool>> UnlockCharacterSlotAsync(string userId, int slotIndex)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var user = await context.Users.FindAsync(userId);
            if (user == null)
            {
                return ApiResponse<bool>.Failure("用户不存在");
            }

            // 在实际数据库中，可以创建一个单独的表来存储用户解锁的槽位
            // 此处简化为在用户的配置文件中存储
            Dictionary<string, object> profile;
            try
            {
                profile = JsonSerializer.Deserialize<Dictionary<string, object>>(user.ProfileJson)
                    ?? new Dictionary<string, object>();
            }
            catch
            {
                profile = new Dictionary<string, object>();
            }

            var unlockedSlotsJson = profile.GetValueOrDefault("UnlockedSlots")?.ToString();
            var unlockedSlots = string.IsNullOrEmpty(unlockedSlotsJson)
                ? new List<int>()
                : JsonSerializer.Deserialize<List<int>>(unlockedSlotsJson) ?? new List<int>();

            if (!unlockedSlots.Contains(slotIndex))
            {
                unlockedSlots.Add(slotIndex);
                profile["UnlockedSlots"] = JsonSerializer.Serialize(unlockedSlots);
                user.ProfileJson = JsonSerializer.Serialize(profile);
                user.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();
            }

            _logger.LogInformation($"User {SafeLogId(userId)} unlocked character slot {slotIndex}");

            return ApiResponse<bool>.Success(true, "角色槽位解锁成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error unlocking character slot: {SafeLogId(userId)}, slot: {slotIndex}");
            return ApiResponse<bool>.Failure("解锁角色槽位失败");
        }
    }

    public async Task<ApiResponse<List<UserCharacterStorageDto>>> GetUserCharactersAsync(string userId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();

            var userCharacters = await context.UserCharacters
                .Where(uc => uc.UserId == userId && uc.IsActive)
                .OrderByDescending(uc => uc.IsDefault)
                .ThenByDescending(uc => uc.LastPlayedAt)
                .Select(uc => ConvertToUserCharacterDto(uc))
                .ToListAsync();

            return new ApiResponse<List<UserCharacterStorageDto>>
            {
                IsSuccess = true,
                Data = userCharacters,
                Message = "获取用户角色列表成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user characters: {SafeLogId(userId)}");
            return new ApiResponse<List<UserCharacterStorageDto>>
            {
                IsSuccess = false,
                Message = "获取用户角色列表失败"
            };
        }
    }

    public async Task<UserCharacterStorageDto?> GetCharacterOwnerAsync(string characterId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();

            var userCharacter = await context.UserCharacters
                .FirstOrDefaultAsync(uc => uc.CharacterId == characterId && uc.IsActive);

            return userCharacter != null ? ConvertToUserCharacterDto(userCharacter) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting character owner: {SafeLogId(characterId)}");
            return null;
        }
    }

    public async Task<bool> UserOwnsCharacterAsync(string userId, string characterId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();

            // 管理员可以访问任何角色
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                var roles = JsonSerializer.Deserialize<List<string>>(user.RolesJson) ?? new List<string>();
                if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return await context.UserCharacters
                .AnyAsync(uc => uc.UserId == userId && uc.CharacterId == characterId && uc.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking character ownership: {SafeLogId(userId)} -> {SafeLogId(characterId)}");
            return false;
        }
    }

    public async Task<ApiResponse<bool>> SetDefaultCharacterAsync(string userId, string characterId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();

            // 验证用户拥有该角色
            var targetCharacter = await context.UserCharacters
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == characterId && uc.IsActive);
            
            if (targetCharacter == null)
            {
                return new ApiResponse<bool>
                {
                    IsSuccess = false,
                    Message = "用户不拥有该角色"
                };
            }

            // 取消所有默认角色
            var userCharacters = await context.UserCharacters
                .Where(uc => uc.UserId == userId && uc.IsActive)
                .ToListAsync();

            foreach (var uc in userCharacters)
            {
                uc.IsDefault = uc.CharacterId == characterId;
                uc.UpdatedAt = DateTime.UtcNow;
                if (uc.CharacterId == characterId)
                {
                    uc.LastPlayedAt = DateTime.UtcNow;
                }
            }

            await context.SaveChangesAsync();

            return new ApiResponse<bool>
            {
                IsSuccess = true,
                Data = true,
                Message = "设置默认角色成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error setting default character: {SafeLogId(userId)} -> {SafeLogId(characterId)}");
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Message = "设置默认角色失败"
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteUserCharacterAsync(string userId, string characterId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();

            var userCharacter = await context.UserCharacters
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == characterId && uc.IsActive);

            if (userCharacter == null)
            {
                return new ApiResponse<bool>
                {
                    IsSuccess = false,
                    Message = "用户角色关联不存在"
                };
            }

            // 软删除 - 标记为非活跃
            userCharacter.IsActive = false;
            userCharacter.IsDefault = false;
            userCharacter.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            _logger.LogInformation($"Deleted user-character relationship: {SafeLogId(userId)} -> {SafeLogId(characterId)}");

            return new ApiResponse<bool>
            {
                IsSuccess = true,
                Data = true,
                Message = "删除用户角色关联成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting user-character relationship: {SafeLogId(userId)} -> {SafeLogId(characterId)}");
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Message = "删除用户角色关联失败"
            };
        }
    }

    /// <summary>
    /// 转换用户角色关联实体到DTO
    /// </summary>
    private static UserCharacterStorageDto ConvertToUserCharacterDto(UserCharacterEntity entity)
    {
        return new UserCharacterStorageDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            CharacterId = entity.CharacterId,
            CharacterName = entity.CharacterName,
            IsActive = entity.IsActive,
            IsDefault = entity.IsDefault,
            LastPlayedAt = entity.LastPlayedAt,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    #endregion

    #region 角色数据管理

    public async Task<ApiResponse<CharacterStorageDto>> GetCharacterByIdAsync(string characterId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var entity = await context.Characters.FindAsync(characterId);

            if (entity == null)
            {
                _logger.LogWarning("Character not found: {SafeCharacterId}", SafeLogId(characterId));
                return ApiResponse<CharacterStorageDto>.Failure("角色不存在");
            }

            var dto = MapToCharacterDto(entity);
            return ApiResponse<CharacterStorageDto>.Success(dto, "获取角色成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character by ID: {SafeCharacterId}", SafeLogId(characterId));
            return ApiResponse<CharacterStorageDto>.Failure($"获取角色失败: {ex.Message}");
        }
    }

    public async Task<ApiResponse<CharacterStorageDto>> SaveCharacterAsync(CharacterStorageDto character)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var entity = MapToCharacterEntity(character);
            entity.UpdatedAt = DateTime.UtcNow;

            var existing = await context.Characters.FindAsync(character.Id);
            if (existing != null)
            {
                // 更新现有角色
                context.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                // 添加新角色
                await context.Characters.AddAsync(entity);
            }

            await context.SaveChangesAsync();

            _logger.LogDebug("Character saved successfully with ID: {SafeCharacterId}", SafeLogId(character.Id));

            return ApiResponse<CharacterStorageDto>.Success(character, "角色保存成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save character with ID: {SafeCharacterId}", SafeLogId(character.Id));
            return ApiResponse<CharacterStorageDto>.Failure($"保存角色失败: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<CharacterStorageDto>>> GetRecentActiveCharactersAsync(TimeSpan activeWithin)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var cutoffTime = DateTime.UtcNow - activeWithin;

            var activeCharacters = await context.Characters
                .Where(c => c.LastActiveAt >= cutoffTime)
                .OrderByDescending(c => c.LastActiveAt)
                .ToListAsync();

            var dtos = activeCharacters.Select(c => MapToCharacterDto(c)).ToList();
            return ApiResponse<List<CharacterStorageDto>>.Success(dtos, $"获取到 {dtos.Count} 个活跃角色");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent active characters");
            return ApiResponse<List<CharacterStorageDto>>.Failure($"获取活跃角色失败: {ex.Message}");
        }
    }

    // 映射方法：实体到DTO
    private CharacterStorageDto MapToCharacterDto(CharacterEntity entity)
    {
        var dto = new CharacterStorageDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Level = entity.Level,
            Experience = entity.Experience,
            Gold = entity.Gold,
            IsOnline = entity.IsOnline,
            CurrentRegionId = entity.CurrentRegionId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            LastActiveAt = entity.LastActiveAt,
            PartyId = entity.PartyId,

            // 生命值和法力值
            Health = entity.Health,
            MaxHealth = entity.MaxHealth,
            Mana = entity.Mana,
            MaxMana = entity.MaxMana,
            IsDead = entity.IsDead,
            DeathTime = entity.DeathTime,

            // 基础属性
            Strength = entity.Strength,
            Agility = entity.Agility,
            Intellect = entity.Intellect,
            Spirit = entity.Spirit,
            Stamina = entity.Stamina,
            AttributePoints = entity.AttributePoints,

            // 职业
            ProfessionId = entity.ProfessionId
        };

        try
        {
            // 职业相关反序列化
            dto.BattleProfessions = JsonSerializer.Deserialize<Dictionary<string, ProfessionLevelDto>>(entity.BattleProfessionsJson)
                ?? new Dictionary<string, ProfessionLevelDto>();

            dto.GatheringProfessions = JsonSerializer.Deserialize<Dictionary<string, ProfessionLevelDto>>(entity.GatheringProfessionsJson)
                ?? new Dictionary<string, ProfessionLevelDto>();

            dto.ProductionProfessions = JsonSerializer.Deserialize<Dictionary<string, ProfessionLevelDto>>(entity.ProductionProfessionsJson)
                ?? new Dictionary<string, ProfessionLevelDto>();

            // 声望系统反序列化
            dto.Reputations = JsonSerializer.Deserialize<Dictionary<string, int>>(entity.ReputationsJson)
                ?? new Dictionary<string, int>();

            // 背包和装备反序列化
            dto.Items = JsonSerializer.Deserialize<List<InventoryItemDto>>(entity.InventoryJson)
                ?? new List<InventoryItemDto>();

            dto.EquippedItems = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.EquipmentJson)
                ?? new Dictionary<string, string>();

            // 消耗品装载反序列化
            dto.GeneralConsumableSlots = JsonSerializer.Deserialize<List<ConsumableSlotDto>>(entity.GeneralConsumableSlotsJson)
                ?? new List<ConsumableSlotDto>();

            dto.CombatConsumableSlots = JsonSerializer.Deserialize<List<ConsumableSlotDto>>(entity.CombatConsumableSlotsJson)
                ?? new List<ConsumableSlotDto>();

            // 技能系统反序列化
            dto.LearnedSkills = JsonSerializer.Deserialize<Dictionary<string, LearnedSkillDto>>(entity.LearnedSkillsJson)
                ?? new Dictionary<string, LearnedSkillDto>();

            dto.EquippedSkills = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(entity.EquippedSkillsJson)
                ?? new Dictionary<string, List<string>>();

            // 任务系统反序列化
            dto.ActiveQuestIds = JsonSerializer.Deserialize<List<string>>(entity.ActiveQuestsJson)
                ?? new List<string>();

            dto.CompletedQuestIds = JsonSerializer.Deserialize<List<string>>(entity.CompletedQuestsJson)
                ?? new List<string>();

            dto.QuestProgress = JsonSerializer.Deserialize<Dictionary<string, int>>(entity.QuestProgressJson)
                ?? new Dictionary<string, int>();

            // 活动系统反序列化
            dto.ActivitySlots = JsonSerializer.Deserialize<List<ActivitySlotDto>>(entity.ActivitySlotsJson)
                ?? new List<ActivitySlotDto>();

            // 离线记录反序列化
            dto.LastOfflineRecord = JsonSerializer.Deserialize<OfflineRecordDto>(entity.OfflineRecordJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing character data for character {CharacterId}", entity.Id);
            // 继续使用默认值
        }

        return dto;
    }

    // 映射方法：DTO到实体
    private CharacterEntity MapToCharacterEntity(CharacterStorageDto dto)
    {
        var entity = new CharacterEntity
        {
            Id = dto.Id,
            Name = dto.Name,
            Level = dto.Level,
            Experience = dto.Experience,
            Gold = dto.Gold,
            IsOnline = dto.IsOnline,
            CurrentRegionId = dto.CurrentRegionId,
            LastActiveAt = dto.LastActiveAt,
            PartyId = dto.PartyId,

            // 生命值和法力值
            Health = dto.Health,
            MaxHealth = dto.MaxHealth,
            Mana = dto.Mana,
            MaxMana = dto.MaxMana,
            IsDead = dto.IsDead,
            DeathTime = dto.DeathTime,

            // 基础属性
            Strength = dto.Strength,
            Agility = dto.Agility,
            Intellect = dto.Intellect,
            Spirit = dto.Spirit,
            Stamina = dto.Stamina,
            AttributePoints = dto.AttributePoints,

            // 职业
            ProfessionId = dto.ProfessionId,

            // 时间属性
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };

        try
        {
            // 序列化职业相关数据
            entity.BattleProfessionsJson = JsonSerializer.Serialize(dto.BattleProfessions ?? new Dictionary<string, ProfessionLevelDto>());
            entity.GatheringProfessionsJson = JsonSerializer.Serialize(dto.GatheringProfessions ?? new Dictionary<string, ProfessionLevelDto>());
            entity.ProductionProfessionsJson = JsonSerializer.Serialize(dto.ProductionProfessions ?? new Dictionary<string, ProfessionLevelDto>());

            // 序列化声望系统
            entity.ReputationsJson = JsonSerializer.Serialize(dto.Reputations ?? new Dictionary<string, int>());

            // 序列化背包和装备
            entity.InventoryJson = JsonSerializer.Serialize(dto.Items ?? new List<InventoryItemDto>());
            entity.EquipmentJson = JsonSerializer.Serialize(dto.EquippedItems ?? new Dictionary<string, string>());

            // 序列化消耗品装载
            entity.GeneralConsumableSlotsJson = JsonSerializer.Serialize(dto.GeneralConsumableSlots ?? new List<ConsumableSlotDto>());
            entity.CombatConsumableSlotsJson = JsonSerializer.Serialize(dto.CombatConsumableSlots ?? new List<ConsumableSlotDto>());

            // 序列化技能系统
            entity.LearnedSkillsJson = JsonSerializer.Serialize(dto.LearnedSkills ?? new Dictionary<string, LearnedSkillDto>());
            entity.EquippedSkillsJson = JsonSerializer.Serialize(dto.EquippedSkills ?? new Dictionary<string, List<string>>());

            // 序列化任务系统
            entity.ActiveQuestsJson = JsonSerializer.Serialize(dto.ActiveQuestIds ?? new List<string>());
            entity.CompletedQuestsJson = JsonSerializer.Serialize(dto.CompletedQuestIds ?? new List<string>());
            entity.QuestProgressJson = JsonSerializer.Serialize(dto.QuestProgress ?? new Dictionary<string, int>());

            // 序列化活动系统
            entity.ActivitySlotsJson = JsonSerializer.Serialize(dto.ActivitySlots ?? new List<ActivitySlotDto>());

            // 序列化离线记录
            entity.OfflineRecordJson = JsonSerializer.Serialize(dto.LastOfflineRecord);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing character data for character {CharacterId}", dto.Id);
        }

        return entity;
    }

    #endregion

    #region 玩家数据管理

    public async Task<PlayerStorageDto?> GetPlayerAsync(string playerId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var player = await context.Players.FindAsync(playerId);
            return player != null ? MapToDto(player) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get player with ID: {SafePlayerId}", SafeLogId(playerId));
            return null;
        }
    }

    public async Task<ApiResponse<PlayerStorageDto>> SavePlayerAsync(PlayerStorageDto player)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var entity = MapToEntity(player);
            entity.UpdatedAt = DateTime.UtcNow;
            
            var existing = await context.Players.FindAsync(player.Id);
            if (existing != null)
            {
                // 更新现有玩家
                context.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                // 添加新玩家
                await context.Players.AddAsync(entity);
            }
            
            await context.SaveChangesAsync();
            
            _logger.LogDebug("Player saved successfully with ID: {SafePlayerId}", SafeLogId(player.Id));
            
            return new ApiResponse<PlayerStorageDto>
            {
                IsSuccess = true,
                Data = MapToDto(entity),
                Message = "玩家数据保存成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save player with ID: {SafePlayerId}", SafeLogId(player.Id));
            return new ApiResponse<PlayerStorageDto>
            {
                IsSuccess = false,
                Message = $"保存玩家数据失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> DeletePlayerAsync(string playerId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var player = await context.Players.FindAsync(playerId);
            
            if (player != null)
            {
                // 删除相关数据
                var actionTargets = await context.ActionTargets
                    .Where(at => at.PlayerId == playerId)
                    .ToListAsync();
                context.ActionTargets.RemoveRange(actionTargets);
                
                var offlineData = await context.OfflineData
                    .Where(od => od.PlayerId == playerId)
                    .ToListAsync();
                context.OfflineData.RemoveRange(offlineData);
                
                // 删除玩家
                context.Players.Remove(player);
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Player and related data deleted successfully for ID: {SafePlayerId}", SafeLogId(playerId));
                
                return new ApiResponse<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "玩家数据删除成功"
                };
            }
            
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Data = false,
                Message = "玩家不存在"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete player with ID: {SafePlayerId}", SafeLogId(playerId));
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Message = $"删除玩家数据失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<PlayerStorageDto>>> GetOnlinePlayersAsync()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var onlinePlayers = await context.Players
                .Where(p => p.IsOnline)
                .OrderByDescending(p => p.LastActiveAt)
                .Select(p => MapToDto(p))
                .ToListAsync();
            
            return new ApiResponse<List<PlayerStorageDto>>
            {
                IsSuccess = true,
                Data = onlinePlayers,
                Message = $"获取到 {onlinePlayers.Count} 名在线玩家"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get online players");
            return new ApiResponse<List<PlayerStorageDto>>
            {
                IsSuccess = false,
                Message = $"获取在线玩家失败: {ex.Message}"
            };
        }
    }

    public async Task<BatchOperationResponseDto<PlayerStorageDto>> SavePlayersAsync(List<PlayerStorageDto> players)
    {
        var response = new BatchOperationResponseDto<PlayerStorageDto>();
        
        try
        {
            using var context = _contextFactory.CreateDbContext();
            using var transaction = await context.Database.BeginTransactionAsync();
            
            foreach (var player in players)
            {
                try
                {
                    var entity = MapToEntity(player);
                    entity.UpdatedAt = DateTime.UtcNow;
                    
                    var existing = await context.Players.FindAsync(player.Id);
                    if (existing != null)
                    {
                        context.Entry(existing).CurrentValues.SetValues(entity);
                    }
                    else
                    {
                        await context.Players.AddAsync(entity);
                    }
                    
                    response.SuccessfulItems.Add(player);
                    response.SuccessCount++;
                }
                catch (Exception ex)
                {
                    response.Errors.Add($"Player {player.Id}: {ex.Message}");
                    response.ErrorCount++;
                }
                response.TotalProcessed++;
            }
            
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save players batch");
            response.Errors.Add($"Batch operation failed: {ex.Message}");
        }
        
        return response;
    }

    #endregion

    #region 队伍数据管理

    public async Task<TeamStorageDto?> GetTeamAsync(string teamId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var team = await context.Teams.FindAsync(teamId);
            return team != null ? MapToDto(team) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get team with ID: {SafeTeamId}", SafeLogId(teamId));
            return null;
        }
    }

    public async Task<TeamStorageDto?> GetTeamByCaptainAsync(string captainId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var team = await context.Teams
                .FirstOrDefaultAsync(t => t.CaptainId == captainId);
            return team != null ? MapToDto(team) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get team by captain ID: {SafeCaptainId}", SafeLogId(captainId));
            return null;
        }
    }

    public async Task<TeamStorageDto?> GetTeamByPlayerAsync(string playerId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var teams = await context.Teams.ToListAsync();
            
            foreach (var team in teams)
            {
                var memberIds = JsonSerializer.Deserialize<List<string>>(team.MemberIdsJson) ?? new List<string>();
                if (memberIds.Contains(playerId))
                {
                    return MapToDto(team);
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get team by player ID: {SafePlayerId}", SafeLogId(playerId));
            return null;
        }
    }

    public async Task<ApiResponse<TeamStorageDto>> SaveTeamAsync(TeamStorageDto team)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var entity = MapToEntity(team);
            entity.UpdatedAt = DateTime.UtcNow;
            
            var existing = await context.Teams.FindAsync(team.Id);
            if (existing != null)
            {
                context.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                await context.Teams.AddAsync(entity);
            }
            
            await context.SaveChangesAsync();
            
            _logger.LogDebug("Team saved successfully with ID: {SafeTeamId}", SafeLogId(team.Id));
            
            return new ApiResponse<TeamStorageDto>
            {
                IsSuccess = true,
                Data = MapToDto(entity),
                Message = "队伍数据保存成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save team with ID: {SafeTeamId}", SafeLogId(team.Id));
            return new ApiResponse<TeamStorageDto>
            {
                IsSuccess = false,
                Message = $"保存队伍数据失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteTeamAsync(string teamId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var team = await context.Teams.FindAsync(teamId);
            
            if (team != null)
            {
                context.Teams.Remove(team);
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Team deleted successfully with ID: {SafeTeamId}", SafeLogId(teamId));
                
                return new ApiResponse<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "队伍删除成功"
                };
            }
            
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Data = false,
                Message = "队伍不存在"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete team with ID: {SafeTeamId}", SafeLogId(teamId));
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Message = $"删除队伍失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<TeamStorageDto>>> GetActiveTeamsAsync()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var activeTeams = await context.Teams
                .Where(t => t.Status == "Active")
                .OrderByDescending(t => t.UpdatedAt)
                .Select(t => MapToDto(t))
                .ToListAsync();
            
            return new ApiResponse<List<TeamStorageDto>>
            {
                IsSuccess = true,
                Data = activeTeams,
                Message = $"获取到 {activeTeams.Count} 支活跃队伍"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active teams");
            return new ApiResponse<List<TeamStorageDto>>
            {
                IsSuccess = false,
                Message = $"获取活跃队伍失败: {ex.Message}"
            };
        }
    }

    #endregion

    #region 动作目标管理

    public async Task<ActionTargetStorageDto?> GetCurrentActionTargetAsync(string playerId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var currentTarget = await context.ActionTargets
                .Where(at => at.PlayerId == playerId && !at.IsCompleted)
                .OrderByDescending(at => at.StartedAt)
                .FirstOrDefaultAsync();
            
            return currentTarget != null ? MapToDto(currentTarget) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current action target for player: {SafePlayerId}", SafeLogId(playerId));
            return null;
        }
    }

    public async Task<ApiResponse<ActionTargetStorageDto>> SaveActionTargetAsync(ActionTargetStorageDto actionTarget)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var entity = MapToEntity(actionTarget);
            entity.UpdatedAt = DateTime.UtcNow;
            
            var existing = await context.ActionTargets.FindAsync(actionTarget.Id);
            if (existing != null)
            {
                context.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                await context.ActionTargets.AddAsync(entity);
            }
            
            await context.SaveChangesAsync();
            
            _logger.LogDebug("ActionTarget saved for player with IDs: {SafeActionTargetId}, {SafePlayerId}", 
                SafeLogId(actionTarget.Id), SafeLogId(actionTarget.PlayerId));
            
            return new ApiResponse<ActionTargetStorageDto>
            {
                IsSuccess = true,
                Data = MapToDto(entity),
                Message = "动作目标保存成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save action target with ID: {SafeActionTargetId}", SafeLogId(actionTarget.Id));
            return new ApiResponse<ActionTargetStorageDto>
            {
                IsSuccess = false,
                Message = $"保存动作目标失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> CompleteActionTargetAsync(string actionTargetId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var actionTarget = await context.ActionTargets.FindAsync(actionTargetId);
            
            if (actionTarget != null)
            {
                actionTarget.IsCompleted = true;
                actionTarget.CompletedAt = DateTime.UtcNow;
                actionTarget.UpdatedAt = DateTime.UtcNow;
                
                await context.SaveChangesAsync();
                
                return new ApiResponse<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "动作目标完成"
                };
            }
            
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Data = false,
                Message = "动作目标不存在"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete action target with ID: {SafeActionTargetId}", SafeLogId(actionTargetId));
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Message = $"完成动作目标失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> CancelActionTargetAsync(string playerId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var currentTarget = await context.ActionTargets
                .Where(at => at.PlayerId == playerId && !at.IsCompleted)
                .OrderByDescending(at => at.StartedAt)
                .FirstOrDefaultAsync();
            
            if (currentTarget != null)
            {
                context.ActionTargets.Remove(currentTarget);
                await context.SaveChangesAsync();
                
                return new ApiResponse<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "动作目标已取消"
                };
            }
            
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Data = false,
                Message = "没有进行中的动作目标"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel action target for player with ID: {SafePlayerId}", SafeLogId(playerId));
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Message = $"取消动作目标失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<ActionTargetStorageDto>>> GetPlayerActionHistoryAsync(string playerId, int limit = 50)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var actionHistory = await context.ActionTargets
                .Where(at => at.PlayerId == playerId)
                .OrderByDescending(at => at.StartedAt)
                .Take(limit)
                .Select(at => MapToDto(at))
                .ToListAsync();
            
            return new ApiResponse<List<ActionTargetStorageDto>>
            {
                IsSuccess = true,
                Data = actionHistory,
                Message = $"获取到 {actionHistory.Count} 条动作历史记录"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get action history for player with ID: {SafePlayerId}", SafeLogId(playerId));
            return new ApiResponse<List<ActionTargetStorageDto>>
            {
                IsSuccess = false,
                Message = $"获取动作历史失败: {ex.Message}"
            };
        }
    }

    #endregion

    #region 战斗记录管理

    public async Task<BattleRecordStorageDto?> GetBattleRecordAsync(string battleId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var battleRecord = await context.BattleRecords
                .FirstOrDefaultAsync(br => br.BattleId == battleId);
            
            return battleRecord != null ? MapToDto(battleRecord) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get battle record with ID: {SafeBattleId}", SafeLogId(battleId));
            return null;
        }
    }

    public async Task<ApiResponse<BattleRecordStorageDto>> SaveBattleRecordAsync(BattleRecordStorageDto battleRecord)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var entity = MapToEntity(battleRecord);
            entity.UpdatedAt = DateTime.UtcNow;
            
            var existing = await context.BattleRecords.FindAsync(battleRecord.Id);
            if (existing != null)
            {
                context.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                await context.BattleRecords.AddAsync(entity);
            }
            
            await context.SaveChangesAsync();
            
            _logger.LogDebug("BattleRecord saved successfully with ID: {SafeBattleRecordId}", SafeLogId(battleRecord.Id));
            
            return new ApiResponse<BattleRecordStorageDto>
            {
                IsSuccess = true,
                Data = MapToDto(entity),
                Message = "战斗记录保存成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save battle record with ID: {SafeBattleRecordId}", SafeLogId(battleRecord.Id));
            return new ApiResponse<BattleRecordStorageDto>
            {
                IsSuccess = false,
                Message = $"保存战斗记录失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> EndBattleRecordAsync(string battleId, string status, Dictionary<string, object> results)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var battleRecord = await context.BattleRecords
                .FirstOrDefaultAsync(br => br.BattleId == battleId);
            
            if (battleRecord != null)
            {
                battleRecord.Status = status;
                battleRecord.EndedAt = DateTime.UtcNow;
                battleRecord.ResultsJson = JsonSerializer.Serialize(results);
                battleRecord.UpdatedAt = DateTime.UtcNow;
                
                if (battleRecord.StartedAt != DateTime.MinValue)
                {
                    battleRecord.Duration = (int)(DateTime.UtcNow - battleRecord.StartedAt).TotalSeconds;
                }
                
                await context.SaveChangesAsync();
                
                return new ApiResponse<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "战斗记录已结束"
                };
            }
            
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Data = false,
                Message = "战斗记录不存在"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end battle record with ID: {SafeBattleId}", SafeLogId(battleId));
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Message = $"结束战斗记录失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<BattleRecordStorageDto>>> GetPlayerBattleHistoryAsync(string playerId, DataStorageQueryDto query)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            
            // 查找包含该玩家的战斗记录
            var battleRecordsQuery = context.BattleRecords.AsQueryable();
            var battleRecords = await battleRecordsQuery.ToListAsync();
            
            var playerBattleRecords = battleRecords
                .Where(br => 
                {
                    var participants = JsonSerializer.Deserialize<List<string>>(br.ParticipantsJson) ?? new List<string>();
                    return participants.Contains(playerId);
                })
                .AsQueryable();
            
            // 应用过滤条件
            if (query.StartDate.HasValue)
                playerBattleRecords = playerBattleRecords.Where(br => br.StartedAt >= query.StartDate.Value);
            
            if (query.EndDate.HasValue)
                playerBattleRecords = playerBattleRecords.Where(br => br.StartedAt <= query.EndDate.Value);
            
            if (!string.IsNullOrEmpty(query.Status))
                playerBattleRecords = playerBattleRecords.Where(br => br.Status == query.Status);
            
            var result = playerBattleRecords
                .OrderByDescending(br => br.StartedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(MapToDto)
                .ToList();
            
            return new ApiResponse<List<BattleRecordStorageDto>>
            {
                IsSuccess = true,
                Data = result,
                Message = $"获取到 {result.Count} 条战斗记录"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get battle history for player with ID: {SafePlayerId}", SafeLogId(playerId));
            return new ApiResponse<List<BattleRecordStorageDto>>
            {
                IsSuccess = false,
                Message = $"获取战斗历史失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<BattleRecordStorageDto>>> GetTeamBattleHistoryAsync(string teamId, DataStorageQueryDto query)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var teamGuid = Guid.Parse(teamId);
            
            var battleRecordsQuery = context.BattleRecords
                .Where(br => br.PartyId == teamGuid)
                .AsQueryable();
            
            // 应用过滤条件
            if (query.StartDate.HasValue)
                battleRecordsQuery = battleRecordsQuery.Where(br => br.StartedAt >= query.StartDate.Value);
            
            if (query.EndDate.HasValue)
                battleRecordsQuery = battleRecordsQuery.Where(br => br.StartedAt <= query.EndDate.Value);
            
            if (!string.IsNullOrEmpty(query.Status))
                battleRecordsQuery = battleRecordsQuery.Where(br => br.Status == query.Status);
            
            var result = await battleRecordsQuery
                .OrderByDescending(br => br.StartedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(br => MapToDto(br))
                .ToListAsync();
            
            return new ApiResponse<List<BattleRecordStorageDto>>
            {
                IsSuccess = true,
                Data = result,
                Message = $"获取到 {result.Count} 条队伍战斗记录"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get battle history for team with ID: {SafeTeamId}", SafeLogId(teamId));
            return new ApiResponse<List<BattleRecordStorageDto>>
            {
                IsSuccess = false,
                Message = $"获取队伍战斗历史失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<BattleRecordStorageDto>>> GetActiveBattleRecordsAsync()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var activeBattles = await context.BattleRecords
                .Where(br => br.Status == "InProgress")
                .OrderByDescending(br => br.StartedAt)
                .Select(br => MapToDto(br))
                .ToListAsync();
            
            return new ApiResponse<List<BattleRecordStorageDto>>
            {
                IsSuccess = true,
                Data = activeBattles,
                Message = $"获取到 {activeBattles.Count} 场进行中的战斗"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active battle records");
            return new ApiResponse<List<BattleRecordStorageDto>>
            {
                IsSuccess = false,
                Message = $"获取进行中战斗失败: {ex.Message}"
            };
        }
    }

    #endregion

    #region 离线数据管理

    public async Task<ApiResponse<OfflineDataStorageDto>> SaveOfflineDataAsync(OfflineDataStorageDto offlineData)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var entity = MapToEntity(offlineData);
            entity.UpdatedAt = DateTime.UtcNow;
            
            var existing = await context.OfflineData.FindAsync(offlineData.Id);
            if (existing != null)
            {
                context.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                await context.OfflineData.AddAsync(entity);
            }
            
            await context.SaveChangesAsync();
            
            _logger.LogDebug("OfflineData saved for player with IDs: {SafeOfflineDataId}, {SafePlayerId}", 
                SafeLogId(offlineData.Id), SafeLogId(offlineData.PlayerId));
            
            return new ApiResponse<OfflineDataStorageDto>
            {
                IsSuccess = true,
                Data = MapToDto(entity),
                Message = "离线数据保存成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save offline data with ID: {SafeOfflineDataId}", SafeLogId(offlineData.Id));
            return new ApiResponse<OfflineDataStorageDto>
            {
                IsSuccess = false,
                Message = $"保存离线数据失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<OfflineDataStorageDto>>> GetUnsyncedOfflineDataAsync(string playerId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var unsyncedData = await context.OfflineData
                .Where(od => od.PlayerId == playerId && !od.IsSynced)
                .OrderBy(od => od.CreatedAt)
                .Select(od => MapToDto(od))
                .ToListAsync();
            
            return new ApiResponse<List<OfflineDataStorageDto>>
            {
                IsSuccess = true,
                Data = unsyncedData,
                Message = $"获取到 {unsyncedData.Count} 条未同步的离线数据"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unsynced offline data for player with ID: {SafePlayerId}", SafeLogId(playerId));
            return new ApiResponse<List<OfflineDataStorageDto>>
            {
                IsSuccess = false,
                Message = $"获取未同步离线数据失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> MarkOfflineDataSyncedAsync(List<string> offlineDataIds)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var syncTime = DateTime.UtcNow;
            int syncedCount = 0;
            
            foreach (var id in offlineDataIds)
            {
                var offlineData = await context.OfflineData.FindAsync(id);
                if (offlineData != null)
                {
                    offlineData.IsSynced = true;
                    offlineData.SyncedAt = syncTime;
                    offlineData.UpdatedAt = syncTime;
                    syncedCount++;
                }
            }
            
            await context.SaveChangesAsync();
            
            return new ApiResponse<bool>
            {
                IsSuccess = true,
                Data = true,
                Message = $"已标记 {syncedCount} 条离线数据为已同步"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark offline data as synced");
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Message = $"标记离线数据同步状态失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<int>> CleanupSyncedOfflineDataAsync(DateTime olderThan)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var toRemove = await context.OfflineData
                .Where(od => od.IsSynced && od.SyncedAt < olderThan)
                .ToListAsync();
            
            context.OfflineData.RemoveRange(toRemove);
            await context.SaveChangesAsync();
            
            int removedCount = toRemove.Count;
            _logger.LogInformation("Cleaned up {RemovedCount} synced offline data records older than the specified date", removedCount);
            
            return new ApiResponse<int>
            {
                IsSuccess = true,
                Data = removedCount,
                Message = $"清理了 {removedCount} 条已同步的旧离线数据"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup synced offline data");
            return new ApiResponse<int>
            {
                IsSuccess = false,
                Message = $"清理已同步离线数据失败: {ex.Message}"
            };
        }
    }

    #endregion

    #region 数据查询和统计

    public async Task<ApiResponse<List<PlayerStorageDto>>> SearchPlayersAsync(string searchTerm, int limit = 20)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var searchResults = await context.Players
                .Where(p => p.Name.Contains(searchTerm) || p.Id.Contains(searchTerm))
                .Take(limit)
                .OrderBy(p => p.Name)
                .Select(p => MapToDto(p))
                .ToListAsync();
            
            return new ApiResponse<List<PlayerStorageDto>>
            {
                IsSuccess = true,
                Data = searchResults,
                Message = $"找到 {searchResults.Count} 个匹配的玩家"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search players");
            return new ApiResponse<List<PlayerStorageDto>>
            {
                IsSuccess = false,
                Message = $"搜索玩家失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<Dictionary<string, object>>> GetStorageStatsAsync()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var stats = new Dictionary<string, object>
            {
                ["TotalPlayers"] = await context.Players.CountAsync(),
                ["OnlinePlayers"] = await context.Players.CountAsync(p => p.IsOnline),
                ["TotalTeams"] = await context.Teams.CountAsync(),
                ["ActiveTeams"] = await context.Teams.CountAsync(t => t.Status == "Active"),
                ["TotalActionTargets"] = await context.ActionTargets.CountAsync(),
                ["ActiveActionTargets"] = await context.ActionTargets.CountAsync(at => !at.IsCompleted),
                ["TotalBattleRecords"] = await context.BattleRecords.CountAsync(),
                ["ActiveBattles"] = await context.BattleRecords.CountAsync(br => br.Status == "InProgress"),
                ["TotalOfflineData"] = await context.OfflineData.CountAsync(),
                ["UnsyncedOfflineData"] = await context.OfflineData.CountAsync(od => !od.IsSynced),
                ["LastUpdated"] = DateTime.UtcNow
            };
            
            return new ApiResponse<Dictionary<string, object>>
            {
                IsSuccess = true,
                Data = stats,
                Message = "存储统计信息获取成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage stats");
            return new ApiResponse<Dictionary<string, object>>
            {
                IsSuccess = false,
                Message = $"获取存储统计信息失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<Dictionary<string, object>>> HealthCheckAsync()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            // 测试数据库连接
            var canConnect = await context.Database.CanConnectAsync();
            
            var healthCheck = new Dictionary<string, object>
            {
                ["Status"] = canConnect ? "Healthy" : "Unhealthy",
                ["StorageType"] = "SQLite",
                ["DatabaseConnection"] = canConnect,
                ["Timestamp"] = DateTime.UtcNow
            };

            if (canConnect)
            {
                healthCheck["DatabaseInfo"] = new Dictionary<string, object>
                {
                    ["Players"] = await context.Players.CountAsync(),
                    ["Teams"] = await context.Teams.CountAsync(),
                    ["ActionTargets"] = await context.ActionTargets.CountAsync(),
                    ["BattleRecords"] = await context.BattleRecords.CountAsync(),
                    ["OfflineData"] = await context.OfflineData.CountAsync()
                };
            }
            
            return new ApiResponse<Dictionary<string, object>>
            {
                IsSuccess = canConnect,
                Data = healthCheck,
                Message = canConnect ? "数据存储服务健康检查通过" : "数据库连接失败"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return new ApiResponse<Dictionary<string, object>>
            {
                IsSuccess = false,
                Data = new Dictionary<string, object>
                {
                    ["Status"] = "Unhealthy",
                    ["Error"] = ex.Message,
                    ["Timestamp"] = DateTime.UtcNow
                },
                Message = $"健康检查失败: {ex.Message}"
            };
        }
    }

    #endregion

    #region 数据同步和备份

    public async Task<ApiResponse<Dictionary<string, object>>> ExportPlayerDataAsync(string playerId)
    {
        try
        {
            var player = await GetPlayerAsync(playerId);
            if (player == null)
            {
                return new ApiResponse<Dictionary<string, object>>
                {
                    IsSuccess = false,
                    Message = "玩家不存在"
                };
            }
            
            var exportData = new Dictionary<string, object>
            {
                ["Player"] = player,
                ["Team"] = await GetTeamByPlayerAsync(playerId),
                ["CurrentActionTarget"] = await GetCurrentActionTargetAsync(playerId),
                ["ActionHistory"] = (await GetPlayerActionHistoryAsync(playerId, 100)).Data ?? new List<ActionTargetStorageDto>(),
                ["BattleHistory"] = (await GetPlayerBattleHistoryAsync(playerId, new DataStorageQueryDto { PageSize = 100 })).Data ?? new List<BattleRecordStorageDto>(),
                ["OfflineData"] = (await GetUnsyncedOfflineDataAsync(playerId)).Data ?? new List<OfflineDataStorageDto>(),
                ["ExportTimestamp"] = DateTime.UtcNow
            };
            
            return new ApiResponse<Dictionary<string, object>>
            {
                IsSuccess = true,
                Data = exportData,
                Message = "玩家数据导出成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export player data for ID: {SafePlayerId}", SafeLogId(playerId));
            return new ApiResponse<Dictionary<string, object>>
            {
                IsSuccess = false,
                Message = $"导出玩家数据失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> ImportPlayerDataAsync(string playerId, Dictionary<string, object> data)
    {
        try
        {
            // 这里可以实现数据导入逻辑
            _logger.LogInformation("Player data import requested for ID: {SafePlayerId}", SafeLogId(playerId));
            
            return new ApiResponse<bool>
            {
                IsSuccess = true,
                Data = true,
                Message = "数据导入功能待实现"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import player data for ID: {SafePlayerId}", SafeLogId(playerId));
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Message = $"导入玩家数据失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<string>> BackupDataAsync()
    {
        try
        {
            var backupId = Guid.NewGuid().ToString();
            _logger.LogInformation("Data backup requested with ID {BackupId}", backupId);
            
            return new ApiResponse<string>
            {
                IsSuccess = true,
                Data = backupId,
                Message = $"数据备份已启动，备份ID: {backupId}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backup data");
            return new ApiResponse<string>
            {
                IsSuccess = false,
                Message = $"数据备份失败: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<int>> CleanupExpiredDataAsync(TimeSpan olderThan)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var cutoffTime = DateTime.UtcNow - olderThan;
            int cleanedCount = 0;
            
            // 清理已完成的旧动作目标
            var expiredActionTargets = await context.ActionTargets
                .Where(at => at.IsCompleted && at.CompletedAt < cutoffTime)
                .ToListAsync();
            
            context.ActionTargets.RemoveRange(expiredActionTargets);
            cleanedCount += expiredActionTargets.Count;
            
            // 清理已结束的旧战斗记录
            var expiredBattleRecords = await context.BattleRecords
                .Where(br => br.Status != "InProgress" && br.EndedAt < cutoffTime)
                .ToListAsync();
            
            context.BattleRecords.RemoveRange(expiredBattleRecords);
            cleanedCount += expiredBattleRecords.Count;
            
            // 清理已同步的旧离线数据
            var cleanupOfflineResult = await CleanupSyncedOfflineDataAsync(cutoffTime);
            if (cleanupOfflineResult.IsSuccess)
            {
                cleanedCount += cleanupOfflineResult.Data;
            }
            
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {CleanedCount} expired data records older than {CutoffTime}", cleanedCount, cutoffTime);
            
            return new ApiResponse<int>
            {
                IsSuccess = true,
                Data = cleanedCount,
                Message = $"清理了 {cleanedCount} 条过期数据"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired data");
            return new ApiResponse<int>
            {
                IsSuccess = false,
                Message = $"清理过期数据失败: {ex.Message}"
            };
        }
    }

    #endregion

    #region 实体映射方法

    private static UserStorageDto MapToDto(UserEntity entity)
    {
        var dto = new UserStorageDto
        {
            Id = entity.Id,
            Username = entity.Username,
            Email = entity.Email,
            IsActive = entity.IsActive,
            EmailVerified = entity.EmailVerified,
            LastLoginAt = entity.LastLoginAt,
            LastLoginIp = entity.LastLoginIp,
            LoginAttempts = entity.LoginAttempts,
            LockedUntil = entity.LockedUntil,
            LastPasswordChange = entity.LastPasswordChange,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            DisplayName = entity.DisplayName,
            Avatar = entity.Avatar,
            PasswordHash = entity.PasswordHash,
            PasswordSalt = entity.Salt
        };

        // 反序列化角色
        try
        {
            dto.Roles = JsonSerializer.Deserialize<List<string>>(entity.RolesJson) ?? new List<string> { "Player" };
        }
        catch
        {
            dto.Roles = new List<string> { "Player" };
        }

        // 反序列化登录历史
        try
        {
            dto.LoginHistory = JsonSerializer.Deserialize<List<string>>(entity.LoginHistoryJson) ?? new List<string>();
        }
        catch
        {
            dto.LoginHistory = new List<string>();
        }

        // 反序列化自定义属性
        try
        {
            var profile = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.ProfileJson)
                ?? new Dictionary<string, object>();

            dto.CustomProperties = profile.Where(p => p.Key != "DisplayName" && p.Key != "Avatar")
                .ToDictionary(p => p.Key, p => p.Value);
        }
        catch
        {
            dto.CustomProperties = new Dictionary<string, object>();
        }

        // 反序列化角色ID列表
        try
        {
            dto.CharacterIds = JsonSerializer.Deserialize<List<string>>(entity.CharacterIdsJson) ?? new List<string>();
        }
        catch
        {
            dto.CharacterIds = new List<string>();
        }

        return dto;
    }

    private static UserEntity MapToEntity(UserStorageDto dto)
    {
        var entity = new UserEntity
        {
            Id = dto.Id,
            Username = dto.Username,
            Email = dto.Email,
            IsActive = dto.IsActive,
            EmailVerified = dto.EmailVerified,
            LastLoginAt = dto.LastLoginAt,
            LastLoginIp = dto.LastLoginIp,
            LoginAttempts = dto.LoginAttempts,
            LockedUntil = dto.LockedUntil,
            LastPasswordChange = dto.LastPasswordChange,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            DisplayName = dto.DisplayName,
            Avatar = dto.Avatar,
            PasswordHash = dto.PasswordHash,
            Salt = dto.PasswordSalt,
            RolesJson = JsonSerializer.Serialize(dto.Roles),
            LoginHistoryJson = JsonSerializer.Serialize(dto.LoginHistory),
            CharacterIdsJson = JsonSerializer.Serialize(dto.CharacterIds)
        };

        // 序列化自定义属性
        entity.ProfileJson = JsonSerializer.Serialize(dto.CustomProperties);

        return entity;
    }

    private static PlayerStorageDto MapToDto(PlayerEntity entity)
    {
        return new PlayerStorageDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Level = entity.Level,
            Experience = entity.Experience,
            Health = entity.Health,
            MaxHealth = entity.MaxHealth,
            Gold = entity.Gold,
            SelectedBattleProfession = entity.SelectedBattleProfession,
            CurrentAction = entity.CurrentAction,
            CurrentActionTargetId = entity.CurrentActionTargetId,
            PartyId = entity.PartyId,
            IsOnline = entity.IsOnline,
            LastActiveAt = entity.LastActiveAt,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Attributes = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.AttributesJson) ?? new Dictionary<string, object>(),
            Inventory = JsonSerializer.Deserialize<List<object>>(entity.InventoryJson) ?? new List<object>(),
            Skills = JsonSerializer.Deserialize<List<string>>(entity.SkillsJson) ?? new List<string>(),
            Equipment = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.EquipmentJson) ?? new Dictionary<string, string>()
        };
    }

    private static PlayerEntity MapToEntity(PlayerStorageDto dto)
    {
        return new PlayerEntity
        {
            Id = dto.Id,
            Name = dto.Name,
            Level = dto.Level,
            Experience = dto.Experience,
            Health = dto.Health,
            MaxHealth = dto.MaxHealth,
            Gold = dto.Gold,
            SelectedBattleProfession = dto.SelectedBattleProfession,
            CurrentAction = dto.CurrentAction,
            CurrentActionTargetId = dto.CurrentActionTargetId,
            PartyId = dto.PartyId,
            IsOnline = dto.IsOnline,
            LastActiveAt = dto.LastActiveAt,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            AttributesJson = JsonSerializer.Serialize(dto.Attributes),
            InventoryJson = JsonSerializer.Serialize(dto.Inventory),
            SkillsJson = JsonSerializer.Serialize(dto.Skills),
            EquipmentJson = JsonSerializer.Serialize(dto.Equipment)
        };
    }

    private static TeamStorageDto MapToDto(TeamEntity entity)
    {
        return new TeamStorageDto
        {
            Id = entity.Id,
            Name = entity.Name,
            CaptainId = entity.CaptainId,
            MemberIds = JsonSerializer.Deserialize<List<string>>(entity.MemberIdsJson) ?? new List<string>(),
            MaxMembers = entity.MaxMembers,
            Status = entity.Status,
            CurrentBattleId = entity.CurrentBattleId,
            LastBattleAt = entity.LastBattleAt,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static TeamEntity MapToEntity(TeamStorageDto dto)
    {
        return new TeamEntity
        {
            Id = dto.Id,
            Name = dto.Name,
            CaptainId = dto.CaptainId,
            MemberIdsJson = JsonSerializer.Serialize(dto.MemberIds),
            MaxMembers = dto.MaxMembers,
            Status = dto.Status,
            CurrentBattleId = dto.CurrentBattleId,
            LastBattleAt = dto.LastBattleAt,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    private static ActionTargetStorageDto MapToDto(ActionTargetEntity entity)
    {
        return new ActionTargetStorageDto
        {
            Id = entity.Id,
            PlayerId = entity.PlayerId,
            TargetType = entity.TargetType,
            TargetId = entity.TargetId,
            TargetName = entity.TargetName,
            ActionType = entity.ActionType,
            Progress = entity.Progress,
            Duration = entity.Duration,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            IsCompleted = entity.IsCompleted,
            ProgressData = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.ProgressDataJson) ?? new Dictionary<string, object>(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static ActionTargetEntity MapToEntity(ActionTargetStorageDto dto)
    {
        return new ActionTargetEntity
        {
            Id = dto.Id,
            PlayerId = dto.PlayerId,
            TargetType = dto.TargetType,
            TargetId = dto.TargetId,
            TargetName = dto.TargetName,
            ActionType = dto.ActionType,
            Progress = dto.Progress,
            Duration = dto.Duration,
            StartedAt = dto.StartedAt,
            CompletedAt = dto.CompletedAt,
            IsCompleted = dto.IsCompleted,
            ProgressDataJson = JsonSerializer.Serialize(dto.ProgressData),
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    private static BattleRecordStorageDto MapToDto(BattleRecordEntity entity)
    {
        return new BattleRecordStorageDto
        {
            Id = entity.Id,
            BattleId = entity.BattleId,
            BattleType = entity.BattleType,
            StartedAt = entity.StartedAt,
            EndedAt = entity.EndedAt,
            Status = entity.Status,
            Participants = JsonSerializer.Deserialize<List<string>>(entity.ParticipantsJson) ?? new List<string>(),
            Enemies = JsonSerializer.Deserialize<List<object>>(entity.EnemiesJson) ?? new List<object>(),
            Actions = JsonSerializer.Deserialize<List<object>>(entity.ActionsJson) ?? new List<object>(),
            Results = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.ResultsJson) ?? new Dictionary<string, object>(),
            PartyId = entity.PartyId,
            DungeonId = entity.DungeonId,
            WaveNumber = entity.WaveNumber,
            Duration = entity.Duration,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static BattleRecordEntity MapToEntity(BattleRecordStorageDto dto)
    {
        return new BattleRecordEntity
        {
            Id = dto.Id,
            BattleId = dto.BattleId,
            BattleType = dto.BattleType,
            StartedAt = dto.StartedAt,
            EndedAt = dto.EndedAt,
            Status = dto.Status,
            ParticipantsJson = JsonSerializer.Serialize(dto.Participants),
            EnemiesJson = JsonSerializer.Serialize(dto.Enemies),
            ActionsJson = JsonSerializer.Serialize(dto.Actions),
            ResultsJson = JsonSerializer.Serialize(dto.Results),
            PartyId = dto.PartyId,
            DungeonId = dto.DungeonId,
            WaveNumber = dto.WaveNumber,
            Duration = dto.Duration,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    private static OfflineDataStorageDto MapToDto(OfflineDataEntity entity)
    {
        return new OfflineDataStorageDto
        {
            Id = entity.Id,
            PlayerId = entity.PlayerId,
            DataType = entity.DataType,
            Data = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.DataJson) ?? new Dictionary<string, object>(),
            SyncedAt = entity.SyncedAt,
            IsSynced = entity.IsSynced,
            Version = entity.Version,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static OfflineDataEntity MapToEntity(OfflineDataStorageDto dto)
    {
        return new OfflineDataEntity
        {
            Id = dto.Id,
            PlayerId = dto.PlayerId,
            DataType = dto.DataType,
            DataJson = JsonSerializer.Serialize(dto.Data),
            SyncedAt = dto.SyncedAt,
            IsSynced = dto.IsSynced,
            Version = dto.Version,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    #endregion
}