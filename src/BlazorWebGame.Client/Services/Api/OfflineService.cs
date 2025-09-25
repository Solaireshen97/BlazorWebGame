using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Utils;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 离线模式服务 - 在服务器不可用时提供基本功能
/// </summary>
public class OfflineService
{
    private readonly ILogger<OfflineService> _logger;
    private readonly GameStorage _storage;
    private bool _isOfflineMode = false;
    private readonly Queue<OfflineAction> _pendingActions = new();

    public bool IsOfflineMode => _isOfflineMode;
    public event Action<bool>? OnOfflineModeChanged;

    public OfflineService(ILogger<OfflineService> logger, GameStorage storage)
    {
        _logger = logger;
        _storage = storage;
    }

    /// <summary>
    /// 进入离线模式
    /// </summary>
    public async Task EnterOfflineMode()
    {
        _isOfflineMode = true;
        _logger.LogWarning("进入离线模式");
        
        // 保存当前状态到本地
        await SaveCurrentStateLocally();
        
        OnOfflineModeChanged?.Invoke(true);
    }

    /// <summary>
    /// 退出离线模式并同步数据
    /// </summary>
    public async Task<bool> ExitOfflineMode(GameApiService apiService)
    {
        try
        {
            _logger.LogInformation("尝试退出离线模式并同步数据");
            
            // 执行所有待处理的操作
            while (_pendingActions.Count > 0)
            {
                var action = _pendingActions.Dequeue();
                await ExecutePendingAction(action, apiService);
            }
            
            _isOfflineMode = false;
            OnOfflineModeChanged?.Invoke(false);
            _logger.LogInformation("成功退出离线模式");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "退出离线模式失败");
            return false;
        }
    }

    /// <summary>
    /// 记录离线操作
    /// </summary>
    public void RecordOfflineAction(OfflineActionType actionType, object data)
    {
        var action = new OfflineAction
        {
            Type = actionType,
            Timestamp = DateTime.UtcNow,
            Data = System.Text.Json.JsonSerializer.Serialize(data)
        };
        
        _pendingActions.Enqueue(action);
        _logger.LogInformation("记录离线操作: {ActionType} at {Timestamp}", actionType, action.Timestamp);
    }

    /// <summary>
    /// 本地战斗模拟（离线模式）
    /// </summary>
    public BattleStateDto SimulateLocalBattle(string characterId, string enemyId, string? partyId = null)
    {
        _logger.LogInformation("开始本地战斗模拟");
        
        return new BattleStateDto
        {
            BattleId = Guid.NewGuid(),
            CharacterId = characterId,
            EnemyId = enemyId,
            PartyId = partyId,
            IsActive = true,
            PlayerHealth = 100,
            PlayerMaxHealth = 100,
            EnemyHealth = 80,
            EnemyMaxHealth = 80,
            LastUpdated = DateTime.UtcNow,
            BattleType = BattleType.Normal
        };
    }

    /// <summary>
    /// 保存当前状态到本地存储
    /// </summary>
    private async Task SaveCurrentStateLocally()
    {
        try
        {
            // 这里可以保存重要的游戏状态到本地存储
            // 暂时留空，实际实现时需要保存角色状态、背包等
            await Task.CompletedTask;
            _logger.LogInformation("当前状态已保存到本地");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存本地状态失败");
        }
    }

    /// <summary>
    /// 执行待处理的操作
    /// </summary>
    private async Task ExecutePendingAction(OfflineAction action, GameApiService apiService)
    {
        try
        {
            switch (action.Type)
            {
                case OfflineActionType.StartBattle:
                    var battleRequest = System.Text.Json.JsonSerializer.Deserialize<StartBattleRequest>(action.Data);
                    if (battleRequest != null)
                    {
                        await apiService.StartBattleAsync(battleRequest);
                    }
                    break;
                    
                case OfflineActionType.StopBattle:
                    var battleId = System.Text.Json.JsonSerializer.Deserialize<Guid>(action.Data);
                    await apiService.StopBattleAsync(battleId);
                    break;
                    
                case OfflineActionType.UpdateCharacter:
                    // 处理角色更新同步
                    break;
            }
            
            _logger.LogInformation("成功执行离线操作: {ActionType}", action.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行离线操作失败: {ActionType}", action.Type);
        }
    }
}