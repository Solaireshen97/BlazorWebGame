using BlazorWebGame.Events;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Battles;
using BlazorWebGame.Models.Dungeons;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Models.Monsters;
using BlazorWebGame.Models.Skills;
using BlazorWebGame.Services.Combat;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Services
{
    /// <summary>
    /// ս��ϵͳ�������棬Э������ս����ϵͳ
    /// </summary>
    public class CombatService
    {
        private readonly BattleManager _battleManager;
        private readonly BattleFlowService _battleFlowService;
        private readonly CombatEngine _combatEngine;
        private readonly SkillSystem _skillSystem;
        private readonly LootService _lootService;
        private readonly CharacterCombatService _characterCombatService;

        /// <summary>
        /// ״̬����¼�
        /// </summary>
        public event Action? OnStateChanged;

        public CombatService(
            InventoryService inventoryService,
            List<Player> allCharacters)
        {
            // ��ʼ�������ӷ���
            _skillSystem = new SkillSystem();
            _characterCombatService = new CharacterCombatService();
            _lootService = new LootService(inventoryService, _skillSystem, allCharacters);
            _battleFlowService = new BattleFlowService(allCharacters);

            // ��ʼ��ս����������ע����������
            _battleManager = new BattleManager(
                allCharacters,
                null!, // CombatEngine ���������ʼ��������
                _battleFlowService,
                _characterCombatService,
                _skillSystem
            );

            // ��ʼ��ս������
            _combatEngine = new CombatEngine(
                _skillSystem,
                _lootService,
                _characterCombatService,
                _battleManager
            );

            // ͨ���������� BattleManager �� CombatEngine������ѭ��������
            var field = typeof(BattleManager).GetField("_combatEngine",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_battleManager, _combatEngine);

            // �����ӷ����״̬����¼�
            _battleManager.OnStateChanged += () => OnStateChanged?.Invoke();
        }

        #region ս����ѯ�ӿ�

        /// <summary>
        /// ��ȡ��Ծս��������
        /// </summary>
        public BattleContext? GetBattleContextForPlayer(string playerId)
        {
            return _battleManager.GetBattleContextForPlayer(playerId);
        }

        /// <summary>
        /// ��ȡ��Ծս��������
        /// </summary>
        public BattleContext? GetBattleContextForParty(Guid partyId)
        {
            return _battleManager.GetBattleContextForParty(partyId);
        }

        /// <summary>
        /// �������Ƿ���ս��ˢ��״̬
        /// </summary>
        public bool IsPlayerInBattleRefresh(string playerId)
        {
            return _battleFlowService.IsPlayerInBattleRefresh(playerId);
        }

        /// <summary>
        /// ��ȡ���ս��ˢ��ʣ��ʱ��
        /// </summary>
        public double GetPlayerBattleRefreshTime(string playerId)
        {
            return _battleFlowService.GetPlayerBattleRefreshTime(playerId);
        }

        #endregion

        #region ս������

        /// <summary>
        /// �������л�Ծս��
        /// </summary>
        public void ProcessAllBattles(double elapsedSeconds)
        {
            _battleManager.ProcessAllBattles(elapsedSeconds);
        }

        /// <summary>
        /// �����ɫ��ս�����ѷ��� - ��ʹ���µ�ս��ϵͳ��
        /// </summary>
        [Obsolete("ʹ���µ�ս��ϵͳ���˷�����Ϊ�����Ա���")]
        public void ProcessCombat(Player character, double elapsedSeconds, Party? party)
        {
            // ����ִ���κ��߼�
            return;
        }

        #endregion

        #region ս������

        /// <summary>
        /// ���ܿ�ʼս��
        /// </summary>
        public bool SmartStartBattle(Player character, Enemy enemyTemplate, Party? party = null, bool ignoreRefreshCheck = false)
        {
            return _battleManager.SmartStartBattle(character, enemyTemplate, party, ignoreRefreshCheck);
        }

        /// <summary>
        /// ��ʼս�����ѷ��� - ��ʹ�� SmartStartBattle��
        /// </summary>
        [Obsolete("ʹ�� SmartStartBattle ����")]
        public void StartCombat(Player character, Enemy enemyTemplate, Party? party)
        {
            // ֱ�ӵ�����ϵͳ
            SmartStartBattle(character, enemyTemplate, party);
        }


        /// <summary>
        /// ��ʼ����ս��
        /// </summary>
        public bool StartDungeon(Party party, string dungeonId)
        {
            return _battleManager.StartDungeon(party, dungeonId);
        }

        /// <summary>
        /// ��ʼ��Զ���ͨս��
        /// </summary>
        public bool StartMultiEnemyBattle(Player character, List<Enemy> enemies, Party? party = null)
        {
            return _battleManager.StartMultiEnemyBattle(character, enemies, party);
        }

        #endregion

        #region ��ɫ״̬����

        /// <summary>
        /// ��ҹ�������
        /// </summary>
        public void PlayerAttackEnemy(Player character, Enemy enemy, Party? party)
        {
            _combatEngine.PlayerAttackEnemy(character, enemy, party);
        }

        /// <summary>
        /// ���˹������
        /// </summary>
        public void EnemyAttackPlayer(Enemy enemy, Player character)
        {
            _combatEngine.EnemyAttackPlayer(enemy, character);
        }

        /// <summary>
        /// �����ɫ����
        /// </summary>
        public void HandleCharacterDeath(Player character)
        {
            var battle = _battleManager.GetBattleContextForPlayer(character.Id);
            _characterCombatService.HandleCharacterDeath(character, battle);
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// ��ɫ����
        /// </summary>
        public void ReviveCharacter(Player character)
        {
            _characterCombatService.ReviveCharacter(character);
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Ϊ��ɫ�����µĵ���ʵ�����ѷ�����
        /// </summary>
        [Obsolete("��ս��ϵͳ���Զ������������")]
        public void SpawnNewEnemyForCharacter(Player character, Enemy enemyTemplate)
        {
            // ������Ҫ
            return;
        }

        #endregion

        #region ����ϵͳ

        /// <summary>
        /// Ӧ�ý�ɫ����Ч��
        /// </summary>
        public void ApplyCharacterSkills(Player character, Enemy enemy)
        {
            _skillSystem.ApplyCharacterSkills(character, enemy);
        }

        /// <summary>
        /// Ӧ�õ��˼���Ч��
        /// </summary>
        public void ApplyEnemySkills(Enemy enemy, Player character)
        {
            _skillSystem.ApplyEnemySkills(enemy, character);
        }

        /// <summary>
        /// װ������
        /// </summary>
        public void EquipSkill(Player character, string skillId, int maxEquippedSkills)
        {
            if (_skillSystem.EquipSkill(character, skillId, maxEquippedSkills))
            {
                OnStateChanged?.Invoke();
            }
        }

        /// <summary>
        /// ж�¼���
        /// </summary>
        public void UnequipSkill(Player character, string skillId)
        {
            if (_skillSystem.UnequipSkill(character, skillId))
            {
                OnStateChanged?.Invoke();
            }
        }

        /// <summary>
        /// ����Ƿ����¼��ܽ���
        /// </summary>
        public void CheckForNewSkillUnlocks(Player character, BattleProfession profession, int level, bool checkAllLevels = false)
        {
            _skillSystem.CheckForNewSkillUnlocks(character, profession, level, checkAllLevels);
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// ������Ҽ�����ȴ
        /// </summary>
        public void ResetPlayerSkillCooldowns(Player character)
        {
            _skillSystem.ResetPlayerSkillCooldowns(character);
        }

        /// <summary>
        /// ����ս��ְҵ
        /// </summary>
        public void SetBattleProfession(Player character, BattleProfession profession)
        {
            _characterCombatService.SetBattleProfession(character, profession);
            OnStateChanged?.Invoke();
        }

        #endregion

        #region ��������

        /// <summary>
        /// �������н�ɫ�б����ڸ����ڲ�״̬��
        /// </summary>
        public void SetAllCharacters(List<Player> characters)
        {
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));

            // ���������ӷ���Ľ�ɫ�б�
            var allCharactersField = typeof(BattleFlowService).GetField("_allCharacters",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            allCharactersField?.SetValue(_battleFlowService, characters);

            var lootAllCharactersField = typeof(LootService).GetField("_allCharacters",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            lootAllCharactersField?.SetValue(_lootService, characters);

            var battleAllCharactersField = typeof(BattleManager).GetField("_allCharacters",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            battleAllCharactersField?.SetValue(_battleManager, characters);
        }

        #endregion
    }
}