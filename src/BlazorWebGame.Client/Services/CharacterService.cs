using BlazorWebGame.Models;
using BlazorWebGame.Services.PlayerServices;
using BlazorWebGame.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;
namespace BlazorWebGame.Services
{
    /// <summary>
    /// 角色系统服务，负责管理角色的创建、存储、状态和生命周期
    /// </summary>
    public class CharacterService
    {
        private readonly GameStorage _gameStorage;
        private readonly CombatService _combatService;
        private readonly IPlayerAttributeService _playerAttributeService;
        private readonly IPlayerProfessionService _playerProfessionService;
        private readonly IPlayerUtilityService _playerUtilityService;

        /// <summary>
        /// 所有角色列表
        /// </summary>
        public List<Player> AllCharacters { get; private set; } = new();
        
        /// <summary>
        /// 当前激活角色
        /// </summary>
        public Player? ActiveCharacter { get; private set; }
        
        /// <summary>
        /// 状态改变事件
        /// </summary>
        public event Action? OnStateChanged;

        public CharacterService(
            GameStorage gameStorage, 
            CombatService combatService,
            IPlayerAttributeService playerAttributeService,
            IPlayerProfessionService playerProfessionService,
            IPlayerUtilityService playerUtilityService)
        {
            _gameStorage = gameStorage;
            _combatService = combatService;
            _playerAttributeService = playerAttributeService;
            _playerProfessionService = playerProfessionService;
            _playerUtilityService = playerUtilityService;
        }

        /// <summary>
        /// 初始化角色系统
        /// </summary>
        public async Task InitializeAsync()
        {
            // TODO: 从存储中加载角色
            // var loadedCharacters = await _gameStorage.LoadCharactersAsync();
            // if (loadedCharacters != null && loadedCharacters.Any())
            // {
            //     AllCharacters = loadedCharacters;
            // }
            // else
            // {
                // 创建默认角色
                AllCharacters.Add(new Player { Name = "测试者" });
                AllCharacters.Add(new Player { Name = "阿尔忒弥斯", Gold = 50 });
            // }
            
            // 设置激活角色
            ActiveCharacter = AllCharacters.FirstOrDefault();

            // 初始化每个角色的状态
            foreach (var character in AllCharacters)
            {
                _playerUtilityService.EnsureDataConsistency(character);
                InitializePlayerState(character);
            }
        }

        /// <summary>
        /// 设置激活角色
        /// </summary>
        public bool SetActiveCharacter(string characterId)
        {
            var character = AllCharacters.FirstOrDefault(c => c.Id == characterId);
            if (character != null && ActiveCharacter?.Id != characterId)
            {
                ActiveCharacter = character;
                NotifyStateChanged();
                return true;
            }
            else
            {
                return false; 
            }
        }

        /// <summary>
        /// 初始化角色状态
        /// </summary>
        public void InitializePlayerState(Player character)
        {
            if (character == null) return;
            
            // 初始化角色的属性
            _playerAttributeService.InitializePlayerAttributes(character);
            
            // 初始化战斗职业技能
            foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession)))
            {
                _combatService.CheckForNewSkillUnlocks(character, profession, _playerProfessionService.GetLevel(character, profession), true);
            }
            
            // 重置技能冷却
            _combatService.ResetPlayerSkillCooldowns(character);
        }

        /// <summary>
        /// 更新角色Buff状态
        /// </summary>
        public void UpdateBuffs(Player character, double elapsedSeconds)
        {
            if (character == null || !character.ActiveBuffs.Any()) return;
            
            bool buffsChanged = false;
            
            // 从后往前遍历以便安全地删除过期的buff
            for (int i = character.ActiveBuffs.Count - 1; i >= 0; i--)
            {
                var buff = character.ActiveBuffs[i];
                buff.TimeRemainingSeconds -= elapsedSeconds;
                
                // 如果buff已过期，移除它
                if (buff.TimeRemainingSeconds <= 0)
                {
                    character.ActiveBuffs.RemoveAt(i);
                    buffsChanged = true;
                }
            }
            
            // 如果buff发生了变化，重新计算最大生命值
            if (buffsChanged) 
            {
                character.Health = Math.Min(character.Health, _playerAttributeService.GetTotalMaxHealth(character));
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// 创建新角色
        /// </summary>
        public void CreateCharacter(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            
            var newCharacter = new Player { Name = name };
            _playerUtilityService.EnsureDataConsistency(newCharacter);
            
            // 初始化角色状态
            InitializePlayerState(newCharacter);
            
            AllCharacters.Add(newCharacter);
            
            // 如果这是第一个角色，自动设为激活角色
            if (ActiveCharacter == null)
            {
                ActiveCharacter = newCharacter;
            }
            
            NotifyStateChanged();
        }

        /// <summary>
        /// 保存角色状态
        /// </summary>
        public async Task SaveStateAsync(Player? character = null)
        {
            // 如果没有指定角色，保存激活角色
            character ??= ActiveCharacter;
            
            if (character != null)
            {
                await _gameStorage.SavePlayerAsync(character);
            }
        }

        /// <summary>
        /// 保存所有角色状态
        /// </summary>
        public async Task SaveAllCharactersAsync()
        {
            foreach (var character in AllCharacters)
            {
                await SaveStateAsync(character);
            }
        }

        /// <summary>
        /// 触发状态改变事件
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();

        /// <summary>
        /// 为角色添加战斗经验值，处理升级和相关事件
        /// </summary>
        public void AddBattleXP(Player player, BattleProfession profession, int amount)
        {
            if (player == null) return;
            
            var (leveledUp, oldLevel, newLevel) = _playerProfessionService.AddBattleXP(player, profession, amount);
            
            // 如果升级了需要处理相关逻辑
            if (leveledUp)
            {
                _playerAttributeService.UpdateBaseAttributes(player);
                
                // 检查新技能解锁
                _combatService.CheckForNewSkillUnlocks(player, profession, newLevel, false);
            }
            
            // 触发事件 - 未来可能需要GameStateService或其它组件
            // 以后可以在这里添加
            
            NotifyStateChanged();
        }

        /// <summary>
        /// 为角色添加采集经验值
        /// </summary>
        public void AddGatheringXP(Player player, GatheringProfession profession, int amount)
        {
            if (player == null) return;
            
            int oldLevel = _playerProfessionService.GetLevel(player, profession);
            
            // 添加经验值
            _playerProfessionService.AddGatheringXP(player, profession, amount);
            
            // 检查是否升级
            int newLevel = _playerProfessionService.GetLevel(player, profession);
            bool leveledUp = newLevel > oldLevel;
            
            // 处理升级相关逻辑
            if (leveledUp)
            {
                // 采集专业升级逻辑
            }
            
            NotifyStateChanged();
        }

        /// <summary>
        /// 为角色添加生产经验值
        /// </summary>
        public void AddProductionXP(Player player, ProductionProfession profession, int amount)
        {
            if (player == null) return;
            
            var (leveledUp, oldLevel, newLevel) = _playerProfessionService.AddProductionXP(player, profession, amount);
            
            // 处理升级相关逻辑
            if (leveledUp)
            {
                // 生产专业升级逻辑
            }
            
            NotifyStateChanged();
        }

        /// <summary>
        /// 获取角色属性信息（使用新的服务）
        /// </summary>
        public AttributeSet GetTotalAttributes(Player player) => _playerAttributeService.GetTotalAttributes(player);
        public int GetTotalAttackPower(Player player) => _playerAttributeService.GetTotalAttackPower(player);
        public int GetTotalMaxHealth(Player player) => _playerAttributeService.GetTotalMaxHealth(player);
        public double GetDamageMultiplier(Player player) => _playerAttributeService.GetDamageMultiplier(player);
        public int GetTotalAccuracy(Player player) => _playerAttributeService.GetTotalAccuracy(player);

        /// <summary>
        /// 获取角色专业信息（使用新的服务）
        /// </summary>
        public int GetLevel(Player player, BattleProfession profession) => _playerProfessionService.GetLevel(player, profession);
        public int GetLevel(Player player, GatheringProfession profession) => _playerProfessionService.GetLevel(player, profession);
        public int GetLevel(Player player, ProductionProfession profession) => _playerProfessionService.GetLevel(player, profession);
        public double GetLevelProgress(Player player, BattleProfession profession) => _playerProfessionService.GetLevelProgress(player, profession);
        public double GetLevelProgress(Player player, GatheringProfession profession) => _playerProfessionService.GetLevelProgress(player, profession);
        public double GetLevelProgress(Player player, ProductionProfession profession) => _playerProfessionService.GetLevelProgress(player, profession);
        public double GetTotalGatheringSpeedBonus(Player player) => _playerProfessionService.GetTotalGatheringSpeedBonus(player);
        public double GetTotalExtraLootChance(Player player) => _playerProfessionService.GetTotalExtraLootChance(player);
        public double GetTotalCraftingSpeedBonus(Player player) => _playerProfessionService.GetTotalCraftingSpeedBonus(player);

        /// <summary>
        /// 获取角色实用信息（使用新的服务）
        /// </summary>
        public bool HasItemInInventory(Player player, string itemId) => _playerUtilityService.HasItemInInventory(player, itemId);
        public ReputationTier GetReputationLevel(Player player, Faction faction) => _playerUtilityService.GetReputationLevel(player, faction);
        public double GetReputationProgressPercentage(Player player, Faction faction) => _playerUtilityService.GetReputationProgressPercentage(player, faction);
    }
}