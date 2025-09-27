using BlazorWebGame.Refactored.Domain.Entities;
using BlazorWebGame.Refactored.Domain.Events;
using BlazorWebGame.Refactored.Infrastructure.Events.Core;
using BlazorWebGame.Refactored.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BlazorWebGame.Refactored.Application.Services;

/// <summary>
/// 游戏状态管理器实现
/// </summary>
public sealed class GameStateManager : IGameStateManager, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly ICharacterRepository _characterRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GameStateManager> _logger;
    
    private Character? _currentCharacter;
    private readonly ConcurrentDictionary<string, object> _gameState = new();
    private readonly Timer _autoSaveTimer;
    
    public Character? CurrentCharacter => _currentCharacter;
    
    public GameStateManager(
        IEventBus eventBus,
        ICharacterRepository characterRepository,
        IMemoryCache cache,
        ILogger<GameStateManager> logger)
    {
        _eventBus = eventBus;
        _characterRepository = characterRepository;
        _cache = cache;
        _logger = logger;
        
        // 订阅需要更新状态的事件
        _eventBus.Subscribe<NewCharacterLeveledUpEvent>(HandleCharacterLeveledUp);
        _eventBus.Subscribe<ItemAcquiredEvent>(HandleItemAcquired);
        _eventBus.Subscribe<NewBattleEndedEvent>(HandleBattleEnded);
        _eventBus.Subscribe<CreateCharacterCommand>(HandleCreateCharacter);
        _eventBus.Subscribe<SelectCharacterCommand>(HandleSelectCharacter);
        
        // 设置自动保存
        _autoSaveTimer = new Timer(async _ => await AutoSaveAsync(), null, 
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }
    
    public async Task LoadCharacterAsync(string characterId, CancellationToken cancellationToken = default)
    {
        // 尝试从缓存加载
        if (_cache.TryGetValue<Character>($"character_{characterId}", out var cachedCharacter))
        {
            _currentCharacter = cachedCharacter;
            _logger.LogDebug("Character {CharacterId} loaded from cache", characterId);
            return;
        }
        
        // 从存储加载
        _currentCharacter = await _characterRepository.GetByIdAsync(characterId, cancellationToken);
        
        if (_currentCharacter != null)
        {
            // 添加到缓存
            _cache.Set($"character_{characterId}", _currentCharacter, TimeSpan.FromMinutes(30));
            _logger.LogInformation("Character {CharacterId} loaded", characterId);
        }
        else
        {
            _logger.LogWarning("Character {CharacterId} not found", characterId);
        }
    }
    
    public async Task SaveCharacterAsync(CancellationToken cancellationToken = default)
    {
        if (_currentCharacter == null) return;
        
        await _characterRepository.UpdateAsync(_currentCharacter, cancellationToken);
        _logger.LogDebug("Character {CharacterId} saved", _currentCharacter.Id);
    }
    
    public T? GetState<T>(string key) where T : class
    {
        return _gameState.TryGetValue(key, out var value) ? value as T : null;
    }
    
    public void SetState<T>(string key, T value) where T : class
    {
        _gameState[key] = value;
    }

    public async Task CreateCharacterAsync(string name, string userId, CancellationToken cancellationToken = default)
    {
        var character = Character.Create(name, Guid.Parse(userId));
        await _characterRepository.AddAsync(character, cancellationToken);
        
        await _eventBus.PublishAsync(new NewCharacterCreatedEvent
        {
            CharacterId = character.Id.ToString(),
            Name = character.Name
        }, cancellationToken);
        
        _logger.LogInformation("Character {Name} created for user {UserId}", name, userId);
    }

    public async Task<List<Character>> GetUserCharactersAsync(string userId, CancellationToken cancellationToken = default)
    {
        // 实现用户角色查询
        var characters = await _characterRepository.GetAllAsync(cancellationToken);
        return characters.Where(c => c.UserId.ToString() == userId).ToList();
    }
    
    private async Task HandleCharacterLeveledUp(NewCharacterLeveledUpEvent evt, CancellationToken cancellationToken)
    {
        if (_currentCharacter?.Id.ToString() != evt.CharacterId) return;
        
        // 角色升级逻辑，但不能直接修改只读属性
        _logger.LogDebug("Character {CharacterId} leveled up from {OldLevel} to {NewLevel}", 
            evt.CharacterId, evt.OldLevel, evt.NewLevel);
    }
    
    private async Task HandleItemAcquired(ItemAcquiredEvent evt, CancellationToken cancellationToken)
    {
        if (_currentCharacter?.Id.ToString() != evt.CharacterId) return;
        
        // 处理物品获得逻辑
        _logger.LogDebug("Character {CharacterId} acquired item {ItemId} x{Quantity}", 
            evt.CharacterId, evt.ItemId, evt.Quantity);
    }
    
    private async Task HandleBattleEnded(NewBattleEndedEvent evt, CancellationToken cancellationToken)
    {
        if (_currentCharacter == null) return;
        
        if (evt.Result.Victory)
        {
            // 处理战斗胜利奖励
            _logger.LogDebug("Character gained {Experience} experience and {Gold} gold", 
                evt.Result.ExperienceGained, evt.Result.GoldGained);
        }
    }

    private async Task HandleCreateCharacter(CreateCharacterCommand command, CancellationToken cancellationToken)
    {
        await CreateCharacterAsync(command.Name, command.UserId, cancellationToken);
    }

    private async Task HandleSelectCharacter(SelectCharacterCommand command, CancellationToken cancellationToken)
    {
        await LoadCharacterAsync(command.CharacterId, cancellationToken);
    }
    
    private async Task AutoSaveAsync()
    {
        try
        {
            await SaveCharacterAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto save failed");
        }
    }
    
    public void Dispose()
    {
        _autoSaveTimer?.Dispose();
        SaveCharacterAsync().Wait(TimeSpan.FromSeconds(5));
    }
}