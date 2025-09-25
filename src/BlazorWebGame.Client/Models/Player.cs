using BlazorWebGame.GameConfig;
using BlazorWebGame.Models.Items;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models
{
    public enum PlayerActionState
    {
        Idle,
        Combat,
        // ϸ�ֲɼ��
        GatheringMining,       // �ɿ�
        GatheringHerbalism,    // ��ҩѧ
        GatheringFishing,      // ����
        // ϸ�������
        CraftingCooking,       // ���
        CraftingAlchemy,       // ����
        CraftingBlacksmithing, // ����
        CraftingJewelcrafting, // �鱦�ӹ�
        CraftingLeatherworking,// ��Ƥ
        CraftingTailoring,     // �÷�
        CraftingEngineering    // ����ѧ
    }
    public record ReputationTier(string Name, int MinValue, int MaxValue, string BarColorClass);
    public class Player
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // ����ÿ����ɫΨһID
        public bool IsDead { get; set; } = false;
        public double RevivalTimeRemaining { get; set; } = 0;
        // ���Ա��Ϊ��ʱ������ȷ��û�������ط�ʹ�ú�ֱ��ɾ��
        [Obsolete("ʹ���µ�ս��ϵͳ")]
        public Enemy? CurrentEnemy { get; set; }
        public GatheringNode? CurrentGatheringNode { get; set; }
        public Recipe? CurrentRecipe { get; set; }
        public double AttackCooldown { get; set; }
        public double GatheringCooldown { get; set; }
        public double CraftingCooldown { get; set; }

        public string Name { get; set; } = "Ӣ��";
        public int Gold { get; set; } = 10000;
        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        public int BaseAttackPower { get; set; } = 10;
        public double AttacksPerSecond { get; set; } = 1.0;

        public BattleProfession SelectedBattleProfession { get; set; } = BattleProfession.Warrior;
        public static readonly List<ReputationTier> ReputationTiers = new()
        {
            // Ϊ����ʾ������ʹ�ý�С����ֵ
            new ReputationTier("����", 0, 1000, "bg-info"),
            new ReputationTier("����", 1000, 3000, "bg-success"),
            new ReputationTier("��", 3000, 6000, "bg-primary"),
            new ReputationTier("���", 6000, 6001, "bg-warning") // ����Ƕ���
        };
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
        public Dictionary<int, string> ProductionFoodQuickSlots { get; set; } = new(); // *** ������������ ***
        public Dictionary<string, double> ConsumableCooldowns { get; set; } = new();
        public Dictionary<Faction, int> Reputation { get; set; } = new();

        // ���ڴ洢�����������ĵ�ǰ���ȣ���������ID��ֵ�ǵ�ǰ�������
        public Dictionary<string, int> QuestProgress { get; set; } = new();

        // ���ڼ�¼�����ڣ�����/���ܣ�����ɵ�����ID����ֹ�ظ����
        public List<string> CompletedQuestIds { get; set; } = new();

        // ��Player����������Щ����
        public List<string> CompletedDungeons { get; set; } = new List<string>();
        public List<string> KilledMonsters { get; set; } = new List<string>();
        public List<string> CompletedQuests { get; set; } = new List<string>();

        public PlayerActionState CurrentAction { get; set; } = PlayerActionState.Idle;
        public HashSet<string> DefeatedMonsterIds { get; set; } = new();
        public HashSet<string> LearnedRecipeIds { get; set; } = new();
        public Player()
        {
            // ���캯������ֻ���ó�ʼ������
            InitializeCollections();
        }

        /// <summary>
        /// 初始化集合数据（已移动到PlayerUtilityService）
        /// </summary>
        [Obsolete("Use PlayerUtilityService.InitializeCollections instead")]
        private void InitializeCollections()
        {
            // 使用 '??=' 操作符，仅在集合为 null时才给它们分配一个新的实例。
            BattleProfessionXP ??= new();
            GatheringProfessionXP ??= new();
            ProductionProfessionXP ??= new();
            EquippedSkills ??= new();

            // 为所有可能的职业初始化经验字典和技能列表
            foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession)))
            {
                BattleProfessionXP.TryAdd(profession, 10000);
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
            // 确保默认配方已被学习
            foreach (var recipe in RecipeData.AllRecipes.Where(r => r.IsDefault))
            {
                LearnedRecipeIds.Add(recipe.Id);
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

            // *** 确保各种关键集合和字典，确保现有和旧集合都是非空 ***
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
            ProductionFoodQuickSlots ??= new();
        }

        /// <summary>
        /// 确保数据一致性（已移动到PlayerUtilityService）
        /// </summary>
        [Obsolete("Use PlayerUtilityService.EnsureDataConsistency instead")]
        public void EnsureDataConsistency()
        {
            InitializeCollections();
        }

        /// <summary>
        /// 为指定采集职业添加经验值（已移动到PlayerProfessionService）
        /// </summary>
        [Obsolete("Use PlayerProfessionService.AddGatheringXP instead")]
        public void AddGatheringXP(GatheringProfession profession, int amount) 
        { 
            if (GatheringProfessionXP.ContainsKey(profession)) 
            { 
                GatheringProfessionXP[profession] += amount; 
            } 
        }
        
        /// <summary>
        /// 为指定战斗职业添加经验值（已移动到PlayerProfessionService）
        /// </summary>
        [Obsolete("Use PlayerProfessionService.AddBattleXP instead")]
        public (bool LeveledUp, int OldLevel, int NewLevel) AddBattleXP(BattleProfession profession, int amount) 
        { 
            if (BattleProfessionXP.ContainsKey(profession)) 
            {
                int oldLevel = GetLevel(profession);
                BattleProfessionXP[profession] += amount;
                int newLevel = GetLevel(profession);
                bool leveledUp = newLevel > oldLevel;
                return (leveledUp, oldLevel, newLevel);
            }
            return (false, 0, 0);
        }
        
        /// <summary>
        /// 为指定生产职业添加经验值（已移动到PlayerProfessionService）
        /// </summary>
        [Obsolete("Use PlayerProfessionService.AddProductionXP instead")]
        public (bool LeveledUp, int OldLevel, int NewLevel) AddProductionXP(ProductionProfession profession, int amount)
        {
            if (ProductionProfessionXP.ContainsKey(profession))
            {
                int oldLevel = GetLevel(profession);
                ProductionProfessionXP[profession] += amount;
                int newLevel = GetLevel(profession);
                bool leveledUp = newLevel > oldLevel;
                return (leveledUp, oldLevel, newLevel);
            }
            return (false, 0, 0);
        }

        /// <summary>
        /// 获取声望等级（已移动到PlayerUtilityService）
        /// </summary>
        [Obsolete("Use PlayerUtilityService.GetReputationLevel instead")]
        public ReputationTier GetReputationLevel(Faction faction)
        {
            var rep = Reputation.GetValueOrDefault(faction, 0);
            // �Ӹߵ��Ͳ��ң����ص�һ�����������ĵȼ�
            return ReputationTiers.LastOrDefault(t => rep >= t.MinValue) ?? ReputationTiers.First();
        }

        /// <summary>
        /// 获取声望进度百分比（已移动到PlayerUtilityService）
        /// </summary>
        [Obsolete("Use PlayerUtilityService.GetReputationProgressPercentage instead")]
        public double GetReputationProgressPercentage(Faction faction)
        {
            var rep = Reputation.GetValueOrDefault(faction, 0);
            var tier = GetReputationLevel(faction);

            // �������ߵȼ���������ֱ������
            if (tier.MaxValue - tier.MinValue <= 1)
            {
                return 100.0;
            }

            var progressInTier = rep - tier.MinValue;
            var totalForTier = tier.MaxValue - tier.MinValue;

            return (double)progressInTier / totalForTier * 100.0;
        }

        /// <summary>
        /// 获取总采集速度加成（已移动到PlayerProfessionService）
        /// </summary>
        [Obsolete("Use PlayerProfessionService.GetTotalGatheringSpeedBonus instead")]
        public double GetTotalGatheringSpeedBonus() 
        { 
            double equipmentBonus = EquippedItems.Values.Select(itemId => ItemData.GetItemById(itemId) as Equipment).Where(eq => eq != null).Sum(eq => eq!.GatheringSpeedBonus); 
            double buffBonus = ActiveBuffs.Where(b => b.BuffType == StatBuffType.GatheringSpeed).Sum(b => b.BuffValue / 100.0); 
            return equipmentBonus + buffBonus; 
        }
        
        /// <summary>
        /// 获取额外战利品几率（已移动到PlayerProfessionService）
        /// </summary>
        [Obsolete("Use PlayerProfessionService.GetTotalExtraLootChance instead")]
        public double GetTotalExtraLootChance() 
        { 
            double equipmentBonus = EquippedItems.Values.Select(itemId => ItemData.GetItemById(itemId) as Equipment).Where(eq => eq != null).Sum(eq => eq!.ExtraLootChanceBonus); 
            double buffBonus = ActiveBuffs.Where(b => b.BuffType == StatBuffType.ExtraLootChance).Sum(b => b.BuffValue / 100.0); 
            return equipmentBonus + buffBonus; 
        }
        
        /// <summary>
        /// 获取总制作速度加成（已移动到PlayerProfessionService）
        /// </summary>
        [Obsolete("Use PlayerProfessionService.GetTotalCraftingSpeedBonus instead")]
        public double GetTotalCraftingSpeedBonus()
        {
            double equipmentBonus = 0.0;
            double buffBonus = ActiveBuffs
                .Where(b => b.BuffType == StatBuffType.CraftingSpeed)
                .Sum(b => b.BuffValue / 100.0);
            return equipmentBonus + buffBonus;
        }
        /// <summary>
        /// 获取战斗职业等级（已移动到PlayerProfessionService）
        /// </summary>
        [Obsolete("Use PlayerProfessionService.GetLevel instead")]
        public int GetLevel(BattleProfession profession) => 
            BattleProfessionXP.TryGetValue(profession, out var xp) ? 
            ExpSystem.GetLevelFromExp(xp) : 1;

        /// <summary>
        /// 获取采集职业等级（已移动到PlayerProfessionService）
        /// </summary>
        [Obsolete("Use PlayerProfessionService.GetLevel instead")]
        public int GetLevel(GatheringProfession profession) => 
            GatheringProfessionXP.TryGetValue(profession, out var xp) ? 
            ExpSystem.GetLevelFromExp(xp) : 1;

        // 通用方法也需要保留
        public int GetLevel(int xp) => ExpSystem.GetLevelFromExp(xp);

        /// <summary>
        /// 获取生产职业等级（已移动到PlayerProfessionService）
        /// </summary>
        [Obsolete("Use PlayerProfessionService.GetLevel instead")]
        public int GetLevel(ProductionProfession profession) =>
            ExpSystem.GetLevelFromExp(ProductionProfessionXP.GetValueOrDefault(profession, 0));

        /// <summary>
        /// 获取战斗职业进度百分比（已移动到PlayerProfessionService）
        /// </summary>
        [Obsolete("Use PlayerProfessionService.GetLevelProgress instead")]
        public double GetLevelProgress(BattleProfession profession) =>
            BattleProfessionXP.TryGetValue(profession, out var xp) ?
            ExpSystem.GetLevelProgressPercentage(xp) : 0;

        /// <summary>
        /// 获取采集职业进度百分比（已移动到PlayerProfessionService）
        /// </summary>  
        [Obsolete("Use PlayerProfessionService.GetLevelProgress instead")]
        public double GetLevelProgress(GatheringProfession profession) =>
            GatheringProfessionXP.TryGetValue(profession, out var xp) ?
            ExpSystem.GetLevelProgressPercentage(xp) : 0;

        /// <summary>
        /// 获取生产职业进度百分比（已移动到PlayerProfessionService）
        /// </summary>
        [Obsolete("Use PlayerProfessionService.GetLevelProgress instead")]
        public double GetLevelProgress(ProductionProfession profession) =>
            ExpSystem.GetLevelProgressPercentage(ProductionProfessionXP.GetValueOrDefault(profession, 0));

        /// <summary>
        /// 获取总攻击力（已移动到PlayerAttributeService）
        /// </summary>
        [Obsolete("Use PlayerAttributeService.GetTotalAttackPower instead")]
        public int GetTotalAttackPower()
        {
            var attrs = GetTotalAttributes();
            int primaryAttrValue = GetPrimaryAttributeValue();
            double apRatio = AttributeSystemConfig.MainAttributeToAPRatio;
            int primaryAttrBonus = (int)(primaryAttrValue * apRatio);
            var equipmentAttack = EquippedItems
                .Select(kv => ItemData.GetItemById(kv.Value) as Equipment)
                .Where(eq => eq != null)
                .Sum(eq => eq!.AttackBonus);
            var buffAttack = ActiveBuffs
                .Where(b => b.BuffType == StatBuffType.AttackPower)
                .Sum(b => b.BuffValue);
            return primaryAttrBonus + equipmentAttack + (int)buffAttack;
        }

        /// <summary>
        /// 获取伤害倍数（已移动到PlayerAttributeService）
        /// </summary>
        [Obsolete("Use PlayerAttributeService.GetDamageMultiplier instead")]
        public double GetDamageMultiplier()
        {
            int primaryAttrValue = GetPrimaryAttributeValue();
            double damageMultiplier = 1.0 + (primaryAttrValue * AttributeSystemConfig.MainAttributeToDamageMultiplier);
            return damageMultiplier;
        }

        /// <summary>
        /// 获取总生命值上限（已移动到PlayerAttributeService）
        /// </summary>
        [Obsolete("Use PlayerAttributeService.GetTotalMaxHealth instead")]
        public int GetTotalMaxHealth()
        {
            var attrs = GetTotalAttributes();
            int baseHealth = AttributeSystemConfig.BaseHealth;
            double staminaRatio = AttributeSystemConfig.StaminaToHealthRatio;
            var staminaBonus = (int)(attrs.Stamina * staminaRatio);
            var equipmentHealth = EquippedItems
                .Select(kv => ItemData.GetItemById(kv.Value) as Equipment)
                .Where(eq => eq != null)
                .Sum(eq => eq!.HealthBonus);
            var buffHealth = ActiveBuffs
                .Where(b => b.BuffType == StatBuffType.MaxHealth)
                .Sum(b => b.BuffValue);
            return baseHealth + staminaBonus + equipmentHealth + (int)buffHealth;
        }

        /// <summary>
        /// 检查是否拥有物品的个数（已移动到PlayerUtilityService）
        /// </summary>
        [Obsolete("Use PlayerUtilityService.HasItemInInventory instead")]
        public bool HasItemInInventory(string itemId)
        {
            return Inventory.Any(s => !s.IsEmpty && s.ItemId == itemId);
        }

        // ��������
        public int AccuracyRating { get; set; } = 0;   // ���еȼ���Ӱ��������

        // ������ɫ��������
        public AttributeSet BaseAttributes { get; set; } = new AttributeSet();
        
        // �������Է���
        
        /// <summary>
        /// 获取角色的总属性值（已移动到PlayerAttributeService）
        /// </summary>
        [Obsolete("Use PlayerAttributeService.GetTotalAttributes instead")]
        public AttributeSet GetTotalAttributes()
        {
            var total = BaseAttributes.Clone();
            foreach (var itemId in EquippedItems.Values)
            {
                if (string.IsNullOrEmpty(itemId)) continue;
                var item = ItemData.GetItemById(itemId) as Equipment;
                if (item?.AttributeBonuses != null)
                {
                    total.Add(item.AttributeBonuses);
                }
            }
            return total;
        }
        
        /// <summary>
        /// 获取当前职业的主属性值（已移动到PlayerAttributeService）
        /// </summary>
        [Obsolete("Use PlayerAttributeService.GetPrimaryAttributeValue instead")]
        public int GetPrimaryAttributeValue()
        {
            var primaryAttr = ProfessionAttributes.GetPrimaryAttribute(SelectedBattleProfession);
            var attrs = GetTotalAttributes();
            
            return primaryAttr switch
            {
                AttributeType.Strength => attrs.Strength,
                AttributeType.Agility => attrs.Agility,
                AttributeType.Intellect => attrs.Intellect,
                AttributeType.Spirit => attrs.Spirit,
                AttributeType.Stamina => attrs.Stamina,
                _ => 0
            };
        }

        /// <summary>
        /// 更新角色基础属性（已移动到PlayerAttributeService）
        /// </summary>
        [Obsolete("Use PlayerAttributeService.UpdateBaseAttributes instead")]
        public void UpdateBaseAttributes()
        {
            if (BaseAttributes == null)
            {
                BaseAttributes = new AttributeSet();
            }

            var initialAttrs = ProfessionAttributes.GetInitialAttributes(SelectedBattleProfession);
            int level = GetLevel(SelectedBattleProfession);
            BaseAttributes = initialAttrs.Clone();

            for (int i = 1; i < level; i++)
            {
                int currentLevel = i + 1;
                var levelUpAttrs = ProfessionAttributes.GetLevelUpAttributesForLevel(SelectedBattleProfession, currentLevel);
                BaseAttributes.Add(levelUpAttrs);
            }
        }

        /// <summary>
        /// 获取总精确度（已移动到PlayerAttributeService）
        /// </summary>  
        [Obsolete("Use PlayerAttributeService.GetTotalAccuracy instead")]
        public int GetTotalAccuracy()
        {
            var attrs = GetTotalAttributes();
            int primaryAttrBonus = GetPrimaryAttributeValue() / 2;
            var equipmentAccuracy = EquippedItems.Values
                .Select(itemId => ItemData.GetItemById(itemId) as Equipment)
                .Where(eq => eq != null)
                .Sum(eq => eq!.AccuracyBonus);
            return primaryAttrBonus + equipmentAccuracy;
        }
    }
}