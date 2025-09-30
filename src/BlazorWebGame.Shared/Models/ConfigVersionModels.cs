using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 配置包
/// </summary>
public class ConfigBundle
{
    public string Version { get; set; } = "1.0.0";
    public string Hash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, ConfigModule> Modules { get; set; } = new();

    /// <summary>
    /// 计算哈希
    /// </summary>
    public void CalculateHash()
    {
        var json = JsonSerializer.Serialize(Modules);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        Hash = Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// 验证完整性
    /// </summary>
    public bool VerifyIntegrity()
    {
        var originalHash = Hash;
        CalculateHash();
        var isValid = Hash == originalHash;
        Hash = originalHash; // 恢复原值
        return isValid;
    }
}

/// <summary>
/// 配置模块
/// </summary>
public class ConfigModule
{
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 版本固定管理
/// </summary>
public class VersionPin
{
    private readonly Dictionary<Guid, string> _pinnedVersions = new();

    /// <summary>
    /// 固定实例版本
    /// </summary>
    public void Pin(Guid instanceId, string version)
    {
        _pinnedVersions[instanceId] = version;
    }

    /// <summary>
    /// 获取固定版本
    /// </summary>
    public string? GetPinnedVersion(Guid instanceId)
    {
        return _pinnedVersions.GetValueOrDefault(instanceId);
    }

    /// <summary>
    /// 释放版本固定
    /// </summary>
    public void Unpin(Guid instanceId)
    {
        _pinnedVersions.Remove(instanceId);
    }
}

/// <summary>
/// 配置迁移
/// </summary>
public abstract class ConfigMigration
{
    public string FromVersion { get; protected set; } = string.Empty;
    public string ToVersion { get; protected set; } = string.Empty;

    public abstract void Migrate(ConfigBundle bundle);
}