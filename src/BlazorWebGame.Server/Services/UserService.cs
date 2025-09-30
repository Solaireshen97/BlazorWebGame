using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;
using BlazorWebGame.Shared.Mappers;
using BlazorWebGame.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 用户服务 - 处理用户认证、注册和管理
/// </summary>
public class UserService
{
    private readonly IDataStorageService _dataStorage;
    private readonly ILogger<UserService> _logger;

    // 密码强度正则表达式
    private static readonly Regex PasswordRegex = new Regex(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d@$!%*?&]{8,}$",
        RegexOptions.Compiled);

    // 邮箱格式正则表达式
    private static readonly Regex EmailRegex = new Regex(
        @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public UserService(IDataStorageService dataStorage, ILogger<UserService> logger)
    {
        _dataStorage = dataStorage;
        _logger = logger;
    }

    /// <summary>
    /// 验证用户凭据并返回用户领域模型
    /// </summary>
    public async Task<User?> ValidateUserAsync(string username, string password)
    {
        try
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            var userDto = await _dataStorage.GetUserByUsernameAsync(username);
            if (userDto == null)
            {
                _logger.LogWarning("Login attempt failed: user not found for username: {Username}", username);
                return null;
            }

            // 检查账户是否被锁定
            if (userDto.LockedUntil.HasValue && userDto.LockedUntil.Value > DateTime.UtcNow)
            {
                _logger.LogWarning("Login attempt failed: account locked for username: {Username} until {LockedUntil}",
                    username, userDto.LockedUntil.Value);
                return null;
            }

            // 检查账户是否激活
            if (!userDto.IsActive)
            {
                _logger.LogWarning("Login attempt failed: account inactive for username: {Username}", username);
                return null;
            }

            // 验证密码
            var isValidPassword = await _dataStorage.ValidateUserPasswordAsync(userDto.Id, password);
            if (isValidPassword)
            {
                _logger.LogInformation("User {Username} authenticated successfully", username);
                return userDto.ToUser();
            }
            else
            {
                // 增加登录失败次数
                await IncrementLoginAttemptsAsync(userDto.Id);
                _logger.LogWarning("Login attempt failed: invalid password for username: {Username}", username);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user credentials for username: {Username}", username);
            return null;
        }
    }

    /// <summary>
    /// 注册新用户
    /// </summary>
    public async Task<ApiResponse<User>> RegisterUserAsync(string username, string password, string email = "")
    {
        try
        {
            // 验证输入
            var validationResult = ValidateRegistrationInput(username, password, email);
            if (!validationResult.Success)
                return new ApiResponse<User>
                {
                    Success = false,
                    Message = validationResult.Message
                };

            // 创建用户
            var user = new User(username, email);

            // 转换为DTO并保存
            var userDto = user.ToDto();
            var result = await _dataStorage.CreateUserAsync(userDto, password);

            if (result.Success)
            {
                _logger.LogInformation("User registered successfully: {Username}", username);
                return new ApiResponse<User>
                {
                    Success = true,
                    Data = result.Data.ToUser(),
                    Message = "用户注册成功"
                };
            }
            else
            {
                return new ApiResponse<User>
                {
                    Success = false,
                    Message = result.Message
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user: {Username}", username);
            return new ApiResponse<User>
            {
                Success = false,
                Message = "用户注册失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    public async Task<User?> GetUserByIdAsync(string userId)
    {
        var userDto = await _dataStorage.GetUserByIdAsync(userId);
        return userDto != null ? userDto.ToUser() : null;
    }

    /// <summary>
    /// 更新用户最后登录信息
    /// </summary>
    public async Task<bool> UpdateLastLoginAsync(string userId, string ipAddress)
    {
        var user = await GetUserByIdAsync(userId);
        if (user != null)
        {
            user.RecordLogin(ipAddress);
            var dto = user.ToDto();
            var result = await _dataStorage.UpdateUserAsync(dto);
            return result.Success;
        }
        return false;
    }

    /// <summary>
    /// 检查用户是否拥有指定角色
    /// </summary>
    public async Task<bool> UserHasRoleAsync(string userId, string role)
    {
        var userDto = await _dataStorage.GetUserByIdAsync(userId);
        if (userDto?.Roles != null)
        {
            return userDto.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }
        return false;
    }

    /// <summary>
    /// 检查用户是否拥有角色（使用数据库验证）
    /// </summary>
    public async Task<bool> UserHasCharacterAsync(string userId, string characterId)
    {
        try
        {
            return await _dataStorage.UserOwnsCharacterAsync(userId, characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying character ownership for user {UserId}, character {CharacterId}", userId, characterId);
            return false;
        }
    }

    /// <summary>
    /// 增加登录尝试次数，如果超过限制则锁定账户
    /// </summary>
    private async Task IncrementLoginAttemptsAsync(string userId)
    {
        try
        {
            var userDto = await _dataStorage.GetUserByIdAsync(userId);
            if (userDto != null)
            {
                var user = userDto.ToUser();
                user.RecordFailedLogin();

                // 更新DTO并保存
                var updatedDto = user.ToDto();
                await _dataStorage.UpdateUserAsync(updatedDto);

                // 如果登录失败次数超过5次，锁定账户30分钟
                if (user.Security.LoginAttempts >= 5)
                {
                    await _dataStorage.LockUserAccountAsync(userId, DateTime.UtcNow.AddMinutes(30));
                    _logger.LogWarning("User account locked due to too many failed login attempts: {UserId}", userId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing login attempts for user: {UserId}", userId);
        }
    }

    /// <summary>
    /// 验证注册输入
    /// </summary>
    private ApiResponse<User> ValidateRegistrationInput(string username, string password, string email)
    {
        // 验证用户名
        if (string.IsNullOrWhiteSpace(username))
        {
            return new ApiResponse<User>
            {
                Success = false,
                Message = "用户名不能为空"
            };
        }

        if (username.Length < 3 || username.Length > 20)
        {
            return new ApiResponse<User>
            {
                Success = false,
                Message = "用户名长度必须在3-20个字符之间"
            };
        }

        if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
        {
            return new ApiResponse<User>
            {
                Success = false,
                Message = "用户名只能包含字母、数字和下划线"
            };
        }

        // 验证密码
        if (string.IsNullOrWhiteSpace(password))
        {
            return new ApiResponse<User>
            {
                Success = false,
                Message = "密码不能为空"
            };
        }

        if (password.Length < 6)
        {
            return new ApiResponse<User>
            {
                Success = false,
                Message = "密码长度至少6个字符"
            };
        }

        // 验证邮箱（如果提供）
        if (!string.IsNullOrWhiteSpace(email))
        {
            if (!EmailRegex.IsMatch(email))
            {
                return new ApiResponse<User>
                {
                    Success = false,
                    Message = "邮箱格式不正确"
                };
            }
        }

        return new ApiResponse<User> { Success = true };
    }

    /// <summary>
    /// 更新用户密码
    /// </summary>
    public async Task<ApiResponse<bool>> UpdatePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        try
        {
            // 验证当前密码
            var isPasswordValid = await _dataStorage.ValidateUserPasswordAsync(userId, currentPassword);
            if (!isPasswordValid)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "当前密码错误"
                };
            }

            // 验证新密码
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "新密码长度至少6个字符"
                };
            }

            // 更新密码
            var result = await _dataStorage.UpdateUserPasswordAsync(userId, newPassword);
            if (result.Success)
            {
                // 更新用户的安全信息
                var user = await GetUserByIdAsync(userId);
                if (user != null)
                {
                    user.Security.UpdatePassword();
                    var updatedDto = user.ToDto();
                    await _dataStorage.UpdateUserAsync(updatedDto);
                }

                _logger.LogInformation("Password updated successfully for user: {UserId}", userId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password for user: {UserId}", userId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "更新密码失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 重置用户密码（管理员功能）
    /// </summary>
    public async Task<ApiResponse<string>> ResetPasswordAsync(string userId)
    {
        try
        {
            // 生成随机密码
            var newPassword = GenerateRandomPassword();

            // 更新密码
            var result = await _dataStorage.UpdateUserPasswordAsync(userId, newPassword);
            if (result.Success)
            {
                // 更新用户的安全信息
                var user = await GetUserByIdAsync(userId);
                if (user != null)
                {
                    user.Security.UpdatePassword();
                    var updatedDto = user.ToDto();
                    await _dataStorage.UpdateUserAsync(updatedDto);
                }

                _logger.LogInformation("Password reset successfully for user: {UserId}", userId);

                return new ApiResponse<string>
                {
                    Success = true,
                    Data = newPassword,
                    Message = "密码重置成功"
                };
            }
            else
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = result.Message
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user: {UserId}", userId);
            return new ApiResponse<string>
            {
                Success = false,
                Message = "重置密码失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 更新用户个人资料
    /// </summary>
    public async Task<ApiResponse<User>> UpdateUserProfileAsync(string userId, string? displayName = null, string? avatar = null)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ApiResponse<User>
                {
                    Success = false,
                    Message = "未找到用户"
                };
            }

            // 更新用户资料
            user.UpdateProfile(displayName, avatar);
            var dto = user.ToDto();
            var result = await _dataStorage.UpdateUserAsync(dto);

            if (result.Success)
            {
                _logger.LogInformation("Profile updated for user: {UserId}", userId);
                return new ApiResponse<User>
                {
                    Success = true,
                    Data = result.Data.ToUser(),
                    Message = "个人资料更新成功"
                };
            }
            else
            {
                return new ApiResponse<User>
                {
                    Success = false,
                    Message = result.Message
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user: {UserId}", userId);
            return new ApiResponse<User>
            {
                Success = false,
                Message = "更新个人资料失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 添加用户角色
    /// </summary>
    public async Task<ApiResponse<bool>> AddUserRoleAsync(string userId, string role)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "角色名称不能为空"
                };
            }

            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "未找到用户"
                };
            }

            // 添加角色
            user.Security.AddRole(role);
            var dto = user.ToDto();
            var result = await _dataStorage.UpdateUserAsync(dto);

            if (result.Success)
            {
                _logger.LogInformation("Role {Role} added to user: {UserId}", role, userId);
                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = $"已成功添加角色: {role}"
                };
            }
            else
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = result.Message
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding role {Role} to user: {UserId}", role, userId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "添加角色失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 移除用户角色
    /// </summary>
    public async Task<ApiResponse<bool>> RemoveUserRoleAsync(string userId, string role)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "未找到用户"
                };
            }

            // 移除角色
            user.Security.RemoveRole(role);
            var dto = user.ToDto();
            var result = await _dataStorage.UpdateUserAsync(dto);

            if (result.Success)
            {
                _logger.LogInformation("Role {Role} removed from user: {UserId}", role, userId);
                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = $"已成功移除角色: {role}"
                };
            }
            else
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = result.Message
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {Role} from user: {UserId}", role, userId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "移除角色失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 验证用户邮箱
    /// </summary>
    public async Task<ApiResponse<bool>> VerifyEmailAsync(string userId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "未找到用户"
                };
            }

            // 验证邮箱
            user.VerifyEmail();
            var dto = user.ToDto();
            var result = await _dataStorage.UpdateUserAsync(dto);

            if (result.Success)
            {
                _logger.LogInformation("Email verified for user: {UserId}", userId);
                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "邮箱验证成功"
                };
            }
            else
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = result.Message
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email for user: {UserId}", userId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "邮箱验证失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 激活用户账户
    /// </summary>
    public async Task<ApiResponse<bool>> ActivateUserAsync(string userId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "未找到用户"
                };
            }

            // 激活账户
            user.Activate();
            var dto = user.ToDto();
            var result = await _dataStorage.UpdateUserAsync(dto);

            if (result.Success)
            {
                _logger.LogInformation("Account activated for user: {UserId}", userId);
                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "账户已激活"
                };
            }
            else
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = result.Message
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating account for user: {UserId}", userId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "激活账户失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 停用用户账户
    /// </summary>
    public async Task<ApiResponse<bool>> DeactivateUserAsync(string userId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "未找到用户"
                };
            }

            // 停用账户
            user.Deactivate();
            var dto = user.ToDto();
            var result = await _dataStorage.UpdateUserAsync(dto);

            if (result.Success)
            {
                _logger.LogInformation("Account deactivated for user: {UserId}", userId);
                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "账户已停用"
                };
            }
            else
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = result.Message
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating account for user: {UserId}", userId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "停用账户失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 解锁用户账户
    /// </summary>
    public async Task<ApiResponse<bool>> UnlockUserAccountAsync(string userId)
    {
        try
        {
            var result = await _dataStorage.UnlockUserAccountAsync(userId);
            if (result.Success)
            {
                // 更新用户安全信息
                var user = await GetUserByIdAsync(userId);
                if (user != null)
                {
                    user.Security.Unlock();
                    var dto = user.ToDto();
                    await _dataStorage.UpdateUserAsync(dto);
                }

                _logger.LogInformation("Account unlocked for user: {UserId}", userId);
                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "账户已解锁"
                };
            }
            else
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking account for user: {UserId}", userId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "解锁账户失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 为用户添加游戏角色
    /// </summary>
    public async Task<ApiResponse<bool>> AddCharacterToUserAsync(string userId, string characterId, string characterName)
    {
        try
        {
            // 创建用户角色关联
            var result = await _dataStorage.CreateUserCharacterAsync(userId, characterId, characterName);
            if (result.Success)
            {
                // 更新用户的角色列表
                var user = await GetUserByIdAsync(userId);
                if (user != null)
                {
                    user.AddCharacter(characterId);
                    var dto = user.ToDto();
                    await _dataStorage.UpdateUserAsync(dto);
                }

                _logger.LogInformation("Character {CharacterId} added to user: {UserId}", characterId, userId);
                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = $"已成功添加角色: {characterName}"
                };
            }
            else
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = result.Message
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding character {CharacterId} to user: {UserId}", characterId, userId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "添加角色失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 从用户移除游戏角色
    /// </summary>
    public async Task<ApiResponse<bool>> RemoveCharacterFromUserAsync(string userId, string characterId)
    {
        try
        {
            // 删除用户角色关联
            var result = await _dataStorage.DeleteUserCharacterAsync(userId, characterId);
            if (result.Success)
            {
                // 更新用户的角色列表
                var user = await GetUserByIdAsync(userId);
                if (user != null)
                {
                    user.RemoveCharacter(characterId);
                    var dto = user.ToDto();
                    await _dataStorage.UpdateUserAsync(dto);
                }

                _logger.LogInformation("Character {CharacterId} removed from user: {UserId}", characterId, userId);
                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "已成功移除角色"
                };
            }
            else
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing character {CharacterId} from user: {UserId}", characterId, userId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "移除角色失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 设置用户的默认角色
    /// </summary>
    public async Task<ApiResponse<bool>> SetDefaultCharacterAsync(string userId, string characterId)
    {
        try
        {
            // 验证用户是否拥有该角色
            if (!await UserHasCharacterAsync(userId, characterId))
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "用户不拥有该角色"
                };
            }

            // 设置默认角色
            var result = await _dataStorage.SetDefaultCharacterAsync(userId, characterId);
            if (result.Success)
            {
                _logger.LogInformation("Default character set to {CharacterId} for user: {UserId}", characterId, userId);
                return result;
            }
            else
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default character {CharacterId} for user: {UserId}", characterId, userId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "设置默认角色失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 获取用户的所有游戏角色
    /// </summary>
    public async Task<ApiResponse<List<UserCharacterRelation>>> GetUserCharactersAsync(string userId)
    {
        try
        {
            var result = await _dataStorage.GetUserCharactersAsync(userId);
            if (result.Success)
            {
                var characters = new List<UserCharacterRelation>();
                foreach (var dto in result.Data)
                {
                    // 这里假设有一个从DTO到领域模型的转换方法
                    // characters.Add(dto.ToUserCharacterRelation());
                    // 如果没有对应的转换方法，需要手动创建
                    characters.Add(new UserCharacterRelation(dto.UserId, dto.CharacterId, dto.IsDefault));
                }

                return new ApiResponse<List<UserCharacterRelation>>
                {
                    Success = true,
                    Data = characters,
                    Message = "获取角色列表成功"
                };
            }
            else
            {
                return new ApiResponse<List<UserCharacterRelation>>
                {
                    Success = false,
                    Message = result.Message
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting characters for user: {UserId}", userId);
            return new ApiResponse<List<UserCharacterRelation>>
            {
                Success = false,
                Message = "获取角色列表失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 生成随机密码
    /// </summary>
    private string GenerateRandomPassword(int length = 10)
    {
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string numberChars = "0123456789";
        const string specialChars = "!@#$%^&*()_-+=";

        var random = new Random();
        var password = new char[length];

        // 确保密码包含至少一个小写字母、一个大写字母和一个数字
        password[0] = lowerChars[random.Next(lowerChars.Length)];
        password[1] = upperChars[random.Next(upperChars.Length)];
        password[2] = numberChars[random.Next(numberChars.Length)];

        // 填充剩余字符
        var allChars = lowerChars + upperChars + numberChars + specialChars;
        for (var i = 3; i < length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }

        // 随机打乱密码字符顺序
        for (var i = 0; i < length; i++)
        {
            var swapIndex = random.Next(length);
            (password[i], password[swapIndex]) = (password[swapIndex], password[i]);
        }

        return new string(password);
    }
}