using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models.Regions
{
    /// <summary>
    /// 区域数据模板类
    /// </summary>
    public static class RegionTemplates
    {
        /// <summary>
        /// 所有区域列表
        /// </summary>
        public static List<Region> All { get; private set; } = InitializeRegions();

        /// <summary>
        /// 获取顶级区域（大陆）
        /// </summary>
        public static List<Region> GetContinents()
        {
            return All.Where(r => r.Type == RegionType.Continent).ToList();
        }

        /// <summary>
        /// 根据ID获取区域
        /// </summary>
        public static Region? GetById(string id)
        {
            return All.FirstOrDefault(r => r.Id == id);
        }

        /// <summary>
        /// 获取指定区域的子区域
        /// </summary>
        public static List<Region> GetSubRegions(string parentId)
        {
            return All.Where(r => r.ParentRegionId == parentId).ToList();
        }

        /// <summary>
        /// 初始化所有区域数据
        /// </summary>
        private static List<Region> InitializeRegions()
        {
            List<Region> regions = new List<Region>();

            // 添加艾泽拉斯大陆
            var azeroth = new Region
            {
                Id = "azeroth",
                Name = "艾泽拉斯",
                Description = "艾泽拉斯是一个充满魔法和危险的大陆，是众多种族的家园。",
                Type = RegionType.Continent,
                RecommendedLevel = 1,
                MinimumLevel = 1
            };
            regions.Add(azeroth);

            // 添加东部王国区域
            var easternKingdoms = new Region
            {
                Id = "eastern-kingdoms",
                Name = "东部王国",
                Description = "东部王国是艾泽拉斯大陆上最大的区域之一，包含多个人类王国和矮人领地。",
                Type = RegionType.Zone,
                RecommendedLevel = 1,
                MinimumLevel = 1,
                ParentRegionId = "azeroth"
            };
            regions.Add(easternKingdoms);
            azeroth.SubRegionIds.Add(easternKingdoms.Id);

            // 添加艾尔文森林子区域
            var elwynnForest = new Region
            {
                Id = "elwynn-forest",
                Name = "艾尔文森林",
                Description = "艾尔文森林是暴风城周围的一片茂密森林，适合初级冒险者。",
                Type = RegionType.Subzone,
                Climate = RegionClimate.Forest,
                RecommendedLevel = 1,
                MinimumLevel = 1,
                ParentRegionId = "eastern-kingdoms",
                MonsterNames = new List<string> { "染病的幼狼", "染病的森林狼", "Murloc", "Goblin", "GoblinElite" }
            };
            regions.Add(elwynnForest);
            easternKingdoms.SubRegionIds.Add(elwynnForest.Id);

            // 添加西部荒野子区域
            var westfall = new Region
            {
                Id = "westfall",
                Name = "西部荒野",
                Description = "西部荒野曾经是人类的粮仓，现在遍布盗匪和饥饿的农民。",
                Type = RegionType.Subzone,
                Climate = RegionClimate.Normal,
                RecommendedLevel = 10,
                MinimumLevel = 8,
                ParentRegionId = "eastern-kingdoms",
                MonsterNames = new List<string> { "Defias", "Harvest Golem", "Vulture" },
                RequiredQuests = new List<string> { "quest_elwynn_completion" }
            };
            regions.Add(westfall);
            easternKingdoms.SubRegionIds.Add(westfall.Id);

            // 添加卡利姆多区域
            var kalimdor = new Region
            {
                Id = "kalimdor",
                Name = "卡利姆多",
                Description = "卡利姆多是艾泽拉斯西部的大陆，是暗夜精灵、牛头人和兽人的家园。",
                Type = RegionType.Zone,
                RecommendedLevel = 1,
                MinimumLevel = 1,
                ParentRegionId = "azeroth"
            };
            regions.Add(kalimdor);
            azeroth.SubRegionIds.Add(kalimdor.Id);

            // 添加杜隆塔尔子区域
            var durotar = new Region
            {
                Id = "durotar",
                Name = "杜隆塔尔",
                Description = "杜隆塔尔是兽人和巨魔的家园，荒芜的红色土地和险峻的峡谷。",
                Type = RegionType.Subzone,
                Climate = RegionClimate.Desert,
                RecommendedLevel = 1,
                MinimumLevel = 1,
                ParentRegionId = "kalimdor",
                MonsterNames = new List<string> { "Scorpid", "Boar", "Quilboar" }
            };
            regions.Add(durotar);
            kalimdor.SubRegionIds.Add(durotar.Id);

            // 添加外域大陆
            var outland = new Region
            {
                Id = "outland",
                Name = "外域",
                Description = "外域是德拉诺残骸，一个被扭曲的破碎世界。",
                Type = RegionType.Continent,
                RecommendedLevel = 58,
                MinimumLevel = 58,
                RequiredDungeons = new List<string> { "forest_ruins" }
            };
            regions.Add(outland);

            // 添加地狱火半岛区域
            var hellfire = new Region
            {
                Id = "hellfire",
                Name = "地狱火半岛",
                Description = "地狱火半岛是外域的入口，满是恶魔和堕落的兽人。",
                Type = RegionType.Zone,
                Climate = RegionClimate.Desert,
                RecommendedLevel = 58,
                MinimumLevel = 58,
                ParentRegionId = "outland",
                MonsterNames = new List<string> { "Fel Orc", "Demon" }
            };
            regions.Add(hellfire);
            outland.SubRegionIds.Add(hellfire.Id);

            return regions;
        }
    }
}