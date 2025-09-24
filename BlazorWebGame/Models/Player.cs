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
        // 细分采集活动
        GatheringMining,       // 采矿
        GatheringHerbalism,    // 草药学
        GatheringFishing,      // 钓鱼
        // 细分制作活动
        CraftingCooking,       // 烹饪
        CraftingAlchemy,       // 炼金
        CraftingBlacksmithing, // 锻造
        CraftingJewelcrafting, // 珠宝加工
        CraftingLeatherworking,// 制皮
        CraftingTailoring,     // 裁缝
        CraftingEngineering    // 工程学
    }
    public record ReputationTier(string Name, int MinValue, int MaxValue, string BarColorClass);
    public class Player
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // 赋予每个角色唯一ID
        public bool IsDead { get; set; } = false;
        public double RevivalTimeRemaining { get; set; } = 0;
        // 可以标记为过时，或在确认没有其他地方使用后直接删除
        [Obsolete("使用新的战斗系统")]
        public Enemy? CurrentEnemy { get; set; }
        public GatheringNode? CurrentGatheringNode { get; set; }
        public Recipe? CurrentRecipe { get; set; }
        public double AttackCooldown { get; set; }
        public double GatheringCooldown { get; set; }
        public double CraftingCooldown { get; set; }

        public string Name { get; set; } = "英雄";
        public int Gold { get; set; } = 10000;
        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        public int BaseAttackPower { get; set; } = 10;
        public double AttacksPerSecond { get; set; } = 1.0;

        public BattleProfession SelectedBattleProfession { get; set; } = BattleProfession.Warrior;
        public static readonly List<ReputationTier> ReputationTiers = new()
        {
            // 为了演示，我们使用较小的阈值
            new ReputationTier("中立", 0, 1000, "bg-info"),
            new ReputationTier("友善", 1000, 3000, "bg-success"),
            new ReputationTier("尊敬", 3000, 6000, "bg-primary"),
            new ReputationTier("崇拜", 6000, 6001, "bg-warning") // 崇拜是顶级
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
        public Dictionary<int, string> ProductionFoodQuickSlots { get; set; } = new(); // *** 这是新增的行 ***
        public Dictionary<string, double> ConsumableCooldowns { get; set; } = new();
        public Dictionary<Faction, int> Reputation { get; set; } = new();

        // 用于存储玩家所有任务的当前进度，键是任务ID，值是当前完成数量
        public Dictionary<string, int> QuestProgress { get; set; } = new();

        // 用于记录本周期（今天/本周）已完成的任务ID，防止重复完成
        public List<string> CompletedQuestIds { get; set; } = new();

        // 在Player类中添加这些属性
        public List<string> CompletedDungeons { get; set; } = new List<string>();
        public List<string> KilledMonsters { get; set; } = new List<string>();
        public List<string> CompletedQuests { get; set; } = new List<string>();

        public PlayerActionState CurrentAction { get; set; } = PlayerActionState.Idle;
        public HashSet<string> DefeatedMonsterIds { get; set; } = new();
        public HashSet<string> LearnedRecipeIds { get; set; } = new();
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
            ProductionFoodQuickSlots ??= new();
        }

        /// <summary>
        /// 当从存储加载玩家数据后，调用此方法来确保数据一致性。
        /// </summary>
        public void EnsureDataConsistency()
        {
            InitializeCollections();
        }

        public void AddGatheringXP(GatheringProfession profession, int amount) { if (GatheringProfessionXP.ContainsKey(profession)) { GatheringProfessionXP[profession] += amount; } }
        /// <summary>
        /// 为指定的战斗职业添加经验值，并返回升级结果
        /// </summary>
        /// <param name="profession">职业</param>
        /// <param name="amount">经验值数量</param>
        /// <returns>包含升级信息的元组</returns>
        public (bool LeveledUp, int OldLevel, int NewLevel) AddBattleXP(BattleProfession profession, int amount) 
        { 
            if (BattleProfessionXP.ContainsKey(profession)) 
            {
                int oldLevel = GetLevel(profession);
                
                // 增加经验值
                BattleProfessionXP[profession] += amount;
                
                // 检查是否升级
                int newLevel = GetLevel(profession);
                bool leveledUp = newLevel > oldLevel;
                
                // 返回升级信息，让服务层决定如何处理
                return (leveledUp, oldLevel, newLevel);
            }
            
            return (false, 0, 0);
        }
        /// <summary>
        /// 为指定的生产职业增加经验值
        /// </summary>
        public (bool LeveledUp, int OldLevel, int NewLevel) AddProductionXP(ProductionProfession profession, int amount)
        {
            if (ProductionProfessionXP.ContainsKey(profession))
            {
                int oldLevel = GetLevel(profession);
                
                // 增加经验值
                ProductionProfessionXP[profession] += amount;
                
                // 检查是否升级
                int newLevel = GetLevel(profession);
                bool leveledUp = newLevel > oldLevel;
                
                return (leveledUp, oldLevel, newLevel);
            }
            
            return (false, 0, 0);
        }

        // 可以添加一个辅助方法来获取声望等级
        public ReputationTier GetReputationLevel(Faction faction)
        {
            var rep = Reputation.GetValueOrDefault(faction, 0);
            // 从高到低查找，返回第一个满足条件的等级
            return ReputationTiers.LastOrDefault(t => rep >= t.MinValue) ?? ReputationTiers.First();
        }

        public double GetReputationProgressPercentage(Faction faction)
        {
            var rep = Reputation.GetValueOrDefault(faction, 0);
            var tier = GetReputationLevel(faction);

            // 如果是最高等级，进度条直接拉满
            if (tier.MaxValue - tier.MinValue <= 1)
            {
                return 100.0;
            }

            var progressInTier = rep - tier.MinValue;
            var totalForTier = tier.MaxValue - tier.MinValue;

            return (double)progressInTier / totalForTier * 100.0;
        }

        public double GetTotalGatheringSpeedBonus() { double equipmentBonus = EquippedItems.Values.Select(itemId => ItemData.GetItemById(itemId) as Equipment).Where(eq => eq != null).Sum(eq => eq!.GatheringSpeedBonus); double buffBonus = ActiveBuffs.Where(b => b.BuffType == StatBuffType.GatheringSpeed).Sum(b => b.BuffValue / 100.0); return equipmentBonus + buffBonus; }
        public double GetTotalExtraLootChance() { double equipmentBonus = EquippedItems.Values.Select(itemId => ItemData.GetItemById(itemId) as Equipment).Where(eq => eq != null).Sum(eq => eq!.ExtraLootChanceBonus); double buffBonus = ActiveBuffs.Where(b => b.BuffType == StatBuffType.ExtraLootChance).Sum(b => b.BuffValue / 100.0); return equipmentBonus + buffBonus; }
        /// <summary>
        /// 获取总的制作速度加成（以小数形式，例如 0.1 代表 +10%）
        /// </summary>
        public double GetTotalCraftingSpeedBonus()
        {
            // 未来可以为装备增加制作速度加成
            double equipmentBonus = 0.0;

            // 从Buff中获取加成
            double buffBonus = ActiveBuffs
                .Where(b => b.BuffType == StatBuffType.CraftingSpeed)
                .Sum(b => b.BuffValue / 100.0); // 将整数百分比 (如 15) 转换为小数 (0.15)

            return equipmentBonus + buffBonus;
        }
        public int GetLevel(BattleProfession profession) => 
            BattleProfessionXP.TryGetValue(profession, out var xp) ? 
            ExpSystem.GetLevelFromExp(xp) : 1;

        public int GetLevel(GatheringProfession profession) => 
            GatheringProfessionXP.TryGetValue(profession, out var xp) ? 
            ExpSystem.GetLevelFromExp(xp) : 1;

        // 泛用方法也需要更新
        public int GetLevel(int xp) => ExpSystem.GetLevelFromExp(xp);

        public int GetLevel(ProductionProfession profession) =>
            ExpSystem.GetLevelFromExp(ProductionProfessionXP.GetValueOrDefault(profession, 0));

        // 添加一个新方法，获取升级进度
        public double GetLevelProgress(BattleProfession profession) =>
            BattleProfessionXP.TryGetValue(profession, out var xp) ?
            ExpSystem.GetLevelProgressPercentage(xp) : 0;
            
        public double GetLevelProgress(GatheringProfession profession) =>
            GatheringProfessionXP.TryGetValue(profession, out var xp) ?
            ExpSystem.GetLevelProgressPercentage(xp) : 0;

        public double GetLevelProgress(ProductionProfession profession) =>
            ExpSystem.GetLevelProgressPercentage(ProductionProfessionXP.GetValueOrDefault(profession, 0));

        public int GetTotalAttackPower()
        {
            var attrs = GetTotalAttributes();
            var baseAttack = 5;
            var primaryAttrBonus = 0;
            
            if (SelectedBattleProfession == BattleProfession.Warrior)
            {
                // 战士: 力量影响攻击力，每点力量增加2点攻击力
                primaryAttrBonus = attrs.Strength * 2;
            }
            else if (SelectedBattleProfession == BattleProfession.Mage)
            {
                // 法师: 智力影响攻击力，每点智力增加2点攻击力
                primaryAttrBonus = attrs.Intellect * 2;
            }
            
            var equipmentAttack = EquippedItems
                .Select(kv => ItemData.GetItemById(kv.Value) as Equipment)
                .Where(eq => eq != null)
                .Sum(eq => eq!.AttackBonus);

            var buffAttack = ActiveBuffs
                .Where(b => b.BuffType == StatBuffType.AttackPower)
                .Sum(b => b.BuffValue);

            return baseAttack + primaryAttrBonus + equipmentAttack + (int)buffAttack;
        }

        // *** 这是修正点 ***
        public int GetTotalMaxHealth()
        {
            var attrs = GetTotalAttributes();
            var baseHealth = 100;
            
            // 耐力影响生命值，每点耐力增加10点生命值
            var staminaBonus = attrs.Stamina * 10;
            
            var equipmentHealth = EquippedItems
                .Select(kv => ItemData.GetItemById(kv.Value) as Equipment)
                .Where(eq => eq != null)
                .Sum(eq => eq!.HealthBonus);

            var buffHealth = ActiveBuffs
                .Where(b => b.BuffType == StatBuffType.MaxHealth)
                .Sum(b => b.BuffValue);

            return baseHealth + staminaBonus + equipmentHealth + (int)buffHealth;
        }

        // 检查是否有物品的辅助方法
        public bool HasItemInInventory(string itemId)
        {
            return Inventory.Any(s => !s.IsEmpty && s.ItemId == itemId);
        }

        // 新增属性
        public int AccuracyRating { get; set; } = 0;   // 命中等级，影响命中率

        // 新增角色基础属性
        public AttributeSet BaseAttributes { get; set; } = new AttributeSet();
        
        // 新增属性方法
        
        /// <summary>
        /// 获取角色的总属性值（基础+装备+buff）
        /// </summary>
        public AttributeSet GetTotalAttributes()
        {
            // 获取基础属性
            var total = BaseAttributes.Clone();
            
            // 添加装备属性加成
            foreach (var itemId in EquippedItems.Values)
            {
                if (string.IsNullOrEmpty(itemId)) continue;
                
                var item = ItemData.GetItemById(itemId) as Equipment;
                if (item?.AttributeBonuses != null)
                {
                    total.Add(item.AttributeBonuses);
                }
            }
            
            // TODO: 如果有buff属性加成，在这里添加
    
            return total;
        }
        
        /// <summary>
        /// 获取当前职业的主属性值
        /// </summary>
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
        /// 更新角色基础属性，应在创建角色或变更职业时调用
        /// </summary>
        public void UpdateBaseAttributes()
        {
            // 获取职业初始属性
            var initialAttrs = ProfessionAttributes.GetInitialAttributes(SelectedBattleProfession);
            
            // 获取每级属性成长
            var levelUpAttrs = ProfessionAttributes.GetLevelUpAttributes(SelectedBattleProfession);
            
            // 计算当前等级的属性
            int level = GetLevel(SelectedBattleProfession);
            int levelBonus = level - 1; // 减去初始等级
    
            // 设置基础属性 = 初始属性 + 等级成长
            BaseAttributes = new AttributeSet
            {
                Strength = initialAttrs.Strength + (levelUpAttrs.Strength * levelBonus),
                Agility = initialAttrs.Agility + (levelUpAttrs.Agility * levelBonus),
                Intellect = initialAttrs.Intellect + (levelUpAttrs.Intellect * levelBonus),
                Spirit = initialAttrs.Spirit + (levelUpAttrs.Spirit * levelBonus),
                Stamina = initialAttrs.Stamina + (levelUpAttrs.Stamina * levelBonus)
            };
        }
        
        /// <summary>
        /// 获取角色的总命中率，受敏捷和主属性影响
        /// </summary>
        public int GetTotalAccuracy()
        {
            // 获取属性总值
            var attrs = GetTotalAttributes();
            
            // 基础命中率来自敏捷
            var baseAccuracy = attrs.Agility * 2;
            
            // 主属性加成
            int primaryAttrBonus = 0;
            if (SelectedBattleProfession == BattleProfession.Warrior)
            {
                // 战士: 力量影响命中
                primaryAttrBonus = attrs.Strength;
            }
            else if (SelectedBattleProfession == BattleProfession.Mage)
            {
                // 法师: 智力影响命中
                primaryAttrBonus = attrs.Intellect;
            }
            
            // 装备直接命中率加成
            var equipmentAccuracy = EquippedItems.Values
                .Select(itemId => ItemData.GetItemById(itemId) as Equipment)
                .Where(eq => eq != null)
                .Sum(eq => eq!.AccuracyBonus);
                
            // TODO: 如果有buff命中率加成，在这里添加
    
            return baseAccuracy + (primaryAttrBonus / 2) + equipmentAccuracy;
        }
    }
}