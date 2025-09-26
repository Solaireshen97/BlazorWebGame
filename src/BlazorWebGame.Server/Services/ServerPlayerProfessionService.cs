using BlazorWebGame.GameConfig;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Enums;
using System.Linq;

namespace BlazorWebGame.Server.Services
{
    /// <summary>
    /// 服务端玩家专业技能管理服务
    /// </summary>
    public class ServerPlayerProfessionService
    {
        private readonly ILogger<ServerPlayerProfessionService> _logger;

        public ServerPlayerProfessionService(ILogger<ServerPlayerProfessionService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取总采集速度加成
        /// </summary>
        public double GetTotalGatheringSpeedBonus(CharacterDetailsDto character)
        {
            double equipmentBonus = (character.EquippedItems?.Values ?? Enumerable.Empty<string>())
                .Select(itemId => ItemData.GetItemById(itemId) as Equipment)
                .Where(eq => eq != null)
                .Sum(eq => eq!.GatheringSpeedBonus);

            // TODO: 添加buff采集速度加成，等待相关系统
            double buffBonus = 0.0;

            return equipmentBonus + buffBonus;
        }

        /// <summary>
        /// 获取总额外战利品概率
        /// </summary>
        public double GetTotalExtraLootChance(CharacterDetailsDto character)
        {
            double equipmentBonus = (character.EquippedItems?.Values ?? Enumerable.Empty<string>())
                .Select(itemId => ItemData.GetItemById(itemId) as Equipment)
                .Where(eq => eq != null)
                .Sum(eq => eq!.ExtraLootChanceBonus);

            // TODO: 添加buff额外战利品概率加成，等待相关系统
            double buffBonus = 0.0;

            return equipmentBonus + buffBonus;
        }

        /// <summary>
        /// 获取总制作速度加成
        /// </summary>
        public double GetTotalCraftingSpeedBonus(CharacterDetailsDto character)
        {
            // 未来可添加装备制作速度加成
            double equipmentBonus = 0.0;

            // TODO: 从Buff中获取加成，等待相关系统
            double buffBonus = 0.0;

            return equipmentBonus + buffBonus;
        }

        /// <summary>
        /// 添加战斗专业经验值
        /// </summary>
        public (bool LeveledUp, int OldLevel, int NewLevel) AddBattleXP(CharacterDetailsDto character, BattleProfession profession, int amount)
        {
            var professionName = profession.ToString();
            int oldXp = character.BattleProfessionXP?.GetValueOrDefault(professionName, 0) ?? 0;
            int oldLevel = ExpSystem.GetLevelFromExp(oldXp);

            // 初始化字典如果为null
            character.BattleProfessionXP ??= new Dictionary<string, int>();

            // 添加经验值
            if (character.BattleProfessionXP.ContainsKey(professionName))
            {
                character.BattleProfessionXP[professionName] += amount;
            }
            else
            {
                character.BattleProfessionXP[professionName] = amount;
            }

            // 检查是否升级
            int newXp = character.BattleProfessionXP[professionName];
            int newLevel = ExpSystem.GetLevelFromExp(newXp);
            bool leveledUp = newLevel > oldLevel;

            if (leveledUp)
            {
                _logger.LogInformation($"Character {character.Name} leveled up in {profession} from {oldLevel} to {newLevel}");
            }

            return (leveledUp, oldLevel, newLevel);
        }

        /// <summary>
        /// 添加采集专业经验值
        /// </summary>
        public (bool LeveledUp, int OldLevel, int NewLevel) AddGatheringXP(CharacterDetailsDto character, GatheringProfession profession, int amount)
        {
            var professionName = profession.ToString();
            int oldXp = character.GatheringProfessionXP?.GetValueOrDefault(professionName, 0) ?? 0;
            int oldLevel = ExpSystem.GetLevelFromExp(oldXp);

            // 初始化字典如果为null
            character.GatheringProfessionXP ??= new Dictionary<string, int>();

            // 添加经验值
            if (character.GatheringProfessionXP.ContainsKey(professionName))
            {
                character.GatheringProfessionXP[professionName] += amount;
            }
            else
            {
                character.GatheringProfessionXP[professionName] = amount;
            }

            // 检查是否升级
            int newXp = character.GatheringProfessionXP[professionName];
            int newLevel = ExpSystem.GetLevelFromExp(newXp);
            bool leveledUp = newLevel > oldLevel;

            if (leveledUp)
            {
                _logger.LogInformation($"Character {character.Name} leveled up in {profession} from {oldLevel} to {newLevel}");
            }

            return (leveledUp, oldLevel, newLevel);
        }

        /// <summary>
        /// 添加生产专业经验值
        /// </summary>
        public (bool LeveledUp, int OldLevel, int NewLevel) AddProductionXP(CharacterDetailsDto character, ProductionProfession profession, int amount)
        {
            var professionName = profession.ToString();
            int oldXp = character.ProductionProfessionXP?.GetValueOrDefault(professionName, 0) ?? 0;
            int oldLevel = ExpSystem.GetLevelFromExp(oldXp);

            // 初始化字典如果为null
            character.ProductionProfessionXP ??= new Dictionary<string, int>();

            // 添加经验值
            if (character.ProductionProfessionXP.ContainsKey(professionName))
            {
                character.ProductionProfessionXP[professionName] += amount;
            }
            else
            {
                character.ProductionProfessionXP[professionName] = amount;
            }

            // 检查是否升级
            int newXp = character.ProductionProfessionXP[professionName];
            int newLevel = ExpSystem.GetLevelFromExp(newXp);
            bool leveledUp = newLevel > oldLevel;

            if (leveledUp)
            {
                _logger.LogInformation($"Character {character.Name} leveled up in {profession} from {oldLevel} to {newLevel}");
            }

            return (leveledUp, oldLevel, newLevel);
        }

        /// <summary>
        /// 获取战斗专业等级
        /// </summary>
        public int GetLevel(CharacterDetailsDto character, BattleProfession profession)
        {
            var professionName = profession.ToString();
            int xp = character.BattleProfessionXP?.GetValueOrDefault(professionName, 0) ?? 0;
            return ExpSystem.GetLevelFromExp(xp);
        }

        /// <summary>
        /// 获取采集专业等级
        /// </summary>
        public int GetLevel(CharacterDetailsDto character, GatheringProfession profession)
        {
            var professionName = profession.ToString();
            int xp = character.GatheringProfessionXP?.GetValueOrDefault(professionName, 0) ?? 0;
            return ExpSystem.GetLevelFromExp(xp);
        }

        /// <summary>
        /// 获取生产专业等级
        /// </summary>
        public int GetLevel(CharacterDetailsDto character, ProductionProfession profession)
        {
            var professionName = profession.ToString();
            int xp = character.ProductionProfessionXP?.GetValueOrDefault(professionName, 0) ?? 0;
            return ExpSystem.GetLevelFromExp(xp);
        }

        /// <summary>
        /// 获取战斗专业等级进度
        /// </summary>
        public double GetLevelProgress(CharacterDetailsDto character, BattleProfession profession)
        {
            var professionName = profession.ToString();
            int xp = character.BattleProfessionXP?.GetValueOrDefault(professionName, 0) ?? 0;
            return ExpSystem.GetLevelProgressPercentage(xp);
        }

        /// <summary>
        /// 获取采集专业等级进度
        /// </summary>
        public double GetLevelProgress(CharacterDetailsDto character, GatheringProfession profession)
        {
            var professionName = profession.ToString();
            int xp = character.GatheringProfessionXP?.GetValueOrDefault(professionName, 0) ?? 0;
            return ExpSystem.GetLevelProgressPercentage(xp);
        }

        /// <summary>
        /// 获取生产专业等级进度
        /// </summary>
        public double GetLevelProgress(CharacterDetailsDto character, ProductionProfession profession)
        {
            var professionName = profession.ToString();
            int xp = character.ProductionProfessionXP?.GetValueOrDefault(professionName, 0) ?? 0;
            return ExpSystem.GetLevelProgressPercentage(xp);
        }
    }
}