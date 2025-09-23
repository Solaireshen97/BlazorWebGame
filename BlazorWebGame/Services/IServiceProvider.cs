namespace BlazorWebGame.Services
{
    /// <summary>
    /// 服务提供者接口，用于服务间依赖访问
    /// </summary>
    public interface IServiceProvider
    {
        /// <summary>
        /// 获取服务实例
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例，如不存在返回null</returns>
        T? GetService<T>() where T : class;
        
        /// <summary>
        /// 获取服务实例，如不存在则抛出异常
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        T GetRequiredService<T>() where T : class;
    }
    
    /// <summary>
    /// 服务提供者的默认实现，使用ServiceLocator
    /// </summary>
    public class ServiceProvider : IServiceProvider
    {
        public T? GetService<T>() where T : class => ServiceLocator.GetService<T>();
        
        public T GetRequiredService<T>() where T : class => ServiceLocator.GetRequiredService<T>();
    }
}