using System;
using System.Collections.Generic;

namespace BlazorWebGame.Rebuild.Services.Core;

/// <summary>
/// 服务端服务定位器 - 提供对DI容器的轻量级访问
/// </summary>
public class ServerServiceLocator
{
    private static IServiceProvider? _serviceProvider;
    private static readonly Dictionary<Type, object> _singletonCache = new();
    private static bool _isInitialized = false;

    /// <summary>
    /// 初始化服务定位器
    /// </summary>
    /// <param name="serviceProvider">DI容器服务提供者</param>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        if (_isInitialized)
            throw new InvalidOperationException("ServerServiceLocator已经初始化");
            
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _singletonCache.Clear();
        _isInitialized = true;
    }

    /// <summary>
    /// 获取已注册的服务
    /// </summary>
    /// <typeparam name="T">要获取的服务类型</typeparam>
    /// <returns>服务实例，如果未注册则返回null</returns>
    public static T? GetService<T>() where T : class
    {
        if (!_isInitialized || _serviceProvider == null)
            throw new InvalidOperationException("ServiceLocator尚未初始化");

        try
        {
            return _serviceProvider.GetService(typeof(T)) as T;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// 获取已注册的服务，如果未注册则抛出异常
    /// </summary>
    /// <typeparam name="T">要获取的服务类型</typeparam>
    /// <returns>服务实例</returns>
    /// <exception cref="InvalidOperationException">请求的服务未注册</exception>
    public static T GetRequiredService<T>() where T : class
    {
        if (!_isInitialized || _serviceProvider == null)
            throw new InvalidOperationException("ServiceLocator尚未初始化");

        var service = _serviceProvider.GetRequiredService(typeof(T)) as T;
        if (service == null)
            throw new InvalidOperationException($"服务 {typeof(T).Name} 未注册或无法转换");
            
        return service;
    }
    
    /// <summary>
    /// 检查服务是否已注册
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>是否已注册</returns>
    public static bool IsRegistered<T>() where T : class
    {
        if (!_isInitialized || _serviceProvider == null)
            return false;

        try
        {
            return _serviceProvider.GetService(typeof(T)) != null;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 获取单例服务并缓存
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>单例服务实例</returns>
    public static T GetSingleton<T>() where T : class
    {
        var type = typeof(T);
        
        if (_singletonCache.TryGetValue(type, out var cachedService))
        {
            return (T)cachedService;
        }
        
        var service = GetRequiredService<T>();
        _singletonCache[type] = service;
        return service;
    }

    /// <summary>
    /// 创建服务的新实例（即使是单例也会创建新实例）
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务的新实例</returns>
    public static T CreateInstance<T>() where T : class
    {
        if (!_isInitialized || _serviceProvider == null)
            throw new InvalidOperationException("ServiceLocator尚未初始化");

        // 使用ActivatorUtilities创建新实例，即使服务是单例注册的
        return ActivatorUtilities.CreateInstance<T>(_serviceProvider);
    }

    /// <summary>
    /// 获取所有指定类型的服务实例
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例集合</returns>
    public static IEnumerable<T> GetServices<T>() where T : class
    {
        if (!_isInitialized || _serviceProvider == null)
            throw new InvalidOperationException("ServiceLocator尚未初始化");

        return _serviceProvider.GetServices(typeof(T)).Cast<T>();
    }

    /// <summary>
    /// 清理缓存
    /// </summary>
    public static void ClearCache()
    {
        _singletonCache.Clear();
    }

    /// <summary>
    /// 重置服务定位器
    /// </summary>
    public static void Reset()
    {
        _serviceProvider = null;
        _singletonCache.Clear();
        _isInitialized = false;
    }

    /// <summary>
    /// 检查是否已初始化
    /// </summary>
    public static bool IsInitialized => _isInitialized;
}