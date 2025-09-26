using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;

namespace BlazorWebGame.Models
{
    public static class ProfessionExtensions
    {
        public static string ToChineseString(this BattleProfession profession)
        {
            return profession switch
            {
                BattleProfession.Warrior => "战士",
                BattleProfession.Mage => "法师",
                _ => profession.ToString() // 作为备用，如果未来有新职业忘记翻译
            };
        }

        public static string ToChineseString(this GatheringProfession profession)
        {
            return profession switch
            {
                GatheringProfession.Miner => "采矿",
                GatheringProfession.Herbalist => "草药学",
                GatheringProfession.Fishing => "钓鱼",
                _ => profession.ToString()
            };
        }

        public static string ToChineseString(this ProductionProfession profession)
        {
            return profession switch
            {
                ProductionProfession.Cooking => "烹饪",
                ProductionProfession.Alchemy => "炼金",
                ProductionProfession.Blacksmithing => "锻造",
                ProductionProfession.Jewelcrafting => "珠宝加工",
                ProductionProfession.Leatherworking => "制皮",
                ProductionProfession.Tailoring => "裁缝",
                ProductionProfession.Engineering => "工程学",
                _ => profession.ToString()
            };
        }

        // 增加一个PlayerActionState的翻译方法
        public static string ToChineseString(this PlayerActionState state)
        {
            return state switch
            {
                PlayerActionState.Idle => "空闲",
                PlayerActionState.Combat => "战斗",
                PlayerActionState.GatheringMining => "采矿中",
                PlayerActionState.GatheringHerbalism => "采集草药中",
                PlayerActionState.GatheringFishing => "钓鱼中",
                PlayerActionState.CraftingCooking => "烹饪中",
                PlayerActionState.CraftingAlchemy => "炼金中",
                PlayerActionState.CraftingBlacksmithing => "锻造中",
                PlayerActionState.CraftingJewelcrafting => "珠宝加工中",
                PlayerActionState.CraftingLeatherworking => "制皮中",
                PlayerActionState.CraftingTailoring => "裁缝中",
                PlayerActionState.CraftingEngineering => "工程制作中",
                _ => state.ToString()
            };
        }
    }
}