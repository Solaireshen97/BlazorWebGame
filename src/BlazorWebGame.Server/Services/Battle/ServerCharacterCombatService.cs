using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Server.Services.Battle;

/// <summary>
/// 服务端角色战斗服务 - 从客户端迁移而来
/// 处理角色在战斗中的状态管理和行为逻辑
/// </summary>
public class ServerCharacterCombatService
{
    private readonly ILogger<ServerCharacterCombatService> _logger;
    private readonly Random _random = new();

    public ServerCharacterCombatService(ILogger<ServerCharacterCombatService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 计算角色伤害输出
    /// </summary>
    public double CalculatePlayerDamage(ServerBattlePlayer player, ServerBattleEnemy target)
    {
        // 基础伤害计算
        var baseDamage = CalculateBaseDamage(player);
        
        // 暴击计算
        var isCritical = IsCriticalHit(player);
        if (isCritical)
        {
            baseDamage *= GetCriticalMultiplier(player);
        }
        
        // 防御减免
        var finalDamage = ApplyDefenseReduction(baseDamage, target);
        
        _logger.LogDebug("Player {PlayerId} deals {Damage} damage to {EnemyId} (Critical: {IsCrit})", 
            player.Id, finalDamage, target.Id, isCritical);
        
        return Math.Max(1, finalDamage); // 确保至少造成1点伤害
    }

    /// <summary>
    /// 计算敌人伤害输出
    /// </summary>
    public double CalculateEnemyDamage(ServerBattleEnemy enemy, ServerBattlePlayer target)
    {
        // 基础伤害计算
        var baseDamage = CalculateEnemyBaseDamage(enemy);
        
        // 暴击计算
        var isCritical = IsEnemyCriticalHit(enemy);
        if (isCritical)
        {
            baseDamage *= 1.5; // 敌人暴击倍数
        }
        
        // 玩家防御减免
        var finalDamage = ApplyPlayerDefenseReduction(baseDamage, target);
        
        _logger.LogDebug("Enemy {EnemyId} deals {Damage} damage to {PlayerId} (Critical: {IsCrit})", 
            enemy.Id, finalDamage, target.Id, isCritical);
        
        return Math.Max(1, finalDamage);
    }

    /// <summary>
    /// 计算玩家基础伤害
    /// </summary>
    private double CalculateBaseDamage(ServerBattlePlayer player)
    {
        // 武器伤害
        var weaponDamage = GetWeaponDamage(player);
        
        // 属性加成
        var attributeBonus = GetAttributeDamageBonus(player);
        
        // 技能加成
        var skillBonus = GetSkillDamageBonus(player);
        
        return weaponDamage + attributeBonus + skillBonus;
    }

    /// <summary>
    /// 计算敌人基础伤害
    /// </summary>
    private double CalculateEnemyBaseDamage(ServerBattleEnemy enemy)
    {
        // 基于等级的基础伤害
        var baseDamage = enemy.Level * 8 + 15;
        
        // 随机浮动 ±20%
        var variance = 0.2;
        var randomMultiplier = 1.0 + (_random.NextDouble() - 0.5) * 2 * variance;
        
        return baseDamage * randomMultiplier;
    }

    /// <summary>
    /// 获取武器伤害
    /// </summary>
    private double GetWeaponDamage(ServerBattlePlayer player)
    {
        // TODO: 从玩家装备中获取武器信息
        // 目前返回基于等级的基础伤害
        return player.Level * 5 + 10;
    }

    /// <summary>
    /// 获取属性伤害加成
    /// </summary>
    private double GetAttributeDamageBonus(ServerBattlePlayer player)
    {
        // 主要属性加成（力量、敏捷、智力）
        var primaryStat = Math.Max(Math.Max(player.Strength, player.Agility), player.Intellect);
        return primaryStat * 0.5;
    }

    /// <summary>
    /// 获取技能伤害加成
    /// </summary>
    private double GetSkillDamageBonus(ServerBattlePlayer player)
    {
        // TODO: 根据玩家技能等级计算加成
        return 0;
    }

    /// <summary>
    /// 判断是否暴击
    /// </summary>
    private bool IsCriticalHit(ServerBattlePlayer player)
    {
        // 基础暴击率5% + 敏捷加成
        var criticalChance = 0.05 + player.Agility * 0.001;
        return _random.NextDouble() < criticalChance;
    }

    /// <summary>
    /// 判断敌人是否暴击
    /// </summary>
    private bool IsEnemyCriticalHit(ServerBattleEnemy enemy)
    {
        // 敌人基础暴击率10%
        return _random.NextDouble() < 0.1;
    }

    /// <summary>
    /// 获取暴击倍数
    /// </summary>
    private double GetCriticalMultiplier(ServerBattlePlayer player)
    {
        // 基础暴击倍数1.5 + 敏捷加成
        return 1.5 + player.Agility * 0.005;
    }

    /// <summary>
    /// 应用防御减免
    /// </summary>
    private double ApplyDefenseReduction(double damage, ServerBattleEnemy target)
    {
        // 简单的防御减免公式
        var defense = target.Level * 2;
        var reduction = defense / (defense + 100.0);
        return damage * (1.0 - reduction);
    }

    /// <summary>
    /// 应用玩家防御减免
    /// </summary>
    private double ApplyPlayerDefenseReduction(double damage, ServerBattlePlayer target)
    {
        // 基于耐力的防御减免
        var defense = target.Stamina * 2;
        var reduction = defense / (defense + 200.0);
        return damage * (1.0 - reduction);
    }

    /// <summary>
    /// 处理玩家受到伤害
    /// </summary>
    public void ApplyDamageToPlayer(ServerBattlePlayer player, double damage)
    {
        player.Health = Math.Max(0, (int)(player.Health - Math.Round(damage)));
        
        if (player.Health <= 0)
        {
            player.IsAlive = false;
            _logger.LogInformation("Player {PlayerId} has been defeated", player.Id);
        }
    }

    /// <summary>
    /// 处理敌人受到伤害
    /// </summary>
    public void ApplyDamageToEnemy(ServerBattleEnemy enemy, double damage)
    {
        enemy.Health = Math.Max(0, (int)(enemy.Health - Math.Round(damage)));
        
        if (enemy.Health <= 0)
        {
            enemy.IsAlive = false;
            _logger.LogInformation("Enemy {EnemyId} has been defeated", enemy.Id);
        }
    }

    /// <summary>
    /// 检查角色是否可以行动
    /// </summary>
    public bool CanPlayerAct(ServerBattlePlayer player)
    {
        return player.IsAlive && player.AttackCooldown <= 0;
    }

    /// <summary>
    /// 检查敌人是否可以行动
    /// </summary>
    public bool CanEnemyAct(ServerBattleEnemy enemy)
    {
        return enemy.IsAlive && enemy.AttackCooldown <= 0;
    }

    /// <summary>
    /// 重置角色战斗状态
    /// </summary>
    public void ResetPlayerCombatState(ServerBattlePlayer player)
    {
        player.CurrentAction = "Idle";
        player.CurrentEnemyId = null;
        player.AttackCooldown = 0;
    }

    /// <summary>
    /// 重置敌人战斗状态
    /// </summary>
    public void ResetEnemyCombatState(ServerBattleEnemy enemy)
    {
        enemy.AttackCooldown = 0;
    }

    /// <summary>
    /// 计算角色攻击速度
    /// </summary>
    public double CalculatePlayerAttackSpeed(ServerBattlePlayer player)
    {
        // 基础攻击速度 + 敏捷加成
        var baseSpeed = 1.0;
        var agilityBonus = player.Agility * 0.01;
        return baseSpeed + agilityBonus;
    }

    /// <summary>
    /// 恢复角色生命值
    /// </summary>
    public void RestorePlayerHealth(ServerBattlePlayer player, double amount)
    {
        player.Health = Math.Min(player.MaxHealth, (int)(player.Health + Math.Round(amount)));
        _logger.LogDebug("Player {PlayerId} restored {Amount} health", player.Id, amount);
    }

    /// <summary>
    /// 恢复角色魔法值
    /// </summary>
    public void RestorePlayerMana(ServerBattlePlayer player, double amount)
    {
        player.Mana = Math.Min(player.MaxMana, (int)(player.Mana + Math.Round(amount)));
        _logger.LogDebug("Player {PlayerId} restored {Amount} mana", player.Id, amount);
    }
}