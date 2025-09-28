using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// 统一数据存储服务接口 - 重构后的高性能数据存储层
/// 支持事务、缓存一致性、批量操作和异步处理
/// </summary>
public interface IUnifiedDataStorageService : IDisposable
{
    #region 核心存储操作

    /// <summary>
    /// 获取实体 - 支持泛型和强类型查询
    /// </summary>
    Task<T?> GetAsync<T>(string id, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 保存实体 - 支持泛型和自动时间戳更新
    /// </summary>
    Task<ApiResponse<T>> SaveAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 删除实体 - 支持级联删除和软删除
    /// </summary>
    Task<ApiResponse<bool>> DeleteAsync<T>(string id, bool softDelete = false, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 批量保存 - 支持事务性批量操作
    /// </summary>
    Task<BatchOperationResponseDto<T>> SaveBatchAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;

    #endregion

    #region 查询操作

    /// <summary>
    /// 条件查询 - 支持复杂查询条件和分页
    /// </summary>
    Task<ApiResponse<List<T>>> QueryAsync<T>(
        QuerySpecification<T> specification,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 计数查询 - 获取满足条件的记录数量
    /// </summary>
    Task<ApiResponse<int>> CountAsync<T>(
        QuerySpecification<T> specification,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 存在性检查 - 快速检查记录是否存在
    /// </summary>
    Task<bool> ExistsAsync<T>(string id, CancellationToken cancellationToken = default) where T : class;

    #endregion

    #region 事务支持

    /// <summary>
    /// 开始事务 - 支持嵌套事务和回滚
    /// </summary>
    Task<IDataTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行事务性操作 - 自动管理事务生命周期
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<IDataTransaction, Task<TResult>> operation,
        CancellationToken cancellationToken = default);

    #endregion

    #region 缓存管理

    /// <summary>
    /// 强制刷新缓存 - 确保数据一致性
    /// </summary>
    Task InvalidateCacheAsync<T>(string id, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 预热缓存 - 提前加载热点数据
    /// </summary>
    Task WarmupCacheAsync<T>(IEnumerable<string> ids, CancellationToken cancellationToken = default) where T : class;

    #endregion

    #region 监控和统计

    /// <summary>
    /// 获取性能统计 - 监控存储层性能
    /// </summary>
    Task<ApiResponse<StoragePerformanceStats>> GetPerformanceStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 健康检查 - 全面的存储层健康状态检查
    /// </summary>
    Task<ApiResponse<StorageHealthStatus>> HealthCheckAsync(CancellationToken cancellationToken = default);

    #endregion

    #region 数据迁移和维护

    /// <summary>
    /// 数据迁移 - 支持版本化数据迁移
    /// </summary>
    Task<ApiResponse<bool>> MigrateDataAsync(int targetVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// 数据备份 - 创建完整数据备份
    /// </summary>
    Task<ApiResponse<string>> CreateBackupAsync(string backupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 数据清理 - 清理过期和冗余数据
    /// </summary>
    Task<ApiResponse<DataCleanupResult>> CleanupDataAsync(
        DataCleanupOptions options,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// 数据事务接口 - 支持ACID事务操作
/// </summary>
public interface IDataTransaction : IDisposable
{
    /// <summary>
    /// 事务ID
    /// </summary>
    string TransactionId { get; }

    /// <summary>
    /// 提交事务
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚事务
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 在事务中执行操作
    /// </summary>
    Task<T?> GetAsync<T>(string id, CancellationToken cancellationToken = default) where T : class;
    Task<ApiResponse<T>> SaveAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
    Task<ApiResponse<bool>> DeleteAsync<T>(string id, CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// 查询规范 - 支持复杂查询条件构建
/// </summary>
public class QuerySpecification<T> where T : class
{
    public List<QueryFilter> Filters { get; set; } = new();
    public List<QuerySort> SortBy { get; set; } = new();
    public int? Skip { get; set; }
    public int? Take { get; set; }
    public bool IncludeDeleted { get; set; } = false;
    public List<string> IncludeRelations { get; set; } = new();
}

/// <summary>
/// 查询过滤器
/// </summary>
public class QueryFilter
{
    public string PropertyName { get; set; } = string.Empty;
    public QueryOperator Operator { get; set; }
    public object? Value { get; set; }
    public QueryLogic Logic { get; set; } = QueryLogic.And;
}

/// <summary>
/// 查询操作符
/// </summary>
public enum QueryOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    StartsWith,
    EndsWith,
    In,
    NotIn,
    IsNull,
    IsNotNull
}

/// <summary>
/// 查询逻辑关系
/// </summary>
public enum QueryLogic
{
    And,
    Or
}

/// <summary>
/// 查询排序
/// </summary>
public class QuerySort
{
    public string PropertyName { get; set; } = string.Empty;
    public SortDirection Direction { get; set; } = SortDirection.Ascending;
}

/// <summary>
/// 排序方向
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}

/// <summary>
/// 存储性能统计
/// </summary>
public class StoragePerformanceStats
{
    public Dictionary<string, long> OperationCounts { get; set; } = new();
    public Dictionary<string, double> AverageResponseTimes { get; set; } = new();
    public Dictionary<string, long> CacheHitRates { get; set; } = new();
    public int ActiveConnections { get; set; }
    public int PendingOperations { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 存储健康状态
/// </summary>
public class StorageHealthStatus
{
    public bool IsHealthy { get; set; }
    public List<string> Issues { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 数据清理选项
/// </summary>
public class DataCleanupOptions
{
    public TimeSpan DataRetentionPeriod { get; set; } = TimeSpan.FromDays(90);
    public bool CleanupBattleRecords { get; set; } = true;
    public bool CleanupOfflineData { get; set; } = true;
    public bool CleanupActionHistory { get; set; } = true;
    public bool VacuumDatabase { get; set; } = false;
    public int BatchSize { get; set; } = 1000;
}

/// <summary>
/// 数据清理结果
/// </summary>
public class DataCleanupResult
{
    public int RecordsDeleted { get; set; }
    public Dictionary<string, int> DeletedByType { get; set; } = new();
    public TimeSpan ElapsedTime { get; set; }
    public long FreedSpaceBytes { get; set; }
    public List<string> Errors { get; set; } = new();
}