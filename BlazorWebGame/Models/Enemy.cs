namespace BlazorWebGame.Models
{
    /// <summary>
    /// ����һ������
    /// </summary>
    public class Enemy
    {
        /// <summary>
        /// ��������
        /// </summary>
        public string Name { get; set; } = "ʷ��ķ";

        /// <summary>
        /// ��ǰ����ֵ
        /// </summary>
        public int Health { get; set; }

        /// <summary>
        /// �������ֵ
        /// </summary>
        public int MaxHealth { get; set; }

        /// <summary>
        /// ������
        /// </summary>
        public int AttackPower { get; set; }

        /// <summary>
        /// �����ٶȣ�ÿ�빥��������
        /// </summary>
        public double AttacksPerSecond { get; set; }

        /// <summary>
        /// ��С��ҵ�����
        /// </summary>
        public int GoldDropMin { get; set; }

        /// <summary>
        /// ����ҵ�����
        /// </summary>
        public int GoldDropMax { get; set; }

        // Ԥ����չ���������Ʒ�б�
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
        /// �����˵���ģ���һ����ʵ������¡��
        /// </summary>
        public Enemy Clone()
        {
            return new Enemy(Name, MaxHealth, AttackPower, AttacksPerSecond, GoldDropMin, GoldDropMax);
        }

        /// <summary>
        /// ���㱾�ε���Ľ������
        /// </summary>
        public int GetGoldDropAmount()
        {
            return new Random().Next(GoldDropMin, GoldDropMax + 1);
        }
    }
}