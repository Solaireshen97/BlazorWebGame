using Fluxor;
using BlazorWebGame.Refactored.Infrastructure.Services;
using BlazorWebGame.Refactored.Application.Interfaces;
using BlazorWebGame.Refactored.Domain.ValueObjects;

namespace BlazorWebGame.Refactored.Presentation.State;

/// <summary>
/// Fluxor Effects - 处理副作用和异步操作
/// </summary>

// ======================
// 认证相关 Effects
// ======================

public class AuthEffects
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthEffects> _logger;

    public AuthEffects(IAuthService authService, ILogger<AuthEffects> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleLogin(LoginAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Attempting login for user: {Username}", action.Username);

            var result = await _authService.LoginAsync(action.Username, action.Password);
            
            if (result.IsSuccess)
            {
                dispatcher.Dispatch(new LoginSuccessAction(
                    result.AccessToken!,
                    result.RefreshToken!,
                    result.UserId!,
                    result.Username!,
                    result.ExpiresAt!.Value
                ));

                // 登录成功后立即加载角色列表
                dispatcher.Dispatch(new LoadCharactersAction());
                
                // 建立SignalR连接
                dispatcher.Dispatch(new ConnectSignalRAction());
            }
            else
            {
                dispatcher.Dispatch(new LoginFailureAction(result.ErrorMessage!));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for user: {Username}", action.Username);
            dispatcher.Dispatch(new LoginFailureAction("登录失败，请重试"));
        }
    }

    [EffectMethod]
    public async Task HandleRefreshToken(RefreshTokenAction action, IDispatcher dispatcher)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync();
            
            if (result.IsSuccess)
            {
                dispatcher.Dispatch(new RefreshTokenSuccessAction(
                    result.AccessToken!,
                    result.ExpiresAt!.Value
                ));
            }
            else
            {
                dispatcher.Dispatch(new RefreshTokenFailureAction(result.ErrorMessage!));
                // Token刷新失败，重定向到登录页
                dispatcher.Dispatch(new LogoutAction());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            dispatcher.Dispatch(new RefreshTokenFailureAction("令牌刷新失败"));
            dispatcher.Dispatch(new LogoutAction());
        }
    }

    [EffectMethod]
    public async Task HandleLogout(LogoutAction action, IDispatcher dispatcher)
    {
        try
        {
            await _authService.LogoutAsync();
            
            // 清理所有状态
            dispatcher.Dispatch(new SignalRDisconnectedAction("User logout"));
            dispatcher.Dispatch(new CacheClearAction());
            dispatcher.Dispatch(new ClearAllNotificationsAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
        }
    }
}

// ======================
// 角色相关 Effects
// ======================

public class CharacterEffects
{
    private readonly ICharacterService _characterService;
    private readonly ISignalRService _signalRService;
    private readonly ILogger<CharacterEffects> _logger;

    public CharacterEffects(
        ICharacterService characterService,
        ISignalRService signalRService,
        ILogger<CharacterEffects> logger)
    {
        _characterService = characterService;
        _signalRService = signalRService;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleLoadCharacters(LoadCharactersAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Loading characters");

            var characters = await _characterService.GetCharactersAsync();
            dispatcher.Dispatch(new LoadCharactersSuccessAction(characters));

            // 为每个角色加入SignalR组
            foreach (var character in characters)
            {
                await _signalRService.JoinCharacterGroupAsync(character.Id);
                dispatcher.Dispatch(new GroupJoinedAction($"character-{character.Id}"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load characters");
            dispatcher.Dispatch(new LoadCharactersFailureAction("加载角色失败"));
        }
    }

    [EffectMethod]
    public async Task HandleSwitchCharacter(SwitchCharacterAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Switching to character: {CharacterId}", action.CharacterId);

            // 加载角色详细信息
            var characterDetail = await _characterService.GetCharacterDetailAsync(action.CharacterId);
            
            if (characterDetail != null)
            {
                dispatcher.Dispatch(new UpdateCharacterSuccessAction(action.CharacterId, new CharacterUpdateData
                {
                    Name = characterDetail.Name,
                    Level = characterDetail.Level,
                    Experience = characterDetail.Experience,
                    Stats = characterDetail.Stats,
                    Resources = characterDetail.Resources,
                    LastLogin = DateTime.UtcNow,
                    IsOnline = true
                }));

                dispatcher.Dispatch(new SwitchCharacterSuccessAction(action.CharacterId));

                // 加入角色特定的SignalR组
                await _signalRService.JoinCharacterGroupAsync(action.CharacterId);
                dispatcher.Dispatch(new GroupJoinedAction($"character-{action.CharacterId}"));

                // 显示切换成功通知
                dispatcher.Dispatch(new ShowNotificationAction(new NotificationMessage
                {
                    Title = "角色切换",
                    Message = $"已切换到角色: {characterDetail.Name}",
                    Type = NotificationType.Success
                }));
            }
            else
            {
                dispatcher.Dispatch(new SwitchCharacterFailureAction("角色不存在"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch character: {CharacterId}", action.CharacterId);
            dispatcher.Dispatch(new SwitchCharacterFailureAction("切换角色失败"));
        }
    }

    [EffectMethod]
    public async Task HandleCreateCharacter(CreateCharacterAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Creating character: {Name} ({Class})", action.Name, action.Class);

            var character = await _characterService.CreateCharacterAsync(action.Name, action.Class);
            dispatcher.Dispatch(new CreateCharacterSuccessAction(character));

            dispatcher.Dispatch(new ShowNotificationAction(new NotificationMessage
            {
                Title = "角色创建",
                Message = $"成功创建角色: {character.Name}",
                Type = NotificationType.Success
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create character: {Name}", action.Name);
            dispatcher.Dispatch(new CreateCharacterFailureAction("创建角色失败"));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteCharacter(DeleteCharacterAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Deleting character: {CharacterId}", action.CharacterId);

            await _characterService.DeleteCharacterAsync(action.CharacterId);
            dispatcher.Dispatch(new DeleteCharacterSuccessAction(action.CharacterId));

            // 离开SignalR组
            await _signalRService.LeaveCharacterGroupAsync(action.CharacterId);
            dispatcher.Dispatch(new GroupLeftAction($"character-{action.CharacterId}"));

            dispatcher.Dispatch(new ShowNotificationAction(new NotificationMessage
            {
                Title = "角色删除",
                Message = "角色已删除",
                Type = NotificationType.Info
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete character: {CharacterId}", action.CharacterId);
            dispatcher.Dispatch(new DeleteCharacterFailureAction("删除角色失败"));
        }
    }
}

// ======================
// 活动相关 Effects
// ======================

public class ActivityEffects
{
    private readonly IActivityService _activityService;
    private readonly ILogger<ActivityEffects> _logger;

    public ActivityEffects(IActivityService activityService, ILogger<ActivityEffects> logger)
    {
        _activityService = activityService;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleStartActivity(StartActivityAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Starting activity {Type} for character {CharacterId}", 
                action.Request.Type, action.CharacterId);

            var result = await _activityService.StartActivityAsync(action.CharacterId, action.Request);
            
            if (result.Success)
            {
                // Create a mock ActivitySummary for the success case
                var activitySummary = new ActivitySummary
                {
                    Id = Guid.NewGuid(),
                    CharacterId = action.CharacterId,
                    Type = action.Request.Type,
                    State = ActivityDisplayState.Active,
                    StartTime = DateTime.UtcNow,
                    Progress = 0.0,
                    Priority = action.Request.Priority,
                    DisplayName = GetActivityName(action.Request.Type),
                    Description = $"{GetActivityName(action.Request.Type)} in progress",
                    CanInterrupt = action.Request.AllowInterrupt
                };
                
                dispatcher.Dispatch(new StartActivitySuccessAction(action.CharacterId, activitySummary));
                
                dispatcher.Dispatch(new ShowNotificationAction(new NotificationMessage
                {
                    Title = "活动开始",
                    Message = $"开始{GetActivityName(action.Request.Type)}",
                    Type = NotificationType.Info
                }));
            }
            else
            {
                dispatcher.Dispatch(new StartActivityFailureAction(result.ErrorMessage ?? "Unknown error"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start activity");
            dispatcher.Dispatch(new StartActivityFailureAction("启动活动失败"));
        }
    }

    [EffectMethod]
    public async Task HandleCancelActivity(CancelActivityAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Cancelling activity: {ActivityId}", action.ActivityId);

            await _activityService.CancelActivityAsync(action.ActivityId);
            dispatcher.Dispatch(new CancelActivitySuccessAction(action.ActivityId));

            dispatcher.Dispatch(new ShowNotificationAction(new NotificationMessage
            {
                Title = "活动取消",
                Message = "活动已取消",
                Type = NotificationType.Warning
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel activity: {ActivityId}", action.ActivityId);
            dispatcher.Dispatch(new CancelActivityFailureAction("取消活动失败"));
        }
    }

    private static string GetActivityName(ActivityType type) => type switch
    {
        ActivityType.Battle => "战斗",
        ActivityType.Gathering => "采集",
        ActivityType.Crafting => "制作",
        ActivityType.Quest => "任务",
        ActivityType.Boss => "Boss战",
        _ => "活动"
    };
}

// ======================
// 战斗相关 Effects
// ======================

public class BattleEffects
{
    private readonly IBattleService _battleService;
    private readonly ISignalRService _signalRService;
    private readonly ILogger<BattleEffects> _logger;

    public BattleEffects(
        IBattleService battleService,
        ISignalRService signalRService,
        ILogger<BattleEffects> logger)
    {
        _battleService = battleService;
        _signalRService = signalRService;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleStartBattle(StartBattleAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Starting battle for character {CharacterId} vs enemy {EnemyId}", 
                action.CharacterId, action.EnemyId);

            var battle = await _battleService.StartBattleAsync(action.CharacterId, action.EnemyId, action.PartyId);
            dispatcher.Dispatch(new StartBattleSuccessAction(battle));

            // 加入战斗SignalR组
            await _signalRService.JoinBattleGroupAsync(battle.Id);
            dispatcher.Dispatch(new GroupJoinedAction($"battle-{battle.Id}"));

            dispatcher.Dispatch(new ShowNotificationAction(new NotificationMessage
            {
                Title = "战斗开始",
                Message = "战斗已开始！",
                Type = NotificationType.Success
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start battle");
            dispatcher.Dispatch(new StartBattleFailureAction("开始战斗失败"));
        }
    }

    [EffectMethod]
    public async Task HandleJoinBattleGroup(JoinBattleGroupAction action, IDispatcher dispatcher)
    {
        try
        {
            await _signalRService.JoinBattleGroupAsync(action.BattleId);
            dispatcher.Dispatch(new GroupJoinedAction($"battle-{action.BattleId}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join battle group: {BattleId}", action.BattleId);
        }
    }

    [EffectMethod]
    public async Task HandleLeaveBattleGroup(LeaveBattleGroupAction action, IDispatcher dispatcher)
    {
        try
        {
            await _signalRService.LeaveBattleGroupAsync(action.BattleId);
            dispatcher.Dispatch(new GroupLeftAction($"battle-{action.BattleId}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave battle group: {BattleId}", action.BattleId);
        }
    }
}

// ======================
// SignalR相关 Effects
// ======================

public class SignalREffects
{
    private readonly ISignalRService _signalRService;
    private readonly ILogger<SignalREffects> _logger;

    public SignalREffects(ISignalRService signalRService, ILogger<SignalREffects> logger)
    {
        _signalRService = signalRService;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleConnectSignalR(ConnectSignalRAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Connecting to SignalR hub");

            await _signalRService.StartAsync();
            dispatcher.Dispatch(new SignalRConnectedAction());

            // 设置事件处理器
            _signalRService.OnCharacterUpdate += async (characterUpdateJson) =>
            {
                try
                {
                    var characterUpdate = System.Text.Json.JsonSerializer.Deserialize<CharacterUpdateEvent>(characterUpdateJson);
                    if (characterUpdate != null)
                    {
                        dispatcher.Dispatch(new UpdateCharacterSuccessAction(characterUpdate.CharacterId, characterUpdate.Data));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process character update");  
                }
            };

            _signalRService.OnBattleUpdate += async (battleUpdateJson) =>
            {
                try
                {
                    var battleUpdate = System.Text.Json.JsonSerializer.Deserialize<BattleUpdateEvent>(battleUpdateJson);
                    if (battleUpdate != null)
                    {
                        dispatcher.Dispatch(new BattleUpdateAction(battleUpdate.BattleId, battleUpdate.Data));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process battle update");
                }
            };

            _signalRService.OnActivityUpdate += async (activityUpdateJson) =>
            {
                try
                {
                    var activityUpdate = System.Text.Json.JsonSerializer.Deserialize<ActivityUpdateEvent>(activityUpdateJson);
                    if (activityUpdate != null)
                    {
                        dispatcher.Dispatch(new ActivityProgressUpdateAction(activityUpdate.ActivityId, activityUpdate.Progress));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process activity update");
                }
            };

            _signalRService.OnNotification += async (notificationJson) =>
            {
                try
                {
                    var notification = System.Text.Json.JsonSerializer.Deserialize<NotificationMessage>(notificationJson);
                    if (notification != null)
                    {
                        dispatcher.Dispatch(new ShowNotificationAction(notification));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process notification");
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SignalR hub");
            dispatcher.Dispatch(new SignalRConnectionFailedAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleJoinGroup(JoinGroupAction action, IDispatcher dispatcher)
    {
        try
        {
            await _signalRService.JoinGroupAsync(action.GroupName);
            dispatcher.Dispatch(new GroupJoinedAction(action.GroupName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join group: {GroupName}", action.GroupName);
        }
    }

    [EffectMethod]
    public async Task HandleLeaveGroup(LeaveGroupAction action, IDispatcher dispatcher)
    {
        try
        {
            await _signalRService.LeaveGroupAsync(action.GroupName);
            dispatcher.Dispatch(new GroupLeftAction(action.GroupName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave group: {GroupName}", action.GroupName);
        }
    }
}

// ======================
// 缓存相关 Effects
// ======================

public class CacheEffects
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheEffects> _logger;

    public CacheEffects(ICacheService cacheService, ILogger<CacheEffects> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleCacheCleanup(CacheCleanupAction action, IDispatcher dispatcher)
    {
        try
        {
            await _cacheService.CleanupExpiredEntriesAsync();
            _logger.LogInformation("Cache cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache cleanup failed");
        }
    }
}

// ======================
// 通用错误处理 Effects
// ======================

public class ErrorHandlingEffects
{
    private readonly ILogger<ErrorHandlingEffects> _logger;

    public ErrorHandlingEffects(ILogger<ErrorHandlingEffects> logger)
    {
        _logger = logger;
    }

    [EffectMethod]
    public Task HandleGlobalError(HandleGlobalErrorAction action, IDispatcher dispatcher)
    {
        _logger.LogError(action.Exception, "Global error in context: {Context}", action.Context);

        dispatcher.Dispatch(new ShowNotificationAction(new NotificationMessage
        {
            Title = "系统错误",
            Message = "发生了未预期的错误，请重试",
            Type = NotificationType.Error,
            Duration = TimeSpan.FromSeconds(10)
        }));

        return Task.CompletedTask;
    }

    [EffectMethod]
    public Task HandleNetworkError(HandleNetworkErrorAction action, IDispatcher dispatcher)
    {
        _logger.LogWarning("Network error: {Error}, Retryable: {IsRetryable}", action.Error, action.IsRetryable);

        dispatcher.Dispatch(new ShowNotificationAction(new NotificationMessage
        {
            Title = "网络错误",
            Message = action.IsRetryable ? "网络连接异常，正在重试..." : "网络连接失败",
            Type = NotificationType.Warning
        }));

        dispatcher.Dispatch(new SetOfflineStatusAction(true));

        return Task.CompletedTask;
    }
}