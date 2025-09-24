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
        Gathering,
        Crafting
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

        public PlayerActionState CurrentAction { get; set; } = PlayerActionState.Idle;
        public HashSet<string> DefeatedMonsterIds { get; set; } = new();
        public HashSet<string> LearnedRecipeIds { get; set; } = new();
        public Player()
        {
            // ���캯������ֻ���ó�ʼ������
            InitializeCollections();
        }

        /// <summary>
        /// ȷ�����м��Ϻ��ֵ䶼�ѳ�ʼ������ֹ�Ӿɴ浵����ʱ����null���á�
        /// </summary>
        private void InitializeCollections()
        {
            // ʹ�� '??=' ����������������� null���͸�������һ���µ�ʵ����
            BattleProfessionXP ??= new();
            GatheringProfessionXP ??= new();
            ProductionProfessionXP ??= new();
            EquippedSkills ??= new();

            // Ϊ���п��ܵ�ְҵ��ʼ������ͼ����б�
            foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession)))
            {
                BattleProfessionXP.TryAdd(profession, 1000);
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
            // ȷ��Ĭ���䷽�ѱ�ѧϰ
            foreach (var recipe in RecipeData.AllRecipes.Where(r => r.IsDefault))
            {
                LearnedRecipeIds.Add(recipe.Id);
            }
            // ��ʼ������
            if (Inventory == null || !Inventory.Any())
            {
                Inventory = new List<InventorySlot>();
                for (int i = 0; i < 20; i++)
                {
                    Inventory.Add(new InventorySlot());
                }
            }

            // *** ������ؼ����������֣�ȷ�������¾ɼ��϶��ǿ� ***
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
        /// ���Ӵ洢����������ݺ󣬵��ô˷�����ȷ������һ���ԡ�
        /// </summary>
        public void EnsureDataConsistency()
        {
            InitializeCollections();
        }

        // ... (�ļ������ಿ�ֱ��ֲ���) ...
        public void AddGatheringXP(GatheringProfession profession, int amount) { if (GatheringProfessionXP.ContainsKey(profession)) { GatheringProfessionXP[profession] += amount; } }
        public void AddBattleXP(BattleProfession profession, int amount) { if (BattleProfessionXP.ContainsKey(profession)) { BattleProfessionXP[profession] += amount; } }
        /// <summary>
        /// Ϊָ��������ְҵ���Ӿ���ֵ
        /// </summary>
        public void AddProductionXP(ProductionProfession profession, int amount)
        {
            if (ProductionProfessionXP.ContainsKey(profession))
            {
                ProductionProfessionXP[profession] += amount;
            }
        }

        // �������һ��������������ȡ�����ȼ�
        public ReputationTier GetReputationLevel(Faction faction)
        {
            var rep = Reputation.GetValueOrDefault(faction, 0);
            // �Ӹߵ��Ͳ��ң����ص�һ�����������ĵȼ�
            return ReputationTiers.LastOrDefault(t => rep >= t.MinValue) ?? ReputationTiers.First();
        }

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

        public double GetTotalGatheringSpeedBonus() { double equipmentBonus = EquippedItems.Values.Select(itemId => ItemData.GetItemById(itemId) as Equipment).Where(eq => eq != null).Sum(eq => eq!.GatheringSpeedBonus); double buffBonus = ActiveBuffs.Where(b => b.BuffType == StatBuffType.GatheringSpeed).Sum(b => b.BuffValue / 100.0); return equipmentBonus + buffBonus; }
        public double GetTotalExtraLootChance() { double equipmentBonus = EquippedItems.Values.Select(itemId => ItemData.GetItemById(itemId) as Equipment).Where(eq => eq != null).Sum(eq => eq!.ExtraLootChanceBonus); double buffBonus = ActiveBuffs.Where(b => b.BuffType == StatBuffType.ExtraLootChance).Sum(b => b.BuffValue / 100.0); return equipmentBonus + buffBonus; }
        /// <summary>
        /// ��ȡ�ܵ������ٶȼӳɣ���С����ʽ������ 0.1 ���� +10%��
        /// </summary>
        public double GetTotalCraftingSpeedBonus()
        {
            // δ������Ϊװ�����������ٶȼӳ�
            double equipmentBonus = 0.0;

            // ��Buff�л�ȡ�ӳ�
            double buffBonus = ActiveBuffs
                .Where(b => b.BuffType == StatBuffType.CraftingSpeed)
                .Sum(b => b.BuffValue / 100.0); // �������ٷֱ� (�� 15) ת��ΪС�� (0.15)

            return equipmentBonus + buffBonus;
        }
        public int GetLevel(BattleProfession profession) => BattleProfessionXP.TryGetValue(profession, out var xp) ? 1 + (xp / 100) : 1;
        public int GetLevel(GatheringProfession profession) => GatheringProfessionXP.TryGetValue(profession, out var xp) ? 1 + (xp / 100) : 1;
        public int GetLevel(int xp) => 1 + (xp / 100);

        public int GetLevel(ProductionProfession profession)
        {
            var xp = ProductionProfessionXP.GetValueOrDefault(profession, 0);
            return 1 + (xp / 100); // ������ʹ����Ŀ�����еĵȼ����㹫ʽ
        }
        // *** ���������� ***
        public int GetTotalAttackPower()
        {
            var baseAttack = 5;
            var equipmentAttack = EquippedItems
                .Select(kv => ItemData.GetItemById(kv.Value) as Equipment)
                .Where(eq => eq != null)
                .Sum(eq => eq!.AttackBonus); // ����: AttackPower -> AttackBonus

            var buffAttack = ActiveBuffs
                .Where(b => b.BuffType == StatBuffType.AttackPower)
                .Sum(b => b.BuffValue);

            return baseAttack + equipmentAttack + (int)buffAttack;
        }

        // *** ���������� ***
        public int GetTotalMaxHealth()
        {
            var baseHealth = 100;
            var equipmentHealth = EquippedItems
                .Select(kv => ItemData.GetItemById(kv.Value) as Equipment)
                .Where(eq => eq != null)
                .Sum(eq => eq!.HealthBonus); // ����: Health -> HealthBonus

            var buffHealth = ActiveBuffs
                .Where(b => b.BuffType == StatBuffType.MaxHealth)
                .Sum(b => b.BuffValue);

            return baseHealth + equipmentHealth + (int)buffHealth;
        }
    }
}