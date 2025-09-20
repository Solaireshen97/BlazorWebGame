namespace BlazorWebGame.Models
{
    public enum EquipmentSlot
    {
        Weapon,
        Chest,
        Legs,
        Head,
        Feet,
        Hands
    }

    public class Equipment : Item
    {
        public EquipmentSlot Slot { get; set; }
        public int AttackBonus { get; set; } = 0;
        public int HealthBonus { get; set; } = 0;
        public double AttackSpeedBonus { get; set; } = 0;

        public Equipment()
        {
            Type = ItemType.Equipment;
            IsStackable = false; // 装备通常不可堆叠
        }
    }
}