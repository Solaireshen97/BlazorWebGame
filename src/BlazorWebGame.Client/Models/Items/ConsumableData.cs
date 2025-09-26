using System.Collections.Generic;
using System.Linq;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Models.Items
{
    /// <summary>
    /// ������������Ʒ����Ʒ������
    /// </summary>
    public static class ConsumableData
    {
        private static readonly List<Consumable> _items = new()
        {
            // --- ҩˮ ---
            new Consumable
            {
                Id = "CON_HP_POTION_1", Name = "��������ҩˮ",
                Description = "�����ָ�50������ֵ��",
                Value = 25,
                Category = ConsumableCategory.Potion,
                Effect = ConsumableEffectType.Heal,
                EffectValue = 50,
                CooldownSeconds = 20,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "����Ʒ", Price = 1 }
            },
            
            // --- ս��ʳ�� ---
            new Consumable
            {
                Id = "CON_FOOD_ATK_1", Name = "������",
                Description = "��60���ڣ����5�㹥������",
                Value = 15,
                Category = ConsumableCategory.Food,
                FoodType = FoodType.Combat,
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.AttackPower,
                EffectValue = 5,
                DurationSeconds = 60,
                CooldownSeconds = 60,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "����Ʒ", Price = 30 }
            },
            new Consumable
            {
                Id = "FOOD_GOBLIN_OMELETTE", Name = "�粼�ּ嵰",
                Description = "ζ������֣�����������ս���и����͡�ʳ�ú���߹�������",
                Type = ItemType.Consumable, IsStackable = true, Value = 25,
                Category = ConsumableCategory.Food, FoodType = FoodType.Combat,
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.AttackPower,
                EffectValue = 2,
                DurationSeconds = 300, CooldownSeconds = 5
            },
            
            // --- �ɼ�ʳ�� ---
            new Consumable
            {
                Id = "CON_FOOD_GATHER_1", Name = "������",
                Description = "��120���ڣ����15%�Ĳɼ��ٶȡ�",
                Value = 20,
                Category = ConsumableCategory.Food,
                FoodType = FoodType.Gathering,
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.GatheringSpeed,
                EffectValue = 15,
                DurationSeconds = 120,
                CooldownSeconds = 120,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "����Ʒ", Price = 50 }
            },
            new Consumable
            {
                Id = "CON_FOOD_LUCK_1", Name = "Ѱ���ߵ���",
                Description = "��120���ڣ��ɼ�ʱ��5%�ļ��ʻ�ö����ջ�",
                Value = 35,
                Category = ConsumableCategory.Food,
                FoodType = FoodType.Gathering,
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.ExtraLootChance,
                EffectValue = 5,
                DurationSeconds = 120,
                CooldownSeconds = 120,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "����Ʒ", Price = 75 }
            },
            new Consumable
            {
                Id = "FOOD_COOKED_TROUT", Name = "������",
                Description = "�򵥵���ζ��ʳ�ú��ʱ���������Ĳɼ��ٶȡ�",
                Type = ItemType.Consumable, IsStackable = true, Value = 10,
                Category = ConsumableCategory.Food, FoodType = FoodType.Gathering,
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.GatheringSpeed,
                EffectValue = 5,
                DurationSeconds = 300, CooldownSeconds = 5
            },
            
            // --- ����ʳ�� ---
            new Consumable
            {
                Id = "CON_FOOD_CRAFT_1", Name = "�����������",
                Description = "���������������רע����180���ڣ����10%�������ٶȡ�",
                Value = 25,
                Category = ConsumableCategory.Food,
                FoodType = FoodType.Production,
                Effect = ConsumableEffectType.StatBuff,
                BuffType = StatBuffType.CraftingSpeed,
                EffectValue = 10,
                DurationSeconds = 180,
                CooldownSeconds = 180,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "����Ʒ", Price = 60 }
            },
            
            // --- ͼֽ�䷽ ---
            new Consumable
            {
                Id = "RECIPE_ITEM_GOBLIN_OMELETTE", Name = "ʳ�ף��粼�ּ嵰",
                Description = "�̻�����������粼�ּ嵰��",
                Type = ItemType.Consumable, IsStackable = false, Value = 50,
                Category = ConsumableCategory.Recipe,
                Effect = ConsumableEffectType.LearnRecipe,
                RecipeIdToLearn = "RECIPE_GOBLIN_OMELETTE",
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "����Ʒ", Price = 1 }
            },
            new Consumable
            {
                Id = "RECIPE_ITEM_ENG_ROUGH_BOMB",
                Name = "�ṹͼ������ͭ��ը��",
                Description = "�̻��������������ͭ��ը����",
                Type = ItemType.Consumable,
                IsStackable = false,
                Value = 30,
                Category = ConsumableCategory.Recipe,
                Effect = ConsumableEffectType.LearnRecipe,
                RecipeIdToLearn = "RECIPE_ENG_ROUGH_BOMB"
            },
            
            // --- ����ѧ��Ʒ ---
            new Consumable
            {
                Id = "CON_ENG_ROUGH_BOMB",
                Name = "����ͭ��ը��",
                Description = "һ�����ȶ��ı�ը����ԶԵ������������Χ�˺���",
                Type = ItemType.Consumable,
                Value = 15,
                IsStackable = true,
                Category = ConsumableCategory.Potion
            }
        };

        /// <summary>
        /// ��ȡ��������Ʒ
        /// </summary>
        public static List<Consumable> Items => _items;

        /// <summary>
        /// ����ID��������Ʒ
        /// </summary>
        public static Consumable? GetById(string id) => _items.FirstOrDefault(i => i.Id == id);
        
        /// <summary>
        /// ��ȡ��������Ʒ��ΪItem����
        /// </summary>
        public static List<Item> AllAsItems => _items.Cast<Item>().ToList();
    }
}