namespace BlazorWebGame.Models
{
    /// <summary>
    /// ������ҵĺ�������
    /// </summary>
    public class Player
    {
        /// <summary>
        /// �������
        /// </summary>
        public string Name { get; set; } = "Ӣ��";

        /// <summary>
        /// �������
        /// </summary>
        public int Gold { get; set; } = 0;

        /// <summary>
        /// ��ǰ����ֵ
        /// </summary>
        public int Health { get; set; } = 100;

        /// <summary>
        /// �������ֵ
        /// </summary>
        public int MaxHealth { get; set; } = 100;

        /// <summary>
        /// ����������
        /// </summary>
        public int BaseAttackPower { get; set; } = 10;

        /// <summary>
        /// �����ٶȣ�ÿ�빥��������
        /// </summary>
        public double AttacksPerSecond { get; set; } = 1.0;

        // Ԥ����չ
        // public List<Item> Items { get; set; } = new List<Item>();
        // public Dictionary<EquipmentSlot, Equipment> EquippedItems { get; set; } = new Dictionary<EquipmentSlot, Equipment>();

        /// <summary>
        /// �����ܹ����������������� + װ���ӳɵȣ�
        /// </summary>
        public int GetTotalAttackPower()
        {
            int total = BaseAttackPower;
            // δ�����Լ���װ���Ĺ�����
            // total += EquippedItems.Values.Sum(eq => eq.AttackBonus);
            return total;
        }
    }
}