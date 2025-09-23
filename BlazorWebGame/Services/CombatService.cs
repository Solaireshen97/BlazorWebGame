using BlazorWebGame.Events;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Battles;
using BlazorWebGame.Models.Dungeons;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Models.Monsters;
using BlazorWebGame.Models.Skills;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Services
{
    /// <summary>
    /// ս��ϵͳ���񣬸�����������ս����ص��߼�
    /// </summary>
    public class CombatService
    {
        private readonly InventoryService _inventoryService;
        private List<Player> _allCharacters;
        private const double RevivalDuration = 2;
        private Dictionary<Guid, BattleContext> _activeBattles = new();
        /// <summary>
        /// ״̬����¼�
        /// </summary>
        public event Action? OnStateChanged;

        public CombatService(InventoryService inventoryService, List<Player> allCharacters)
        {
            _inventoryService = inventoryService;
            _allCharacters = allCharacters;
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

            foreach (var battle in _activeBattles.Values)
            {
                ProcessBattle(battle, elapsedSeconds);

                // ���ս���Ƿ����
                if (battle.State == BattleState.Completed)
                {
                    battlesToRemove.Add(battle.Id);
                }
            }

            // �Ƴ�����ɵ�ս��
            foreach (var id in battlesToRemove)
            {
                _activeBattles.Remove(id);
            }

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
            if (battle.State != BattleState.Active)
                return;

            // ������ҹ���
            foreach (var player in battle.Players.Where(p => !p.IsDead))
            {
                ProcessPlayerAttack(battle, player, elapsedSeconds);
            }

            // ������˹���
            foreach (var enemy in battle.Enemies.ToList()) // ʹ��ToList�Ա������ʱ���ϱ��޸�
            {
                ProcessEnemyAttack(battle, enemy, elapsedSeconds);
            }

            // ���ս��״̬
            CheckBattleStatus(battle);
        }

        /// <summary>
        /// ������ҹ���
        /// </summary>
        private void ProcessPlayerAttack(BattleContext battle, Player player, double elapsedSeconds)
        {
            player.AttackCooldown -= elapsedSeconds;
            if (player.AttackCooldown <= 0)
            {
                // ѡ��Ŀ��
                var targetEnemy = SelectTargetForPlayer(battle, player);
                if (targetEnemy != null)
                {
                    // ��¼��ҵ�Ŀ��
                    battle.PlayerTargets[player.Id] = targetEnemy.Name;

                    // ִ�й���
                    PlayerAttackEnemy(player, targetEnemy, battle.Party);
                }

                // ������ȴ
                player.AttackCooldown += 1.0 / player.AttacksPerSecond;
            }
        }

        /// <summary>
        /// ������˹���
        /// </summary>
        private void ProcessEnemyAttack(BattleContext battle, Enemy enemy, double elapsedSeconds)
        {
            enemy.EnemyAttackCooldown -= elapsedSeconds;
            if (enemy.EnemyAttackCooldown <= 0)
            {
                // ѡ��Ŀ��
                var targetPlayer = SelectTargetForEnemy(battle, enemy);
                if (targetPlayer != null)
                {
                    // ִ�й���
                    EnemyAttackPlayer(enemy, targetPlayer);
                }

                // ������ȴ
                enemy.EnemyAttackCooldown += 1.0 / enemy.AttacksPerSecond;
            }
        }

        /// <summary>
        /// Ϊ���ѡ��Ŀ��
        /// </summary>
        private Enemy? SelectTargetForPlayer(BattleContext battle, Player player)
        {
            // ���û�е��ˣ�����null
            if (!battle.Enemies.Any())
                return null;

            // �������Ƿ�����Ŀ��
            if (battle.PlayerTargets.TryGetValue(player.Id, out var targetName))
            {
                var existingTarget = battle.Enemies.FirstOrDefault(e => e.Name == targetName);
                if (existingTarget != null)
                    return existingTarget;
            }

            // ���ݲ���ѡ����Ŀ��
            switch (battle.PlayerTargetStrategy)
            {
                case TargetSelectionStrategy.LowestHealth:
                    return battle.Enemies.OrderBy(e => (double)e.Health / e.MaxHealth).FirstOrDefault();

                case TargetSelectionStrategy.HighestHealth:
                    return battle.Enemies.OrderByDescending(e => (double)e.Health / e.MaxHealth).FirstOrDefault();

                case TargetSelectionStrategy.Random:
                default:
                    return battle.Enemies[new Random().Next(battle.Enemies.Count)];
            }
        }

        /// <summary>
        /// Ϊ����ѡ��Ŀ��
        /// </summary>
        private Player? SelectTargetForEnemy(BattleContext battle, Enemy enemy)
        {
            // ��ȡ���д������
            var alivePlayers = battle.Players.Where(p => !p.IsDead).ToList();
            if (!alivePlayers.Any())
                return null;

            // ���ݲ���ѡ��Ŀ��
            switch (battle.EnemyTargetStrategy)
            {
                case TargetSelectionStrategy.HighestThreat:
                    // �����ʵ�֣������������������вֵ����
                    return alivePlayers.OrderByDescending(p => p.GetTotalAttackPower()).FirstOrDefault();

                case TargetSelectionStrategy.Random:
                default:
                    return alivePlayers[new Random().Next(alivePlayers.Count)];
            }
        }

        /// <summary>
        /// ���ս��״̬
        /// </summary>
        private void CheckBattleStatus(BattleContext battle)
        {
            if (battle.IsCompleted)
            {
                battle.State = BattleState.Completed;

                // �����һ�ʤ��������
                if (battle.IsVictory)
                {
                    HandleBattleVictory(battle);
                }
                else
                {
                    HandleBattleDefeat(battle);
                }
            }
        }

        /// <summary>
        /// ����ս��ʤ��
        /// </summary>
        private void HandleBattleVictory(BattleContext battle)
        {
            // ����ս��ʤ������
            if (battle.BattleType == BattleType.Dungeon && !string.IsNullOrEmpty(battle.DungeonId))
            {
                var dungeon = DungeonData.GetDungeonById(battle.DungeonId);
                if (dungeon != null)
                {
                    // ����Ƿ������һ��
                    if (battle.WaveNumber >= dungeon.Waves.Count)
                    {
                        // ������ɽ���
                        DistributeDungeonRewards(battle, dungeon);
                    }
                    else
                    {
                        // ������һ��
                        PrepareDungeonWave(battle, dungeon, battle.WaveNumber + 1);
                    }
                }
            }
            // ��ͨս��ʤ������
            else
            {
                // ����ս�����ͷ��佱��
                if (battle.BattleType == BattleType.Party && battle.Party != null)
                {
                    // �����Ŷӵ���
                    battle.Party.CurrentEnemy = battle.Enemies.FirstOrDefault()?.Clone();
                    if (battle.Party.CurrentEnemy != null)
                    {
                        InitializeEnemySkills(battle.Party.CurrentEnemy);
                    }
                }
                else
                {
                    // ����ս����Ϊÿ����������µ���
                    foreach (var player in battle.Players)
                    {
                        if (!player.IsDead && battle.Enemies.Any())
                        {
                            player.CurrentEnemy = battle.Enemies.First().Clone();
                            if (player.CurrentEnemy != null)
                            {
                                InitializeEnemySkills(player.CurrentEnemy);
                            }
                        }
                    }
                }
            }

            // ����ս������¼�
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.BattleCompleted);
        }

        /// <summary>
        /// ����ս��ʧ��
        /// </summary>
        private void HandleBattleDefeat(BattleContext battle)
        {
            // ����ս��ʧ���¼�
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.BattleDefeated);
        }

        /// <summary>
        /// ���丱������
        /// </summary>
        private void DistributeDungeonRewards(BattleContext battle, Dungeon dungeon)
        {
            var alivePlayers = battle.Players.Where(p => !p.IsDead).ToList();
            if (!alivePlayers.Any())
                return;

            var random = new Random();

            // ����ÿ������
            foreach (var reward in dungeon.Rewards)
            {
                // ���ݸ��ʾ����Ƿ����
                if (random.NextDouble() <= reward.DropChance)
                {
                    // ���ѡ��һ����һ����Ʒ����
                    if (!string.IsNullOrEmpty(reward.ItemId) && reward.ItemQuantity > 0)
                    {
                        var luckyPlayer = alivePlayers[random.Next(alivePlayers.Count)];
                        _inventoryService.AddItemToInventory(luckyPlayer, reward.ItemId, reward.ItemQuantity);
                    }

                    // ������һ�ý�Һ;���
                    foreach (var player in alivePlayers)
                    {
                        // ��ҽ���
                        if (reward.Gold > 0)
                        {
                            player.Gold += reward.Gold;
                        }

                        // ���齱��
                        if (reward.Experience > 0)
                        {
                            var profession = player.SelectedBattleProfession;
                            var oldLevel = player.GetLevel(profession);
                            player.AddBattleXP(profession, reward.Experience);

                            if (player.GetLevel(profession) > oldLevel)
                            {
                                CheckForNewSkillUnlocks(player, profession, player.GetLevel(profession));
                            }
                        }
                    }
                }
            }

            // ���¸�����ɼ�¼
            foreach (var player in alivePlayers)
            {
                //if (!player.CompletedDungeons.Contains(dungeon.Id))
                //{
                //    player.CompletedDungeons.Add(dungeon.Id);
                //}
            }
        }

        /// <summary>
        /// ׼������ս������
        /// </summary>
        private void PrepareDungeonWave(BattleContext battle, Dungeon dungeon, int waveNumber)
        {
            if (waveNumber <= 0 || waveNumber > dungeon.Waves.Count)
                return;

            var wave = dungeon.Waves[waveNumber - 1];
            battle.WaveNumber = waveNumber;
            battle.Enemies.Clear();

            // ���ɲ��ε���
            foreach (var spawnInfo in wave.Enemies)
            {
                var template = MonsterTemplates.All.FirstOrDefault(m => m.Name == spawnInfo.EnemyTemplateName);
                if (template != null)
                {
                    for (int i = 0; i < spawnInfo.Count; i++)
                    {
                        var enemy = template.Clone();

                        // Ӧ�õȼ������Ե���
                        if (spawnInfo.LevelAdjustment != 0)
                        {
                            enemy.Level += spawnInfo.LevelAdjustment;
                            enemy.AttackPower = AdjustStatByLevel(enemy.AttackPower, spawnInfo.LevelAdjustment);
                        }

                        // Ӧ��Ѫ������
                        if (spawnInfo.HealthMultiplier != 1.0)
                        {
                            enemy.MaxHealth = (int)(enemy.MaxHealth * spawnInfo.HealthMultiplier);
                            enemy.Health = enemy.MaxHealth;
                        }

                        // ��ʼ��������ȴ
                        InitializeEnemySkills(enemy);

                        // ��ӵ��˵�ս��
                        battle.Enemies.Add(enemy);
                    }
                }
            }

            // ����״̬Ϊ��Ծ
            battle.State = BattleState.Active;

            // �����²����¼�
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.DungeonWaveStarted, battle.Players.FirstOrDefault());
        }

        /// <summary>
        /// ���ݵȼ���������ֵ
        /// </summary>
        private int AdjustStatByLevel(int baseStat, int levelAdjustment)
        {
            // ��ʵ�֣�ÿ������10%
            return (int)(baseStat * (1 + 0.1 * levelAdjustment));
        }

        /// <summary>
        /// �����ɫ��ս��
        /// </summary>
        public void ProcessCombat(Player character, double elapsedSeconds, Party? party)
        {
            // ���ڻ���ʹ����ϵͳ��ս�������ּ���
            // �����Ľ�ɫ�������κ�ս������
            if (character.IsDead)
                return;

            var targetEnemy = party?.CurrentEnemy ?? character.CurrentEnemy;

            if (targetEnemy == null)
                return;

            // ��ҹ����߼�
            character.AttackCooldown -= elapsedSeconds;
            if (character.AttackCooldown <= 0)
            {
                PlayerAttackEnemy(character, targetEnemy, party);
                character.AttackCooldown += 1.0 / character.AttacksPerSecond;
            }

            // ���˹����߼�
            targetEnemy.EnemyAttackCooldown -= elapsedSeconds;
            if (targetEnemy.EnemyAttackCooldown <= 0)
            {
                Player? playerToAttack = null;
                if (party != null)
                {
                    // ����ֻ��ѡ����ŵĳ�Ա���й���
                    var aliveMembers = _allCharacters.Where(c => party.MemberIds.Contains(c.Id) && !c.IsDead).ToList();
                    if (aliveMembers.Any())
                    {
                        playerToAttack = aliveMembers[new Random().Next(aliveMembers.Count)];
                    }
                }
                else
                {
                    playerToAttack = character; // ����ģʽ
                }

                if (playerToAttack != null)
                {
                    EnemyAttackPlayer(targetEnemy, playerToAttack);
                }

                // ֻ�е�����ȷʵ�����ˣ�������������ȴ
                if (playerToAttack != null)
                {
                    targetEnemy.EnemyAttackCooldown += 1.0 / targetEnemy.AttacksPerSecond;
                }
            }
        }

        /// <summary>
        /// ��ҹ�������
        /// </summary>
        public void PlayerAttackEnemy(Player character, Enemy enemy, Party? party)
        {
            // Ӧ�ü��ܺ���ͨ����
            ApplyCharacterSkills(character, enemy);

            // ��¼ԭʼѪ�����ڼ����˺�
            int originalHealth = enemy.Health;
            enemy.Health -= character.GetTotalAttackPower();
            int damageDealt = originalHealth - enemy.Health;

            // �������������¼�
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseCombatEvent(
                GameEventType.EnemyDamaged,
                character,
                enemy,
                damageDealt,
                null,
                party
            );

            // �������Ѫ������0������ս��Ʒ����
            if (enemy.Health <= 0)
            {
                // �������������¼�
                gameStateService?.RaiseCombatEvent(
                    GameEventType.EnemyKilled,
                    character,
                    enemy,
                    null,
                    null,
                    party
                );

                // ����Ƿ�����ս��ϵͳ�еĵ���
                var battle = _activeBattles.Values.FirstOrDefault(b => b.Enemies.Contains(enemy));
                if (battle != null)
                {
                    // ��ս�����Ƴ�����
                    battle.Enemies.Remove(enemy);

                    // ������ҵ�Ŀ��
                    foreach (var playerId in battle.PlayerTargets.Keys.ToList())
                    {
                        if (battle.PlayerTargets[playerId] == enemy.Name)
                        {
                            battle.PlayerTargets.Remove(playerId);
                        }
                    }

                    // ս������������ProcessBattle�н���
                }
                else
                {
                    // ��ս��ϵͳ��ʹ��ԭ�����߼�
                    var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemy.Name) ?? enemy;

                    if (party != null)
                    {
                        // �Ŷӽ�������
                        HandlePartyLoot(party, enemy, originalTemplate);
                    }
                    else
                    {
                        // ���˽�������
                        HandleSoloLoot(character, enemy, originalTemplate);
                    }
                }
            }
        }

        /// <summary>
        /// �����Ŷӻ��ܵ��˺��ս��Ʒ����
        /// </summary>
        private void HandlePartyLoot(Party party, Enemy enemy, Enemy originalTemplate)
        {
            // ��ȡ�����Ա�б�
            var partyMembers = _allCharacters.Where(c => party.MemberIds.Contains(c.Id)).ToList();
            if (!partyMembers.Any())
            {
                party.CurrentEnemy = originalTemplate.Clone();
                return;
            }

            var memberCount = partyMembers.Count;
            var random = new Random();

            // ������
            var totalGold = enemy.GetGoldDropAmount();
            var goldPerMember = totalGold / memberCount;
            var remainderGold = totalGold % memberCount;

            foreach (var member in partyMembers)
            {
                member.Gold += goldPerMember;
            }

            if (remainderGold > 0)
            {
                var luckyMemberForGold = partyMembers[random.Next(memberCount)];
                luckyMemberForGold.Gold += remainderGold;
            }

            // ����ս��Ʒ
            foreach (var lootItem in enemy.LootTable)
            {
                if (random.NextDouble() <= lootItem.Value)
                {
                    var luckyMemberForLoot = partyMembers[random.Next(memberCount)];
                    _inventoryService.AddItemToInventory(luckyMemberForLoot, lootItem.Key, 1);
                }
            }

            // ���侭����������
            foreach (var member in partyMembers)
            {
                var profession = member.SelectedBattleProfession;
                var oldLevel = member.GetLevel(profession);
                member.AddBattleXP(profession, enemy.XpReward);
                
                if (member.GetLevel(profession) > oldLevel)
                {
                    CheckForNewSkillUnlocks(member, profession, member.GetLevel(profession));
                }

                UpdateQuestProgress(member, QuestType.KillMonster, enemy.Name, 1);
                UpdateQuestProgress(member, QuestType.KillMonster, "any", 1);
                member.DefeatedMonsterIds.Add(enemy.Name);
            }

            // Ϊ�Ŷ������µ���
            party.CurrentEnemy = originalTemplate.Clone();
            InitializeEnemySkills(party.CurrentEnemy);
        }

        /// <summary>
        /// �����˻��ܵ��˺��ս��Ʒ����
        /// </summary>
        private void HandleSoloLoot(Player character, Enemy enemy, Enemy originalTemplate)
        {
            // ��ҽ���
            character.Gold += enemy.GetGoldDropAmount();
            
            // ������Ʒ
            var random = new Random();
            foreach (var lootItem in enemy.LootTable)
            {
                if (random.NextDouble() <= lootItem.Value)
                {
                    _inventoryService.AddItemToInventory(character, lootItem.Key, 1);
                }
            }

            // ����ֵ���������
            var profession = character.SelectedBattleProfession;
            var oldLevel = character.GetLevel(profession);
            character.AddBattleXP(profession, enemy.XpReward);
            
            if (character.GetLevel(profession) > oldLevel)
            {
                CheckForNewSkillUnlocks(character, profession, character.GetLevel(profession));
            }

            UpdateQuestProgress(character, QuestType.KillMonster, enemy.Name, 1);
            UpdateQuestProgress(character, QuestType.KillMonster, "any", 1);
            character.DefeatedMonsterIds.Add(enemy.Name);

            // Ϊ��������µ���
            character.CurrentEnemy = originalTemplate.Clone();
            InitializeEnemySkills(character.CurrentEnemy);
        }

        /// <summary>
        /// ���˹������
        /// </summary>
        public void EnemyAttackPlayer(Enemy enemy, Player character)
        {
            ApplyEnemySkills(enemy, character);
            character.Health -= enemy.AttackPower;
            
            if (character.Health <= 0)
            {
                HandleCharacterDeath(character);
            }
        }

        /// <summary>
        /// �����ɫ����
        /// </summary>
        public void HandleCharacterDeath(Player character)
        {
            // �����ɫ�Ѿ����ˣ���û��Ҫ��ִ��һ�������߼���
            if (character.IsDead) return;

            character.IsDead = true;
            character.Health = 0;
            character.RevivalTimeRemaining = RevivalDuration;

            // ����ʱ�Ƴ��󲿷�buff��������ʳ��buff
            character.ActiveBuffs.RemoveAll(buff =>
            {
                var item = ItemData.GetItemById(buff.SourceItemId);
                return item is Consumable consumable && consumable.Category != ConsumableCategory.Food;
            });
            
            NotifyStateChanged();
        }

        /// <summary>
        /// ��ɫ����
        /// </summary>
        public void ReviveCharacter(Player character)
        {
            character.IsDead = false;
            character.Health = character.GetTotalMaxHealth();
            character.RevivalTimeRemaining = 0;
            NotifyStateChanged();
        }

        /// <summary>
        /// ��ʼս��
        /// </summary>
        public void StartCombat(Player character, Enemy enemyTemplate, Party? party)
        {
            if (character == null || enemyTemplate == null) return;

            if (party != null)
            {
                // �Ŷ�ս���߼�
                HandlePartyStartCombat(character, enemyTemplate, party);
            }
            else
            {
                // ����ս���߼�
                HandleSoloStartCombat(character, enemyTemplate);
            }

            NotifyStateChanged();
        }

        /// <summary>
        /// ��ʼ����ս��
        /// </summary>
        public bool StartDungeon(Party party, string dungeonId)
        {
            if (party == null || string.IsNullOrEmpty(dungeonId))
                return false;

            var dungeon = DungeonData.GetDungeonById(dungeonId);
            if (dungeon == null)
                return false;

            // ��֤��������
            var members = _allCharacters.Where(c => party.MemberIds.Contains(c.Id)).ToList();
            if (members.Count < dungeon.MinPlayers || members.Count > dungeon.MaxPlayers)
                return false;

            // ����ս��������
            var battle = new BattleContext
            {
                BattleType = BattleType.Dungeon,
                Party = party,
                DungeonId = dungeonId,
                State = BattleState.Preparing
            };

            // ��Ӳ�������
            foreach (var member in members)
            {
                if (!member.IsDead)
                {
                    battle.Players.Add(member);

                    // �������״̬
                    member.CurrentAction = PlayerActionState.Combat;
                    member.AttackCooldown = 0;

                    // ���������
                    member.CurrentGatheringNode = null;
                    member.CurrentRecipe = null;
                    member.GatheringCooldown = 0;
                    member.CraftingCooldown = 0;
                }
            }

            // ׼����һ��ս��
            PrepareDungeonWave(battle, dungeon, 1);

            // ��ӵ���Ծս���б�
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

            // ����ս��������
            var battle = new BattleContext
            {
                BattleType = party != null ? BattleType.Party : BattleType.Solo,
                Party = party,
                State = BattleState.Active
            };

            // ������
            if (party != null)
            {
                // �Ŷ�ս��
                var members = _allCharacters.Where(c => party.MemberIds.Contains(c.Id)).ToList();
                foreach (var member in members)
                {
                    if (!member.IsDead)
                    {
                        battle.Players.Add(member);
                        member.CurrentAction = PlayerActionState.Combat;
                        member.AttackCooldown = 0;
                    }
                }
            }
            else
            {
                // ����ս��
                battle.Players.Add(character);
                character.CurrentAction = PlayerActionState.Combat;
                character.AttackCooldown = 0;
            }

            // ��ӵ���
            foreach (var enemyTemplate in enemies)
            {
                var enemy = enemyTemplate.Clone();
                InitializeEnemySkills(enemy);
                battle.Enemies.Add(enemy);
            }

            // ��ӵ���Ծս��
            _activeBattles[battle.Id] = battle;

            NotifyStateChanged();
            return true;
        }

        /// <summary>
        /// �����Ŷӿ�ʼս��
        /// </summary>
        private void HandlePartyStartCombat(Player character, Enemy enemyTemplate, Party party)
        {
            // ֻ�жӳ����Է����Ŷ�ս��
            if (party.CaptainId != character.Id)
                return;

            // ����Ѿ��ڴ�ͬһ�����ˣ�����Ҫ���¿�ʼ
            if (party.CurrentEnemy?.Name == enemyTemplate.Name)
                return;

            // �������˸���
            var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemyTemplate.Name) ?? enemyTemplate;
            party.CurrentEnemy = originalTemplate.Clone();
            InitializeEnemySkills(party.CurrentEnemy);

            // �������ж����Ա��ս��״̬
            foreach (var memberId in party.MemberIds)
            {
                var member = _allCharacters.FirstOrDefault(c => c.Id == memberId);
                if (member != null && !member.IsDead)
                {
                    // �����Ա�������ɼ��������ȷ�ս�����ǿ��еĻ������״̬
                    if (member.CurrentAction != PlayerActionState.Idle && member.CurrentAction != PlayerActionState.Combat)
                    {
                        member.CurrentGatheringNode = null;
                        member.CurrentRecipe = null;
                        member.GatheringCooldown = 0;
                        member.CraftingCooldown = 0;
                    }

                    // ����ս��״̬
                    member.CurrentAction = PlayerActionState.Combat;
                    member.AttackCooldown = 0;
                }
            }
        }

        /// <summary>
        /// ������˿�ʼս��
        /// </summary>
        private void HandleSoloStartCombat(Player character, Enemy enemyTemplate)
        {
            // ����Ѿ��ڴ�ͬһ�����ˣ�����Ҫ���¿�ʼ
            if (character.CurrentAction == PlayerActionState.Combat && character.CurrentEnemy?.Name == enemyTemplate.Name)
                return;

            // ���õ�ǰ״̬
            character.CurrentGatheringNode = null;
            character.CurrentRecipe = null;
            character.GatheringCooldown = 0;
            character.CraftingCooldown = 0;
            
            // ����ս��״̬
            character.CurrentAction = PlayerActionState.Combat;
            character.CurrentEnemy = enemyTemplate.Clone();
            InitializeEnemySkills(character.CurrentEnemy);
        }

        /// <summary>
        /// ��ʼ�����˵ļ�����ȴ
        /// </summary>
        private void InitializeEnemySkills(Enemy enemy)
        {
            enemy.SkillCooldowns.Clear();
            foreach (var skillId in enemy.SkillIds)
            {
                var skill = SkillData.GetSkillById(skillId);
                if (skill != null)
                {
                    enemy.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
                }
            }
            enemy.EnemyAttackCooldown = 1.0 / enemy.AttacksPerSecond;
        }

        /// <summary>
        /// Ӧ�ý�ɫ����Ч��
        /// </summary>
        public void ApplyCharacterSkills(Player character, Enemy enemy)
        {
            var profession = character.SelectedBattleProfession;
            if (!character.EquippedSkills.ContainsKey(profession))
                return;

            var equippedSkillIds = character.EquippedSkills[profession];

            foreach (var skillId in equippedSkillIds)
            {
                var cooldown = character.SkillCooldowns.GetValueOrDefault(skillId, 0);

                if (cooldown == 0)
                {
                    var skill = SkillData.GetSkillById(skillId);
                    if (skill == null) continue;

                    // ����Ч������
                    switch (skill.EffectType)
                    {
                        case SkillEffectType.DirectDamage:
                            enemy.Health -= (int)skill.EffectValue;
                            break;
                        case SkillEffectType.Heal:
                            var healAmount = skill.EffectValue < 1.0
                                ? (int)(character.GetTotalMaxHealth() * skill.EffectValue)
                                : (int)skill.EffectValue;
                            character.Health = Math.Min(character.GetTotalMaxHealth(), character.Health + healAmount);
                            break;
                    }
                    
                    // ���ܴ����������ȴ
                    character.SkillCooldowns[skillId] = skill.CooldownRounds;
                }
                else if (cooldown > 0)
                {
                    // ������ȴʱ�����
                    character.SkillCooldowns[skillId] = cooldown - 1;
                }
            }
        }

        /// <summary>
        /// Ӧ�õ��˼���Ч��
        /// </summary>
        public void ApplyEnemySkills(Enemy enemy, Player character)
        {
            foreach (var skillId in enemy.SkillIds)
            {
                var cooldown = enemy.SkillCooldowns.GetValueOrDefault(skillId, 0);

                if (cooldown == 0)
                {
                    var skill = SkillData.GetSkillById(skillId);
                    if (skill == null) continue;

                    switch (skill.EffectType)
                    {
                        case SkillEffectType.DirectDamage:
                            character.Health -= (int)skill.EffectValue;
                            break;
                        case SkillEffectType.Heal:
                            var healAmount = skill.EffectValue < 1.0
                                ? (int)(enemy.MaxHealth * skill.EffectValue)
                                : (int)skill.EffectValue;
                            enemy.Health = Math.Min(enemy.MaxHealth, enemy.Health + healAmount);
                            break;
                    }
                    enemy.SkillCooldowns[skillId] = skill.CooldownRounds;
                }
                else if (cooldown > 0)
                {
                    enemy.SkillCooldowns[skillId] = cooldown - 1;
                }
            }
        }

        /// <summary>
        /// װ������
        /// </summary>
        public void EquipSkill(Player character, string skillId, int maxEquippedSkills)
        {
            if (character == null) return;
            
            var profession = character.SelectedBattleProfession;
            var equipped = character.EquippedSkills[profession];
            var skill = SkillData.GetSkillById(skillId);
            
            if (skill == null || skill.Type == SkillType.Fixed || equipped.Contains(skillId)) return;
            
            if (equipped.Count(id => SkillData.GetSkillById(id)?.Type != SkillType.Fixed) < maxEquippedSkills)
            {
                equipped.Add(skillId);
                character.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// ж�¼���
        /// </summary>
        public void UnequipSkill(Player character, string skillId)
        {
            if (character == null) return;
            
            var skill = SkillData.GetSkillById(skillId);
            if (skill == null || skill.Type == SkillType.Fixed) return;
            
            if (character.EquippedSkills[character.SelectedBattleProfession].Remove(skillId))
            {
                character.SkillCooldowns.Remove(skillId);
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// ����Ƿ����¼��ܽ���
        /// </summary>
        public void CheckForNewSkillUnlocks(Player character, BattleProfession profession, int level, bool checkAllLevels = false)
        {
            if (character == null) return;
            
            var skillsToLearnQuery = SkillData.AllSkills.Where(s => s.RequiredProfession == profession);
            
            if (checkAllLevels)
            {
                skillsToLearnQuery = skillsToLearnQuery.Where(s => s.RequiredLevel <= level);
            }
            else
            {
                skillsToLearnQuery = skillsToLearnQuery.Where(s => s.RequiredLevel == level);
            }
            
            var newlyLearnedSkills = skillsToLearnQuery.ToList();
            
            foreach (var skill in newlyLearnedSkills)
            {
                if (skill.Type == SkillType.Shared)
                {
                    character.LearnedSharedSkills.Add(skill.Id);
                }
                
                if (skill.Type == SkillType.Fixed)
                {
                    if (!character.EquippedSkills.TryGetValue(profession, out var equipped) || !equipped.Contains(skill.Id))
                    {
                        character.EquippedSkills[profession].Insert(0, skill.Id);
                    }
                }
            }
            
            if (newlyLearnedSkills.Any())
            {
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// ������Ҽ�����ȴ
        /// </summary>
        public void ResetPlayerSkillCooldowns(Player character)
        {
            if (character == null) return;
            
            character.SkillCooldowns.Clear();
            
            foreach (var skillId in character.EquippedSkills.Values.SelectMany(s => s))
            {
                var skill = SkillData.GetSkillById(skillId);
                if (skill != null)
                {
                    character.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
                }
            }
        }

        /// <summary>
        /// ����ս��ְҵ
        /// </summary>
        public void SetBattleProfession(Player character, BattleProfession profession)
        {
            if (character != null)
            {
                character.SelectedBattleProfession = profession;
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// �����������
        /// </summary>
        private void UpdateQuestProgress(Player character, QuestType type, string targetId, int amount)
        {
            if (character == null) return;
            
            // ֱ��ʹ��ServiceLocator��ȡQuestService
            var questService = ServiceLocator.GetService<QuestService>();
            if (questService != null)
            {
                questService.UpdateQuestProgress(character, type, targetId, amount);
            }
        }

        /// <summary>
        /// ����״̬����¼�
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();

        /// <summary>
        /// Ϊ��ɫ�����µĵ���ʵ��
        /// </summary>
        public void SpawnNewEnemyForCharacter(Player character, Enemy enemyTemplate)
        {
            if (character == null || enemyTemplate == null) return;
            
            // ���ҵ���ģ��
            var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemyTemplate.Name) ?? enemyTemplate;
            
            // ��¡����
            character.CurrentEnemy = originalTemplate.Clone();
            
            // ��ʼ�����˼�����ȴ
            InitializeEnemySkills(character.CurrentEnemy);
        }

        /// <summary>
        /// �������н�ɫ�б�
        /// </summary>
        public void SetAllCharacters(List<Player> characters)
        {
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));

            _allCharacters = characters;
        }

        /// <summary>
        /// �������ݾ�̬��
        /// </summary>
        public static class DungeonData
        {
            /// <summary>
            /// ���п��ø���
            /// </summary>
            public static List<Dungeon> AllDungeons { get; } = new();

            /// <summary>
            /// ͨ��ID��ȡ����
            /// </summary>
            public static Dungeon? GetDungeonById(string id)
            {
                return AllDungeons.FirstOrDefault(d => d.Id == id);
            }

            /// <summary>
            /// ��ʼ����������
            /// </summary>
            static DungeonData()
            {
                // ʾ����������
                AllDungeons.Add(new Dungeon
                {
                    Id = "forest_ruins",
                    Name = "ɭ���ż�",
                    Description = "һ���������ĹŴ��ż������ڱ�����Ұ�������ǿ��ռ�ݡ�",
                    RecommendedLevel = 5,
                    MinPlayers = 1,
                    MaxPlayers = 3,
                    Waves = new List<DungeonWave>
                {
                    new DungeonWave
                    {
                        WaveNumber = 1,
                        Description = "�������",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "ɭ����", Count = 3 },
                            new EnemySpawnInfo { EnemyTemplateName = "ǿ��", Count = 1 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 2,
                        Description = "�ڲ�Ѳ��",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "ǿ��", Count = 2 },
                            new EnemySpawnInfo { EnemyTemplateName = "ǿ��������", Count = 2 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 3,
                        Description = "����BOSS",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "ǿ��ͷĿ", Count = 1, IsElite = true, HealthMultiplier = 1.5 }
                        }
                    }
                },
                    Rewards = new List<DungeonReward>
                {
                    new DungeonReward { Gold = 500, Experience = 1000 },
                    new DungeonReward { ItemId = "rare_sword", ItemQuantity = 1, DropChance = 0.3 },
                    new DungeonReward { ItemId = "healing_potion", ItemQuantity = 5, DropChance = 0.8 }
                },
                    CooldownHours = 24
                });

                // ���Լ�����Ӹ��ั��...
            }
        }
    }
}