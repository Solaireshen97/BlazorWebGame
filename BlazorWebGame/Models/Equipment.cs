namespace BlazorWebGame.Models
{
    /// <summary>
    /// 定义所有可用的装备槽位，灵感来源于经典MMORPG
    /// </summary>
    public enum EquipmentSlot
    {
        // --- 核心护甲 (左侧) ---
        Head,     // 头部
        Neck,     // 颈部
        Shoulder, // 肩部
        Back,     // 背部 (披风)
        Chest,    // 胸部
        Wrist,    // 手腕 (护腕)

        // --- 核心护甲 (右侧) ---
        Hands,    // 手部 (手套)
        Waist,    // 腰部 (腰带)
        Legs,     // 腿部
        Feet,     // 脚部

        // --- 饰品和戒指 (右侧) ---
        Finger1,  // 第一个戒指
        Finger2,  // 第二个戒指
        Trinket1, // 第一个饰品
        Trinket2, // 第二个饰品

        // --- 武器 (底部) ---
        MainHand, // 主手武器
        OffHand   // 副手 (可以是盾牌或副手武器)
    }

    public class Equipment : Item
    {
        public EquipmentSlot Slot { get; set; }
        public int AttackBonus { get; set; } = 0;
        public int HealthBonus { get; set; } = 0;
        public double AttackSpeedBonus { get; set; } = 0;
        public double GatheringSpeedBonus { get; set; } = 0;
        public double ExtraLootChanceBonus { get; set; } = 0;

        public Equipment()
        {
            Type = ItemType.Equipment;
            IsStackable = false;
        }
    }
}