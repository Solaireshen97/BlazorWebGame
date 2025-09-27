using BlazorWebGame.Refactored.Domain.ValueObjects;
using BlazorWebGame.Refactored.Domain.Entities;

namespace BlazorWebGame.Refactored.Presentation.State;

/// <summary>
/// Fluxor Actions - 定义所有状态变更操作
/// </summary>

// ======================
// 认证相关 Actions
// ======================

public record LoginAction(string Username, string Password);
public record LoginSuccessAction(string AccessToken, string RefreshToken, string UserId, string Username, DateTime ExpiresAt);
public record LoginFailureAction(string Error);
public record LogoutAction();
public record RefreshTokenAction();
public record RefreshTokenSuccessAction(string AccessToken, DateTime ExpiresAt);
public record RefreshTokenFailureAction(string Error);

// ======================
// 角色相关 Actions
// ======================

public record LoadCharactersAction();
public record LoadCharactersSuccessAction(IEnumerable<CharacterData> Characters);
public record LoadCharactersFailureAction(string Error);

public record SwitchCharacterAction(Guid CharacterId);
public record SwitchCharacterSuccessAction(Guid CharacterId);
public record SwitchCharacterFailureAction(string Error);

public record SelectCharacterAction(Guid CharacterId);
public record NavigateToCharacterCreationAction();

public record CreateCharacterAction(string Name, CharacterClass Class);
public record CreateCharacterSuccessAction(CharacterData Character);
public record CreateCharacterFailureAction(string Error);

public record UpdateCharacterAction(Guid CharacterId, CharacterUpdateData Data);
public record UpdateCharacterSuccessAction(Guid CharacterId, CharacterUpdateData Data);

public record DeleteCharacterAction(Guid CharacterId);
public record DeleteCharacterSuccessAction(Guid CharacterId);
public record DeleteCharacterFailureAction(string Error);

// ======================
// 活动相关 Actions
// ======================

public record StartActivityAction(Guid CharacterId, ActivityRequest Request);
public record StartActivitySuccessAction(Guid CharacterId, ActivitySummary Activity);
public record StartActivityFailureAction(string Error);

public record CancelActivityAction(Guid ActivityId);
public record CancelActivitySuccessAction(Guid ActivityId);
public record CancelActivityFailureAction(string Error);

public record PauseActivityAction(Guid ActivityId);
public record ResumeActivityAction(Guid ActivityId);

public record ActivityProgressUpdateAction(Guid ActivityId, double Progress);
public record ActivityCompletedAction(Guid ActivityId, ActivityResult Result);
public record ActivityStateUpdateAction(Guid ActivityId, Domain.ValueObjects.ActivityState State, Dictionary<string, object>? Data = null);

// ======================
// 战斗相关 Actions
// ======================

public record StartBattleAction(Guid CharacterId, string EnemyId, string? PartyId = null);
public record StartBattleSuccessAction(BattleData Battle);
public record StartBattleFailureAction(string Error);

public record BattleUpdateAction(Guid BattleId, BattleUpdateData UpdateData);
public record BattleActionExecutedAction(Guid BattleId, BattleAction Action);
public record BattleEndedAction(Guid BattleId, BattleStatus Status, ActivityResult? Result);

public record JoinBattleGroupAction(Guid BattleId);
public record LeaveBattleGroupAction(Guid BattleId);

// ======================
// 实时通信相关 Actions
// ======================

public record ConnectSignalRAction();
public record SignalRConnectedAction();
public record SignalRDisconnectedAction(string? Reason = null);
public record SignalRReconnectedAction();
public record SignalRConnectionFailedAction(string Error);

public record JoinGroupAction(string GroupName);
public record LeaveGroupAction(string GroupName);
public record GroupJoinedAction(string GroupName);
public record GroupLeftAction(string GroupName);

public record ReceiveRealtimeEventAction(RealtimeEvent Event);
public record ProcessRealtimeEventAction(Guid EventId);
public record SendHeartbeatAction();
public record HeartbeatReceivedAction(DateTime ServerTime);

public record UpdateServerTimeDriftAction(TimeSpan Drift);

// ======================
// UI相关 Actions
// ======================

public record ToggleSidebarAction();
public record NavigateToPageAction(string PageName);
public record ShowNotificationAction(NotificationMessage Notification);
public record HideNotificationAction(Guid NotificationId);
public record ClearAllNotificationsAction();
public record MarkNotificationAsReadAction(Guid NotificationId);

public record SetLoadingStateAction(string Key, bool IsLoading);
public record SetGlobalErrorAction(string? Error);
public record SetOfflineStatusAction(bool IsOffline);

public record ShowModalAction(string ModalType, Dictionary<string, object>? Data = null);
public record HideModalAction();

// ======================
// 缓存相关 Actions
// ======================

public record CacheSetAction(string Key, object Data, TimeSpan? Expiration = null);
public record CacheGetAction(string Key);
public record CacheRemoveAction(string Key);
public record CacheClearAction();
public record CacheCleanupAction();

public record CacheHitAction(string Key);
public record CacheMissAction(string Key);

// ======================
// 批量更新 Actions
// ======================

public record BulkUpdateAction(IEnumerable<IBulkUpdateItem> Updates);
public record BulkCharacterUpdateAction(Dictionary<Guid, CharacterUpdateData> Updates);
public record BulkActivityUpdateAction(Dictionary<Guid, ActivityUpdateData> Updates);

// ======================
// 数据传输对象
// ======================

/// <summary>
/// 角色更新数据
/// </summary>
public record CharacterUpdateData
{
    public string? Name { get; init; }
    public int? Level { get; init; }
    public BigNumber? Experience { get; init; }
    public CharacterStats? Stats { get; init; }
    public ResourcePool? Resources { get; init; }
    public Vector3? Position { get; init; }
    public DateTime? LastLogin { get; init; }
    public bool? IsOnline { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// 活动请求
/// </summary>
public record ActivityRequest
{
    public ActivityType Type { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = new();
    public int Priority { get; init; } = 0;
    public bool AllowInterrupt { get; init; } = true;
}

/// <summary>
/// 活动更新数据
/// </summary>
public record ActivityUpdateData
{
    public ActivityState? State { get; init; }
    public double? Progress { get; init; }
    public DateTime? EndTime { get; init; }
    public bool? CanInterrupt { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// 战斗更新数据
/// </summary>
public record BattleUpdateData
{
    public BattleStatus? Status { get; init; }
    public List<BattleParticipant>? Players { get; init; }
    public List<BattleParticipant>? Enemies { get; init; }
    public List<BattleAction>? RecentActions { get; init; }
    public DateTime? LastUpdated { get; init; }
}

/// <summary>
/// 批量更新项接口
/// </summary>
public interface IBulkUpdateItem
{
    string UpdateType { get; }
    Guid EntityId { get; }
    Dictionary<string, object> Data { get; }
}

/// <summary>
/// 批量更新项实现
/// </summary>
public record BulkUpdateItem(
    string UpdateType,
    Guid EntityId,
    Dictionary<string, object> Data
) : IBulkUpdateItem;

// ======================
// 复合 Actions (组合多个操作)
// ======================

/// <summary>
/// 完整的角色切换操作 (包含加载详情、订阅更新等)
/// </summary>
public record SwitchCharacterCompleteAction(Guid CharacterId);

/// <summary>
/// 完整的登录操作 (包含获取角色列表、建立连接等)
/// </summary>
public record LoginCompleteAction(string Username, string Password);

/// <summary>
/// 完整的登出操作 (包含清理状态、断开连接等)
/// </summary>
public record LogoutCompleteAction();

/// <summary>
/// 完整的离线恢复操作
/// </summary>
public record OfflineRecoveryAction();

/// <summary>
/// 应用程序初始化操作
/// </summary>
public record InitializeApplicationAction();

/// <summary>
/// 应用程序初始化完成
/// </summary>
public record ApplicationInitializedAction();

// ======================
// 错误处理 Actions
// ======================

/// <summary>
/// 全局错误处理
/// </summary>
public record HandleGlobalErrorAction(Exception Exception, string Context);

/// <summary>
/// 网络错误处理
/// </summary>
public record HandleNetworkErrorAction(string Error, bool IsRetryable = true);

/// <summary>
/// 认证错误处理
/// </summary>
public record HandleAuthErrorAction(string Error);

/// <summary>
/// 数据同步错误处理
/// </summary>
public record HandleSyncErrorAction(string Error, string EntityType, Guid? EntityId = null);

// ======================
// 性能监控 Actions
// ======================

/// <summary>
/// 记录性能指标
/// </summary>
public record RecordPerformanceMetricAction(string MetricName, double Value, Dictionary<string, string>? Tags = null);

/// <summary>
/// 开始性能测量
/// </summary>
public record StartPerformanceMeasureAction(string OperationName);

/// <summary>
/// 结束性能测量
/// </summary>
public record EndPerformanceMeasureAction(string OperationName);

// ======================
// 调试相关 Actions
// ======================

/// <summary>
/// 调试状态转储
/// </summary>
public record DumpStateAction(string Context);

/// <summary>
/// 重置应用状态 (开发调试用)
/// </summary>
public record ResetApplicationStateAction();

/// <summary>
/// 设置调试模式
/// </summary>
public record SetDebugModeAction(bool Enabled);