using BlazorWebGame.Shared.Interfaces;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// 数据存储服务工厂接口
/// </summary>
public interface IDataStorageServiceFactory
{
    /// <summary>
    /// 创建数据存储服务实例
    /// </summary>
    /// <param name="storageType">存储类型：Memory, SQLite, SqlServer</param>
    /// <returns>数据存储服务实例</returns>
    IDataStorageService CreateDataStorageService(string storageType);
    
    /// <summary>
    /// 获取支持的存储类型列表
    /// </summary>
    /// <returns>支持的存储类型</returns>
    IEnumerable<string> GetSupportedStorageTypes();
    
    /// <summary>
    /// 验证存储类型是否支持
    /// </summary>
    /// <param name="storageType">存储类型</param>
    /// <returns>是否支持</returns>
    bool IsStorageTypeSupported(string storageType);
    
    /// <summary>
    /// 获取数据存储服务的健康状态
    /// </summary>
    /// <param name="storageType">存储类型</param>
    /// <returns>健康状态信息</returns>
    Task<Dictionary<string, object>> GetStorageHealthAsync(string storageType);
}