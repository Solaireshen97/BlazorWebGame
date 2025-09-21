namespace BlazorWebGame.Models
{
    // 战斗职业
    public enum BattleProfession
    {
        Warrior, // 战士
        Mage     // 法师
    }

    // 采集职业
    public enum GatheringProfession
    {
        Miner,     // 采矿
        Herbalist, // 草药学
        Fishing    // 钓鱼
    }

    // 生产职业
    public enum ProductionProfession
    {
        Cooking,      // 烹饪
        Alchemy,      // 炼金
        Blacksmithing, // 锻造
        Jewelcrafting, // 珠宝加工
        Leatherworking // <-- 在这里添加
    }
}