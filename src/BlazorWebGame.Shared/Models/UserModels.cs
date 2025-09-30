using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 用户领域模型 - 表示系统用户账户
/// </summary>
public class User
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public bool EmailVerified { get; private set; } = false;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; private set; } = DateTime.MinValue;
    public string LastLoginIp { get; private set; } = string.Empty;
    public UserProfile Profile { get; private set; } = new();
    public UserSecurity Security { get; private set; } = new();
    
    // 用户拥有的角色列表
    private readonly List<string> _characterIds = new();
    public IReadOnlyList<string> CharacterIds => _characterIds.AsReadOnly();

    // 私有构造函数，用于反序列化
    private User() { }

    /// <summary>
    /// 创建新用户
    /// </summary>
    public User(string username, string email)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("用户名不能为空", nameof(username));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("邮箱不能为空", nameof(email));

        Username = username.Trim();
        Email = email.Trim().ToLowerInvariant();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    public void UpdateProfile(string? displayName = null, string? avatar = null)
    {
        Profile.UpdateProfile(displayName, avatar);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 记录登录
    /// </summary>
    public void RecordLogin(string ipAddress)
    {
        LastLoginAt = DateTime.UtcNow;
        LastLoginIp = ipAddress ?? string.Empty;
        Security.RecordSuccessfulLogin();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 记录登录失败
    /// </summary>
    public void RecordFailedLogin()
    {
        Security.RecordFailedLogin();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 验证邮箱
    /// </summary>
    public void VerifyEmail()
    {
        EmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 激活用户
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 停用用户
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 添加角色
    /// </summary>
    public void AddCharacter(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            throw new ArgumentException("角色ID不能为空", nameof(characterId));
        
        if (!_characterIds.Contains(characterId))
        {
            _characterIds.Add(characterId);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 移除角色
    /// </summary>
    public void RemoveCharacter(string characterId)
    {
        if (_characterIds.Remove(characterId))
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 检查是否拥有角色
    /// </summary>
    public bool HasCharacter(string characterId)
    {
        return _characterIds.Contains(characterId);
    }
}

/// <summary>
/// 用户档案信息
/// </summary>
public class UserProfile
{
    public string DisplayName { get; private set; } = string.Empty;
    public string Avatar { get; private set; } = string.Empty;
    public Dictionary<string, object> CustomProperties { get; private set; } = new();

    /// <summary>
    /// 更新档案信息
    /// </summary>
    public void UpdateProfile(string? displayName = null, string? avatar = null)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            DisplayName = displayName.Trim();
        if (!string.IsNullOrWhiteSpace(avatar))
            Avatar = avatar.Trim();
    }

    /// <summary>
    /// 设置自定义属性
    /// </summary>
    public void SetCustomProperty(string key, object value)
    {
        CustomProperties[key] = value;
    }

    /// <summary>
    /// 获取自定义属性
    /// </summary>
    public T? GetCustomProperty<T>(string key, T? defaultValue = default)
    {
        if (CustomProperties.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return defaultValue;
    }
}

/// <summary>
/// 用户安全信息
/// </summary>
public class UserSecurity
{
    public List<string> Roles { get; private set; } = new() { "Player" };
    public int LoginAttempts { get; private set; } = 0;
    public DateTime? LockedUntil { get; private set; }
    public DateTime? LastPasswordChange { get; private set; }
    public List<string> LoginHistory { get; private set; } = new();

    private const int MaxLoginAttempts = 5;
    private const int LockoutMinutes = 30;
    private const int MaxLoginHistoryCount = 10;

    /// <summary>
    /// 记录成功登录
    /// </summary>
    public void RecordSuccessfulLogin()
    {
        LoginAttempts = 0;
        LockedUntil = null;
        
        var loginRecord = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - Success";
        LoginHistory.Insert(0, loginRecord);
        
        if (LoginHistory.Count > MaxLoginHistoryCount)
            LoginHistory.RemoveAt(LoginHistory.Count - 1);
    }

    /// <summary>
    /// 记录失败登录
    /// </summary>
    public void RecordFailedLogin()
    {
        LoginAttempts++;
        
        if (LoginAttempts >= MaxLoginAttempts)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
        }
        
        var loginRecord = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - Failed";
        LoginHistory.Insert(0, loginRecord);
        
        if (LoginHistory.Count > MaxLoginHistoryCount)
            LoginHistory.RemoveAt(LoginHistory.Count - 1);
    }

    /// <summary>
    /// 检查账户是否被锁定
    /// </summary>
    public bool IsLocked()
    {
        return LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// 解锁账户
    /// </summary>
    public void Unlock()
    {
        LoginAttempts = 0;
        LockedUntil = null;
    }

    /// <summary>
    /// 添加角色
    /// </summary>
    public void AddRole(string role)
    {
        if (!string.IsNullOrWhiteSpace(role) && !Roles.Contains(role))
            Roles.Add(role);
    }

    /// <summary>
    /// 移除角色
    /// </summary>
    public void RemoveRole(string role)
    {
        Roles.Remove(role);
    }

    /// <summary>
    /// 检查是否拥有角色
    /// </summary>
    public bool HasRole(string role)
    {
        return Roles.Contains(role);
    }

    /// <summary>
    /// 更新密码（记录时间）
    /// </summary>
    public void UpdatePassword()
    {
        LastPasswordChange = DateTime.UtcNow;
        LoginAttempts = 0;
        LockedUntil = null;
    }
}

/// <summary>
/// 用户角色关联领域模型
/// </summary>
public class UserCharacterRelation
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string UserId { get; private set; } = string.Empty;
    public string CharacterId { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public bool IsDefault { get; private set; } = false;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime LastPlayedAt { get; private set; } = DateTime.UtcNow;

    // 私有构造函数，用于反序列化
    private UserCharacterRelation() { }

    /// <summary>
    /// 创建用户角色关联
    /// </summary>
    public UserCharacterRelation(string userId, string characterId, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("用户ID不能为空", nameof(userId));
        if (string.IsNullOrWhiteSpace(characterId))
            throw new ArgumentException("角色ID不能为空", nameof(characterId));

        UserId = userId;
        CharacterId = characterId;
        IsDefault = isDefault;
        CreatedAt = DateTime.UtcNow;
        LastPlayedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置为默认角色
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
    }

    /// <summary>
    /// 取消默认角色
    /// </summary>
    public void UnsetAsDefault()
    {
        IsDefault = false;
    }

    /// <summary>
    /// 激活关联
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// 停用关联
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// 更新游戏时间
    /// </summary>
    public void UpdateLastPlayed()
    {
        LastPlayedAt = DateTime.UtcNow;
    }


    /// <summary>
    /// 用户工厂类 - 用于创建用户实例的特殊情况
    /// </summary>
    internal static class UserFactory
    {
        /// <summary>
        /// 从存储数据创建用户
        /// </summary>
        internal static User CreateFromStorage(
            string id,
            string username,
            string email,
            bool isActive,
            bool emailVerified,
            DateTime createdAt,
            DateTime updatedAt)
        {
            // 创建用户实例
            var user = new User(username, email);

            // 反射设置私有字段
            var idField = typeof(User).GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (idField != null) idField.SetValue(user, id);

            var createdAtField = typeof(User).GetField("_createdAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (createdAtField != null) createdAtField.SetValue(user, createdAt);

            var updatedAtField = typeof(User).GetField("_updatedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (updatedAtField != null) updatedAtField.SetValue(user, updatedAt);

            // 设置状态
            if (isActive) user.Activate(); else user.Deactivate();
            if (emailVerified) user.VerifyEmail();

            return user;
        }

        /// <summary>
        /// 设置最后登录信息
        /// </summary>
        internal static void SetLastLoginInfo(User user, DateTime lastLoginAt, string lastLoginIp)
        {
            var lastLoginAtField = typeof(User).GetField("_lastLoginAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (lastLoginAtField != null) lastLoginAtField.SetValue(user, lastLoginAt);

            var lastLoginIpField = typeof(User).GetField("_lastLoginIp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (lastLoginIpField != null) lastLoginIpField.SetValue(user, lastLoginIp);
        }
    }
}