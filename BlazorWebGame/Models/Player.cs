namespace BlazorWebGame.Models
{
    /// <summary>
    /// 代表玩家的核心数据
    /// </summary>
    public class Player
    {
        /// <summary>
        /// 玩家名称
        /// </summary>
        public string Name { get; set; } = "英雄";

        /// <summary>
        /// 金币数量
        /// </summary>
        public int Gold { get; set; } = 0;

        /// <summary>
        /// 当前生命值
        /// </summary>
        public int Health { get; set; } = 100;

        /// <summary>
        /// 最大生命值
        /// </summary>
        public int MaxHealth { get; set; } = 100;

        /// <summary>
        /// 基础攻击力
        /// </summary>
        public int BaseAttackPower { get; set; } = 10;

        /// <summary>
        /// 攻击速度（每秒攻击次数）
        /// </summary>
        public double AttacksPerSecond { get; set; } = 1.0;

        // 预留扩展
        // public List<Item> Items { get; set; } = new List<Item>();
        // public Dictionary<EquipmentSlot, Equipment> EquippedItems { get; set; } = new Dictionary<EquipmentSlot, Equipment>();

        /// <summary>
        /// 计算总攻击力（基础攻击力 + 装备加成等）
        /// </summary>
        public int GetTotalAttackPower()
        {
            int total = BaseAttackPower;
            // 未来可以加上装备的攻击力
            // total += EquippedItems.Values.Sum(eq => eq.AttackBonus);
            return total;
        }
    }
}