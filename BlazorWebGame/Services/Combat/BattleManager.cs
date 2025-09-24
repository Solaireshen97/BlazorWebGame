using BlazorWebGame.Events;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Battles;
using BlazorWebGame.Models.Dungeons;
using BlazorWebGame.Models.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Services.Combat
{
    /// <summary>
    /// ս�������� - ����ս��ʵ�����������ڹ���
    /// </summary>
    public class BattleManager
    {
        private readonly Dictionary<Guid, BattleContext> _activeBattles = new();
        private readonly List<Player> _allCharacters;
        private readonly CombatEngine _combatEngine;
        private readonly BattleFlowService _battleFlowService;
        private readonly CharacterCombatService _characterCombatService;
        private readonly SkillSystem _skillSystem;
        private readonly LootService _lootService;  // �������

        /// <summary>
        /// ״̬����¼�
        /// </summary>
        public event Action? OnStateChanged;

        public BattleManager(
            List<Player> allCharacters,
            CombatEngine combatEngine,
            BattleFlowService battleFlowService,
            CharacterCombatService characterCombatService,
            SkillSystem skillSystem,
            LootService lootService)  // ��Ӳ���
        {
            _allCharacters = allCharacters;
            _combatEngine = combatEngine;
            _battleFlowService = battleFlowService;
            _characterCombatService = characterCombatService;
            _skillSystem = skillSystem;
            _lootService = lootService;  // ��Ӹ�ֵ
        }

        /// <summary>
        /// ��ȡ��Ծս��������
        /// </summary>
        public BattleContext? GetBattleContextForPlayer(string playerId)
        {
            return _activeBattles.Values.FirstOrDefault(b => b.Players.Any(p => p.Id == playerId));
        }

        /// <summary>
        /// ��ȡ��Ծս��������
        /// </summary>
        public BattleContext? GetBattleContextForParty(Guid partyId)
        {
            return _activeBattles.Values.FirstOrDefault(b => b.Party?.Id == partyId);
        }

        /// <summary>
        /// �������л�Ծս��
        /// </summary>
        public void ProcessAllBattles(double elapsedSeconds)
        {
            var battlesToRemove = new List<Guid>();

            // �����Ծս��
            foreach (var battle in _activeBattles.Values)
            {
                ProcessBattle(battle, elapsedSeconds);

                // ���ս���Ƿ����
                if (battle.State == BattleState.Completed)
                {
                    // �ռ�������Ϣ����ս��ˢ��
                    var enemyInfos = _battleFlowService.CollectEnemyInfos(battle);

                    // �������в���ս�������״̬
                    foreach (var player in battle.Players)
                    {
                        player.CurrentAction = PlayerActionState.Idle;
                        player.CurrentEnemy = null;
                        player.AttackCooldown = 0;
                    }

                    // �������״̬
                    if (battle.Party != null)
                    {
                        battle.Party.CurrentEnemy = null;
                    }

                    // ֪ͨս�����̷�����ս������
                    _battleFlowService.OnBattleCompleted(battle, enemyInfos);

                    battlesToRemove.Add(battle.Id);
                }
            }

            // �Ƴ�����ɵ�ս��
            foreach (var id in battlesToRemove)
            {
                _activeBattles.Remove(id);
            }

            // ����ս��ˢ���߼�
            _battleFlowService.ProcessBattleRefresh(elapsedSeconds, this);

            if (battlesToRemove.Any())
            {
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// ������ս��
        /// </summary>
        private void ProcessBattle(BattleContext battle, double elapsedSeconds)
        {
            // ֻ�����Ծ״̬��ս��
            if (battle.State != BattleState.Active)
                return;

            // ������Ҹ����ʱ
            _characterCombatService.ProcessPlayerRevival(battle, elapsedSeconds);

            // ����Ƿ��д������
            var alivePlayers = battle.Players.Where(p => !p.IsDead).ToList();

            if (alivePlayers.Any())
            {
                // �д�����ʱ��������ҹ���
                foreach (var player in alivePlayers)
                {
                    _combatEngine.ProcessPlayerAttack(battle, player, elapsedSeconds);
                }

                // ������˹���
                foreach (var enemy in battle.Enemies.ToList())
                {
                    _combatEngine.ProcessEnemyAttack(battle, enemy, elapsedSeconds);
                }
            }
            else
            {
                // ��������������������е��˵Ĺ�����ȴ
                foreach (var enemy in battle.Enemies)
                {
                    enemy.EnemyAttackCooldown = 1.0 / enemy.AttacksPerSecond;
                }
            }

            // ���ս��״̬
            CheckBattleStatus(battle);
        }

        private void CheckBattleStatus(BattleContext battle)
        {
            // ���⴦�������Զ�����ĸ���
            if (battle.BattleType == BattleType.Dungeon && battle.AllowAutoRevive)
            {
                // ��ʹ�������������Ҳ����ս��
                if (!battle.Enemies.Any())
                {
                    // ����ȫ������������ LootService ����������һ��
                    _lootService.HandleBattleVictory(battle, _battleFlowService);
                }
                // ���������Ƿ�ȫ������
                return;
            }

            // ��ͨս����ɼ��
            if (battle.IsCompleted)
            {
                battle.State = BattleState.Completed;

                var gameStateService = ServiceLocator.GetService<GameStateService>();
                if (battle.IsVictory)
                {
                    // ����ʤ���������� LootService
                    _lootService.HandleBattleVictory(battle, _battleFlowService);

                    gameStateService?.RaiseEvent(GameEventType.BattleCompleted);
                }
                else
                {
                    // ʧ�ܴ���
                    _lootService.HandleBattleDefeat(battle);

                    gameStateService?.RaiseEvent(GameEventType.BattleDefeated);
                }
            }
        }
        /// <summary>
        /// ���ܿ�ʼս�������ݳ����Զ�ѡ����ʵ�ս��ģʽ
        /// </summary>
        public bool SmartStartBattle(Player character, Enemy enemyTemplate, Party? party = null, bool ignoreRefreshCheck = false)
        {
            if (character == null || enemyTemplate == null)
                return false;

            // ���ս��ˢ��״̬
            if (!ignoreRefreshCheck && _battleFlowService.IsPlayerInBattleRefresh(character.Id))
            {
                return false;
            }

            // ������ǰ��ɫ���Լ������Ա��������ս��
            var playersToCheck = new List<Player> { character };
            if (party != null)
            {
                playersToCheck.AddRange(_allCharacters.Where(c => party.MemberIds.Contains(c.Id)));
            }

            foreach (var player in playersToCheck.Distinct())
            {
                var existingBattle = GetBattleContextForPlayer(player.Id);
                if (existingBattle != null)
                {
                    // �����ͬһ��ս���е�ͬ���͵��ˣ���������
                    if (existingBattle.Players.Contains(character) &&
                        existingBattle.Enemies.Any(e => e.Name == enemyTemplate.Name))
                        return true;

                    // ���������ǰս��
                    foreach (var p in existingBattle.Players)
                    {
                        p.CurrentAction = PlayerActionState.Idle;
                        p.CurrentEnemy = null;
                        p.AttackCooldown = 0;
                    }
                    _activeBattles.Remove(existingBattle.Id);
                }
            }

            // �Ŷӳ�Աˢ��״̬���
            if (!ignoreRefreshCheck && party != null)
            {
                foreach (var memberId in party.MemberIds)
                {
                    if (_battleFlowService.IsPlayerInBattleRefresh(memberId))
                    {
                        return false;
                    }
                }
            }

            // ������ս��������ԭ���߼���
            var battle = new BattleContext
            {
                BattleType = party != null ? BattleType.Party : BattleType.Solo,
                Party = party,
                State = BattleState.Active,
                PlayerTargetStrategy = TargetSelectionStrategy.Random,
                EnemyTargetStrategy = TargetSelectionStrategy.Random,
                AllowAutoRevive = true
            };

            // ������
            if (party != null)
            {
                var members = _allCharacters.Where(c => party.MemberIds.Contains(c.Id) && !c.IsDead).ToList();
                foreach (var member in members)
                {
                    battle.Players.Add(member);
                    _characterCombatService.PrepareCharacterForBattle(member);
                }

                var enemyCount = _battleFlowService.DetermineEnemyCount(members.Count);
                for (int i = 0; i < enemyCount; i++)
                {
                    var enemy = enemyTemplate.Clone();
                    _skillSystem.InitializeEnemySkills(enemy);

                    // ���: ��ʼ������ս������
                    MonsterTemplates.InitializeCombatAttributes(enemy);

                    // ��ʼ�����˹�����ȴ
                    enemy.EnemyAttackCooldown = 1.0 / enemy.AttacksPerSecond;

                    battle.Enemies.Add(enemy);
                }
            }
            else
            {
                battle.Players.Add(character);
                _characterCombatService.PrepareCharacterForBattle(character);

                // �ڵ���ģʽ����
                var enemy = enemyTemplate.Clone();
                _skillSystem.InitializeEnemySkills(enemy);

                // ���: ��ʼ������ս������
                MonsterTemplates.InitializeCombatAttributes(enemy);

                // ��ʼ�����˹�����ȴ
                enemy.EnemyAttackCooldown = 1.0 / enemy.AttacksPerSecond;

                battle.Enemies.Add(enemy);
            }

            // �����ϵͳ��״̬
            character.CurrentEnemy = null;
            if (party != null)
            {
                party.CurrentEnemy = null;
            }

            _activeBattles[battle.Id] = battle;
            NotifyStateChanged();
            return true;
        }

        /// <summary>
        /// ��ʼ����ս��
        /// </summary>
        public bool StartDungeon(Party party, string dungeonId)
        {
            if (party == null || string.IsNullOrEmpty(dungeonId))
                return false;

            var dungeon = DungeonTemplates.GetDungeonById(dungeonId);
            if (dungeon == null)
                return false;

            // ��֤��������
            var members = _allCharacters.Where(c => party.MemberIds.Contains(c.Id)).ToList();
            if (members.Count < dungeon.MinPlayers || members.Count > dungeon.MaxPlayers)
                return false;

            // �������ж����Ա�ĵ�ǰս��
            foreach (var member in members)
            {
                var existingBattle = GetBattleContextForPlayer(member.Id);
                if (existingBattle != null)
                {
                    // ����ս��״̬
                    foreach (var player in existingBattle.Players)
                    {
                        player.CurrentAction = PlayerActionState.Idle;
                        player.CurrentEnemy = null;
                        player.AttackCooldown = 0;
                    }

                    // �ӻ�Ծս�����Ƴ�
                    _activeBattles.Remove(existingBattle.Id);
                }
            }

            // ����ս��������
            var battle = new BattleContext
            {
                BattleType = BattleType.Dungeon,
                Party = party,
                DungeonId = dungeonId,
                State = BattleState.Preparing,
                AllowAutoRevive = dungeon.AllowAutoRevive
            };

            // ��Ӳ�������
            foreach (var member in members.Where(m => !m.IsDead))
            {
                battle.Players.Add(member);
                _characterCombatService.PrepareCharacterForBattle(member);
            }

            // ׼����һ��ս��
            _battleFlowService.PrepareDungeonWave(battle, dungeon, 1, _skillSystem);

            _activeBattles[battle.Id] = battle;
            NotifyStateChanged();
            return true;
        }

        /// <summary>
        /// ��ʼ��Զ���ͨս��
        /// </summary>
        public bool StartMultiEnemyBattle(Player character, List<Enemy> enemies, Party? party = null)
        {
            if (character == null || enemies == null || !enemies.Any())
                return false;

            var battle = new BattleContext
            {
                BattleType = party != null ? BattleType.Party : BattleType.Solo,
                Party = party,
                State = BattleState.Active,
                PlayerTargetStrategy = TargetSelectionStrategy.Random,
                EnemyTargetStrategy = TargetSelectionStrategy.Random,
                AllowAutoRevive = true
            };

            // ������
            if (party != null)
            {
                var members = _allCharacters.Where(c => party.MemberIds.Contains(c.Id) && !c.IsDead).ToList();
                foreach (var member in members)
                {
                    battle.Players.Add(member);
                    _characterCombatService.PrepareCharacterForBattle(member);
                }
            }
            else
            {
                battle.Players.Add(character);
                _characterCombatService.PrepareCharacterForBattle(character);
            }

            foreach (var enemyTemplate in enemies)
            {
                var enemy = enemyTemplate.Clone();
                _skillSystem.InitializeEnemySkills(enemy);

                // ���: ��ʼ������ս������
                MonsterTemplates.InitializeCombatAttributes(enemy);

                // ��ʼ�����˹�����ȴ
                enemy.EnemyAttackCooldown = 1.0 / enemy.AttacksPerSecond;

                battle.Enemies.Add(enemy);
            }

            _activeBattles[battle.Id] = battle;
            NotifyStateChanged();
            return true;
        }

        /// <summary>
        /// ���ս������Ծ�б���BattleFlowServiceʹ�ã�
        /// </summary>
        internal void AddBattle(BattleContext battle)
        {
            _activeBattles[battle.Id] = battle;
            NotifyStateChanged();
        }

        /// <summary>
        /// ��ȡ���л�Ծս���������������ѯ��
        /// </summary>
        public IEnumerable<BattleContext> GetActiveBattles()
        {
            return _activeBattles.Values;
        }

        /// <summary>
        /// ��ս�����Ƴ����ˣ���CombatEngineʹ�ã�
        /// </summary>
        internal void RemoveEnemyFromBattle(BattleContext battle, Enemy enemy)
        {
            battle.Enemies.Remove(enemy);

            // ������ҵ�Ŀ��
            foreach (var playerId in battle.PlayerTargets.Keys.ToList())
            {
                if (battle.PlayerTargets[playerId] == enemy.Name)
                {
                    battle.PlayerTargets.Remove(playerId);
                }
            }
        }

        /// <summary>
        /// ֹͣս��
        /// </summary>
        public void StopBattle(BattleContext battle)
        {
            if (battle == null || !_activeBattles.ContainsKey(battle.Id))
                return;

            // ����ս��״̬Ϊ��ȡ��
            battle.State = BattleState.Cancelled;

            // �������в���ս�������״̬
            foreach (var player in battle.Players)
            {
                player.CurrentAction = PlayerActionState.Idle;
                player.CurrentEnemy = null;
                player.AttackCooldown = 0;
            }

            // �������״̬������У�
            if (battle.Party != null)
            {
                battle.Party.CurrentEnemy = null;
            }

            // �ӻ�Ծս���б����Ƴ�
            _activeBattles.Remove(battle.Id);

            // �Ƴ���ص�ˢ��״̬
            _battleFlowService.CancelBattleRefresh(battle.Id);

            // ����״̬����¼�
            NotifyStateChanged();

            // ����ս��ȡ���¼�
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.BattleCancelled, battle.Players.FirstOrDefault());
        }

        /// <summary>
        /// ֹͣ��ҵ�����ս��
        /// </summary>
        public void StopPlayerBattles(string playerId)
        {
            var battleToStop = GetBattleContextForPlayer(playerId);
            if (battleToStop != null)
            {
                StopBattle(battleToStop);
            }
        }

        /// <summary>
        /// ֹͣ���������ս��
        /// </summary>
        public void StopPartyBattles(Guid partyId)
        {
            var battleToStop = GetBattleContextForParty(partyId);
            if (battleToStop != null)
            {
                StopBattle(battleToStop);
            }
        }

        /// <summary>
        /// ����״̬����¼�
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}