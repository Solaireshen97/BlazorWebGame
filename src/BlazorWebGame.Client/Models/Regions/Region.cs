using System.Collections.Generic;
using BlazorWebGame.Models.Dungeons;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Models.Monsters;

namespace BlazorWebGame.Models.Regions
{
    /// <summary>
    /// 区域类型
    /// </summary>
    public enum RegionType
    {
        Continent,      // 大陆
        Zone,           // 区域
        Subzone         // 子区域
    }

    /// <summary>
    /// 区域气候
    /// </summary>
    public enum RegionClimate
    {
        Normal,         // 普通
        Desert,         // 沙漠
        Snow,           // 雪地
        Swamp,          // 沼泽
        Forest,         // 森林
        Mountains,      // 山脉
        Underwater      // 水下
    }

    /// <summary>
    /// 游戏区域，包含怪物、访问条件等信息
    /// </summary>
    public class Region
    {
        /// <summary>
        /// 区域唯一标识符
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 区域名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 区域描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 区域类型
        /// </summary>
        public RegionType Type { get; set; } = RegionType.Zone;

        /// <summary>
        /// 区域气候
        /// </summary>
        public RegionClimate Climate { get; set; } = RegionClimate.Normal;

        /// <summary>
        /// 推荐等级
        /// </summary>
        public int RecommendedLevel { get; set; } = 1;

        /// <summary>
        /// 最低进入等级要求
        /// </summary>
        public int MinimumLevel { get; set; } = 1;

        /// <summary>
        /// 父区域ID (如果是子区域)
        /// </summary>
        public string? ParentRegionId { get; set; }

        /// <summary>
        /// 子区域列表
        /// </summary>
        public List<string> SubRegionIds { get; set; } = new List<string>();

        /// <summary>
        /// 区域内的怪物名称列表（引用MonsterTemplates中的怪物）
        /// </summary>
        public List<string> MonsterNames { get; set; } = new List<string>();

        /// <summary>
        /// 进入此区域所需完成的副本ID列表
        /// </summary>
        public List<string> RequiredDungeons { get; set; } = new List<string>();

        /// <summary>
        /// 进入此区域所需击杀的怪物名称列表
        /// </summary>
        public List<string> RequiredKills { get; set; } = new List<string>();

        /// <summary>
        /// 进入此区域所需物品ID列表
        /// </summary>
        public List<string> RequiredItems { get; set; } = new List<string>();

        /// <summary>
        /// 进入此区域所需的任务ID列表
        /// </summary>
        public List<string> RequiredQuests { get; set; } = new List<string>();

        /// <summary>
        /// 区域进入限制说明
        /// </summary>
        public string AccessRestrictionMessage { get; set; } = string.Empty;

        /// <summary>
        /// 检查玩家是否可以进入此区域
        /// </summary>
        public bool CanPlayerAccess(Player player)
        {
            // 检查等级要求
            if (player.GetLevel(player.SelectedBattleProfession) < MinimumLevel)
                return false;

            // 检查所需副本
            foreach (var dungeonId in RequiredDungeons)
            {
                if (!player.CompletedDungeons.Contains(dungeonId))
                    return false;
            }

            // 检查所需击杀
            foreach (var monsterName in RequiredKills)
            {
                if (!player.KilledMonsters.Contains(monsterName))
                    return false;
            }

            // 检查所需物品
            foreach (var itemId in RequiredItems)
            {
                if (!player.HasItemInInventory(itemId))
                    return false;
            }

            // 检查所需任务
            foreach (var questId in RequiredQuests)
            {
                if (!player.CompletedQuests.Contains(questId))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 检查队伍是否可以进入此区域（所有成员都必须满足要求）
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
        /// 获取此区域的怪物列表
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
        /// 获取进入限制的描述信息
        /// </summary>
        public string GetAccessRequirementsDescription()
        {
            List<string> requirements = new List<string>();
            
            if (MinimumLevel > 1)
                requirements.Add($"需要等级 {MinimumLevel}+");
                
            if (RequiredDungeons.Count > 0)
            {
                List<string> dungeonNames = new List<string>();
                foreach (var dungeonId in RequiredDungeons)
                {
                    var dungeon = DungeonTemplates.GetDungeonById(dungeonId);
                    if (dungeon != null)
                        dungeonNames.Add(dungeon.Name);
                }
                requirements.Add($"需要完成副本: {string.Join(", ", dungeonNames)}");
            }
            
            if (RequiredKills.Count > 0)
                requirements.Add($"需要击杀: {string.Join(", ", RequiredKills)}");
                
            if (RequiredItems.Count > 0)
            {
                List<string> itemNames = new List<string>();
                foreach (var itemId in RequiredItems)
                {
                    var item = ItemData.GetItemById(itemId);
                    if (item != null)
                        itemNames.Add(item.Name);
                }
                requirements.Add($"需要物品: {string.Join(", ", itemNames)}");
            }
            
            if (RequiredQuests.Count > 0)
                requirements.Add($"需要完成任务: {string.Join(", ", RequiredQuests)}");
                
            if (!string.IsNullOrEmpty(AccessRestrictionMessage))
                requirements.Add(AccessRestrictionMessage);
                
            return string.Join("; ", requirements);
        }
    }
}