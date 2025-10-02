using BlazorWebGame.Shared.Models;
using System;
using System.Linq;

namespace BlazorWebGame.Shared.Mappers;

/// <summary>
/// 战斗领域模型映射器
/// 将Character领域模型转换为Battle领域模型
/// </summary>
public static class BattleMapper
{
    /// <summary>
    /// 将Character转换为BattlePlayer
    /// </summary>
    public static BattlePlayer ToBattlePlayer(Character character)
    {
        if (character == null)
            throw new ArgumentNullException(nameof(character));

        var battlePlayer = new BattlePlayer(
            character.Id,
            character.Name,
            character.Level,
            character.Attributes,
            character.Professions.SelectedBattleProfession
        );

        // 设置组队信息
        if (character.PartyId.HasValue)
        {
            battlePlayer.SetPartyId(character.PartyId);
        }

        // 设置装备的技能（根据当前职业）
        var equippedSkills = character.SkillManager.GetEquippedSkills(character.Professions.SelectedBattleProfession);
        foreach (var skillId in equippedSkills)
        {
            battlePlayer.EquippedSkills.Add(skillId);
        }

        return battlePlayer;
    }

    /// <summary>
    /// 将Enemy转换为BattleEnemy
    /// </summary>
    public static BattleEnemy ToBattleEnemy(Enemy enemy)
    {
        if (enemy == null)
            throw new ArgumentNullException(nameof(enemy));

        return new BattleEnemy(enemy);
    }

    /// <summary>
    /// 批量转换Character为BattlePlayer
    /// </summary>
    public static System.Collections.Generic.List<BattlePlayer> ToBattlePlayers(
        System.Collections.Generic.IEnumerable<Character> characters)
    {
        return characters.Select(ToBattlePlayer).ToList();
    }

    /// <summary>
    /// 批量转换Enemy为BattleEnemy
    /// </summary>
    public static System.Collections.Generic.List<BattleEnemy> ToBattleEnemies(
        System.Collections.Generic.IEnumerable<Enemy> enemies)
    {
        return enemies.Select(ToBattleEnemy).ToList();
    }

    /// <summary>
    /// 将战斗结果应用回Character
    /// </summary>
    public static void ApplyBattleResultToCharacter(Character character, BattlePlayer battlePlayer, BattleResult result)
    {
        if (character == null)
            throw new ArgumentNullException(nameof(character));
        if (battlePlayer == null)
            throw new ArgumentNullException(nameof(battlePlayer));
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        // 更新生命值和法力值
        // 需要先恢复到满值，然后根据战斗结果调整
        var healthDiff = character.Vitals.MaxHealth - battlePlayer.Health;
        var manaDiff = character.Vitals.MaxMana - battlePlayer.Mana;
        if (healthDiff > 0)
        {
            character.Vitals.TakeDamage(healthDiff);
        }
        else if (healthDiff < 0)
        {
            character.Vitals.Heal(-healthDiff);
        }
        
        if (manaDiff > 0)
        {
            character.Vitals.ConsumeMana(manaDiff);
        }
        else if (manaDiff < 0)
        {
            character.Vitals.RestoreMana(-manaDiff);
        }

        // 应用经验值奖励
        if (result.ExperienceGained > 0)
        {
            character.GainExperience(result.ExperienceGained);
        }

        // 应用金币奖励
        if (result.GoldGained > 0)
        {
            character.GainGold(result.GoldGained);
        }

        // 应用掉落物品
        foreach (var loot in result.ItemsLooted)
        {
            character.Inventory.AddItem(loot.ItemId, loot.Quantity);
        }

        // 应用职业经验
        foreach (var profExp in result.ProfessionExperience)
        {
            character.Professions.GainProfessionExperience("Battle", profExp.Key, profExp.Value);
        }

        // 应用声望奖励
        foreach (var rep in result.ReputationGains)
        {
            character.GainReputation(rep.Key, rep.Value);
        }
    }
}
