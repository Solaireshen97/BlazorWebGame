using Fluxor;
using System.Collections.Immutable;

namespace BlazorWebGame.Refactored.Presentation.State;

/// <summary>
/// Fluxor Reducers - 处理状态变更的纯函数
/// </summary>

// ======================
// 认证状态 Reducers
// ======================

public static class AuthReducers
{
    [ReducerMethod]
    public static AuthState ReduceLoginAction(AuthState state, LoginAction action) =>
        state with { IsLoading = true, Error = null };

    [ReducerMethod]
    public static AuthState ReduceLoginSuccess(AuthState state, LoginSuccessAction action) =>
        state with 
        { 
            IsLoading = false,
            IsAuthenticated = true,
            AccessToken = action.AccessToken,
            RefreshToken = action.RefreshToken,
            UserId = action.UserId,
            Username = action.Username,
            TokenExpiresAt = action.ExpiresAt,
            Error = null
        };

    [ReducerMethod]
    public static AuthState ReduceLoginFailure(AuthState state, LoginFailureAction action) =>
        state with 
        { 
            IsLoading = false,
            IsAuthenticated = false,
            Error = action.Error,
            AccessToken = null,
            RefreshToken = null,
            UserId = null,
            Username = null,
            TokenExpiresAt = null
        };

    [ReducerMethod]
    public static AuthState ReduceLogout(AuthState state, LogoutAction action) =>
        new AuthState(); // 重置为初始状态

    [ReducerMethod]
    public static AuthState ReduceRefreshTokenSuccess(AuthState state, RefreshTokenSuccessAction action) =>
        state with 
        { 
            AccessToken = action.AccessToken,
            TokenExpiresAt = action.ExpiresAt,
            Error = null
        };

    [ReducerMethod]
    public static AuthState ReduceRefreshTokenFailure(AuthState state, RefreshTokenFailureAction action) =>
        state with 
        { 
            IsAuthenticated = false,
            AccessToken = null,
            RefreshToken = null,
            Error = action.Error
        };
}

// ======================
// 角色状态 Reducers
// ======================

public static class CharacterReducers
{
    [ReducerMethod]
    public static CharacterState ReduceLoadCharacters(CharacterState state, LoadCharactersAction action) =>
        state with { IsLoading = true, Error = null };

    [ReducerMethod]
    public static CharacterState ReduceLoadCharactersSuccess(CharacterState state, LoadCharactersSuccessAction action) =>
        state with 
        { 
            IsLoading = false,
            Characters = action.Characters.ToImmutableDictionary(c => c.Id),
            LastUpdated = DateTime.UtcNow,
            Error = null
        };

    [ReducerMethod]
    public static CharacterState ReduceLoadCharactersFailure(CharacterState state, LoadCharactersFailureAction action) =>
        state with 
        { 
            IsLoading = false,
            Error = action.Error
        };

    [ReducerMethod]
    public static CharacterState ReduceSwitchCharacter(CharacterState state, SwitchCharacterAction action) =>
        state with { CurrentCharacterId = action.CharacterId };

    [ReducerMethod]
    public static CharacterState ReduceCreateCharacterSuccess(CharacterState state, CreateCharacterSuccessAction action) =>
        state with 
        { 
            Characters = state.Characters.Add(action.Character.Id, action.Character),
            LastUpdated = DateTime.UtcNow
        };

    [ReducerMethod]
    public static CharacterState ReduceUpdateCharacterSuccess(CharacterState state, UpdateCharacterSuccessAction action)
    {
        if (!state.Characters.TryGetValue(action.CharacterId, out var character))
            return state;

        var updatedCharacter = ApplyCharacterUpdate(character, action.Data);
        return state with 
        { 
            Characters = state.Characters.SetItem(action.CharacterId, updatedCharacter),
            LastUpdated = DateTime.UtcNow
        };
    }

    [ReducerMethod]
    public static CharacterState ReduceDeleteCharacterSuccess(CharacterState state, DeleteCharacterSuccessAction action)
    {
        var newState = state with 
        { 
            Characters = state.Characters.Remove(action.CharacterId)
        };

        // 如果删除的是当前角色，清除当前角色ID
        if (state.CurrentCharacterId == action.CharacterId)
        {
            newState = newState with { CurrentCharacterId = null };
        }

        return newState;
    }

    [ReducerMethod]
    public static CharacterState ReduceBulkCharacterUpdate(CharacterState state, BulkCharacterUpdateAction action)
    {
        var characters = state.Characters;
        
        foreach (var (characterId, updateData) in action.Updates)
        {
            if (characters.TryGetValue(characterId, out var character))
            {
                var updatedCharacter = ApplyCharacterUpdate(character, updateData);
                characters = characters.SetItem(characterId, updatedCharacter);
            }
        }

        return state with 
        { 
            Characters = characters,
            LastUpdated = DateTime.UtcNow
        };
    }

    private static CharacterData ApplyCharacterUpdate(CharacterData character, CharacterUpdateData updateData) =>
        character with
        {
            Name = updateData.Name ?? character.Name,
            Level = updateData.Level ?? character.Level,
            Experience = updateData.Experience ?? character.Experience,
            Stats = updateData.Stats ?? character.Stats,
            Resources = updateData.Resources ?? character.Resources,
            Position = updateData.Position ?? character.Position,
            LastLogin = updateData.LastLogin ?? character.LastLogin,
            IsOnline = updateData.IsOnline ?? character.IsOnline,
            Metadata = updateData.Metadata ?? character.Metadata
        };
}

// ======================
// 活动状态 Reducers
// ======================

public static class ActivityReducers
{
    [ReducerMethod]
    public static ActivityState ReduceStartActivitySuccess(ActivityState state, StartActivitySuccessAction action) =>
        state with 
        { 
            Activities = state.Activities.Add(action.Activity.Id, action.Activity)
        };

    [ReducerMethod]
    public static ActivityState ReduceCancelActivitySuccess(ActivityState state, CancelActivitySuccessAction action) =>
        state with 
        { 
            Activities = state.Activities.Remove(action.ActivityId)
        };

    [ReducerMethod]
    public static ActivityState ReduceActivityProgressUpdate(ActivityState state, ActivityProgressUpdateAction action)
    {
        if (!state.Activities.TryGetValue(action.ActivityId, out var activity))
            return state;

        var updatedActivity = activity with { Progress = action.Progress };
        return state with 
        { 
            Activities = state.Activities.SetItem(action.ActivityId, updatedActivity)
        };
    }

    [ReducerMethod]
    public static ActivityState ReduceActivityCompleted(ActivityState state, ActivityCompletedAction action) =>
        state with 
        { 
            Activities = state.Activities.Remove(action.ActivityId)
        };

    [ReducerMethod]
    public static ActivityState ReduceActivityStateUpdate(ActivityState state, ActivityStateUpdateAction action)
    {
        if (!state.Activities.TryGetValue(action.ActivityId, out var activity))
            return state;

        var updatedActivity = activity with 
        { 
            State = StateMappers.MapDomainToDisplayState(action.State),
            Metadata = action.Data != null ? 
                new Dictionary<string, object>(activity.Metadata.Concat(action.Data)) : 
                activity.Metadata
        };

        return state with 
        { 
            Activities = state.Activities.SetItem(action.ActivityId, updatedActivity)
        };
    }
}

// ======================
// 战斗状态 Reducers
// ======================

public static class BattleReducers
{
    [ReducerMethod]
    public static BattleState ReduceStartBattleSuccess(BattleState state, StartBattleSuccessAction action) =>
        state with 
        { 
            ActiveBattles = state.ActiveBattles.Add(action.Battle.Id, action.Battle),
            CurrentBattleId = action.Battle.Id,
            LastUpdated = DateTime.UtcNow
        };

    [ReducerMethod]
    public static BattleState ReduceBattleUpdate(BattleState state, BattleUpdateAction action)
    {
        if (!state.ActiveBattles.TryGetValue(action.BattleId, out var battle))
            return state;

        var updatedBattle = ApplyBattleUpdate(battle, action.UpdateData);
        return state with 
        { 
            ActiveBattles = state.ActiveBattles.SetItem(action.BattleId, updatedBattle),
            LastUpdated = DateTime.UtcNow
        };
    }

    [ReducerMethod]
    public static BattleState ReduceBattleActionExecuted(BattleState state, BattleActionExecutedAction action)
    {
        if (!state.ActiveBattles.TryGetValue(action.BattleId, out var battle))
            return state;

        var recentActions = battle.RecentActions.TakeLast(9).Append(action.Action).ToList();
        var updatedBattle = battle with 
        { 
            RecentActions = recentActions,
            LastUpdated = DateTime.UtcNow
        };

        return state with 
        { 
            ActiveBattles = state.ActiveBattles.SetItem(action.BattleId, updatedBattle)
        };
    }

    [ReducerMethod]
    public static BattleState ReduceBattleEnded(BattleState state, BattleEndedAction action)
    {
        var newState = state with 
        { 
            ActiveBattles = state.ActiveBattles.Remove(action.BattleId)
        };

        if (state.CurrentBattleId == action.BattleId)
        {
            newState = newState with { CurrentBattleId = null };
        }

        return newState;
    }

    private static BattleData ApplyBattleUpdate(BattleData battle, BattleUpdateData updateData) =>
        battle with
        {
            Status = updateData.Status ?? battle.Status,
            Players = updateData.Players ?? battle.Players,
            Enemies = updateData.Enemies ?? battle.Enemies,
            RecentActions = updateData.RecentActions ?? battle.RecentActions,
            LastUpdated = updateData.LastUpdated ?? DateTime.UtcNow
        };
}

// ======================
// UI状态 Reducers
// ======================

public static class UIReducers
{
    [ReducerMethod]
    public static UIState ReduceToggleSidebar(UIState state, ToggleSidebarAction action) =>
        state with { IsSidebarExpanded = !state.IsSidebarExpanded };

    [ReducerMethod]
    public static UIState ReduceNavigateToPage(UIState state, NavigateToPageAction action) =>
        state with { CurrentPage = action.PageName };

    [ReducerMethod]
    public static UIState ReduceShowNotification(UIState state, ShowNotificationAction action) =>
        state with 
        { 
            Notifications = state.Notifications.Enqueue(action.Notification)
        };

    [ReducerMethod]
    public static UIState ReduceHideNotification(UIState state, HideNotificationAction action)
    {
        var notifications = ImmutableQueue<NotificationMessage>.Empty;
        
        foreach (var notification in state.Notifications)
        {
            if (notification.Id != action.NotificationId)
            {
                notifications = notifications.Enqueue(notification);
            }
        }

        return state with { Notifications = notifications };
    }

    [ReducerMethod]
    public static UIState ReduceClearAllNotifications(UIState state, ClearAllNotificationsAction action) =>
        state with { Notifications = ImmutableQueue<NotificationMessage>.Empty };

    [ReducerMethod]
    public static UIState ReduceSetLoadingState(UIState state, SetLoadingStateAction action)
    {
        var loadingStates = new Dictionary<string, bool>(state.LoadingStates)
        {
            [action.Key] = action.IsLoading
        };

        return state with { LoadingStates = loadingStates };
    }

    [ReducerMethod]
    public static UIState ReduceSetGlobalError(UIState state, SetGlobalErrorAction action) =>
        state with { GlobalError = action.Error };

    [ReducerMethod]
    public static UIState ReduceSetOfflineStatus(UIState state, SetOfflineStatusAction action) =>
        state with { IsOffline = action.IsOffline };
}

// ======================
// 实时通信状态 Reducers
// ======================

public static class RealtimeReducers
{
    [ReducerMethod]
    public static RealtimeState ReduceSignalRConnected(RealtimeState state, SignalRConnectedAction action) =>
        state with 
        { 
            ConnectionState = Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected,
            ReconnectAttempts = 0
        };

    [ReducerMethod]
    public static RealtimeState ReduceSignalRDisconnected(RealtimeState state, SignalRDisconnectedAction action) =>
        state with 
        { 
            ConnectionState = Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Disconnected,
            JoinedGroups = ImmutableHashSet<string>.Empty
        };

    [ReducerMethod]
    public static RealtimeState ReduceSignalRReconnected(RealtimeState state, SignalRReconnectedAction action) =>
        state with 
        { 
            ConnectionState = Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected,
            ReconnectAttempts = 0
        };

    [ReducerMethod]
    public static RealtimeState ReduceGroupJoined(RealtimeState state, GroupJoinedAction action) =>
        state with 
        { 
            JoinedGroups = state.JoinedGroups.Add(action.GroupName)
        };

    [ReducerMethod]
    public static RealtimeState ReduceGroupLeft(RealtimeState state, GroupLeftAction action) =>
        state with 
        { 
            JoinedGroups = state.JoinedGroups.Remove(action.GroupName)
        };

    [ReducerMethod]
    public static RealtimeState ReduceReceiveRealtimeEvent(RealtimeState state, ReceiveRealtimeEventAction action) =>
        state with 
        { 
            PendingEvents = state.PendingEvents.Enqueue(action.Event)
        };

    [ReducerMethod]
    public static RealtimeState ReduceProcessRealtimeEvent(RealtimeState state, ProcessRealtimeEventAction action)
    {
        var events = ImmutableQueue<RealtimeEvent>.Empty;
        
        foreach (var evt in state.PendingEvents)
        {
            if (evt.Id != action.EventId)
            {
                events = events.Enqueue(evt);
            }
        }

        return state with { PendingEvents = events };
    }

    [ReducerMethod]
    public static RealtimeState ReduceHeartbeatReceived(RealtimeState state, HeartbeatReceivedAction action) =>
        state with { LastHeartbeat = action.ServerTime };

    [ReducerMethod]
    public static RealtimeState ReduceUpdateServerTimeDrift(RealtimeState state, UpdateServerTimeDriftAction action) =>
        state with { ServerTimeDrift = action.Drift };
}

// ======================
// 缓存状态 Reducers
// ======================

public static class CacheReducers
{
    [ReducerMethod]
    public static CacheState ReduceCacheSet(CacheState state, CacheSetAction action)
    {
        var expiration = action.Expiration ?? TimeSpan.FromMinutes(15);
        var entry = new CacheEntry
        {
            Data = action.Data,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(expiration),
            Size = EstimateSize(action.Data),
            AccessCount = 0
        };

        return state with 
        { 
            MemoryCache = state.MemoryCache.SetItem(action.Key, entry),
            TotalCacheSize = state.TotalCacheSize + entry.Size
        };
    }

    [ReducerMethod]
    public static CacheState ReduceCacheRemove(CacheState state, CacheRemoveAction action)
    {
        if (!state.MemoryCache.TryGetValue(action.Key, out var entry))
            return state;

        return state with 
        { 
            MemoryCache = state.MemoryCache.Remove(action.Key),
            TotalCacheSize = state.TotalCacheSize - entry.Size
        };
    }

    [ReducerMethod]
    public static CacheState ReduceCacheClear(CacheState state, CacheClearAction action) =>
        state with 
        { 
            MemoryCache = ImmutableDictionary<string, CacheEntry>.Empty,
            TotalCacheSize = 0,
            LastCleanup = DateTime.UtcNow
        };

    [ReducerMethod]
    public static CacheState ReduceCacheHit(CacheState state, CacheHitAction action) =>
        state with { HitCount = state.HitCount + 1 };

    [ReducerMethod]
    public static CacheState ReduceCacheMiss(CacheState state, CacheMissAction action) =>
        state with { MissCount = state.MissCount + 1 };

    private static long EstimateSize(object data)
    {
        // 简单的大小估算，实际项目中可能需要更精确的计算
        return System.Text.Json.JsonSerializer.Serialize(data).Length * 2; // 粗略估算
    }
}

// ======================
// 全局数据管理 Reducers
// ======================

public static class GlobalDataReducers
{
    [ReducerMethod(typeof(ClearAllDataAction))]
    public static AppState ReduceClearAllData(AppState state) =>
        new AppState(); // 重置为初始状态
    
    [ReducerMethod(typeof(ResetApplicationStateAction))]
    public static AppState ReduceResetApplicationState(AppState state) =>
        new AppState(); // 重置为初始状态
}

/// <summary>
/// 状态映射工具类
/// </summary>
public static class StateMappers
{
    public static ActivityDisplayState MapDomainToDisplayState(Domain.ValueObjects.ActivityState domainState)
    {
        return domainState switch
        {
            Domain.ValueObjects.ActivityState.Active => ActivityDisplayState.Active,
            Domain.ValueObjects.ActivityState.Paused => ActivityDisplayState.Paused,
            Domain.ValueObjects.ActivityState.Completed => ActivityDisplayState.Completed,
            Domain.ValueObjects.ActivityState.Cancelled => ActivityDisplayState.Cancelled,
            Domain.ValueObjects.ActivityState.Failed => ActivityDisplayState.Failed,
            _ => ActivityDisplayState.Active
        };
    }
}