namespace BlazorWebGame.Models
{
    public enum ItemType
    {
        Equipment, // 装备
        Consumable, // 消耗品
        Material,  // 材料
    }

    public class Item
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ItemType Type { get; set; }
        public bool IsStackable { get; set; } = true; // 是否可堆叠
    }
}