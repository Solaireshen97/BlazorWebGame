using System.Text.Json;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace BlazorWebGame.Refactored.State.Reactive;

/// <summary>
/// 状态动作接口
/// </summary>
public interface IStateAction
{
    void Execute(object state);
}

/// <summary>
/// 响应式状态存储
/// </summary>
public class ReactiveStore<TState> : IDisposable where TState : class, new()
{
    private readonly BehaviorSubject<TState> _state;
    private readonly Subject<IStateAction> _actions = new();
    private readonly ILogger<ReactiveStore<TState>>? _logger;
    
    public IObservable<TState> State => _state.AsObservable();
    public TState CurrentState => _state.Value;
    
    public ReactiveStore(TState? initialState = null, ILogger<ReactiveStore<TState>>? logger = null)
    {
        _logger = logger;
        _state = new BehaviorSubject<TState>(initialState ?? new TState());
        
        // 处理动作
        _actions.Subscribe(action =>
        {
            try
            {
                var newState = ProcessAction(action, _state.Value);
                _state.OnNext(newState);
                _logger?.LogDebug("State updated by action {ActionType}", action.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing action {ActionType}", action.GetType().Name);
            }
        });
    }
    
    public void Dispatch(IStateAction action)
    {
        _actions.OnNext(action);
    }
    
    public IObservable<TProjection> Select<TProjection>(Func<TState, TProjection> selector)
    {
        return State.Select(selector).DistinctUntilChanged();
    }
    
    public IObservable<TProjection> SelectMany<TProjection>(Func<TState, IObservable<TProjection>> selector)
    {
        return State.SelectMany(selector);
    }
    
    public IObservable<TProjection> Where<TProjection>(Func<TState, bool> predicate, Func<TState, TProjection> selector)
    {
        return State.Where(predicate).Select(selector);
    }
    
    private TState ProcessAction(IStateAction action, TState currentState)
    {
        // 使用 JSON 序列化实现 Immer 风格的不可变更新
        var stateJson = JsonSerializer.Serialize(currentState);
        var draft = JsonSerializer.Deserialize<TState>(stateJson);
        
        if (draft != null)
        {
            action.Execute(draft);
            return draft;
        }
        
        return currentState;
    }
    
    public void Dispose()
    {
        _actions?.Dispose();
        _state?.Dispose();
    }
}

/// <summary>
/// 游戏状态
/// </summary>
public class GameState
{
    public CharacterState? Character { get; set; }
    public BattleState? Battle { get; set; }
    public InventoryState Inventory { get; set; } = new();
    public UIState UI { get; set; } = new();
    public SystemState System { get; set; } = new();
}

/// <summary>
/// 角色状态
/// </summary>
public class CharacterState
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public long Experience { get; set; }
    public long Gold { get; set; }
    public Dictionary<string, int> Stats { get; set; } = new();
    public Dictionary<string, object> Activities { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime LastUpdate { get; set; }
}

/// <summary>
/// 战斗状态
/// </summary>
public class BattleState
{
    public Guid BattleId { get; set; }
    public string BattleType { get; set; } = string.Empty;
    public List<BattleParticipant> Players { get; set; } = new();
    public List<BattleParticipant> Enemies { get; set; } = new();
    public int WaveNumber { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime StartTime { get; set; }
}

/// <summary>
/// 战斗参与者
/// </summary>
public class BattleParticipant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public Dictionary<string, object> Stats { get; set; } = new();
    public bool IsAlive { get; set; } = true;
}

/// <summary>
/// 物品栏状态
/// </summary>
public class InventoryState
{
    public List<InventoryItem> Items { get; set; } = new();
    public int MaxSlots { get; set; } = 50;
    public Dictionary<string, int> Materials { get; set; } = new();
}

/// <summary>
/// 物品栏物品
/// </summary>
public class InventoryItem
{
    public string ItemId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Rarity { get; set; } = "Common";
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// UI状态
/// </summary>
public class UIState
{
    public string ActivePage { get; set; } = "Home";
    public bool IsLoading { get; set; }
    public List<NotificationMessage> Notifications { get; set; } = new();
    public Dictionary<string, bool> Modals { get; set; } = new();
    public Dictionary<string, object> ComponentStates { get; set; } = new();
}

/// <summary>
/// 通知消息
/// </summary>
public class NotificationMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info"; // Success, Warning, Error, Info
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int Duration { get; set; } = 5000; // milliseconds
}

/// <summary>
/// 系统状态
/// </summary>
public class SystemState
{
    public bool IsOnline { get; set; } = true;
    public DateTime ServerTime { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Settings { get; set; } = new();
    public List<string> Logs { get; set; } = new();
}

/// <summary>
/// 游戏存储
/// </summary>
public class GameStore : ReactiveStore<GameState>
{
    public GameStore(ILogger<ReactiveStore<GameState>>? logger = null) 
        : base(new GameState(), logger) { }
    
    // 角色相关选择器
    public IObservable<CharacterState?> Character => Select(s => s.Character);
    public IObservable<long> Gold => Select(s => s.Character?.Gold ?? 0);
    public IObservable<int> Level => Select(s => s.Character?.Level ?? 0);
    public IObservable<string> CharacterName => Select(s => s.Character?.Name ?? string.Empty);
    
    // 战斗相关选择器
    public IObservable<bool> IsInBattle => Select(s => s.Battle != null);
    public IObservable<BattleState?> Battle => Select(s => s.Battle);
    
    // 物品栏选择器
    public IObservable<List<InventoryItem>> Items => Select(s => s.Inventory.Items);
    public IObservable<int> ItemCount => Select(s => s.Inventory.Items.Count);
    public IObservable<Dictionary<string, int>> Materials => Select(s => s.Inventory.Materials);
    
    // UI选择器
    public IObservable<string> ActivePage => Select(s => s.UI.ActivePage);
    public IObservable<bool> IsLoading => Select(s => s.UI.IsLoading);
    public IObservable<List<NotificationMessage>> Notifications => Select(s => s.UI.Notifications);
    
    // 系统选择器
    public IObservable<bool> IsOnline => Select(s => s.System.IsOnline);
    public IObservable<DateTime> ServerTime => Select(s => s.System.ServerTime);
}

/// <summary>
/// 示例动作
/// </summary>
public class UpdateCharacterAction : IStateAction
{
    private readonly CharacterState _character;
    
    public UpdateCharacterAction(CharacterState character)
    {
        _character = character;
    }
    
    public void Execute(object state)
    {
        if (state is GameState gameState)
        {
            gameState.Character = _character;
        }
    }
}

public class AddItemAction : IStateAction
{
    private readonly InventoryItem _item;
    
    public AddItemAction(InventoryItem item)
    {
        _item = item;
    }
    
    public void Execute(object state)
    {
        if (state is GameState gameState)
        {
            gameState.Inventory.Items.Add(_item);
        }
    }
}

public class StartBattleAction : IStateAction
{
    private readonly BattleState _battle;
    
    public StartBattleAction(BattleState battle)
    {
        _battle = battle;
    }
    
    public void Execute(object state)
    {
        if (state is GameState gameState)
        {
            gameState.Battle = _battle;
        }
    }
}

public class EndBattleAction : IStateAction
{
    public void Execute(object state)
    {
        if (state is GameState gameState)
        {
            gameState.Battle = null;
        }
    }
}

public class AddNotificationAction : IStateAction
{
    private readonly NotificationMessage _notification;
    
    public AddNotificationAction(NotificationMessage notification)
    {
        _notification = notification;
    }
    
    public void Execute(object state)
    {
        if (state is GameState gameState)
        {
            gameState.UI.Notifications.Add(_notification);
        }
    }
}

public class RemoveNotificationAction : IStateAction
{
    private readonly Guid _notificationId;
    
    public RemoveNotificationAction(Guid notificationId)
    {
        _notificationId = notificationId;
    }
    
    public void Execute(object state)
    {
        if (state is GameState gameState)
        {
            gameState.UI.Notifications.RemoveAll(n => n.Id == _notificationId);
        }
    }
}

public class SetLoadingAction : IStateAction
{
    private readonly bool _isLoading;
    
    public SetLoadingAction(bool isLoading)
    {
        _isLoading = isLoading;
    }
    
    public void Execute(object state)
    {
        if (state is GameState gameState)
        {
            gameState.UI.IsLoading = _isLoading;
        }
    }
}

public class SetActivePageAction : IStateAction
{
    private readonly string _page;
    
    public SetActivePageAction(string page)
    {
        _page = page;
    }
    
    public void Execute(object state)
    {
        if (state is GameState gameState)
        {
            gameState.UI.ActivePage = _page;
        }
    }
}