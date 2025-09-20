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

        // --- 系统字段 ---

        /// <summary>
        /// 激活的增益效果列表
        /// </summary>
        public List<Buff> ActiveBuffs { get; set; } = new();

        /// <summary>
        /// 已经学会的共享技能ID
        /// </summary>
        public HashSet<string> LearnedSharedSkills { get; set; } = new();

        /// <summary>
        /// 每个职业装备的技能ID列表
        /// </summary>
        public Dictionary<BattleProfession, List<string>> EquippedSkills { get; set; } = new();

        /// <summary>
        /// 追踪技能的当前冷却回合数. Key: SkillId, Value: 剩余回合
        /// </summary>
        public Dictionary<string, int> SkillCooldowns { get; set; } = new();

        /// <summary>
        /// 玩家的背包，列表索引直接对应物品栏的位置。
        /// </summary>
        public List<InventorySlot> Inventory { get; set; } = new();

        /// <summary>
        /// 已穿戴的装备. Key: 装备位置, Value: ItemId
        /// </summary>
        public Dictionary<EquipmentSlot, string> EquippedItems { get; set; } = new();

        /// <summary>
        /// 需要自动出售的物品ID
        /// </summary>
        public HashSet<string> AutoSellItemIds { get; set; } = new();

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

            for (int i = 0; i < 20; i++)
            {
                Inventory.Add(new InventorySlot());
            }
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
        /// 根据经验值获取等级（通用UI显示，非内部逻辑）
        /// </summary>
        public int GetLevel(int xp) => 1 + (xp / 100);

        /// <summary>
        /// 获取玩家的总攻击力 (基础 + 装备 + Buff)
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
            int buffBonus = ActiveBuffs.Where(b => b.BuffType == StatBuffType.AttackPower).Sum(b => b.BuffValue);
            return this.BaseAttackPower + equipmentBonus + buffBonus;
        }

        /// <summary>
        /// 获取玩家的总最大生命值 (基础 + 装备 + Buff)
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
            int buffBonus = ActiveBuffs.Where(b => b.BuffType == StatBuffType.MaxHealth).Sum(b => b.BuffValue);
            return this.MaxHealth + equipmentBonus + buffBonus;
        }
    }
}