using BlazorWebGame.Refactored.Domain.Entities;
using BlazorWebGame.Refactored.Domain.ValueObjects;
using BlazorWebGame.Refactored.Presentation.State;
using Fluxor;

namespace BlazorWebGame.Refactored.Infrastructure.Services;

/// <summary>
/// 示例数据生成服务，用于演示和开发
/// </summary>
public class SampleDataService
{
    private readonly IDispatcher _dispatcher;
    private readonly Random _random = new();

    public SampleDataService(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// 生成示例角色数据
    /// </summary>
    public async Task GenerateSampleCharactersAsync()
    {
        var sampleCharacters = new[]
        {
            new { Name = "勇敢的战士", Class = CharacterClass.Warrior, Level = 12 },
            new { Name = "智慧法师", Class = CharacterClass.Mage, Level = 8 },
            new { Name = "敏捷射手", Class = CharacterClass.Archer, Level = 15 }
        };

        foreach (var character in sampleCharacters)
        {
            // 创建角色
            _dispatcher.Dispatch(new CreateCharacterAction(character.Name, character.Class));
            
            // 等待一小段时间确保状态更新
            await Task.Delay(50);
        }
    }

    /// <summary>
    /// 为指定角色生成示例活动
    /// </summary>
    public void GenerateSampleActivitiesForCharacter(Guid characterId)
    {
        var activityTypes = Enum.GetValues<ActivityType>();
        var numberOfActivities = _random.Next(1, 3); // 1-2个活动

        for (int i = 0; i < numberOfActivities; i++)
        {
            var activityType = activityTypes[_random.Next(activityTypes.Length)];
            var parameters = GenerateActivityParameters(activityType);
            
            var request = new ActivityRequest
            {
                Type = activityType,
                Parameters = parameters.Parameters,
                Priority = _random.Next(1, 5),
                AllowInterrupt = true
            };
            
            _dispatcher.Dispatch(new StartActivityAction(characterId, request));
        }
    }

    /// <summary>
    /// 生成完整的演示数据集
    /// </summary>
    public async Task GenerateFullDemoDataAsync()
    {
        // 生成示例角色
        await GenerateSampleCharactersAsync();
        
        // 等待状态更新完成
        await Task.Delay(200);
        
        // 为每个角色添加一些示例活动
        // 注意：在实际应用中，我们需要从状态中获取角色ID
        // 这里我们使用预定义的示例数据
    }

    /// <summary>
    /// 重置所有数据
    /// </summary>
    public void ResetAllData()
    {
        _dispatcher.Dispatch(new ClearAllDataAction());
    }

    private ActivityParameters GenerateActivityParameters(ActivityType type)
    {
        var parameters = new ActivityParameters();
        
        return type switch
        {
            ActivityType.Battle => GenerateBattleParameters(parameters),
            ActivityType.Gathering => GenerateGatheringParameters(parameters),
            ActivityType.Crafting => GenerateCraftingParameters(parameters),
            _ => parameters
        };
    }

    private ActivityParameters GenerateBattleParameters(ActivityParameters parameters)
    {
        // 生成随机敌人ID
        parameters.SetValue("EnemyId", Guid.NewGuid());
        parameters.SetValue("EnemyName", GetRandomEnemyName());
        parameters.SetValue("EnemyLevel", _random.Next(1, 20));
        return parameters;
    }

    private ActivityParameters GenerateGatheringParameters(ActivityParameters parameters)
    {
        var gatheringTypes = Enum.GetValues<SampleGatheringType>();
        var randomType = gatheringTypes[_random.Next(gatheringTypes.Length)];
        
        // Map to domain GatheringType - simplified mapping
        var domainGatheringType = randomType switch
        {
            SampleGatheringType.Mining => Domain.ValueObjects.GatheringType.Mining,
            SampleGatheringType.Herbalism => Domain.ValueObjects.GatheringType.Herbalism,
            SampleGatheringType.Fishing => Domain.ValueObjects.GatheringType.Fishing,
            SampleGatheringType.Logging => Domain.ValueObjects.GatheringType.Logging,
            _ => Domain.ValueObjects.GatheringType.Mining
        };
        
        parameters.SetValue("GatheringType", domainGatheringType);
        parameters.SetValue("Location", GetRandomLocation());
        parameters.SetValue("EstimatedTime", TimeSpan.FromMinutes(_random.Next(5, 30)));
        return parameters;
    }

    private ActivityParameters GenerateCraftingParameters(ActivityParameters parameters)
    {
        var recipe = GenerateRandomRecipe();
        parameters.SetValue("Recipe", recipe);
        parameters.SetValue("Quantity", _random.Next(1, 5));
        return parameters;
    }

    private Recipe GenerateRandomRecipe()
    {
        var recipes = new[]
        {
            "铁剑", "皮甲", "生命药水", "魔法卷轴", "箭矢"
        };
        
        var recipeName = recipes[_random.Next(recipes.Length)];
        return new Recipe 
        { 
            Name = recipeName,
            Level = _random.Next(1, 10),
            Materials = GenerateRandomMaterials()
        };
    }

    private Dictionary<string, int> GenerateRandomMaterials()
    {
        var materials = new[] { "铁矿", "皮革", "木材", "魔法水晶", "药草" };
        var result = new Dictionary<string, int>();
        
        var materialCount = _random.Next(2, 4);
        for (int i = 0; i < materialCount; i++)
        {
            var material = materials[_random.Next(materials.Length)];
            if (!result.ContainsKey(material))
            {
                result[material] = _random.Next(1, 5);
            }
        }
        
        return result;
    }

    private string GetRandomEnemyName()
    {
        var enemies = new[]
        {
            "哥布林战士", "骷髅弓箭手", "森林狼", "石头巨魔", "暗影刺客",
            "火焰法师", "冰霜巨人", "毒蛛女王", "龙族守卫", "恶魔领主"
        };
        
        return enemies[_random.Next(enemies.Length)];
    }

    private string GetRandomLocation()
    {
        var locations = new[]
        {
            "新手村附近", "暗黑森林", "荒芜山脉", "水晶洞穴", "古老遗迹",
            "魔法花园", "龙之谷", "精灵森林", "矮人矿山", "海边悬崖"
        };
        
        return locations[_random.Next(locations.Length)];
    }
}

/// <summary>
/// 简化的配方类，用于演示
/// </summary>
public class Recipe
{
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public Dictionary<string, int> Materials { get; set; } = new();
}

/// <summary>
/// 采集类型枚举
/// </summary>
public enum SampleGatheringType
{
    Mining,    // 挖矿
    Herbalism, // 草药学
    Fishing,   // 钓鱼
    Logging    // 伐木
}