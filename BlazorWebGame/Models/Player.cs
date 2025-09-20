using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models
{
    public class Player
    {
        public string Name { get; set; } = "英雄";
        public int Gold { get; set; } = 0;
        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        public int BaseAttackPower { get; set; } = 10;
        public double AttacksPerSecond { get; set; } = 1.0;

        public BattleProfession SelectedBattleProfession { get; set; } = BattleProfession.Warrior;
        public Dictionary<BattleProfession, int> BattleProfessionXP { get; set; } = new();
        public Dictionary<GatheringProfession, int> GatheringProfessionXP { get; set; } = new();
        public Dictionary<ProductionProfession, int> ProductionProfessionXP { get; set; } = new();

        // --- 技能系统字段 ---

        /// <summary>
        /// 玩家已经解锁的所有共享技能ID
        /// </summary>
        public HashSet<string> LearnedSharedSkills { get; set; } = new();

        /// <summary>
        /// 每个职业装备的技能ID列表
        /// </summary>
        public Dictionary<BattleProfession, List<string>> EquippedSkills { get; set; } = new();

        /// <summary>
        /// 追踪技能的当前冷却回合数。Key: SkillId, Value: 剩余回合
        /// </summary>
        public Dictionary<string, int> SkillCooldowns { get; set; } = new();

        // --- 新增属性 ---
        /// <summary>
        /// 玩家的库存。Key: ItemId, Value: 数量
        /// </summary>
        public Dictionary<string, int> Inventory { get; set; } = new();

        /// <summary>
        /// 玩家已穿戴的装备。Key: 装备槽位, Value: ItemId
        /// </summary>
        public Dictionary<EquipmentSlot, string> EquippedItems { get; set; } = new();

        public Player()
        {
            // 初始化职业
            foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession)))
            {
                BattleProfessionXP.TryAdd(profession, 0);
                EquippedSkills.TryAdd(profession, new List<string>());
            }
            GatheringProfessionXP.TryAdd(GatheringProfession.Miner, 0);
            ProductionProfessionXP.TryAdd(ProductionProfession.Tailor, 0);
        }

        public void AddBattleXP(BattleProfession profession, int amount)
        {
            if (BattleProfessionXP.ContainsKey(profession))
            {
                BattleProfessionXP[profession] += amount;
            }
        }

        /// <summary>
        /// 获取指定战斗职业的等级
        /// </summary>
        public int GetLevel(BattleProfession profession)
        {
            return BattleProfessionXP.TryGetValue(profession, out var xp) ? 1 + (xp / 100) : 1;
        }

        /// <summary>
        /// 根据经验值计算等级（通常用于UI显示或内部计算）
        /// </summary>
        public int GetLevel(int xp) => 1 + (xp / 100);

        /// <summary>
        /// 获取玩家的总攻击力（基础攻击力 + 装备加成）
        /// </summary>
        public int GetTotalAttackPower()
        {
            int equipmentBonus = 0;
            foreach (var itemId in EquippedItems.Values)
            {
                if (ItemData.GetItemById(itemId) is Equipment eq)
                {
                    equipmentBonus += eq.AttackBonus;
                }
            }
            return this.BaseAttackPower + equipmentBonus;
        }

        /// <summary>
        /// 获取玩家的总生命值上限（基础生命值 + 装备加成）
        /// </summary>
        public int GetTotalMaxHealth()
        {
            int equipmentBonus = 0;
            foreach (var itemId in EquippedItems.Values)
            {
                if (ItemData.GetItemById(itemId) is Equipment eq)
                {
                    equipmentBonus += eq.HealthBonus;
                }
            }
            return this.MaxHealth + equipmentBonus;
        }
    }
}