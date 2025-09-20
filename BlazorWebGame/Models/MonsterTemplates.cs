using System.Collections.Generic;

namespace BlazorWebGame.Models
{
    public static class MonsterTemplates
    {
        public static List<Enemy> All { get; } = new List<Enemy>
        {
            new Enemy("ʷ��ķ", 30, 3, 0.4, 1, 5, 10),
            new Enemy("�粼��", 50, 5, 0.5, 5, 15, 25),
            new Enemy("����", 80, 8, 0.8, 10, 25, 50)
        };
    }
}