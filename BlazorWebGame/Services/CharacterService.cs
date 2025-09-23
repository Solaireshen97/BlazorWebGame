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
        public void SetActiveCharacter(string characterId)
        {
            var character = AllCharacters.FirstOrDefault(c => c.Id == characterId);
            if (character != null && ActiveCharacter?.Id != characterId)
            {
                ActiveCharacter = character;
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// 初始化角色状态
        /// </summary>
        public void InitializePlayerState(Player character)
        {
            if (character == null) return;
            
            // 初始化战斗职业技能
            foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession)))
            {
                _combatService.CheckForNewSkillUnlocks(character, profession, character.GetLevel(profession), true);
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
            
            AllCharacters.Add(newCharacter);
            
            // 如果这是第一个角色，自动设为活跃角色
            if (ActiveCharacter == null)
            {
                ActiveCharacter = newCharacter;
            }
            
            InitializePlayerState(newCharacter);
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
    }
}