using System.Diagnostics;
using System.Collections.Concurrent;
using BlazorWebGame.Server.Configuration;
using Microsoft.Extensions.Options;

namespace BlazorWebGame.Server.Services.GameSystem;

/// <summary>
/// 性能监控服务
/// </summary>
public class PerformanceMonitoringService : IDisposable
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly MonitoringOptions _options;
    private readonly ConcurrentDictionary<string, PerformanceMetrics> _operationMetrics = new();
    private readonly Timer? _reportingTimer;
    private readonly Process _currentProcess;
    private bool _disposed = false;

    public PerformanceMonitoringService(
        ILogger<PerformanceMonitoringService> logger,
        IOptions<MonitoringOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _currentProcess = Process.GetCurrentProcess();
        
        if (_options.EnableMetrics)
        {
            _reportingTimer = new Timer(ReportMetrics, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }
    }

    /// <summary>
    /// 监控操作性能
    /// </summary>
    public PerformanceTracker TrackOperation(string operationName)
    {
        return new PerformanceTracker(operationName, this, _options.SlowRequestThresholdMs);
    }

    /// <summary>
    /// 记录操作性能数据
    /// </summary>
    internal void RecordOperation(string operationName, long elapsedMs, bool isSuccess)
    {
        if (!_options.EnablePerformanceLogging) return;

        var metrics = _operationMetrics.GetOrAdd(operationName, _ => new PerformanceMetrics());
        
        metrics.TotalCalls++;
        metrics.TotalTimeMs += elapsedMs;
        metrics.MinTimeMs = Math.Min(metrics.MinTimeMs, elapsedMs);
        metrics.MaxTimeMs = Math.Max(metrics.MaxTimeMs, elapsedMs);
        
        if (isSuccess)
            metrics.SuccessfulCalls++;
        else
            metrics.FailedCalls++;

        if (elapsedMs > _options.SlowRequestThresholdMs)
        {
            _logger.LogWarning("Slow operation detected: {Operation} took {ElapsedMs}ms", 
                operationName, elapsedMs);
        }
    }

    /// <summary>
    /// 获取系统性能指标
    /// </summary>
    public SystemPerformanceSnapshot GetSystemMetrics()
    {
        _currentProcess.Refresh();
        
        return new SystemPerformanceSnapshot
        {
            Timestamp = DateTime.UtcNow,
            CpuUsagePercent = GetCpuUsage(),
            MemoryUsageMB = _currentProcess.WorkingSet64 / 1024 / 1024,
            ThreadCount = _currentProcess.Threads.Count,
            HandleCount = _currentProcess.HandleCount,
            GCGen0Collections = GC.CollectionCount(0),
            GCGen1Collections = GC.CollectionCount(1),
            GCGen2Collections = GC.CollectionCount(2),
            TotalMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024
        };
    }

    /// <summary>
    /// 获取操作性能统计
    /// </summary>
    public Dictionary<string, PerformanceMetrics> GetOperationMetrics()
    {
        return new Dictionary<string, PerformanceMetrics>(_operationMetrics);
    }

    private double GetCpuUsage()
    {
        try
        {
            return _currentProcess.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / 1000.0 * 100.0;
        }
        catch
        {
            return 0.0;
        }
    }

    private void ReportMetrics(object? state)
    {
        try
        {
            var systemMetrics = GetSystemMetrics();
            var operationMetrics = GetOperationMetrics();

            _logger.LogInformation("System Performance: CPU: {CpuUsage}%, Memory: {MemoryMB}MB, Threads: {ThreadCount}", 
                systemMetrics.CpuUsagePercent, systemMetrics.MemoryUsageMB, systemMetrics.ThreadCount);

            foreach (var kvp in operationMetrics.Where(x => x.Value.TotalCalls > 0))
            {
                var metrics = kvp.Value;
                var avgTime = metrics.TotalTimeMs / (double)metrics.TotalCalls;
                
                _logger.LogInformation("Operation '{Operation}': Calls: {Calls}, Avg: {AvgMs}ms, Max: {MaxMs}ms, Success Rate: {SuccessRate}%",
                    kvp.Key, metrics.TotalCalls, avgTime.ToString("F1"), metrics.MaxTimeMs, 
                    ((double)metrics.SuccessfulCalls / metrics.TotalCalls * 100).ToString("F1"));
            }

            // 重置指标
            _operationMetrics.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting performance metrics");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _reportingTimer?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// 性能跟踪器
/// </summary>
public class PerformanceTracker : IDisposable
{
    private readonly string _operationName;
    private readonly PerformanceMonitoringService _service;
    private readonly Stopwatch _stopwatch;
    private readonly int _slowThresholdMs;
    private bool _disposed = false;
    private bool _isSuccess = true;

    internal PerformanceTracker(string operationName, PerformanceMonitoringService service, int slowThresholdMs)
    {
        _operationName = operationName;
        _service = service;
        _slowThresholdMs = slowThresholdMs;
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// 标记操作为失败
    /// </summary>
    public void MarkAsFailed()
    {
        _isSuccess = false;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _stopwatch.Stop();
            _service.RecordOperation(_operationName, _stopwatch.ElapsedMilliseconds, _isSuccess);
            _disposed = true;
        }
    }
}

/// <summary>
/// 性能指标
/// </summary>
public class PerformanceMetrics
{
    public long TotalCalls { get; set; }
    public long SuccessfulCalls { get; set; }
    public long FailedCalls { get; set; }
    public long TotalTimeMs { get; set; }
    public long MinTimeMs { get; set; } = long.MaxValue;
    public long MaxTimeMs { get; set; }
}

/// <summary>
/// 系统性能快照
/// </summary>
public class SystemPerformanceSnapshot
{
    public DateTime Timestamp { get; set; }
    public double CpuUsagePercent { get; set; }
    public long MemoryUsageMB { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public int GCGen0Collections { get; set; }
    public int GCGen1Collections { get; set; }
    public int GCGen2Collections { get; set; }
    public long TotalMemoryMB { get; set; }
}
