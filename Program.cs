using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.Models;
using BlazorWebGame.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.WriteLine("=== 服务端战斗系统测试 ===");

// 设置依赖注入和日志
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddSingleton<ServerSkillSystem>();
services.AddSingleton<ServerLootService>();
services.AddSingleton<ServerCombatEngine>();

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var skillSystem = serviceProvider.GetRequiredService<ServerSkillSystem>();
var lootService = serviceProvider.GetRequiredService<ServerLootService>();
var combatEngine = serviceProvider.GetRequiredService<ServerCombatEngine>();

// 创建测试战斗
var battle = new ServerBattleContext
{
    BattleId = Guid.NewGuid(),
    BattleType = "Test"
};

// 创建玩家
var player = new ServerBattlePlayer
{
    Id = "player-1",
    Name = "测试英雄",
    Health = 100,
    MaxHealth = 100,
    BaseAttackPower = 20,
    AttacksPerSecond = 1.2,
    Level = 5,
    SelectedBattleProfession = "Warrior",
    CriticalChance = 0.15,
    CriticalMultiplier = 2.0,
    EquippedSkills = new List<string> { "warrior_charge" }
};
battle.Players.Add(player);

// 创建敌人
var enemy = new ServerBattleEnemy
{
    Id = "enemy-1",
    Name = "测试哥布林",
    Health = 80,
    MaxHealth = 80,
    BaseAttackPower = 15,
    AttacksPerSecond = 1.0,
    Level = 4,
    XpReward = 50,
    MinGoldReward = 10,
    MaxGoldReward = 25,
    EnemyType = "Goblin",
    LootTable = new Dictionary<string, double>
    {
        { "iron_sword", 0.3 },
        { "health_potion", 0.5 }
    }
};
battle.Enemies.Add(enemy);

logger.LogInformation("创建战斗: {PlayerName} vs {EnemyName}", player.Name, enemy.Name);

// 模拟战斗
int round = 1;
while (battle.HasActiveParticipants && round <= 5)
{
    logger.LogInformation("=== 第 {Round} 回合 ===", round);
    
    logger.LogInformation("玩家血量: {Health}/{MaxHealth}, 敌人血量: {EnemyHealth}/{EnemyMaxHealth}",
        player.Health, player.MaxHealth, enemy.Health, enemy.MaxHealth);

    // 玩家攻击
    if (player.IsAlive)
    {
        combatEngine.ProcessPlayerAttack(battle, player, 1.0);
        
        // 有概率使用技能
        if (Random.Shared.NextDouble() < 0.4) // 40% 概率
        {
            combatEngine.ExecuteSkillAttack(battle, player, "warrior_charge", enemy.Id);
        }
    }

    // 敌人攻击
    if (enemy.IsAlive)
    {
        combatEngine.ProcessEnemyAttack(battle, enemy, 1.0);
    }

    // 显示战斗动作
    var recentActions = battle.ActionHistory.TakeLast(3);
    foreach (var action in recentActions)
    {
        logger.LogInformation("动作: {ActorName} 对 {TargetName} 使用 {ActionType}{Skill}, 造成 {Damage} 伤害{Critical}",
            action.ActorName, action.TargetName, action.ActionType,
            !string.IsNullOrEmpty(action.SkillId) ? $"({action.SkillId})" : "",
            action.Damage, action.IsCritical ? " (暴击!)" : "");
    }

    Thread.Sleep(1000); // 暂停1秒观察效果
    round++;
}

// 战斗结果
bool victory = battle.Players.Any(p => p.IsAlive);
var result = combatEngine.CalculateBattleRewards(battle, victory);

logger.LogInformation("=== 战斗结束 ===");
logger.LogInformation("胜利方: {Winner}", victory ? "玩家胜利" : "敌人胜利");

if (victory)
{
    logger.LogInformation("奖励: {Exp} 经验, {Gold} 金币", result.ExperienceGained, result.GoldGained);
    if (result.ItemsLooted.Any())
    {
        logger.LogInformation("掉落物品: {Items}", string.Join(", ", result.ItemsLooted));
    }
}

// 技能系统测试
logger.LogInformation("\n=== 技能系统测试 ===");
var testSkills = new[] { "warrior_charge", "mage_fireball", "rogue_backstab" };

foreach (var skillId in testSkills)
{
    var skill = skillSystem.GetSkillById(skillId);
    if (skill != null)
    {
        logger.LogInformation("技能: {Name} - {Description}, 效果值: {Value}, 冷却: {Cooldown}秒",
            skill.Name, skill.Description, skill.EffectValue, skill.CooldownSeconds);
    }
}

logger.LogInformation("=== 测试完成 ===");
