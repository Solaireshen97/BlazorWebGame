using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server;

/// <summary>
/// 测试类，演示服务端战斗系统的功能
/// </summary>
public class TestBattleSystem
{
    public static void RunBattleTest(ILogger logger)
    {
        logger.LogInformation("=== 服务端战斗系统测试开始 ===");

        // 创建测试用的服务
        var skillSystem = new ServerSkillSystem(logger as ILogger<ServerSkillSystem>);
        var lootService = new ServerLootService(logger as ILogger<ServerLootService>);
        var combatEngine = new ServerCombatEngine(logger as ILogger<ServerCombatEngine>, skillSystem, lootService);

        // 创建测试战斗上下文
        var battle = CreateTestBattle();
        logger.LogInformation("创建测试战斗: 玩家 {PlayerName} vs 敌人 {EnemyName}", 
            battle.Players[0].Name, battle.Enemies[0].Name);

        // 模拟战斗循环
        int round = 1;
        while (battle.HasActiveParticipants && round <= 10)
        {
            logger.LogInformation("=== 第 {Round} 回合 ===", round);
            
            var player = battle.Players[0];
            var enemy = battle.Enemies[0];

            logger.LogInformation("战斗前状态 - 玩家血量: {PlayerHealth}/{PlayerMaxHealth}, 敌人血量: {EnemyHealth}/{EnemyMaxHealth}",
                player.Health, player.MaxHealth, enemy.Health, enemy.MaxHealth);

            // 处理玩家攻击
            if (player.IsAlive)
            {
                combatEngine.ProcessPlayerAttack(battle, player, 1.0);
            }

            // 处理敌人攻击
            if (enemy.IsAlive)
            {
                combatEngine.ProcessEnemyAttack(battle, enemy, 1.0);
            }

            // 显示最近的战斗动作
            var recentActions = battle.ActionHistory.TakeLast(2);
            foreach (var action in recentActions)
            {
                logger.LogInformation("动作: {ActorName} 对 {TargetName} 使用 {ActionType}, 造成 {Damage} 伤害{Critical}",
                    action.ActorName, action.TargetName, action.ActionType, action.Damage,
                    action.IsCritical ? " (暴击!)" : "");
            }

            round++;
        }

        // 计算战斗结果
        bool victory = battle.Players.Any(p => p.IsAlive);
        var battleResult = combatEngine.CalculateBattleRewards(battle, victory);

        logger.LogInformation("=== 战斗结束 ===");
        logger.LogInformation("胜利方: {Winner}", victory ? "玩家" : "敌人");
        if (victory)
        {
            logger.LogInformation("奖励: {Exp} 经验, {Gold} 金币, {Items} 个物品",
                battleResult.ExperienceGained, battleResult.GoldGained, battleResult.ItemsLooted.Count);
            
            if (battleResult.ItemsLooted.Any())
            {
                logger.LogInformation("掉落物品: {Items}", string.Join(", ", battleResult.ItemsLooted));
            }
        }

        // 测试技能系统
        TestSkillSystem(skillSystem, logger);

        logger.LogInformation("=== 服务端战斗系统测试完成 ===");
    }

    private static ServerBattleContext CreateTestBattle()
    {
        var battle = new ServerBattleContext
        {
            BattleId = Guid.NewGuid(),
            BattleType = "Test"
        };

        // 创建测试玩家
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
            EquippedSkills = new List<string> { "warrior_charge", "warrior_shield_bash" }
        };
        battle.Players.Add(player);

        // 创建测试敌人
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
            CriticalChance = 0.1,
            CriticalMultiplier = 1.8,
            LootTable = new Dictionary<string, double>
            {
                { "iron_sword", 0.2 },
                { "health_potion", 0.4 },
                { "gold_coin", 0.6 }
            },
            EquippedSkills = new List<string> { "goblin_slash" }
        };
        battle.Enemies.Add(enemy);

        return battle;
    }

    private static void TestSkillSystem(ServerSkillSystem skillSystem, ILogger logger)
    {
        logger.LogInformation("\n=== 技能系统测试 ===");

        // 测试获取技能信息
        var skills = new[] { "warrior_charge", "mage_fireball", "rogue_backstab" };
        
        foreach (var skillId in skills)
        {
            var skill = skillSystem.GetSkillById(skillId);
            if (skill != null)
            {
                logger.LogInformation("技能: {Name} ({Id}) - {Description}, 伤害: {Damage}, 冷却: {Cooldown}秒",
                    skill.Name, skill.Id, skill.Description, skill.EffectValue, skill.CooldownSeconds);
            }
        }

        // 创建简单的技能测试战斗
        var testBattle = new ServerBattleContext();
        var caster = new ServerBattlePlayer
        {
            Id = "skill-tester",
            Name = "技能测试者",
            Health = 100,
            MaxHealth = 100,
            BaseAttackPower = 25,
            EquippedSkills = new List<string> { "warrior_charge" }
        };
        var target = new ServerBattleEnemy
        {
            Id = "skill-target",
            Name = "技能目标",
            Health = 60,
            MaxHealth = 60
        };

        testBattle.Players.Add(caster);
        testBattle.Enemies.Add(target);

        logger.LogInformation("技能测试前 - 目标血量: {Health}/{MaxHealth}", target.Health, target.MaxHealth);

        // 测试技能执行
        bool skillUsed = skillSystem.ExecuteSkill("warrior_charge", caster, target, testBattle);
        
        logger.LogInformation("技能使用{Result} - 目标血量: {Health}/{MaxHealth}", 
            skillUsed ? "成功" : "失败", target.Health, target.MaxHealth);

        if (testBattle.ActionHistory.Any())
        {
            var action = testBattle.ActionHistory.Last();
            logger.LogInformation("技能效果: {ActorName} 使用 {SkillId} 对 {TargetName} 造成 {Damage} 伤害",
                action.ActorName, action.SkillId, action.TargetName, action.Damage);
        }
    }
}