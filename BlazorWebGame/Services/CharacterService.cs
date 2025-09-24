using BlazorWebGame.Models;
using BlazorWebGame.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorWebGame.Services
{
    /// <summary>
    /// 角色系统服务，负责管理角色的创建、加载、状态和属性
    /// </summary>
    public class CharacterService
    {
        private readonly GameStorage _gameStorage;
        private readonly CombatService _combatService;

        /// <summary>
        /// 所有角色列表
        /// </summary>
        public List<Player> AllCharacters { get; private set; } = new();
        
        /// <summary>
        /// 当前活跃角色
        /// </summary>
        public Player? ActiveCharacter { get; private set; }
        
        /// <summary>
        /// 状态变更事件
        /// </summary>
        public event Action? OnStateChanged;

        public CharacterService(GameStorage gameStorage, CombatService combatService)
        {
            _gameStorage = gameStorage;
            _combatService = combatService;
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
                AllCharacters.Add(new Player { Name = "索拉尔" });
                AllCharacters.Add(new Player { Name = "阿尔特留斯", Gold = 50 });
            // }
            
            // 设置活跃角色
            ActiveCharacter = AllCharacters.FirstOrDefault();

            // 初始化每个角色的状态
            foreach (var character in AllCharacters)
            {
                character.EnsureDataConsistency();
                InitializePlayerState(character);
            }
        }

        /// <summary>
        /// 设置活跃角色
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
            
            // 初始化角色基础属性
            InitializePlayerAttributes(character);
            
            // 初始化战斗职业技能
            foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession)))
            {
                _combatService.CheckForNewSkillUnlocks(character, profession, character.GetLevel(profession), true);
            }
            
            // 重置技能冷却
            _combatService.ResetPlayerSkillCooldowns(character);
        }

        // 更新初始化玩家属性的方法
        public void InitializePlayerAttributes(Player character)
        {
            if (character == null) return;

            // 确保基础属性已设置
            if (character.BaseAttributes == null)
            {
                character.BaseAttributes = new AttributeSet();
            }

            // 更新基础属性
            character.UpdateBaseAttributes();

            // 设置生命值为最大值
            character.MaxHealth = character.GetTotalMaxHealth();
            character.Health = character.MaxHealth;
        }

        /// <summary>
        /// 更新角色Buff状态
        /// </summary>
        public void UpdateBuffs(Player character, double elapsedSeconds)
        {
            if (character == null || !character.ActiveBuffs.Any()) return;
            
            bool buffsChanged = false;
            
            // 从后往前遍历，以便安全地删除过期的buff
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
            
            // 如果buff发生了变化，重新计算生命上限
            if (buffsChanged) 
            {
                character.Health = Math.Min(character.Health, character.GetTotalMaxHealth());
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
            newCharacter.EnsureDataConsistency();
            
            // 初始化角色状态
            InitializePlayerState(newCharacter);
            
            AllCharacters.Add(newCharacter);
            
            // 如果这是第一个角色，自动设为活跃角色
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
            // 如果没有指定角色，保存活跃角色
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
        /// 触发状态变更事件
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();

        /// <summary>
        /// 为角色添加战斗经验值，处理等级提升和事件
        /// </summary>
        public void AddBattleXP(Player player, BattleProfession profession, int amount)
        {
            if (player == null) return;
            
            int oldLevel = player.GetLevel(profession);
            long oldXp = player.BattleProfessionXP.GetValueOrDefault(profession, 0);
            
            // 增加经验值
            player.AddBattleXP(profession, amount);
            
            // 检查是否升级
            int newLevel = player.GetLevel(profession);
            bool leveledUp = newLevel > oldLevel;
            
            // 如果升级，需要更新属性
            if (leveledUp)
            {
                player.UpdateBaseAttributes();
                
                // 检查新技能解锁
                _combatService.CheckForNewSkillUnlocks(player, profession, newLevel, false);
            }
            
            // 触发事件 - 这需要GameStateService的事件管理器
            // 稍后解决这个问题
            
            NotifyStateChanged();
        }

        /// <summary>
        /// 为角色添加采集经验值
        /// </summary>
        public void AddGatheringXP(Player player, GatheringProfession profession, int amount)
        {
            if (player == null) return;
            
            int oldLevel = player.GetLevel(profession);
            
            // 增加经验值
            player.AddGatheringXP(profession, amount);
            
            // 检查是否升级
            int newLevel = player.GetLevel(profession);
            bool leveledUp = newLevel > oldLevel;
            
            // 如果升级处理额外逻辑
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
            
            int oldLevel = player.GetLevel(profession);
            
            // 增加经验值
            player.AddProductionXP(profession, amount);
            
            // 检查是否升级
            int newLevel = player.GetLevel(profession);
            bool leveledUp = newLevel > oldLevel;
            
            // 如果升级处理额外逻辑
            if (leveledUp)
            {
                // 生产专业升级逻辑
            }
            
            NotifyStateChanged();
        }
    }
}