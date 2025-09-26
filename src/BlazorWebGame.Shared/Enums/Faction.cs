namespace BlazorWebGame.Shared.Enums
{
    public enum Faction
    {
        StormwindGuard,    // 暴风城卫兵 (侧重战斗与基础材料)
        IronforgeBrotherhood, // 铁炉堡兄弟会 (侧重锻造、采矿、工程学)
        ArgentDawn           // 银色黎明 (侧重炼金、草药、对抗亡灵等)
    }

    public static class FactionData
    {
        public static string GetName(Faction faction)
        {
            return faction switch
            {
                Faction.StormwindGuard => "暴风城卫兵",
                Faction.IronforgeBrotherhood => "铁炉堡兄弟会",
                Faction.ArgentDawn => "银色黎明",
                _ => "未知势力"
            };
        }
    }
}