using System.Collections.Generic;
using BlazorWebGame.Models.Dungeons;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Models.Monsters;

namespace BlazorWebGame.Models.Regions
{
    /// <summary>
    /// ��������
    /// </summary>
    public enum RegionType
    {
        Continent,      // ��½
        Zone,           // ����
        Subzone         // ������
    }

    /// <summary>
    /// ��������
    /// </summary>
    public enum RegionClimate
    {
        Normal,         // ��ͨ
        Desert,         // ɳĮ
        Snow,           // ѩ��
        Swamp,          // ����
        Forest,         // ɭ��
        Mountains,      // ɽ��
        Underwater      // ˮ��
    }

    /// <summary>
    /// ��Ϸ���򣬰������������������Ϣ
    /// </summary>
    public class Region
    {
        /// <summary>
        /// ����Ψһ��ʶ��
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// ��������
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// ��������
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// ��������
        /// </summary>
        public RegionType Type { get; set; } = RegionType.Zone;

        /// <summary>
        /// ��������
        /// </summary>
        public RegionClimate Climate { get; set; } = RegionClimate.Normal;

        /// <summary>
        /// �Ƽ��ȼ�
        /// </summary>
        public int RecommendedLevel { get; set; } = 1;

        /// <summary>
        /// ��ͽ���ȼ�Ҫ��
        /// </summary>
        public int MinimumLevel { get; set; } = 1;

        /// <summary>
        /// ������ID (�����������)
        /// </summary>
        public string? ParentRegionId { get; set; }

        /// <summary>
        /// �������б�
        /// </summary>
        public List<string> SubRegionIds { get; set; } = new List<string>();

        /// <summary>
        /// �����ڵĹ��������б�����MonsterTemplates�еĹ��
        /// </summary>
        public List<string> MonsterNames { get; set; } = new List<string>();

        /// <summary>
        /// ���������������ɵĸ���ID�б�
        /// </summary>
        public List<string> RequiredDungeons { get; set; } = new List<string>();

        /// <summary>
        /// ��������������ɱ�Ĺ��������б�
        /// </summary>
        public List<string> RequiredKills { get; set; } = new List<string>();

        /// <summary>
        /// ���������������ƷID�б�
        /// </summary>
        public List<string> RequiredItems { get; set; } = new List<string>();

        /// <summary>
        /// ������������������ID�б�
        /// </summary>
        public List<string> RequiredQuests { get; set; } = new List<string>();

        /// <summary>
        /// �����������˵��
        /// </summary>
        public string AccessRestrictionMessage { get; set; } = string.Empty;

        /// <summary>
        /// �������Ƿ���Խ��������
        /// </summary>
        public bool CanPlayerAccess(Player player)
        {
            // ���ȼ�Ҫ��
            if (player.GetLevel(player.SelectedBattleProfession) < MinimumLevel)
                return false;

            // ������踱��
            foreach (var dungeonId in RequiredDungeons)
            {
                if (!player.CompletedDungeons.Contains(dungeonId))
                    return false;
            }

            // ��������ɱ
            foreach (var monsterName in RequiredKills)
            {
                if (!player.KilledMonsters.Contains(monsterName))
                    return false;
            }

            // ���������Ʒ
            foreach (var itemId in RequiredItems)
            {
                if (!player.HasItemInInventory(itemId))
                    return false;
            }

            // �����������
            foreach (var questId in RequiredQuests)
            {
                if (!player.CompletedQuests.Contains(questId))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// �������Ƿ���Խ�����������г�Ա����������Ҫ��
        /// </summary>
        public bool CanPartyAccess(Party party, IEnumerable<Player> partyMembers)
        {
            foreach (var member in partyMembers)
            {
                if (!CanPlayerAccess(member))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// ��ȡ������Ĺ����б�
        /// </summary>
        public List<Enemy> GetMonsters()
        {
            List<Enemy> monsters = new List<Enemy>();
            foreach (var monsterName in MonsterNames)
            {
                var monster = MonsterTemplates.GetByName(monsterName);
                if (monster != null)
                    monsters.Add(monster);
            }
            return monsters;
        }

        /// <summary>
        /// ��ȡ�������Ƶ�������Ϣ
        /// </summary>
        public string GetAccessRequirementsDescription()
        {
            List<string> requirements = new List<string>();
            
            if (MinimumLevel > 1)
                requirements.Add($"��Ҫ�ȼ� {MinimumLevel}+");
                
            if (RequiredDungeons.Count > 0)
            {
                List<string> dungeonNames = new List<string>();
                foreach (var dungeonId in RequiredDungeons)
                {
                    var dungeon = DungeonTemplates.GetDungeonById(dungeonId);
                    if (dungeon != null)
                        dungeonNames.Add(dungeon.Name);
                }
                requirements.Add($"��Ҫ��ɸ���: {string.Join(", ", dungeonNames)}");
            }
            
            if (RequiredKills.Count > 0)
                requirements.Add($"��Ҫ��ɱ: {string.Join(", ", RequiredKills)}");
                
            if (RequiredItems.Count > 0)
            {
                List<string> itemNames = new List<string>();
                foreach (var itemId in RequiredItems)
                {
                    var item = ItemData.GetItemById(itemId);
                    if (item != null)
                        itemNames.Add(item.Name);
                }
                requirements.Add($"��Ҫ��Ʒ: {string.Join(", ", itemNames)}");
            }
            
            if (RequiredQuests.Count > 0)
                requirements.Add($"��Ҫ�������: {string.Join(", ", RequiredQuests)}");
                
            if (!string.IsNullOrEmpty(AccessRestrictionMessage))
                requirements.Add(AccessRestrictionMessage);
                
            return string.Join("; ", requirements);
        }
    }
}