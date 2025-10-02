using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.DTOs.Character
{
    /// <summary>
    /// 角色槽位DTO
    /// </summary>
    public class CharacterSlotDto
    {
        public int SlotIndex { get; set; }
        public string State { get; set; } = "Locked"; // Locked/Unlocked/Occupied
        public CharacterSummaryDto? Character { get; set; }
        public string? UnlockCondition { get; set; }
        public DateTime? LastPlayedAt { get; set; }
    }

    /// <summary>
    /// 角色摘要DTO
    /// </summary>
    public class CharacterSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public string ProfessionName { get; set; } = string.Empty;
        public string ProfessionIcon { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public DateTime LastActiveAt { get; set; }
    }

    /// <summary>
    /// 角色完整信息DTO
    /// </summary>
    public class CharacterFullDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Experience { get; set; }
        public int ExperienceToNextLevel { get; set; }
        public int Gold { get; set; }

        // 生命值
        public CharacterVitalsDto Vitals { get; set; } = new();

        // 属性
        public CharacterAttributesDto Attributes { get; set; } = new();

        // 职业信息
        public ProfessionInfoDto Profession { get; set; } = new();

        // 装备
        public EquipmentDto Equipment { get; set; } = new();

        // 技能
        public SkillSystemDto Skills { get; set; } = new();

        // 当前区域
        public string? CurrentRegionId { get; set; }
        public string? CurrentRegionName { get; set; }

        // 声望
        public Dictionary<string, ReputationDto> Reputations { get; set; } = new();

        // 统计
        public CharacterStatisticsDto Statistics { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime LastActiveAt { get; set; }
    }

    /// <summary>
    /// 角色生命值DTO
    /// </summary>
    public class CharacterVitalsDto
    {
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Mana { get; set; }
        public int MaxMana { get; set; }
        public double HealthPercentage => MaxHealth > 0 ? (double)Health / MaxHealth * 100 : 0;
        public double ManaPercentage => MaxMana > 0 ? (double)Mana / MaxMana * 100 : 0;
        public double HealthRegen { get; set; }
        public double ManaRegen { get; set; }
    }

    /// <summary>
    /// 角色属性DTO
    /// </summary>
    public class CharacterAttributesDto
    {
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Intellect { get; set; }
        public int Spirit { get; set; }
        public int Stamina { get; set; }
        public int AvailablePoints { get; set; }

        // 衍生属性
        public double AttackPower { get; set; }
        public double SpellPower { get; set; }
        public double CriticalChance { get; set; }
        public double CriticalDamage { get; set; }
        public double AttackSpeed { get; set; }
        public double CastSpeed { get; set; }
        public double Armor { get; set; }
        public double MagicResistance { get; set; }
    }

    /// <summary>
    /// 职业信息DTO
    /// </summary>
    public class ProfessionInfoDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Experience { get; set; }
        public int ExperienceToNextLevel { get; set; }
        public List<string> Specializations { get; set; } = new();
        public string? ActiveSpecialization { get; set; }
    }

    /// <summary>
    /// 装备DTO
    /// </summary>
    public class EquipmentDto
    {
        public EquipmentSlotDto? Weapon { get; set; }
        public EquipmentSlotDto? OffHand { get; set; }
        public EquipmentSlotDto? Helmet { get; set; }
        public EquipmentSlotDto? Chest { get; set; }
        public EquipmentSlotDto? Gloves { get; set; }
        public EquipmentSlotDto? Boots { get; set; }
        public EquipmentSlotDto? Ring1 { get; set; }
        public EquipmentSlotDto? Ring2 { get; set; }
        public EquipmentSlotDto? Amulet { get; set; }

        public int TotalGearScore { get; set; }
        public List<string> ActiveSetBonuses { get; set; } = new();
    }

    /// <summary>
    /// 装备槽位DTO
    /// </summary>
    public class EquipmentSlotDto
    {
        public string ItemId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Rarity { get; set; } = string.Empty;
        public int Level { get; set; }
        public int GearScore { get; set; }
        public string Icon { get; set; } = string.Empty;
        public List<string> Stats { get; set; } = new();
    }

    /// <summary>
    /// 技能系统DTO
    /// </summary>
    public class SkillSystemDto
    {
        public List<SkillSlotDto> ActiveSkills { get; set; } = new();
        public List<SkillSlotDto> PassiveSkills { get; set; } = new();
        public int AvailableSkillPoints { get; set; }

        // 添加此属性以兼容战斗测试页面
        public List<AvailableSkillDto> Available
        {
            get
            {
                // 从已装备的主动技能中提取可用技能信息
                return ActiveSkills
                    .Where(s => s.SkillId != null && s.IsUnlocked)
                    .Select(s => new AvailableSkillDto
                    {
                        Id = s.SkillId!,
                        Name = s.SkillName ?? "未命名技能",
                        Icon = s.SkillIcon ?? "",
                        Level = s.SkillLevel
                    })
                    .ToList();
            }
        }
    }

    /// <summary>
    /// 可用技能DTO
    /// </summary>
    public class AvailableSkillDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Level { get; set; }
    }

    /// <summary>
    /// 技能槽位DTO
    /// </summary>
    public class SkillSlotDto
    {
        public int SlotIndex { get; set; }
        public string? SkillId { get; set; }
        public string? SkillName { get; set; }
        public string? SkillIcon { get; set; }
        public int SkillLevel { get; set; }
        public string? Keybind { get; set; }
        public bool IsUnlocked { get; set; }
    }

    /// <summary>
    /// 声望DTO
    /// </summary>
    public class ReputationDto
    {
        public string FactionId { get; set; } = string.Empty;
        public string FactionName { get; set; } = string.Empty;
        public int Current { get; set; }
        public int Max { get; set; }
        public string Level { get; set; } = string.Empty; // Hostile/Unfriendly/Neutral/Friendly/Honored/Exalted
        public double Percentage => Max > 0 ? (double)Current / Max * 100 : 0;
    }

    /// <summary>
    /// 角色统计DTO
    /// </summary>
    public class CharacterStatisticsDto
    {
        public int TotalPlayTime { get; set; } // 分钟
        public int MonstersKilled { get; set; }
        public int DungeonsCompleted { get; set; }
        public int QuestsCompleted { get; set; }
        public int Deaths { get; set; }
        public long TotalDamageDealt { get; set; }
        public long TotalHealingDone { get; set; }
        public int ItemsCrafted { get; set; }
        public int ItemsLooted { get; set; }
        public Dictionary<string, int> AchievementPoints { get; set; } = new();
    }

    /// <summary>
    /// 创建角色请求
    /// </summary>
    public class CreateCharacterRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? StartingProfessionId { get; set; }
        public int SlotIndex { get; set; }
    }

    /// <summary>
    /// 角色名称验证请求
    /// </summary>
    public class ValidateCharacterNameRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// 角色名称验证结果
    /// </summary>
    public class ValidateCharacterNameResult
    {
        public bool IsValid { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 切换角色请求
    /// </summary>
    public class SwitchCharacterRequest
    {
        public string CharacterId { get; set; } = string.Empty;
    }

    /// <summary>
    /// 分配属性点请求
    /// </summary>
    public class AllocateAttributePointsRequest
    {
        public Dictionary<string, int> Points { get; set; } = new();
    }

    /// <summary>
    /// 角色花名册DTO
    /// </summary>
    public class RosterDto
    {
        public string UserId { get; set; } = string.Empty;
        public List<CharacterSlotDto> Slots { get; set; } = new();
        public string? ActiveCharacterId { get; set; }
        public int MaxSlots { get; set; }
        public int UnlockedSlots { get; set; }
    }

    /// <summary>
    /// 离线收益DTO
    /// </summary>
    public class OfflineProgressDto
    {
        public string CharacterId { get; set; } = string.Empty;
        public DateTime OfflineAt { get; set; }
        public DateTime ReturnAt { get; set; }
        public TimeSpan OfflineDuration { get; set; }
        public TimeSpan EffectiveDuration { get; set; }

        public int ExperienceGained { get; set; }
        public int GoldGained { get; set; }
        public Dictionary<string, int> ResourcesGained { get; set; } = new();
        public List<OfflineActivityResultDto> CompletedActivities { get; set; } = new();
        public List<string> LootedItems { get; set; } = new();
    }

    /// <summary>
    /// 离线活动结果DTO
    /// </summary>
    public class OfflineActivityResultDto
    {
        public string ActivityName { get; set; } = string.Empty;
        public int CyclesCompleted { get; set; }
        public Dictionary<string, int> Rewards { get; set; } = new();
    }
}