using System;
using System.Collections.Generic;

namespace BlazorWebGame.Services
{
    /// <summary>
    /// 服务定位器，用于获取服务实例的统一入口
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();
        private static bool _isInitialized = false;

        /// <summary>
        /// 初始化服务定位器
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
                throw new InvalidOperationException("ServiceLocator已经初始化");
                
            _services.Clear();
            _isInitialized = true;
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <param name="service">服务实例</param>
        /// <param name="overwrite">是否覆盖已有注册</param>
        /// <exception cref="ArgumentNullException">服务实例为null</exception>
        /// <exception cref="InvalidOperationException">服务已注册且不允许覆盖</exception>
        public static void RegisterService<T>(T service, bool overwrite = false) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service), "不能注册null服务");

            var type = typeof(T);
            if (_services.ContainsKey(type) && !overwrite)
                throw new InvalidOperationException($"服务类型 {type.Name} 已注册，如需覆盖请设置overwrite=true");

            _services[type] = service;
        }

        /// <summary>
        /// 获取已注册的服务
        /// </summary>
        /// <typeparam name="T">要获取的服务类型</typeparam>
        /// <returns>服务实例，如未注册则返回null</returns>
        public static T? GetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return (T)service;
                
            return null;
        }
        
        /// <summary>
        /// 获取已注册的服务，如未注册则抛出异常
        /// </summary>
        /// <typeparam name="T">要获取的服务类型</typeparam>
        /// <returns>服务实例</returns>
        /// <exception cref="InvalidOperationException">请求的服务未注册</exception>
        public static T GetRequiredService<T>() where T : class
        {
            var service = GetService<T>();
            if (service == null)
                throw new InvalidOperationException($"服务 {typeof(T).Name} 未注册");
                
            return service;
        }
        
        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>是否已注册</returns>
        public static bool IsRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// 移除注册的服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>是否成功移除</returns>
        public static bool RemoveService<T>() where T : class
        {
            return _services.Remove(typeof(T));
        }
        
        /// <summary>
        /// 清除所有注册的服务
        /// </summary>
        public static void ClearAllServices()
        {
            _services.Clear();
        }
    }
}