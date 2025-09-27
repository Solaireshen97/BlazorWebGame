using BlazorWebGame.Refactored.Domain.Entities;
using BlazorWebGame.Refactored.Domain.ValueObjects;
using BlazorWebGame.Refactored.Domain.Events;
using BlazorWebGame.Refactored.Infrastructure.Persistence;
using BlazorWebGame.Refactored.Infrastructure.Events.Core;

namespace BlazorWebGame.Refactored.Domain.Services;

/// <summary>
/// 角色领域服务 - 处理复杂的业务逻辑
/// </summary>
public class CharacterDomainService
{
    private readonly ICharacterRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CharacterDomainService> _logger;
    
    public CharacterDomainService(
        ICharacterRepository repository, 
        IEventBus eventBus,
        ILogger<CharacterDomainService> logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }
    
    /// <summary>
    /// 创建新角色
    /// </summary>
    public async Task<Result<Character>> CreateCharacterAsync(
        CharacterName name, 
        CharacterClass characterClass, 
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 业务规则验证
            var nameValidation = await ValidateCharacterNameAsync(name, cancellationToken);
            if (!nameValidation.IsSuccess)
                return Result<Character>.Failure(nameValidation.Error);
            
            // 检查用户角色数量限制
            var characterCount = await _repository.CountByUserIdAsync(userId.Value, cancellationToken);
            if (characterCount >= 5)
                return Result<Character>.Failure("Maximum character limit reached");
            
            // 使用工厂创建角色
            var character = Character.Create(name.Value, characterClass, userId.Value);
            
            // 应用初始化规则
            ApplyStartingBonus(character, characterClass);
            
            // 保存到仓储
            await _repository.AddAsync(character, cancellationToken);
            
            // 发布领域事件 - 使用事件适配器
            var gameEvent = new NewCharacterCreatedEvent
            {
                CharacterId = character.Id.ToString(),
                Name = character.Name
            };
            await _eventBus.PublishAsync(gameEvent, cancellationToken);
            
            _logger.LogInformation("Character {CharacterName} created successfully for user {UserId}", 
                name.Value, userId.Value);
            
            return Result<Character>.Success(character);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating character {CharacterName} for user {UserId}", 
                name.Value, userId.Value);
            return Result<Character>.Failure("An error occurred while creating the character");
        }
    }
    
    private async Task<ValueObjects.Result> ValidateCharacterNameAsync(CharacterName name, CancellationToken cancellationToken)
    {
        // 检查名称唯一性
        var exists = await _repository.ExistsByNameAsync(name.Value, cancellationToken);
        if (exists)
            return ValueObjects.Result.Failure($"Character name '{name}' is already taken");
        
        // 检查名称是否包含禁用词
        if (ContainsForbiddenWords(name))
            return ValueObjects.Result.Failure("Character name contains forbidden words");
        
        return ValueObjects.Result.Success();
    }
    
    private bool ContainsForbiddenWords(CharacterName name)
    {
        // 实现禁用词检查逻辑
        var forbiddenWords = new[] { "admin", "gm", "moderator", "系统", "管理员" };
        return forbiddenWords.Any(word => 
            name.Value.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
    
    private void ApplyStartingBonus(Character character, CharacterClass characterClass)
    {
        // 根据职业应用初始加成
        switch (characterClass)
        {
            case CharacterClass.Warrior:
                // 战士获得额外力量和生命值
                character.Stats = character.Stats with 
                { 
                    Strength = character.Stats.Strength + 5,
                    Vitality = character.Stats.Vitality + 3
                };
                break;
            case CharacterClass.Mage:
                // 法师获得额外智力和魔法值
                character.Stats = character.Stats with 
                { 
                    Intelligence = character.Stats.Intelligence + 5,
                    Vitality = character.Stats.Vitality + 2
                };
                break;
            case CharacterClass.Archer:
                // 弓箭手获得额外敏捷
                character.Stats = character.Stats with 
                { 
                    Agility = character.Stats.Agility + 5,
                    Strength = character.Stats.Strength + 2
                };
                break;
        }
        
        // 设置初始资源
        character.Resources = character.Resources.AddGold(new BigNumber(1000));
        
        // 添加新手礼包材料
        character.Resources = character.Resources
            .AddMaterial("wood", 10)
            .AddMaterial("stone", 10)
            .AddMaterial("iron_ore", 5);
    }
}