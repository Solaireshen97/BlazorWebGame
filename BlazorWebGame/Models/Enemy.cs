namespace BlazorWebGame.Models
{
    /// <summary>
    /// 代表一个敌人
    /// </summary>
    public class Enemy
    {
        /// <summary>
        /// 敌人名称
        /// </summary>
        public string Name { get; set; } = "史莱姆";

        /// <summary>
        /// 当前生命值
        /// </summary>
        public int Health { get; set; }

        /// <summary>
        /// 最大生命值
        /// </summary>
        public int MaxHealth { get; set; }

        /// <summary>
        /// 攻击力
        /// </summary>
        public int AttackPower { get; set; }

        /// <summary>
        /// 攻击速度（每秒攻击次数）
        /// </summary>
        public double AttacksPerSecond { get; set; }

        /// <summary>
        /// 最小金币掉落量
        /// </summary>
        public int GoldDropMin { get; set; }

        /// <summary>
        /// 最大金币掉落量
        /// </summary>
        public int GoldDropMax { get; set; }

        // 预留扩展：掉落的物品列表
        // public List<ItemDrop> ItemDrops { get; set; } = new List<ItemDrop>();

        public Enemy(string name, int maxHealth, int attackPower, double attacksPerSecond, int goldDropMin, int goldDropMax)
        {
            Name = name;
            MaxHealth = maxHealth;
            Health = maxHealth;
            AttackPower = attackPower;
            AttacksPerSecond = attacksPerSecond;
            GoldDropMin = goldDropMin;
            GoldDropMax = goldDropMax;
        }

        /// <summary>
        /// 创建此敌人模板的一个新实例（克隆）
        /// </summary>
        public Enemy Clone()
        {
            return new Enemy(Name, MaxHealth, AttackPower, AttacksPerSecond, GoldDropMin, GoldDropMax);
        }

        /// <summary>
        /// 计算本次掉落的金币数量
        /// </summary>
        public int GetGoldDropAmount()
        {
            return new Random().Next(GoldDropMin, GoldDropMax + 1);
        }
    }
}