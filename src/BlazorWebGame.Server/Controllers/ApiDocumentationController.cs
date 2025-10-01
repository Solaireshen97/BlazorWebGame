using Microsoft.AspNetCore.Mvc;
using BlazorWebGame.Shared.DTOs;
using System.Reflection;
using BlazorWebGame.Server.Services.System;

namespace BlazorWebGame.Server.Controllers;

/// <summary>
/// API文档和服务器功能展示控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ApiDocumentationController : ControllerBase
{
    private readonly ILogger<ApiDocumentationController> _logger;
    private readonly PerformanceMonitoringService _performanceService;

    public ApiDocumentationController(
        ILogger<ApiDocumentationController> logger,
        PerformanceMonitoringService performanceService)
    {
        _logger = logger;
        _performanceService = performanceService;
    }

    /// <summary>
    /// 获取服务器API概述和功能列表
    /// </summary>
    [HttpGet("overview")]
    public ActionResult<ApiResponse<ServerApiOverview>> GetApiOverview()
    {
        using var tracker = _performanceService.TrackOperation("GetApiOverview");

        var overview = new ServerApiOverview
        {
            ServerName = "BlazorWebGame Server",
            Version = GetServerVersion(),
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            StartTime = GetServerStartTime(),
            ApiEndpoints = GetAvailableEndpoints(),
            FeatureStatus = GetServerFeatureStatus(),
            ServerCapabilities = GetServerCapabilities()
        };

        return Ok(new ApiResponse<ServerApiOverview>
        {
            Success = true,
            Data = overview,
            Message = "API概述获取成功"
        });
    }

    /// <summary>
    /// 获取详细的API端点列表
    /// </summary>
    [HttpGet("endpoints")]
    public ActionResult<ApiResponse<List<ApiEndpointInfo>>> GetApiEndpoints()
    {
        using var tracker = _performanceService.TrackOperation("GetApiEndpoints");

        var endpoints = GetAvailableEndpoints();

        return Ok(new ApiResponse<List<ApiEndpointInfo>>
        {
            Success = true,
            Data = endpoints,
            Message = $"找到 {endpoints.Count} 个API端点"
        });
    }

    /// <summary>
    /// 获取服务器功能状态
    /// </summary>
    [HttpGet("features")]
    public ActionResult<ApiResponse<Dictionary<string, FeatureStatus>>> GetFeatureStatus()
    {
        using var tracker = _performanceService.TrackOperation("GetFeatureStatus");

        var features = GetServerFeatureStatus();

        return Ok(new ApiResponse<Dictionary<string, FeatureStatus>>
        {
            Success = true,
            Data = features,
            Message = "功能状态获取成功"
        });
    }

    /// <summary>
    /// 获取服务器详细信息
    /// </summary>
    [HttpGet("server-info")]
    public ActionResult<ApiResponse<ServerDetailInfo>> GetServerInfo()
    {
        using var tracker = _performanceService.TrackOperation("GetServerInfo");

        var serverInfo = new ServerDetailInfo
        {
            Version = GetServerVersion(),
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            StartTime = GetServerStartTime(),
            Uptime = DateTime.UtcNow - GetServerStartTime(),
            MachineName = Environment.MachineName,
            ProcessorCount = Environment.ProcessorCount,
            WorkingSet = GC.GetTotalMemory(false),
            GCInfo = new GarbageCollectionInfo
            {
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                TotalMemory = GC.GetTotalMemory(false)
            }
        };

        return Ok(new ApiResponse<ServerDetailInfo>
        {
            Success = true,
            Data = serverInfo,
            Message = "服务器详细信息获取成功"
        });
    }

    private string GetServerVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0.0";
    }

    private DateTime GetServerStartTime()
    {
        // 简化的启动时间计算，实际应用中可以使用更精确的方式
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        return currentProcess.StartTime;
    }

    private List<ApiEndpointInfo> GetAvailableEndpoints()
    {
        return new List<ApiEndpointInfo>
        {
            // 认证相关
            new ApiEndpointInfo
            {
                Category = "认证",
                Method = "POST",
                Path = "/api/auth/demo-login",
                Description = "演示用户登录，获取JWT令牌",
                RequiresAuth = false
            },

            // 角色管理
            new ApiEndpointInfo
            {
                Category = "角色管理",
                Method = "GET",
                Path = "/api/character",
                Description = "获取所有角色列表",
                RequiresAuth = false
            },
            new ApiEndpointInfo
            {
                Category = "角色管理",
                Method = "POST",
                Path = "/api/character",
                Description = "创建新角色",
                RequiresAuth = false
            },
            new ApiEndpointInfo
            {
                Category = "角色管理",
                Method = "GET",
                Path = "/api/character/{id}",
                Description = "获取指定角色详细信息",
                RequiresAuth = false
            },

            // 战斗系统
            new ApiEndpointInfo
            {
                Category = "战斗系统",
                Method = "POST",
                Path = "/api/battle/start",
                Description = "开始新战斗",
                RequiresAuth = true
            },
            new ApiEndpointInfo
            {
                Category = "战斗系统",
                Method = "GET",
                Path = "/api/battle/state/{battleId}",
                Description = "获取战斗状态",
                RequiresAuth = true
            },
            new ApiEndpointInfo
            {
                Category = "战斗系统",
                Method = "POST",
                Path = "/api/battle/{battleId}/stop",
                Description = "停止战斗",
                RequiresAuth = true
            },

            // 组队系统
            new ApiEndpointInfo
            {
                Category = "组队系统",
                Method = "POST",
                Path = "/api/party",
                Description = "创建新队伍",
                RequiresAuth = true
            },
            new ApiEndpointInfo
            {
                Category = "组队系统",
                Method = "POST",
                Path = "/api/party/join",
                Description = "加入队伍",
                RequiresAuth = true
            },
            new ApiEndpointInfo
            {
                Category = "组队系统",
                Method = "POST",
                Path = "/api/party/leave",
                Description = "离开队伍",
                RequiresAuth = true
            },

            // 装备系统
            new ApiEndpointInfo
            {
                Category = "装备系统",
                Method = "POST",
                Path = "/api/equipment/generate",
                Description = "生成随机装备",
                RequiresAuth = false
            },

            // 库存系统
            new ApiEndpointInfo
            {
                Category = "库存系统",
                Method = "GET",
                Path = "/api/inventory/{characterId}",
                Description = "获取角色库存",
                RequiresAuth = true
            },
            new ApiEndpointInfo
            {
                Category = "库存系统",
                Method = "POST",
                Path = "/api/inventory/add",
                Description = "添加物品到库存",
                RequiresAuth = true
            },

            // 生产系统
            new ApiEndpointInfo
            {
                Category = "生产系统",
                Method = "GET",
                Path = "/api/production/gathering-nodes",
                Description = "获取采集节点列表",
                RequiresAuth = false
            },
            new ApiEndpointInfo
            {
                Category = "生产系统",
                Method = "POST",
                Path = "/api/production/start-gathering",
                Description = "开始采集",
                RequiresAuth = true
            },

            // 任务系统
            new ApiEndpointInfo
            {
                Category = "任务系统",
                Method = "GET",
                Path = "/api/quest/{characterId}",
                Description = "获取角色任务状态",
                RequiresAuth = true
            },
            new ApiEndpointInfo
            {
                Category = "任务系统",
                Method = "POST",
                Path = "/api/quest/accept",
                Description = "接受任务",
                RequiresAuth = true
            },

            // 监控系统
            new ApiEndpointInfo
            {
                Category = "监控系统",
                Method = "GET",
                Path = "/api/monitoring/system-metrics",
                Description = "获取系统性能指标",
                RequiresAuth = false
            },
            new ApiEndpointInfo
            {
                Category = "监控系统",
                Method = "GET",
                Path = "/api/monitoring/operation-metrics",
                Description = "获取操作性能统计",
                RequiresAuth = false
            },

            // 数据存储
            new ApiEndpointInfo
            {
                Category = "数据存储",
                Method = "GET",
                Path = "/api/datastorage/stats",
                Description = "获取数据存储统计",
                RequiresAuth = false
            },

            // 健康检查
            new ApiEndpointInfo
            {
                Category = "系统",
                Method = "GET",
                Path = "/health",
                Description = "详细健康检查",
                RequiresAuth = false
            },
            new ApiEndpointInfo
            {
                Category = "系统",
                Method = "GET",
                Path = "/health/simple",
                Description = "简单健康检查",
                RequiresAuth = false
            },
            new ApiEndpointInfo
            {
                Category = "系统",
                Method = "GET",
                Path = "/api/info",
                Description = "服务器基本信息",
                RequiresAuth = false
            }
        };
    }

    private Dictionary<string, FeatureStatus> GetServerFeatureStatus()
    {
        return new Dictionary<string, FeatureStatus>
        {
            { "用户认证", new FeatureStatus { IsEnabled = true, Status = "正常", Description = "JWT令牌认证系统" } },
            { "角色管理", new FeatureStatus { IsEnabled = true, Status = "正常", Description = "角色创建、查询、更新" } },
            { "战斗系统", new FeatureStatus { IsEnabled = true, Status = "正常", Description = "回合制战斗，支持单人和组队" } },
            { "组队系统", new FeatureStatus { IsEnabled = true, Status = "正常", Description = "多人组队功能" } },
            { "装备系统", new FeatureStatus { IsEnabled = true, Status = "正常", Description = "装备生成和管理" } },
            { "库存系统", new FeatureStatus { IsEnabled = true, Status = "正常", Description = "物品存储和管理" } },
            { "生产系统", new FeatureStatus { IsEnabled = true, Status = "正常", Description = "采集、制作等生产活动" } },
            { "任务系统", new FeatureStatus { IsEnabled = true, Status = "正常", Description = "日常、周常任务管理" } },
            { "离线结算", new FeatureStatus { IsEnabled = true, Status = "实验性", Description = "离线进度计算" } },
            { "实时通信", new FeatureStatus { IsEnabled = true, Status = "正常", Description = "SignalR实时更新" } },
            { "性能监控", new FeatureStatus { IsEnabled = true, Status = "正常", Description = "系统性能实时监控" } },
            { "数据存储", new FeatureStatus { IsEnabled = true, Status = "正常", Description = "持久化数据存储" } },
            { "安全防护", new FeatureStatus { IsEnabled = true, Status = "正常", Description = "CORS、速率限制、认证" } }
        };
    }

    private List<string> GetServerCapabilities()
    {
        return new List<string>
        {
            "🎮 自动放置游戏核心机制",
            "👤 多角色管理系统",
            "⚔️ 实时战斗引擎",
            "👥 多人组队协作",
            "🛡️ 动态装备生成",
            "🎒 智能库存管理",
            "⛏️ 多样化生产系统",
            "📋 任务系统（日常/周常）",
            "💤 离线进度结算",
            "🌐 实时数据同步",
            "📊 性能监控与优化",
            "🔒 企业级安全防护",
            "📱 跨平台支持",
            "🚀 高并发处理",
            "📈 可扩展架构"
        };
    }
}

/// <summary>
/// 服务器API概述
/// </summary>
public class ServerApiOverview
{
    public string ServerName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public List<ApiEndpointInfo> ApiEndpoints { get; set; } = new();
    public Dictionary<string, FeatureStatus> FeatureStatus { get; set; } = new();
    public List<string> ServerCapabilities { get; set; } = new();
}

/// <summary>
/// API端点信息
/// </summary>
public class ApiEndpointInfo
{
    public string Category { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresAuth { get; set; }
}

/// <summary>
/// 功能状态
/// </summary>
public class FeatureStatus
{
    public bool IsEnabled { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 服务器详细信息
/// </summary>
public class ServerDetailInfo
{
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public TimeSpan Uptime { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public long WorkingSet { get; set; }
    public GarbageCollectionInfo GCInfo { get; set; } = new();
}

/// <summary>
/// 垃圾回收信息
/// </summary>
public class GarbageCollectionInfo
{
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
    public long TotalMemory { get; set; }
}