namespace BlazorWebGame.Models
{
    public enum EquipmentSlot
    {
        Weapon,
        Chest,
        Legs,
        Head,
        Feet,
        Hands,
        Finger, // *** 新增 ***
        Neck    // *** 新增 ***
    }

    public class Equipment : Item
    {
        public EquipmentSlot Slot { get; set; }
        public int AttackBonus { get; set; } = 0;
        public int HealthBonus { get; set; } = 0;
        public double AttackSpeedBonus { get; set; } = 0;

        /// <summary>
        /// 采集速度加成 (例如, 0.1 代表 +10%)
        /// </summary>
        public double GatheringSpeedBonus { get; set; } = 0;

        /// <summary>
        /// 采集时额外获得一个物品的几率加成 (例如, 0.05 代表 +5%)
        /// </summary>
        public double ExtraLootChanceBonus { get; set; } = 0;

        public Equipment()
        {
            Type = ItemType.Equipment;
            IsStackable = false; // 装备通常不可堆叠
        }
    }
}