using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models
{
    public enum PlayerActionState
    {
        Idle,
        Combat,
        Gathering
    }

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

        public List<Buff> ActiveBuffs { get; set; } = new();
        public HashSet<string> LearnedSharedSkills { get; set; } = new();
        public Dictionary<BattleProfession, List<string>> EquippedSkills { get; set; } = new();
        public Dictionary<string, int> SkillCooldowns { get; set; } = new();
        public List<InventorySlot> Inventory { get; set; } = new();
        public Dictionary<EquipmentSlot, string> EquippedItems { get; set; } = new();
        public HashSet<string> AutoSellItemIds { get; set; } = new();

        public Dictionary<int, string> PotionQuickSlots { get; set; } = new();
        public Dictionary<int, string> CombatFoodQuickSlots { get; set; } = new();
        public Dictionary<int, string> GatheringFoodQuickSlots { get; set; } = new();
        public Dictionary<string, double> ConsumableCooldowns { get; set; } = new();

        public PlayerActionState CurrentAction { get; set; } = PlayerActionState.Idle;
        public HashSet<string> DefeatedMonsterIds { get; set; } = new();

        public Player()
        {
            // 构造函数现在只调用初始化方法
            InitializeCollections();
        }

        /// <summary>
        /// 确保所有集合和字典都已初始化，防止从旧存档加载时出现null引用。
        /// </summary>
        private void InitializeCollections()
        {
            // 使用 '??=' 操作符，如果属性是 null，就给它分配一个新的实例。
            BattleProfessionXP ??= new();
            GatheringProfessionXP ??= new();
            ProductionProfessionXP ??= new();
            EquippedSkills ??= new();

            // 为所有可能的职业初始化经验和技能列表
            foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession)))
            {
                BattleProfessionXP.TryAdd(profession, 0);
                EquippedSkills.TryAdd(profession, new List<string>());
            }
            foreach (var profession in (GatheringProfession[])Enum.GetValues(typeof(GatheringProfession)))
            {
                GatheringProfessionXP.TryAdd(profession, 0);
            }
            foreach (var profession in (ProductionProfession[])Enum.GetValues(typeof(ProductionProfession)))
            {
                ProductionProfessionXP.TryAdd(profession, 0);
            }

            // 初始化背包
            if (Inventory == null || !Inventory.Any())
            {
                Inventory = new List<InventorySlot>();
                for (int i = 0; i < 20; i++)
                {
                    Inventory.Add(new InventorySlot());
                }
            }

            // *** 这是最关键的修正部分：确保所有新旧集合都非空 ***
            ActiveBuffs ??= new();
            LearnedSharedSkills ??= new();
            SkillCooldowns ??= new();
            EquippedItems ??= new();
            AutoSellItemIds ??= new();
            DefeatedMonsterIds ??= new();
            ConsumableCooldowns ??= new();

            PotionQuickSlots ??= new();
            CombatFoodQuickSlots ??= new();
            GatheringFoodQuickSlots ??= new();
        }

        /// <summary>
        /// 当从存储加载玩家数据后，调用此方法来确保数据一致性。
        /// </summary>
        public void EnsureDataConsistency()
        {
            InitializeCollections();
        }

        // ... (文件的其余部分保持不变) ...
        public void AddGatheringXP(GatheringProfession profession, int amount) { if (GatheringProfessionXP.ContainsKey(profession)) { GatheringProfessionXP[profession] += amount; } }
        public void AddBattleXP(BattleProfession profession, int amount) { if (BattleProfessionXP.ContainsKey(profession)) { BattleProfessionXP[profession] += amount; } }
        public double GetTotalGatheringSpeedBonus() { double equipmentBonus = EquippedItems.Values.Select(itemId => ItemData.GetItemById(itemId) as Equipment).Where(eq => eq != null).Sum(eq => eq!.GatheringSpeedBonus); double buffBonus = ActiveBuffs.Where(b => b.BuffType == StatBuffType.GatheringSpeed).Sum(b => b.BuffValue / 100.0); return equipmentBonus + buffBonus; }
        public double GetTotalExtraLootChance() { double equipmentBonus = EquippedItems.Values.Select(itemId => ItemData.GetItemById(itemId) as Equipment).Where(eq => eq != null).Sum(eq => eq!.ExtraLootChanceBonus); double buffBonus = ActiveBuffs.Where(b => b.BuffType == StatBuffType.ExtraLootChance).Sum(b => b.BuffValue / 100.0); return equipmentBonus + buffBonus; }
        public int GetLevel(BattleProfession profession) => BattleProfessionXP.TryGetValue(profession, out var xp) ? 1 + (xp / 100) : 1;
        public int GetLevel(GatheringProfession profession) => GatheringProfessionXP.TryGetValue(profession, out var xp) ? 1 + (xp / 100) : 1;
        public int GetLevel(int xp) => 1 + (xp / 100);
        public int GetTotalAttackPower() { int equipmentBonus = EquippedItems.Values.Select(itemId => ItemData.GetItemById(itemId) as Equipment).Where(eq => eq != null).Sum(eq => eq!.AttackBonus); int buffBonus = ActiveBuffs.Where(b => b.BuffType == StatBuffType.AttackPower).Sum(b => b.BuffValue); return this.BaseAttackPower + equipmentBonus + buffBonus; }
        public int GetTotalMaxHealth() { int equipmentBonus = EquippedItems.Values.Select(itemId => ItemData.GetItemById(itemId) as Equipment).Where(eq => eq != null).Sum(eq => eq!.HealthBonus); int buffBonus = ActiveBuffs.Where(b => b.BuffType == StatBuffType.MaxHealth).Sum(b => b.BuffValue); return this.MaxHealth + equipmentBonus + buffBonus; }
    }
}