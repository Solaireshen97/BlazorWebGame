using BlazorWebGame.Server.Services.Equipments;
using BlazorWebGame.Server.Services.Skill;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Server.Services.Battle;

/// <summary>
/// 服务端战斗引擎 - 从客户端战斗逻辑迁移而来
/// </summary>
public class ServerCombatEngine
{
    private readonly ILogger<ServerCombatEngine> _logger;
    private readonly ServerSkillSystem _skillSystem;
    private readonly ServerLootService _lootService;
    private readonly Random _random = new();

    public ServerCombatEngine(ILogger<ServerCombatEngine> logger, ServerSkillSystem skillSystem, ServerLootService lootService)
    {
        _logger = logger;
        _skillSystem = skillSystem;
        _lootService = lootService;
    }

    /// <summary>
    /// 处理玩家攻击逻辑
    /// </summary>
    public void ProcessPlayerAttack(ServerBattleContext battle, ServerBattlePlayer player, double deltaTime)
    {
        // 应用技能系统
        _skillSystem.ApplyPlayerSkills(player, null!, battle, deltaTime);
        
        player.AttackCooldown -= deltaTime;
        if (player.AttackCooldown <= 0)
        {
            // 选择目标
            var targetEnemy = SelectTargetForPlayer(battle, player);
            if (targetEnemy != null)
            {
                // 记录玩家的目标
                battle.PlayerTargets[player.Id] = targetEnemy.Id;

                // 执行攻击
                ExecutePlayerAttack(player, targetEnemy, battle);
            }

            // 重置冷却
            player.AttackCooldown += 1.0 / player.AttacksPerSecond;
        }
    }

    /// <summary>
    /// 处理敌人攻击逻辑
    /// </summary>
    public void ProcessEnemyAttack(ServerBattleContext battle, ServerBattleEnemy enemy, double deltaTime)
    {
        // 应用敌人技能系统
        var targetPlayer = SelectTargetForEnemy(battle, enemy);
        if (targetPlayer != null)
        {
            _skillSystem.ApplyEnemySkills(enemy, targetPlayer, battle, deltaTime);
        }
        
        enemy.AttackCooldown -= deltaTime;
        if (enemy.AttackCooldown <= 0)
        {
            // 选择目标
            if (targetPlayer != null)
            {
                // 执行攻击
                ExecuteEnemyAttack(enemy, targetPlayer, battle);
            }

            // 重置冷却
            enemy.AttackCooldown += 1.0 / enemy.AttacksPerSecond;
        }
    }

    /// <summary>
    /// 为玩家选择攻击目标
    /// </summary>
    private ServerBattleEnemy? SelectTargetForPlayer(ServerBattleContext battle, ServerBattlePlayer player)
    {
        // 优先攻击已锁定的目标
        if (battle.PlayerTargets.TryGetValue(player.Id, out var targetId))
        {
            var lockedTarget = battle.Enemies.FirstOrDefault(e => e.Id == targetId && e.IsAlive);
            if (lockedTarget != null)
                return lockedTarget;
        }

        // 选择血量最少的敌人
        return battle.Enemies
            .Where(e => e.IsAlive)
            .OrderBy(e => e.Health)
            .FirstOrDefault();
    }

    /// <summary>
    /// 为敌人选择攻击目标
    /// </summary>
    private ServerBattlePlayer? SelectTargetForEnemy(ServerBattleContext battle, ServerBattleEnemy enemy)
    {
        // 简单AI：攻击血量最少的玩家
        return battle.Players
            .Where(p => p.IsAlive)
            .OrderBy(p => p.Health)
            .FirstOrDefault();
    }

    /// <summary>
    /// 执行玩家攻击
    /// </summary>
    private void ExecutePlayerAttack(ServerBattlePlayer player, ServerBattleEnemy enemy, ServerBattleContext battle)
    {
        // 计算伤害
        var damage = CalculatePlayerDamage(player, enemy);
        
        // 记录原始血量
        int originalHealth = enemy.Health;
        
        // 应用伤害
        ApplyDamageToEnemy(enemy, damage);
        
        // 计算实际造成的伤害
        int actualDamage = originalHealth - enemy.Health;
        bool isCritical = _random.NextDouble() < player.CriticalChance;

        // 记录战斗动作
        var action = new ServerBattleAction
        {
            ActorId = player.Id,
            ActorName = player.Name,
            TargetId = enemy.Id,
            TargetName = enemy.Name,
            ActionType = "Attack",
            Damage = actualDamage,
            IsCritical = isCritical,
            Timestamp = DateTime.UtcNow
        };

        battle.ActionHistory.Add(action);
        
        _logger.LogDebug("Player {PlayerName} attacks {EnemyName} for {Damage} damage", 
            player.Name, enemy.Name, actualDamage);

        // 检查敌人是否死亡
        if (!enemy.IsAlive)
        {
            HandleEnemyDeath(enemy, battle);
        }
    }

    /// <summary>
    /// 执行敌人攻击
    /// </summary>
    private void ExecuteEnemyAttack(ServerBattleEnemy enemy, ServerBattlePlayer player, ServerBattleContext battle)
    {
        // 计算伤害
        var damage = CalculateEnemyDamage(enemy, player);
        
        // 记录原始血量
        int originalHealth = player.Health;
        
        // 应用伤害
        ApplyDamageToPlayer(player, damage);
        
        // 计算实际造成的伤害
        int actualDamage = originalHealth - player.Health;
        bool isCritical = _random.NextDouble() < enemy.CriticalChance;

        // 记录战斗动作
        var action = new ServerBattleAction
        {
            ActorId = enemy.Id,
            ActorName = enemy.Name,
            TargetId = player.Id,
            TargetName = player.Name,
            ActionType = "Attack",
            Damage = actualDamage,
            IsCritical = isCritical,
            Timestamp = DateTime.UtcNow
        };

        battle.ActionHistory.Add(action);
        
        _logger.LogDebug("Enemy {EnemyName} attacks {PlayerName} for {Damage} damage", 
            enemy.Name, player.Name, actualDamage);

        // 检查玩家是否死亡
        if (!player.IsAlive)
        {
            HandlePlayerDeath(player, battle);
        }
    }

    /// <summary>
    /// 计算玩家伤害
    /// </summary>
    private int CalculatePlayerDamage(ServerBattlePlayer player, ServerBattleEnemy enemy)
    {
        // 基础攻击力
        var baseDamage = player.BaseAttackPower;
        
        // 随机变动（±20%）
        var variance = 0.2;
        var randomFactor = 1.0 + (_random.NextDouble() * 2 - 1) * variance;
        var damage = (int)(baseDamage * randomFactor);
        
        // 暴击处理
        if (_random.NextDouble() < player.CriticalChance)
        {
            damage = (int)(damage * player.CriticalMultiplier);
        }
        
        return Math.Max(1, damage); // 至少造成1点伤害
    }

    /// <summary>
    /// 计算敌人伤害
    /// </summary>
    private int CalculateEnemyDamage(ServerBattleEnemy enemy, ServerBattlePlayer player)
    {
        // 基础攻击力
        var baseDamage = enemy.BaseAttackPower;
        
        // 随机变动（±20%）
        var variance = 0.2;
        var randomFactor = 1.0 + (_random.NextDouble() * 2 - 1) * variance;
        var damage = (int)(baseDamage * randomFactor);
        
        // 暴击处理
        if (_random.NextDouble() < enemy.CriticalChance)
        {
            damage = (int)(damage * enemy.CriticalMultiplier);
        }
        
        // 躲避检查
        if (_random.NextDouble() < player.DodgeChance)
        {
            return 0; // 完全躲避
        }
        
        return Math.Max(1, damage); // 至少造成1点伤害
    }

    /// <summary>
    /// 对敌人应用伤害
    /// </summary>
    private void ApplyDamageToEnemy(ServerBattleEnemy enemy, int damage)
    {
        enemy.Health = Math.Max(0, enemy.Health - damage);
    }

    /// <summary>
    /// 对玩家应用伤害
    /// </summary>
    private void ApplyDamageToPlayer(ServerBattlePlayer player, int damage)
    {
        player.Health = Math.Max(0, player.Health - damage);
    }

    /// <summary>
    /// 处理敌人死亡
    /// </summary>
    private void HandleEnemyDeath(ServerBattleEnemy enemy, ServerBattleContext battle)
    {
        var action = new ServerBattleAction
        {
            ActorId = enemy.Id,
            ActorName = enemy.Name,
            ActionType = "Death",
            Timestamp = DateTime.UtcNow
        };

        battle.ActionHistory.Add(action);
        _logger.LogInformation("Enemy {EnemyName} has been defeated in battle {BattleId}", 
            enemy.Name, battle.BattleId);
    }

    /// <summary>
    /// 处理玩家死亡
    /// </summary>
    private void HandlePlayerDeath(ServerBattlePlayer player, ServerBattleContext battle)
    {
        var action = new ServerBattleAction
        {
            ActorId = player.Id,
            ActorName = player.Name,
            ActionType = "Death",
            Timestamp = DateTime.UtcNow
        };

        battle.ActionHistory.Add(action);
        _logger.LogInformation("Player {PlayerName} has been defeated in battle {BattleId}", 
            player.Name, battle.BattleId);
    }

    /// <summary>
    /// 计算战斗奖励
    /// </summary>
    public BattleResultDto CalculateBattleRewards(ServerBattleContext battle, bool victory)
    {
        var result = _lootService.CalculateBattleRewards(battle, victory);
        
        // 分配经验给玩家
        if (victory && result.ExperienceGained > 0)
        {
            _lootService.DistributeExperienceToPlayers(battle.Players, result.ExperienceGained);
        }

        return result;
    }

    /// <summary>
    /// 执行技能攻击
    /// </summary>
    public bool ExecuteSkillAttack(ServerBattleContext battle, ServerBattlePlayer player, string skillId, string? targetId)
    {
        var target = battle.Enemies.FirstOrDefault(e => e.Id == targetId && e.IsAlive);
        if (target == null)
        {
            // 自动选择目标
            target = battle.Enemies.FirstOrDefault(e => e.IsAlive);
        }

        if (target != null)
        {
            return _skillSystem.ExecuteSkill(skillId, player, target, battle);
        }

        return false;
    }
}