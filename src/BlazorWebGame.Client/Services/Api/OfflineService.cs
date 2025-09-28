using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Utils;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 纯在线服务 - 移除离线模式，只支持在线游戏
/// 原来的离线功能已完全移除，这个类现在只是一个存根以保持兼容性
/// </summary>
public class OfflineService
{
    private readonly ILogger<OfflineService> _logger;

    // 始终返回 false - 纯在线游戏不支持离线模式
    public bool IsOfflineMode => false;
    
    // 保留事件以保持兼容性，但永远不会触发
    public event Action<bool>? OnOfflineModeChanged;

    public OfflineService(ILogger<OfflineService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 纯在线游戏不支持进入离线模式
    /// </summary>
    [Obsolete("纯在线游戏不支持离线模式")]
    public async Task EnterOfflineMode()
    {
        _logger.LogWarning("纯在线游戏不支持离线模式，忽略进入离线模式请求");
        await Task.CompletedTask;
    }

    /// <summary>
    /// 纯在线游戏不需要退出离线模式
    /// </summary>
    [Obsolete("纯在线游戏不支持离线模式")]
    public async Task<bool> ExitOfflineMode(GameApiService apiService)
    {
        _logger.LogInformation("纯在线游戏始终在线，无需退出离线模式");
        return await Task.FromResult(true);
    }

    /// <summary>
    /// 纯在线游戏不支持记录离线操作
    /// </summary>
    [Obsolete("纯在线游戏不支持离线操作")]
    public void RecordOfflineAction(OfflineActionType actionType, object data)
    {
        _logger.LogWarning("纯在线游戏不支持离线操作记录，忽略操作: {ActionType}", actionType);
    }

    /// <summary>
    /// 纯在线游戏不支持本地战斗模拟
    /// </summary>
    [Obsolete("纯在线游戏不支持本地战斗模拟")]
    public BattleStateDto SimulateLocalBattle(string characterId, string enemyId, string? partyId = null)
    {
        _logger.LogWarning("纯在线游戏不支持本地战斗模拟");
        throw new NotSupportedException("纯在线游戏不支持本地战斗模拟，请使用服务端API");
    }

    /// <summary>
    /// 纯在线游戏不支持本地战斗模拟
    /// </summary>
    [Obsolete("纯在线游戏不支持本地战斗模拟")]
    public async Task<BattleStateDto> SimulateLocalBattleAsync(string characterId, string enemyId, string? partyId = null)
    {
        _logger.LogWarning("纯在线游戏不支持本地战斗模拟");
        await Task.CompletedTask;
        throw new NotSupportedException("纯在线游戏不支持本地战斗模拟，请使用服务端API");
    }

    /// <summary>
    /// 所有其他离线方法都已移除 - 纯在线游戏不需要这些功能
    /// 如果需要调用任何其他方法，请使用对应的服务端API
    /// </summary>
}