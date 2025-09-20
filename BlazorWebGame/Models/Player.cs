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

        public List<Buff> ActiveBuffs { get; set; } = new();
        public HashSet<string> LearnedSharedSkills { get; set; } = new();
        public Dictionary<BattleProfession, List<string>> EquippedSkills { get; set; } = new();
        public Dictionary<string, int> SkillCooldowns { get; set; } = new();
        public List<InventorySlot> Inventory { get; set; } = new();
        public Dictionary<EquipmentSlot, string> EquippedItems { get; set; } = new();
        public HashSet<string> AutoSellItemIds { get; set; } = new();

        /// <summary>
        /// 消耗品快捷栏。Key: 栏位ID (0-1 药剂, 2-3 食物), Value: ItemId
        /// </summary>
        public Dictionary<int, string> QuickSlots { get; set; } = new();

        /// <summary>
        /// 追踪消耗品的冷却时间。Key: ItemId, Value: 剩余冷却秒数
        /// </summary>
        public Dictionary<string, double> ConsumableCooldowns { get; set; } = new();


        public Player()
        {
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

        public int GetLevel(BattleProfession profession)
        {
            return BattleProfessionXP.TryGetValue(profession, out var xp) ? 1 + (xp / 100) : 1;
        }

        public int GetLevel(int xp) => 1 + (xp / 100);

        public int GetTotalAttackPower()
        {
            int equipmentBonus = EquippedItems.Values
                .Select(itemId => ItemData.GetItemById(itemId) as Equipment)
                .Where(eq => eq != null)
                .Sum(eq => eq!.AttackBonus);

            int buffBonus = ActiveBuffs.Where(b => b.BuffType == StatBuffType.AttackPower).Sum(b => b.BuffValue);
            return this.BaseAttackPower + equipmentBonus + buffBonus;
        }

        public int GetTotalMaxHealth()
        {
            int equipmentBonus = EquippedItems.Values
                .Select(itemId => ItemData.GetItemById(itemId) as Equipment)
                .Where(eq => eq != null)
                .Sum(eq => eq!.HealthBonus);

            int buffBonus = ActiveBuffs.Where(b => b.BuffType == StatBuffType.MaxHealth).Sum(b => b.BuffValue);
            return this.MaxHealth + equipmentBonus + buffBonus;
        }
    }
}