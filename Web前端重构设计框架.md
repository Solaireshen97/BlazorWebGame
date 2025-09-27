# Web 放置/MMORPG 游戏优化架构设计（Blazor + C# + SignalR）

> 目标：支持单账号最多5个角色并行活动，采用 **Blazor WebAssembly + C# + SignalR Core + MediatR + FluxorState**。优化重点：性能、可扩展性、实时同步。

---

## 1. 技术栈选择（优化版）

### 1.1 核心技术
| 层级 | 技术选择 | 理由 |
|------|----------|------|
| 前端框架 | Blazor WebAssembly | C#全栈开发，类型安全，与后端共享模型 |
| 状态管理 | Fluxor | Redux模式，支持时间旅行调试，适合复杂状态 |
| 实时通信 | SignalR Core | 原生.NET集成，自动降级，断线重连 |
| HTTP客户端 | Refit + Polly | 声明式API，自动重试，熔断器模式 |
| 缓存层 | IMemoryCache + IndexedDB | 内存热数据 + 持久化冷数据 |
| CQRS | MediatR | 命令查询分离，清晰的业务逻辑 |
| 验证 | FluentValidation | 声明式验证，前后端复用 |
| 日志 | Serilog | 结构化日志，性能分析 |

### 1.2 辅助库
```csharp
// 必需包
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="8.0.*" />
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.*" />
<PackageReference Include="Fluxor.Blazor.Web" Version="6.0.*" />
<PackageReference Include="Refit.HttpClientFactory" Version="7.0.*" />
<PackageReference Include="Polly.Extensions.Http" Version="3.0.*" />
<PackageReference Include="MediatR" Version="12.0.*" />
<PackageReference Include="FluentValidation" Version="11.0.*" />
<PackageReference Include="Blazored.LocalStorage" Version="4.5.*" />
```

---

## 2. 分层架构（Clean Architecture）

```
BlazorWebGame/
├── Domain/                      # 领域层
│   ├── Entities/               # 核心实体
│   ├── ValueObjects/           # 值对象
│   ├── Events/                 # 领域事件
│   └── Interfaces/             # 领域服务接口
├── Application/                 # 应用层
│   ├── Commands/               # CQRS命令
│   ├── Queries/                # CQRS查询
│   ├── Behaviors/              # 管道行为（验证、日志、缓存）
│   ├── DTOs/                   # 数据传输对象
│   └── Interfaces/             # 应用服务接口
├── Infrastructure/              # 基础设施层
│   ├── Services/               # 外部服务实现
│   ├── SignalR/                # SignalR客户端
│   ├── Http/                   # HTTP客户端
│   ├── Cache/                  # 缓存实现
│   └── Persistence/            # IndexedDB实现
├── Presentation/                # 表现层
│   ├── Pages/                  # Blazor页面
│   ├── Components/             # 可复用组件
│   ├── Shared/                 # 共享布局
│   └── State/                  # Fluxor状态管理
└── Shared/                      # 前后端共享
    ├── Models/                 # 共享模型
    ├── Constants/              # 常量定义
    └── Extensions/             # 扩展方法
```

---

## 3. 核心领域模型（优化版）

### 3.1 角色聚合根
```csharp
public class Character : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public int Level { get; private set; }
    public BigNumber Experience { get; private set; }
    public CharacterClass Class { get; private set; }
    public CharacterStats Stats { get; private set; }
    public ActivitySlots Activities { get; private set; } // 活动槽位管理
    public ResourcePool Resources { get; private set; }   // 资源池
    public CooldownTracker Cooldowns { get; private set; } // 冷却追踪
    
    // 领域方法
    public Result StartActivity(ActivityType type, ActivityParameters parameters);
    public Result CancelActivity(Guid activityId);
    public void ApplyExperience(BigNumber amount);
    public void UpdateStats(CharacterStats newStats);
}
```

### 3.2 活动系统（统一抽象）
```csharp
public abstract class Activity : Entity
{
    public Guid Id { get; protected set; }
    public Guid CharacterId { get; protected set; }
    public ActivityType Type { get; protected set; }
    public ActivityState State { get; protected set; }
    public DateTime StartTimeUtc { get; protected set; }
    public DateTime? EndTimeUtc { get; protected set; }
    public int Priority { get; protected set; } // 显示优先级
    public ActivityMetadata Metadata { get; protected set; }
    
    public abstract double GetProgress(DateTime currentTimeUtc);
    public abstract bool CanInterrupt();
    public abstract ActivityResult Complete();
    public abstract void UpdateState(ActivityUpdateData data);
}

// 具体实现
public class BattleActivity : Activity { }
public class GatheringActivity : Activity { }
public class CraftingActivity : Activity { }
public class QuestActivity : Activity { }
```

### 3.3 战斗系统
```csharp
public class Battle : AggregateRoot
{
    public Guid Id { get; private set; }
    public BattleType Type { get; private set; }
    public List<BattleParticipant> Participants { get; private set; }
    public List<Enemy> Enemies { get; private set; }
    public BattleTimeline Timeline { get; private set; } // 战斗时间轴
    public BattleState State { get; private set; }
    public Formation Formation { get; private set; }
    
    // 战斗节奏控制
    public DateTime NextActionTimeUtc { get; private set; }
    public Queue<BattleAction> ActionQueue { get; private set; }
    
    public BattleAction CalculateNextAction();
    public void ExecuteAction(BattleAction action);
    public BattleResult ProcessTick(DateTime currentTimeUtc);
}
```

---

## 4. 状态管理设计（Fluxor）

### 4.1 状态结构
```csharp
// 全局状态
public record AppState
{
    public AuthState Auth { get; init; }
    public CharacterState Characters { get; init; }
    public BattleState Battles { get; init; }
    public ActivityState Activities { get; init; }
    public UIState UI { get; init; }
    public RealtimeState Realtime { get; init; }
    public CacheState Cache { get; init; }
}

// 角色状态
public record CharacterState
{
    public Guid? CurrentCharacterId { get; init; }
    public ImmutableDictionary<Guid, CharacterData> Characters { get; init; }
    public ImmutableDictionary<Guid, ActivitySummary> ActiveActivities { get; init; }
    public bool IsLoading { get; init; }
    public string? Error { get; init; }
}

// 实时状态
public record RealtimeState
{
    public HubConnectionState ConnectionState { get; init; }
    public ImmutableHashSet<string> JoinedGroups { get; init; }
    public ImmutableQueue<RealtimeEvent> PendingEvents { get; init; }
    public DateTime LastHeartbeat { get; init; }
    public TimeSpan ServerTimeDrift { get; init; }
}
```

### 4.2 Actions & Reducers
```csharp
// Actions
public record LoadCharactersAction();
public record LoadCharactersSuccessAction(IEnumerable<CharacterData> Characters);
public record SwitchCharacterAction(Guid CharacterId);
public record UpdateCharacterAction(Guid CharacterId, CharacterUpdateData Data);
public record StartActivityAction(Guid CharacterId, ActivityRequest Request);
public record ActivityProgressUpdateAction(Guid ActivityId, double Progress);

// Reducer示例
public static class CharacterReducers
{
    [ReducerMethod]
    public static CharacterState ReduceSwitchCharacter(
        CharacterState state, 
        SwitchCharacterAction action)
    {
        return state with 
        { 
            CurrentCharacterId = action.CharacterId,
            // 触发预加载当前角色详情
        };
    }
}
```

### 4.3 Effects（副作用）
```csharp
public class CharacterEffects
{
    private readonly ICharacterService _characterService;
    private readonly ISignalRService _signalRService;
    
    [EffectMethod]
    public async Task HandleLoadCharacters(
        LoadCharactersAction action, 
        IDispatcher dispatcher)
    {
        try
        {
            var characters = await _characterService.GetCharactersAsync();
            dispatcher.Dispatch(new LoadCharactersSuccessAction(characters));
            
            // 加入SignalR组
            foreach (var character in characters)
            {
                await _signalRService.JoinCharacterGroupAsync(character.Id);
            }
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new LoadCharactersFailureAction(ex.Message));
        }
    }
}
```

---

## 5. 实时同步优化策略

### 5.1 SignalR Hub设计
```csharp
public interface IGameHub
{
    // 客户端调用
    Task JoinCharacterGroup(Guid characterId);
    Task LeaveCharacterGroup(Guid characterId);
    Task SubscribeToBattle(Guid battleId);
    Task RequestSync(SyncRequest request);
    
    // 服务器推送
    Task OnCharacterUpdate(CharacterUpdateEvent evt);
    Task OnActivityUpdate(ActivityUpdateEvent evt);
    Task OnBattleUpdate(BattleUpdateEvent evt);
    Task OnBulkUpdate(BulkUpdateEvent evt); // 批量更新
    Task OnTimeSyncPulse(TimeSyncData data);
}
```

### 5.2 事件合并与批处理
```csharp
public class EventAggregator
{
    private readonly Channel<GameEvent> _eventChannel;
    private readonly IDispatcher _dispatcher;
    private readonly TimeSpan _batchWindow = TimeSpan.FromMilliseconds(50);
    
    public async Task ProcessEventsAsync(CancellationToken ct)
    {
        var batch = new List<GameEvent>();
        using var timer = new PeriodicTimer(_batchWindow);
        
        while (!ct.IsCancellationRequested)
        {
            // 收集批次内的所有事件
            while (_eventChannel.Reader.TryRead(out var evt))
            {
                batch.Add(evt);
            }
            
            if (batch.Any())
            {
                // 合并同类事件
                var merged = MergeEvents(batch);
                // 批量分发
                _dispatcher.Dispatch(new BulkUpdateAction(merged));
                batch.Clear();
            }
            
            await timer.WaitForNextTickAsync(ct);
        }
    }
    
    private IEnumerable<GameEvent> MergeEvents(List<GameEvent> events)
    {
        // 按实体ID分组，保留最新状态
        return events
            .GroupBy(e => new { e.EntityType, e.EntityId })
            .Select(g => g.OrderByDescending(e => e.Timestamp).First());
    }
}
```

### 5.3 动态轮询策略
```csharp
public class AdaptivePollingService
{
    private readonly Dictionary<string, PollingContext> _contexts = new();
    
    public TimeSpan CalculateInterval(string key, PollingHints hints)
    {
        var context = _contexts.GetOrAdd(key, _ => new PollingContext());
        
        // 基础间隔
        var baseInterval = hints.DefaultInterval;
        
        // 根据活动状态调整
        if (hints.HasActiveActivities)
        {
            // 检查即将完成的活动
            var nearCompletion = hints.Activities
                .Where(a => a.RemainingSeconds < 10)
                .Any();
            
            if (nearCompletion)
            {
                baseInterval = TimeSpan.FromSeconds(1);
            }
            else if (hints.Activities.Any(a => a.RemainingSeconds < 30))
            {
                baseInterval = TimeSpan.FromSeconds(2);
            }
        }
        
        // 根据网络状态调整
        if (context.ConsecutiveFailures > 0)
        {
            baseInterval = baseInterval * Math.Pow(2, Math.Min(context.ConsecutiveFailures, 4));
        }
        
        // 窗口失焦降频
        if (!hints.IsWindowFocused)
        {
            baseInterval = baseInterval * 2;
        }
        
        // 限制范围
        return TimeSpan.FromMilliseconds(
            Math.Clamp(baseInterval.TotalMilliseconds, 1000, 30000)
        );
    }
}
```

---

## 6. 性能优化策略

### 6.1 虚拟化与懒加载
```csharp
@* 角色列表虚拟化 *@
<Virtualize Items="@FilteredCharacters" Context="character" ItemSize="120">
    <ItemContent>
        <CharacterCard Character="@character" />
    </ItemContent>
    <Placeholder>
        <CharacterCardSkeleton />
    </Placeholder>
</Virtualize>
```

### 6.2 缓存策略
```csharp
public class CacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILocalStorageService _localStorage;
    
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        CacheOptions options)
    {
        // L1: 内存缓存
        if (_memoryCache.TryGetValue(key, out T cached))
        {
            return cached;
        }
        
        // L2: LocalStorage
        if (options.UsePersistentCache)
        {
            var stored = await _localStorage.GetItemAsync<T>(key);
            if (stored != null && !IsExpired(key))
            {
                _memoryCache.Set(key, stored, options.MemoryExpiration);
                return stored;
            }
        }
        
        // L3: 远程获取
        var value = await factory();
        
        // 写入缓存
        _memoryCache.Set(key, value, options.MemoryExpiration);
        if (options.UsePersistentCache)
        {
            await _localStorage.SetItemAsync(key, value);
        }
        
        return value;
    }
}
```

### 6.3 组件优化
```csharp
// 使用 ShouldRender 控制渲染
public class ActivityProgressBar : ComponentBase
{
    private double _lastProgress;
    
    [Parameter] public Activity Activity { get; set; }
    
    protected override bool ShouldRender()
    {
        var currentProgress = Activity.GetProgress(DateTime.UtcNow);
        var shouldRender = Math.Abs(currentProgress - _lastProgress) > 0.01; // 1%变化才渲染
        
        if (shouldRender)
        {
            _lastProgress = currentProgress;
        }
        
        return shouldRender;
    }
}
```

---

## 7. 时间同步系统

### 7.1 时间同步服务
```csharp
public class TimeSyncService
{
    private TimeSpan _serverDrift = TimeSpan.Zero;
    private readonly Queue<TimeSyncSample> _samples = new(10);
    private readonly SemaphoreSlim _syncLock = new(1);
    
    public DateTime ServerNow => DateTime.UtcNow + _serverDrift;
    
    public async Task UpdateDrift(DateTime serverTime, TimeSpan latency)
    {
        await _syncLock.WaitAsync();
        try
        {
            var adjustedServerTime = serverTime + (latency / 2);
            var localTime = DateTime.UtcNow;
            var drift = adjustedServerTime - localTime;
            
            _samples.Enqueue(new TimeSyncSample
            {
                Drift = drift,
                Latency = latency,
                Timestamp = localTime
            });
            
            if (_samples.Count > 10)
                _samples.Dequeue();
            
            // 使用中位数减少异常值影响
            _serverDrift = CalculateMedianDrift();
        }
        finally
        {
            _syncLock.Release();
        }
    }
    
    private TimeSpan CalculateMedianDrift()
    {
        var validSamples = _samples
            .Where(s => DateTime.UtcNow - s.Timestamp < TimeSpan.FromMinutes(5))
            .OrderBy(s => s.Drift)
            .ToList();
        
        if (validSamples.Count == 0)
            return TimeSpan.Zero;
        
        return validSamples[validSamples.Count / 2].Drift;
    }
}
```

### 7.2 进度插值
```csharp
public class ProgressInterpolator
{
    private readonly TimeSyncService _timeSync;
    
    public double InterpolateProgress(
        DateTime startUtc,
        DateTime endUtc,
        InterpolationOptions options = null)
    {
        var now = _timeSync.ServerNow;
        
        if (now <= startUtc)
            return 0;
        
        if (now >= endUtc)
        {
            if (options?.ClampToMax == true)
                return 1.0;
            
            // 超时处理
            return 1.0 + (now - endUtc).TotalSeconds / 100; // 显示超时
        }
        
        var totalDuration = (endUtc - startUtc).TotalMilliseconds;
        var elapsed = (now - startUtc).TotalMilliseconds;
        var progress = elapsed / totalDuration;
        
        // 应用缓动函数
        if (options?.EasingFunction != null)
        {
            progress = options.EasingFunction(progress);
        }
        
        return Math.Clamp(progress, 0, 1);
    }
}
```

---

## 8. 查询与命令模式（CQRS）

### 8.1 查询设计
```csharp
// 查询定义
public record GetCharacterDetailQuery(Guid CharacterId) : IRequest<CharacterDetail>;
public record GetCharacterActivitiesQuery(Guid CharacterId) : IRequest<IEnumerable<Activity>>;
public record GetBattleStateQuery(Guid BattleId) : IRequest<BattleState>;

// 查询处理器
public class GetCharacterDetailQueryHandler : IRequestHandler<GetCharacterDetailQuery, CharacterDetail>
{
    private readonly ICharacterRepository _repository;
    private readonly ICacheService _cache;
    
    public async Task<CharacterDetail> Handle(
        GetCharacterDetailQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"character:{request.CharacterId}";
        
        return await _cache.GetOrCreateAsync(
            cacheKey,
            async () => await _repository.GetDetailAsync(request.CharacterId),
            new CacheOptions 
            { 
                MemoryExpiration = TimeSpan.FromSeconds(30),
                UsePersistentCache = true 
            }
        );
    }
}
```

### 8.2 命令设计
```csharp
// 命令定义
public record StartBattleCommand(
    Guid CharacterId,
    Guid EnemyId,
    BattleOptions Options) : IRequest<BattleResult>;

// 命令处理器
public class StartBattleCommandHandler : IRequestHandler<StartBattleCommand, BattleResult>
{
    private readonly IBattleService _battleService;
    private readonly IValidator<StartBattleCommand> _validator;
    private readonly IEventBus _eventBus;
    
    public async Task<BattleResult> Handle(
        StartBattleCommand request,
        CancellationToken cancellationToken)
    {
        // 验证
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);
        
        // 执行
        var result = await _battleService.StartBattleAsync(
            request.CharacterId,
            request.EnemyId,
            request.Options
        );
        
        // 发布事件
        await _eventBus.PublishAsync(new BattleStartedEvent
        {
            BattleId = result.BattleId,
            CharacterId = request.CharacterId,
            StartTime = DateTime.UtcNow
        });
        
        return result;
    }
}
```

---

## 9. 多角色管理系统

### 9.1 角色切换器组件
```razor
@* Components/CharacterSwitcher.razor *@
@inherits FluxorComponent

<div class="character-switcher">
    @foreach (var character in State.Value.Characters.Characters.Values)
    {
        <CharacterTab 
            Character="@character"
            IsActive="@(character.Id == State.Value.Characters.CurrentCharacterId)"
            Activities="@GetCharacterActivities(character.Id)"
            OnClick="@(() => SwitchCharacter(character.Id))" />
    }
    @if (State.Value.Characters.Characters.Count < 5)
    {
        <button class="create-character-btn" @onclick="ShowCreateCharacterDialog">
            <i class="fas fa-plus"></i>
        </button>
    }
</div>

@code {
    [Inject] private IState<AppState> State { get; set; }
    [Inject] private IDispatcher Dispatcher { get; set; }
    
    private void SwitchCharacter(Guid characterId)
    {
        Dispatcher.Dispatch(new SwitchCharacterAction(characterId));
    }
    
    private IEnumerable<ActivitySummary> GetCharacterActivities(Guid characterId)
    {
        return State.Value.Activities.ActiveActivities
            .Where(a => a.Value.CharacterId == characterId)
            .Select(a => a.Value)
            .OrderBy(a => a.Priority)
            .Take(2); // 只显示最重要的2个活动
    }
}
```

### 9.2 活动优先级系统
```csharp
public static class ActivityPriorityCalculator
{
    public static int CalculatePriority(Activity activity, DateTime serverNow)
    {
        var baseScore = activity.Type switch
        {
            ActivityType.Battle => 1000,
            ActivityType.Boss => 1100,
            ActivityType.Crafting => 800,
            ActivityType.Gathering => 600,
            ActivityType.Quest => 700,
            ActivityType.Idle => 100,
            _ => 500
        };
        
        // 即将完成的活动提高优先级
        if (activity.EndTimeUtc.HasValue)
        {
            var remaining = (activity.EndTimeUtc.Value - serverNow).TotalSeconds;
            if (remaining < 10)
                baseScore += 500;
            else if (remaining < 30)
                baseScore += 200;
        }
        
        // 需要玩家干预的活动
        if (activity.RequiresInteraction)
            baseScore += 300;
        
        return baseScore;
    }
}
```

---

## 10. 错误处理与恢复

### 10.1 全局错误边界
```csharp
public class GlobalErrorBoundary : ErrorBoundary
{
    [Inject] private ILogger<GlobalErrorBoundary> Logger { get; set; }
    [Inject] private IErrorReportingService ErrorReporting { get; set; }
    
    protected override async Task OnErrorAsync(Exception exception)
    {
        Logger.LogError(exception, "Unhandled error in component");
        
        // 报告到服务器
        await ErrorReporting.ReportAsync(new ErrorReport
        {
            Exception = exception,
            Context = CurrentContext,
            UserAgent = await JSRuntime.InvokeAsync<string>("getUserAgent")
        });
        
        // 显示友好错误信息
        await ShowErrorNotification(exception);
        
        // 尝试恢复
        if (CanRecover(exception))
        {
            await AttemptRecovery();
        }
    }
}
```

### 10.2 重试策略
```csharp
public class RetryPolicyFactory
{
    public IAsyncPolicy<T> CreatePolicy<T>(RetryOptions options)
    {
        var retryPolicy = Policy<T>
            .HandleResult(r => !IsSuccessful(r))
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                options.MaxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: async (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.Values["Logger"] as ILogger;
                    logger?.LogWarning(
                        "Retry {RetryCount} after {Delay}ms",
                        retryCount,
                        timespan.TotalMilliseconds
                    );
                });
        
        var circuitBreakerPolicy = Policy<T>
            .HandleResult(r => !IsSuccessful(r))
            .CircuitBreakerAsync(
                options.CircuitBreakerThreshold,
                TimeSpan.FromSeconds(options.CircuitBreakerDuration)
            );
        
        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
    }
}
```

---

## 11. 开发工具与调试

### 11.1 开发者面板
```razor
@* Components/DevPanel.razor *@
@if (IsDevMode)
{
    <div class="dev-panel @(IsExpanded ? "expanded" : "collapsed")">
        <div class="dev-header" @onclick="ToggleExpanded">
            <span>Dev Tools</span>
            <span class="status">
                Drift: @ServerDrift.TotalMilliseconds.ToString("F0")ms |
                SignalR: @ConnectionState |
                Cache: @CacheStats
            </span>
        </div>
        
        @if (IsExpanded)
        {
            <div class="dev-content">
                <StateInspector />
                <EventLog />
                <PerformanceMetrics />
                <ActionButtons />
            </div>
        }
    </div>
}
```

### 11.2 性能监控
```csharp
public class PerformanceMonitor
{
    private readonly Dictionary<string, PerformanceMetric> _metrics = new();
    
    public IDisposable MeasureOperation(string operationName)
    {
        return new OperationTimer(operationName, this);
    }
    
    private class OperationTimer : IDisposable
    {
        private readonly string _name;
        private readonly PerformanceMonitor _monitor;
        private readonly Stopwatch _stopwatch;
        
        public OperationTimer(string name, PerformanceMonitor monitor)
        {
            _name = name;
            _monitor = monitor;
            _stopwatch = Stopwatch.StartNew();
        }
        
        public void Dispose()
        {
            _stopwatch.Stop();
            _monitor.RecordMetric(_name, _stopwatch.ElapsedMilliseconds);
        }
    }
}
```

---

## 12. 渐进式实施路线图

### Phase 1: 基础框架（第1-2周）
- [x] 项目结构搭建
- [x] Blazor + Fluxor基础配置
- [x] 认证系统（JWT Token）
- [x] 基础UI布局（顶部栏、侧边栏）
- [x] SignalR连接管理

### Phase 2: 角色系统（第3-4周）
- [ ] 角色列表与切换
- [ ] 角色详情展示
- [ ] 基础属性系统
- [ ] 角色创建/删除

### Phase 3: 活动系统（第5-6周）
- [ ] Activity抽象实现
- [ ] 进度条组件
- [ ] 活动列表展示
- [ ] 优先级排序

### Phase 4: 战斗系统（第7-8周）
- [ ] 战斗初始化
- [ ] 战斗进程管理
- [ ] 实时战斗更新
- [ ] 战斗结果处理

### Phase 5: 实时同步（第9-10周）
- [ ] 时间同步优化
- [ ] 事件批处理
- [ ] 断线重连优化
- [ ] 缓存策略完善

### Phase 6: 性能优化（第11-12周）
- [ ] 虚拟化实现
- [ ] 懒加载优化
- [ ] 内存管理
- [ ] 渲染优化

---

## 13. 关键配置与常量

```csharp
public static class GameConstants
{
    // 角色限制
    public const int MaxCharactersPerAccount = 5;
    public const int MaxActiveActivitiesPerCharacter = 3;
    
    // 轮询间隔
    public static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan BattlePollingInterval = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan FastPollingInterval = TimeSpan.FromSeconds(1);
    
    // 缓存配置
    public static readonly TimeSpan CharacterCacheExpiration = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan StaticDataCacheExpiration = TimeSpan.FromHours(1);
    
    // SignalR配置
    public static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);
    public static readonly int MaxReconnectAttempts = 5;
    
    // UI配置
    public const int MaxNotifications = 20;
    public const int ActivityListPageSize = 10;
}
```

---

## 14. 测试策略

### 14.1 单元测试
```csharp
[TestClass]
public class ActivityProgressTests
{
    [TestMethod]
    public void InterpolateProgress_ShouldReturnCorrectValue()
    {
        // Arrange
        var interpolator = new ProgressInterpolator(new MockTimeSyncService());
        var start = DateTime.UtcNow.AddSeconds(-30);
        var end = DateTime.UtcNow.AddSeconds(30);
        
        // Act
        var progress = interpolator.InterpolateProgress(start, end);
        
        // Assert
        Assert.AreEqual(0.5, progress, 0.01);
    }
}
```

### 14.2 集成测试
```csharp
[TestClass]
public class CharacterServiceIntegrationTests
{
    [TestMethod]
    public async Task SwitchCharacter_ShouldUpdateStateCorrectly()
    {
        // Arrange
        using var ctx = new TestContext();
        var store = ctx.Services.GetRequiredService<IStore>();
        var dispatcher = ctx.Services.GetRequiredService<IDispatcher>();
        
        // Act
        dispatcher.Dispatch(new SwitchCharacterAction(TestData.CharacterId));
        await Task.Delay(100); // 等待effects执行
        
        // Assert
        var state = store.Features[nameof(CharacterState)].GetState<CharacterState>();
        Assert.AreEqual(TestData.CharacterId, state.CurrentCharacterId);
    }
}
```

---

## 15. 安全考虑

### 15.1 输入验证
```csharp
public class StartBattleCommandValidator : AbstractValidator<StartBattleCommand>
{
    public StartBattleCommandValidator()
    {
        RuleFor(x => x.CharacterId)
            .NotEmpty()
            .Must(BeValidCharacterId)
            .WithMessage("Invalid character ID");
        
        RuleFor(x => x.EnemyId)
            .NotEmpty();
        
        RuleFor(x => x.Options)
            .NotNull()
            .SetValidator(new BattleOptionsValidator());
    }
    
    private bool BeValidCharacterId(Guid characterId)
    {
        // 验证角色属于当前用户
        return true; // 实际实现需要检查
    }
}
```

### 15.2 权限检查
```csharp
public class AuthorizationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAuthorizationService _authService;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IAuthorizable authorizable)
        {
            var authResult = await _authService.AuthorizeAsync(
                authorizable.GetRequiredPermissions()
            );
            
            if (!authResult.Succeeded)
                throw new UnauthorizedException();
        }
        
        return await next();
    }
}
```

---

## 16. 总结与下一步

### 核心改进点
1. **技术栈优化**: 采用Blazor实现C#全栈开发，类型安全性更高
2. **架构清晰**: Clean Architecture分层，职责明确
3. **性能提升**: 多级缓存、虚拟化、智能轮询
4. **实时同步**: 事件批处理、时间同步优化
5. **可测试性**: CQRS模式便于单元测试
6. **可扩展性**: 插件式架构，新功能易于添加

### 立即行动项
1. 创建项目基础结构
2. 配置Blazor + Fluxor + SignalR
3. 实现认证系统
4. 搭建基础UI框架
5. 实现第一个角色切换功能

### 风险缓解
- **性能问题**: 提前做性能基准测试
- **状态复杂度**: 使用Fluxor DevTools监控
- **网络不稳定**: 实现完善的重连机制
- **浏览器兼容**: 定期在目标浏览器测试

这份优化架构提供了更具体的实现细节和代码示例，可以直接用于指导开发。