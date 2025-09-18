namespace BlazorWebGame.Models
{
    /// <summary>
    /// 提供一个静态的怪物模板列表
    /// </summary>
    public static class MonsterTemplates
    {
        public static List<Enemy> All { get; } = new List<Enemy>
        {
            new Enemy("史莱姆", 30, 3, 0.4, 1, 5),
            new Enemy("哥布林", 50, 5, 0.5, 5, 15),
            new Enemy("野狼", 80, 8, 0.8, 10, 25)
            // 以后可以在这里添加更多更强的怪物
        };
    }
}