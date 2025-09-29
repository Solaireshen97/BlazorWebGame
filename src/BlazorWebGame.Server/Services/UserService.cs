using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;
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
    /// 验证用户凭据
    /// </summary>
    public async Task<UserStorageDto?> ValidateUserAsync(string username, string password)
    {
        try
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            var user = await _dataStorage.GetUserByUsernameAsync(username);
            if (user == null)
            {
                _logger.LogWarning("Login attempt failed: user not found for username: {Username}", username);
                return null;
            }

            // 检查账户是否被锁定
            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
            {
                _logger.LogWarning("Login attempt failed: account locked for username: {Username} until {LockedUntil}", 
                    username, user.LockedUntil.Value);
                return null;
            }

            // 检查账户是否激活
            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt failed: account inactive for username: {Username}", username);
                return null;
            }

            // 验证密码
            var isValidPassword = await _dataStorage.ValidateUserPasswordAsync(user.Id, password);
            if (isValidPassword)
            {
                _logger.LogInformation("User {Username} authenticated successfully", username);
                return user;
            }
            else
            {
                // 增加登录失败次数
                await IncrementLoginAttemptsAsync(user.Id);
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
    public async Task<ApiResponse<UserStorageDto>> RegisterUserAsync(string username, string password, string email = "")
    {
        try
        {
            // 验证输入
            var validationResult = ValidateRegistrationInput(username, password, email);
            if (!validationResult.Success)
                return validationResult;

            // 创建用户DTO
            var userDto = new UserStorageDto
            {
                Username = username.Trim(),
                Email = email.Trim().ToLowerInvariant(),
                IsActive = true,
                EmailVerified = false,
                Roles = new List<string> { "Player" }
            };

            // 创建用户
            var result = await _dataStorage.CreateUserAsync(userDto, password);
            if (result.Success)
            {
                _logger.LogInformation("User registered successfully: {Username}", username);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user: {Username}", username);
            return new ApiResponse<UserStorageDto>
            {
                Success = false,
                Message = "用户注册失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    public async Task<UserStorageDto?> GetUserByIdAsync(string userId)
    {
        return await _dataStorage.GetUserByIdAsync(userId);
    }

    /// <summary>
    /// 更新用户最后登录信息
    /// </summary>
    public async Task<bool> UpdateLastLoginAsync(string userId, string ipAddress)
    {
        var result = await _dataStorage.UpdateUserLastLoginAsync(userId, ipAddress);
        return result.Success;
    }

    /// <summary>
    /// 检查用户是否拥有指定角色
    /// </summary>
    public async Task<bool> UserHasRoleAsync(string userId, string role)
    {
        var user = await _dataStorage.GetUserByIdAsync(userId);
        return user?.Roles.Contains(role, StringComparer.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// 检查用户是否拥有角色（为了兼容现有代码）
    /// </summary>
    public async Task<bool> UserHasCharacterAsync(string userId, string characterId)
    {
        var user = await _dataStorage.GetUserByIdAsync(userId);
        if (user == null) return false;

        // 管理员可以访问任何角色
        if (user.Roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            return true;

        // 简单的角色检查 - 用户可以访问自己ID开头的角色
        return characterId.StartsWith(userId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 增加登录尝试次数，如果超过限制则锁定账户
    /// </summary>
    private async Task IncrementLoginAttemptsAsync(string userId)
    {
        try
        {
            var user = await _dataStorage.GetUserByIdAsync(userId);
            if (user != null)
            {
                user.LoginAttempts++;
                
                // 如果登录失败次数超过5次，锁定账户30分钟
                if (user.LoginAttempts >= 5)
                {
                    await _dataStorage.LockUserAccountAsync(userId, DateTime.UtcNow.AddMinutes(30));
                    _logger.LogWarning("User account locked due to too many failed login attempts: {UserId}", userId);
                }
                else
                {
                    await _dataStorage.UpdateUserAsync(user);
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
    private ApiResponse<UserStorageDto> ValidateRegistrationInput(string username, string password, string email)
    {
        // 验证用户名
        if (string.IsNullOrWhiteSpace(username))
        {
            return new ApiResponse<UserStorageDto>
            {
                Success = false,
                Message = "用户名不能为空"
            };
        }

        if (username.Length < 3 || username.Length > 20)
        {
            return new ApiResponse<UserStorageDto>
            {
                Success = false,
                Message = "用户名长度必须在3-20个字符之间"
            };
        }

        if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
        {
            return new ApiResponse<UserStorageDto>
            {
                Success = false,
                Message = "用户名只能包含字母、数字和下划线"
            };
        }

        // 验证密码
        if (string.IsNullOrWhiteSpace(password))
        {
            return new ApiResponse<UserStorageDto>
            {
                Success = false,
                Message = "密码不能为空"
            };
        }

        if (password.Length < 6)
        {
            return new ApiResponse<UserStorageDto>
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
                return new ApiResponse<UserStorageDto>
                {
                    Success = false,
                    Message = "邮箱格式不正确"
                };
            }
        }

        return new ApiResponse<UserStorageDto> { Success = true };
    }
}