using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 配置热更新管理器
/// </summary>
public class ConfigHotReloadManager
{
    private ConfigBundle _currentBundle = new();
    private readonly Dictionary<string, ConfigBundle> _versionCache = new();
    private readonly VersionPin _versionPin = new();
    private readonly List<IConfigChangeListener> _listeners = new();

    /// <summary>
    /// 加载新配置
    /// </summary>
    public async Task<ConfigLoadResult> LoadConfig(ConfigBundle newBundle)
    {
        var result = new ConfigLoadResult();

        try
        {
            // 验证配置完整性
            if (!newBundle.VerifyIntegrity())
            {
                result.Success = false;
                result.Error = "配置完整性验证失败";
                return result;
            }

            // 执行验证
            var validation = await ValidateConfig(newBundle);
            if (!validation.IsValid)
            {
                result.Success = false;
                result.Error = string.Join(", ", validation.Errors);
                return result;
            }

            // 检测变更
            var changes = DetectChanges(_currentBundle, newBundle);
            result.Changes = changes;

            // 备份旧版本
            _versionCache[_currentBundle.Version] = _currentBundle;

            // 应用新配置
            _currentBundle = newBundle;

            // 通知监听器
            await NotifyListeners(changes);

            result.Success = true;
            result.Version = newBundle.Version;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 验证配置
    /// </summary>
    private async Task<ConfigValidation> ValidateConfig(ConfigBundle bundle)
    {
        var validation = new ConfigValidation();

        // 验证技能引用
        await Task.Run(() =>
        {
            // TODO: 实现引用完整性检查
            // 检查技能ID引用、物品ID引用、区域ID引用等
        });

        return validation;
    }

    /// <summary>
    /// 检测配置变更
    /// </summary>
    private ConfigChanges DetectChanges(ConfigBundle oldBundle, ConfigBundle newBundle)
    {
        var changes = new ConfigChanges();

        foreach (var module in newBundle.Modules)
        {
            if (!oldBundle.Modules.ContainsKey(module.Key))
            {
                changes.AddedModules.Add(module.Key);
            }
            else if (oldBundle.Modules[module.Key].Hash != module.Value.Hash)
            {
                changes.ModifiedModules.Add(module.Key);
            }
        }

        foreach (var module in oldBundle.Modules)
        {
            if (!newBundle.Modules.ContainsKey(module.Key))
            {
                changes.RemovedModules.Add(module.Key);
            }
        }

        return changes;
    }

    /// <summary>
    /// 通知监听器
    /// </summary>
    private async Task NotifyListeners(ConfigChanges changes)
    {
        var tasks = _listeners.Select(l => l.OnConfigChanged(changes));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 获取固定版本的配置
    /// </summary>
    public ConfigBundle? GetPinnedConfig(Guid instanceId)
    {
        var version = _versionPin.GetPinnedVersion(instanceId);
        if (version == null)
            return _currentBundle;

        return _versionCache.GetValueOrDefault(version) ?? _currentBundle;
    }
}

/// <summary>
/// 配置加载结果
/// </summary>
public class ConfigLoadResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string Version { get; set; } = string.Empty;
    public ConfigChanges? Changes { get; set; }
}

/// <summary>
/// 配置验证结果
/// </summary>
public class ConfigValidation
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// 配置变更
/// </summary>
public class ConfigChanges
{
    public List<string> AddedModules { get; set; } = new();
    public List<string> ModifiedModules { get; set; } = new();
    public List<string> RemovedModules { get; set; } = new();

    public bool HasChanges =>
        AddedModules.Count > 0 ||
        ModifiedModules.Count > 0 ||
        RemovedModules.Count > 0;
}

/// <summary>
/// 配置变更监听器
/// </summary>
public interface IConfigChangeListener
{
    Task OnConfigChanged(ConfigChanges changes);
}