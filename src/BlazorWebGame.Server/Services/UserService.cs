using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using BlazorWebGame.Server.Data;
using BlazorWebGame.Shared.Models;
using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 用户服务 - 处理用户注册、登录和管理
/// </summary>
public interface IUserService
{
    Task<UserEntity?> GetByIdAsync(string userId);
    Task<UserEntity?> GetByUsernameAsync(string username);
    Task<UserEntity?> GetByEmailAsync(string email);
    Task<UserEntity?> ValidateUserAsync(string username, string password);
    Task<UserEntity> CreateUserAsync(string username, string email, string password, List<string>? roles = null);
    Task<bool> UpdateUserAsync(UserEntity user);
    Task<bool> UpdateRefreshTokenAsync(string userId, string refreshToken, DateTime expiryTime);
    Task<bool> IsUsernameAvailableAsync(string username);
    Task<bool> IsEmailAvailableAsync(string email);
    Task<bool> DeactivateUserAsync(string userId);
    Task UpdateLastLoginAsync(string userId);
}

/// <summary>
/// 用户服务实现
/// </summary>
public class UserService : IUserService
{
    private readonly ConsolidatedGameDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(ConsolidatedGameDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    public async Task<UserEntity?> GetByIdAsync(string userId)
    {
        try
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by ID: {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    public async Task<UserEntity?> GetByUsernameAsync(string username)
    {
        try
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by username: {Username}", username);
            return null;
        }
    }

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    public async Task<UserEntity?> GetByEmailAsync(string email)
    {
        try
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
            return null;
        }
    }

    /// <summary>
    /// 验证用户凭据
    /// </summary>
    public async Task<UserEntity?> ValidateUserAsync(string username, string password)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("User not found: {Username}", username);
                return null;
            }

            if (VerifyPassword(password, user.PasswordHash, user.Salt))
            {
                _logger.LogInformation("User validated successfully: {Username}", username);
                return user;
            }

            _logger.LogWarning("Invalid password for user: {Username}", username);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user: {Username}", username);
            return null;
        }
    }

    /// <summary>
    /// 创建新用户
    /// </summary>
    public async Task<UserEntity> CreateUserAsync(string username, string email, string password, List<string>? roles = null)
    {
        try
        {
            var salt = GenerateSalt();
            var passwordHash = HashPassword(password, salt);
            var roleList = roles ?? new List<string> { "Player" };

            var user = new UserEntity
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                Salt = salt,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                IsActive = true,
                Roles = JsonSerializer.Serialize(roleList)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User created successfully: {Username} (ID: {UserId})", username, user.Id);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Username}", username);
            throw;
        }
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    public async Task<bool> UpdateUserAsync(UserEntity user)
    {
        try
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("User updated successfully: {UserId}", user.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", user.Id);
            return false;
        }
    }

    /// <summary>
    /// 更新刷新令牌
    /// </summary>
    public async Task<bool> UpdateRefreshTokenAsync(string userId, string refreshToken, DateTime expiryTime)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = expiryTime;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating refresh token for user: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 检查用户名是否可用
    /// </summary>
    public async Task<bool> IsUsernameAvailableAsync(string username)
    {
        try
        {
            return !await _context.Users.AnyAsync(u => u.Username == username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking username availability: {Username}", username);
            return false;
        }
    }

    /// <summary>
    /// 检查邮箱是否可用
    /// </summary>
    public async Task<bool> IsEmailAvailableAsync(string email)
    {
        try
        {
            return !await _context.Users.AnyAsync(u => u.Email == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email availability: {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// 停用用户
    /// </summary>
    public async Task<bool> DeactivateUserAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User deactivated: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 更新最后登录时间
    /// </summary>
    public async Task UpdateLastLoginAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login time for user: {UserId}", userId);
        }
    }

    /// <summary>
    /// 生成随机盐值
    /// </summary>
    private static string GenerateSalt()
    {
        var saltBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        return Convert.ToBase64String(saltBytes);
    }

    /// <summary>
    /// 哈希密码
    /// </summary>
    private static string HashPassword(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        
        var combined = new byte[saltBytes.Length + passwordBytes.Length];
        Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
        Buffer.BlockCopy(passwordBytes, 0, combined, saltBytes.Length, passwordBytes.Length);

        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(combined);
            return Convert.ToBase64String(hashBytes);
        }
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    private static bool VerifyPassword(string password, string passwordHash, string salt)
    {
        var computedHash = HashPassword(password, salt);
        return computedHash == passwordHash;
    }
}