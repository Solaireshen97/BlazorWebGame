using BlazorWebGame.Models;
using BlazorWebGame.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorWebGame.Services
{
    /// <summary>
    /// ��ɫϵͳ���񣬸�������ɫ�Ĵ��������ء�״̬������
    /// </summary>
    public class CharacterService
    {
        private readonly GameStorage _gameStorage;
        private readonly CombatService _combatService;

        /// <summary>
        /// ���н�ɫ�б�
        /// </summary>
        public List<Player> AllCharacters { get; private set; } = new();
        
        /// <summary>
        /// ��ǰ��Ծ��ɫ
        /// </summary>
        public Player? ActiveCharacter { get; private set; }
        
        /// <summary>
        /// ״̬����¼�
        /// </summary>
        public event Action? OnStateChanged;

        public CharacterService(GameStorage gameStorage, CombatService combatService)
        {
            _gameStorage = gameStorage;
            _combatService = combatService;
        }

        /// <summary>
        /// ��ʼ����ɫϵͳ
        /// </summary>
        public async Task InitializeAsync()
        {
            // TODO: �Ӵ洢�м��ؽ�ɫ
            // var loadedCharacters = await _gameStorage.LoadCharactersAsync();
            // if (loadedCharacters != null && loadedCharacters.Any())
            // {
            //     AllCharacters = loadedCharacters;
            // }
            // else
            // {
                // ����Ĭ�Ͻ�ɫ
                AllCharacters.Add(new Player { Name = "������" });
                AllCharacters.Add(new Player { Name = "��������˹", Gold = 50 });
            // }
            
            // ���û�Ծ��ɫ
            ActiveCharacter = AllCharacters.FirstOrDefault();

            // ��ʼ��ÿ����ɫ��״̬
            foreach (var character in AllCharacters)
            {
                character.EnsureDataConsistency();
                InitializePlayerState(character);
            }
        }

        /// <summary>
        /// ���û�Ծ��ɫ
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
        /// ��ʼ����ɫ״̬
        /// </summary>
        public void InitializePlayerState(Player character)
        {
            if (character == null) return;
            
            // ��ʼ��ս��ְҵ����
            foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession)))
            {
                _combatService.CheckForNewSkillUnlocks(character, profession, character.GetLevel(profession), true);
            }
            
            // ���ü�����ȴ
            _combatService.ResetPlayerSkillCooldowns(character);
        }

        /// <summary>
        /// ���½�ɫBuff״̬
        /// </summary>
        public void UpdateBuffs(Player character, double elapsedSeconds)
        {
            if (character == null || !character.ActiveBuffs.Any()) return;
            
            bool buffsChanged = false;
            
            // �Ӻ���ǰ�������Ա㰲ȫ��ɾ�����ڵ�buff
            for (int i = character.ActiveBuffs.Count - 1; i >= 0; i--)
            {
                var buff = character.ActiveBuffs[i];
                buff.TimeRemainingSeconds -= elapsedSeconds;
                
                // ���buff�ѹ��ڣ��Ƴ���
                if (buff.TimeRemainingSeconds <= 0)
                {
                    character.ActiveBuffs.RemoveAt(i);
                    buffsChanged = true;
                }
            }
            
            // ���buff�����˱仯�����¼�����������
            if (buffsChanged) 
            {
                character.Health = Math.Min(character.Health, character.GetTotalMaxHealth());
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// �����½�ɫ
        /// </summary>
        public void CreateCharacter(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            
            var newCharacter = new Player { Name = name };
            newCharacter.EnsureDataConsistency();
            
            AllCharacters.Add(newCharacter);
            
            // ������ǵ�һ����ɫ���Զ���Ϊ��Ծ��ɫ
            if (ActiveCharacter == null)
            {
                ActiveCharacter = newCharacter;
            }
            
            InitializePlayerState(newCharacter);
            NotifyStateChanged();
        }

        /// <summary>
        /// �����ɫ״̬
        /// </summary>
        public async Task SaveStateAsync(Player? character = null)
        {
            // ���û��ָ����ɫ�������Ծ��ɫ
            character ??= ActiveCharacter;
            
            if (character != null)
            {
                await _gameStorage.SavePlayerAsync(character);
            }
        }

        /// <summary>
        /// �������н�ɫ״̬
        /// </summary>
        public async Task SaveAllCharactersAsync()
        {
            foreach (var character in AllCharacters)
            {
                await SaveStateAsync(character);
            }
        }

        /// <summary>
        /// ����״̬����¼�
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}