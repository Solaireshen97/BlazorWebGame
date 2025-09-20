using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models
{
    public class Player
    {
        public string Name { get; set; } = "Ӣ��";
        public int Gold { get; set; } = 0;
        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        public int BaseAttackPower { get; set; } = 10;
        public double AttacksPerSecond { get; set; } = 1.0;

        public BattleProfession SelectedBattleProfession { get; set; } = BattleProfession.Warrior;
        public Dictionary<BattleProfession, int> BattleProfessionXP { get; set; } = new();
        public Dictionary<GatheringProfession, int> GatheringProfessionXP { get; set; } = new();
        public Dictionary<ProductionProfession, int> ProductionProfessionXP { get; set; } = new();

        // --- ����ϵͳ�ֶ� ---

        /// <summary>
        /// ����Ѿ����������й�����ID
        /// </summary>
        public HashSet<string> LearnedSharedSkills { get; set; } = new();

        /// <summary>
        /// ÿ��ְҵװ���ļ���ID�б�
        /// </summary>
        public Dictionary<BattleProfession, List<string>> EquippedSkills { get; set; } = new();

        /// <summary>
        /// ׷�ټ��ܵĵ�ǰ��ȴ�غ�����Key: SkillId, Value: ʣ��غ�
        /// </summary>
        public Dictionary<string, int> SkillCooldowns { get; set; } = new();

        // --- �������� ---
        /// <summary>
        /// ��ҵĿ�档Key: ItemId, Value: ����
        /// </summary>
        public Dictionary<string, int> Inventory { get; set; } = new();

        /// <summary>
        /// ����Ѵ�����װ����Key: װ����λ, Value: ItemId
        /// </summary>
        public Dictionary<EquipmentSlot, string> EquippedItems { get; set; } = new();

        public Player()
        {
            // ��ʼ��ְҵ
            foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession)))
            {
                BattleProfessionXP.TryAdd(profession, 0);
                EquippedSkills.TryAdd(profession, new List<string>());
            }
            GatheringProfessionXP.TryAdd(GatheringProfession.Miner, 0);
            ProductionProfessionXP.TryAdd(ProductionProfession.Tailor, 0);
        }

        public void AddBattleXP(BattleProfession profession, int amount)
        {
            if (BattleProfessionXP.ContainsKey(profession))
            {
                BattleProfessionXP[profession] += amount;
            }
        }

        /// <summary>
        /// ��ȡָ��ս��ְҵ�ĵȼ�
        /// </summary>
        public int GetLevel(BattleProfession profession)
        {
            return BattleProfessionXP.TryGetValue(profession, out var xp) ? 1 + (xp / 100) : 1;
        }

        /// <summary>
        /// ���ݾ���ֵ����ȼ���ͨ������UI��ʾ���ڲ����㣩
        /// </summary>
        public int GetLevel(int xp) => 1 + (xp / 100);

        /// <summary>
        /// ��ȡ��ҵ��ܹ����������������� + װ���ӳɣ�
        /// </summary>
        public int GetTotalAttackPower()
        {
            int equipmentBonus = 0;
            foreach (var itemId in EquippedItems.Values)
            {
                if (ItemData.GetItemById(itemId) is Equipment eq)
                {
                    equipmentBonus += eq.AttackBonus;
                }
            }
            return this.BaseAttackPower + equipmentBonus;
        }

        /// <summary>
        /// ��ȡ��ҵ�������ֵ���ޣ���������ֵ + װ���ӳɣ�
        /// </summary>
        public int GetTotalMaxHealth()
        {
            int equipmentBonus = 0;
            foreach (var itemId in EquippedItems.Values)
            {
                if (ItemData.GetItemById(itemId) is Equipment eq)
                {
                    equipmentBonus += eq.HealthBonus;
                }
            }
            return this.MaxHealth + equipmentBonus;
        }
    }
}