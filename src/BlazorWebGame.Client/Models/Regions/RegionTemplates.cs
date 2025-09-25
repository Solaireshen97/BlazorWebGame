using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models.Regions
{
    /// <summary>
    /// ��������ģ����
    /// </summary>
    public static class RegionTemplates
    {
        /// <summary>
        /// ���������б�
        /// </summary>
        public static List<Region> All { get; private set; } = InitializeRegions();

        /// <summary>
        /// ��ȡ�������򣨴�½��
        /// </summary>
        public static List<Region> GetContinents()
        {
            return All.Where(r => r.Type == RegionType.Continent).ToList();
        }

        /// <summary>
        /// ����ID��ȡ����
        /// </summary>
        public static Region? GetById(string id)
        {
            return All.FirstOrDefault(r => r.Id == id);
        }

        /// <summary>
        /// ��ȡָ�������������
        /// </summary>
        public static List<Region> GetSubRegions(string parentId)
        {
            return All.Where(r => r.ParentRegionId == parentId).ToList();
        }

        /// <summary>
        /// ��ʼ��������������
        /// </summary>
        private static List<Region> InitializeRegions()
        {
            List<Region> regions = new List<Region>();

            // ��Ӱ�����˹��½
            var azeroth = new Region
            {
                Id = "azeroth",
                Name = "������˹",
                Description = "������˹��һ������ħ����Σ�յĴ�½�����ڶ�����ļ�԰��",
                Type = RegionType.Continent,
                RecommendedLevel = 1,
                MinimumLevel = 1
            };
            regions.Add(azeroth);

            // ��Ӷ�����������
            var easternKingdoms = new Region
            {
                Id = "eastern-kingdoms",
                Name = "��������",
                Description = "���������ǰ�����˹��½����������֮һ������������������Ͱ�����ء�",
                Type = RegionType.Zone,
                RecommendedLevel = 1,
                MinimumLevel = 1,
                ParentRegionId = "azeroth"
            };
            regions.Add(easternKingdoms);
            azeroth.SubRegionIds.Add(easternKingdoms.Id);

            // ��Ӱ�����ɭ��������
            var elwynnForest = new Region
            {
                Id = "elwynn-forest",
                Name = "������ɭ��",
                Description = "������ɭ���Ǳ������Χ��һƬï��ɭ�֣��ʺϳ���ð���ߡ�",
                Type = RegionType.Subzone,
                Climate = RegionClimate.Forest,
                RecommendedLevel = 1,
                MinimumLevel = 1,
                ParentRegionId = "eastern-kingdoms",
                MonsterNames = new List<string> { "Ⱦ��������", "Ⱦ����ɭ����", "Murloc", "Goblin", "GoblinElite" }
            };
            regions.Add(elwynnForest);
            easternKingdoms.SubRegionIds.Add(elwynnForest.Id);

            // ���������Ұ������
            var westfall = new Region
            {
                Id = "westfall",
                Name = "������Ұ",
                Description = "������Ұ��������������֣����ڱ鲼���˺ͼ�����ũ��",
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

            // ��ӿ���ķ������
            var kalimdor = new Region
            {
                Id = "kalimdor",
                Name = "����ķ��",
                Description = "����ķ���ǰ�����˹�����Ĵ�½���ǰ�ҹ���顢ţͷ�˺����˵ļ�԰��",
                Type = RegionType.Zone,
                RecommendedLevel = 1,
                MinimumLevel = 1,
                ParentRegionId = "azeroth"
            };
            regions.Add(kalimdor);
            azeroth.SubRegionIds.Add(kalimdor.Id);

            // ��Ӷ�¡����������
            var durotar = new Region
            {
                Id = "durotar",
                Name = "��¡����",
                Description = "��¡���������˺;�ħ�ļ�԰�����ߵĺ�ɫ���غ��վ���Ͽ�ȡ�",
                Type = RegionType.Subzone,
                Climate = RegionClimate.Desert,
                RecommendedLevel = 1,
                MinimumLevel = 1,
                ParentRegionId = "kalimdor",
                MonsterNames = new List<string> { "Scorpid", "Boar", "Quilboar" }
            };
            regions.Add(durotar);
            kalimdor.SubRegionIds.Add(durotar.Id);

            // ��������½
            var outland = new Region
            {
                Id = "outland",
                Name = "����",
                Description = "�����ǵ���ŵ�к���һ����Ť�����������硣",
                Type = RegionType.Continent,
                RecommendedLevel = 58,
                MinimumLevel = 58,
                RequiredDungeons = new List<string> { "forest_ruins" }
            };
            regions.Add(outland);

            // ��ӵ�����뵺����
            var hellfire = new Region
            {
                Id = "hellfire",
                Name = "������뵺",
                Description = "������뵺���������ڣ����Ƕ�ħ�Ͷ�������ˡ�",
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