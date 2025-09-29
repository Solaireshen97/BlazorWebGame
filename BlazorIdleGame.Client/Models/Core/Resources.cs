namespace BlazorIdleGame.Client.Models.Core
{
    public class Resources
    {
        public long Gold { get; set; }
        public long Wood { get; set; }
        public long Stone { get; set; }
        public long Iron { get; set; }
        public long Herbs { get; set; }
        public long Leather { get; set; }

        public Dictionary<string, long> ToDict()
        {
            return new Dictionary<string, long>
            {
                ["gold"] = Gold,
                ["wood"] = Wood,
                ["stone"] = Stone,
                ["iron"] = Iron,
                ["herbs"] = Herbs,
                ["leather"] = Leather
            };
        }
    }
}