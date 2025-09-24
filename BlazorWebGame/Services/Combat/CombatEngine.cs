using BlazorWebGame.Events;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Battles;
using BlazorWebGame.Models.Monsters;
using System;
using System.Linq;

namespace BlazorWebGame.Services.Combat
{
    /// <summary>
    /// ս������ - ����ս��������˺�����
    /// </summary>
    public class CombatEngine
    {
        private readonly SkillSystem _skillSystem;
        private readonly LootService _lootService;
        private readonly CharacterCombatService _characterCombatService;
        private readonly BattleManager _battleManager;

        public CombatEngine(
            SkillSystem skillSystem,
            LootService lootService,
            CharacterCombatService characterCombatService,
            BattleManager battleManager)
        {
            _skillSystem = skillSystem;
            _lootService = lootService;
            _characterCombatService = characterCombatService;
            _battleManager = battleManager;
        }

        /// <summary>
        /// ������ҹ���
        /// </summary>
        public void ProcessPlayerAttack(BattleContext battle, Player player, double elapsedSeconds)
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
                    ExecutePlayerAttack(player, targetEnemy, battle);
                }

                // ������ȴ
                player.AttackCooldown += 1.0 / player.AttacksPerSecond;
            }
        }

        /// <summary>
        /// ������˹���
        /// </summary>
        public void ProcessEnemyAttack(BattleContext battle, Enemy enemy, double elapsedSeconds)
        {
            enemy.EnemyAttackCooldown -= elapsedSeconds;
            if (enemy.EnemyAttackCooldown <= 0)
            {
                // ѡ��Ŀ��
                var targetPlayer = SelectTargetForEnemy(battle, enemy);
                if (targetPlayer != null)
                {
                    // ִ�й���
                    ExecuteEnemyAttack(enemy, targetPlayer, battle);
                }

                // ������ȴ
                enemy.EnemyAttackCooldown += 1.0 / enemy.AttacksPerSecond;
            }
        }

        /// <summary>
        /// ִ����ҹ���
        /// </summary>
        private void ExecutePlayerAttack(Player character, Enemy enemy, BattleContext battle)
        {
            // Ӧ�ü���Ч��
            _skillSystem.ApplyCharacterSkills(character, enemy);

            // �����˺�
            var damage = CalculatePlayerDamage(character, enemy);
            
            // ��¼ԭʼѪ��
            int originalHealth = enemy.Health;
            
            // Ӧ���˺�
            ApplyDamageToEnemy(enemy, damage);
            
            // ����ʵ����ɵ��˺�
            int actualDamage = originalHealth - enemy.Health;

            // �����˺��¼�
            RaiseDamageEvent(character, enemy, actualDamage, battle);

            // ����������������������߼�
            if (enemy.Health <= 0)
            {
                HandleEnemyDeath(character, enemy, battle);
            }
        }

        /// <summary>
        /// ִ�е��˹���
        /// </summary>
        private void ExecuteEnemyAttack(Enemy enemy, Player character, BattleContext battle)
        {
            // Ӧ�ü���Ч��
            _skillSystem.ApplyEnemySkills(enemy, character);

            // �����˺�
            var damage = CalculateEnemyDamage(enemy, character);
            
            // ��¼ԭʼѪ��
            int originalHealth = character.Health;
            
            // Ӧ���˺�
            ApplyDamageToPlayer(character, damage);
            
            // ����ʵ����ɵ��˺�
            int actualDamage = originalHealth - character.Health;

            // �����˺��¼�
            RaisePlayerDamagedEvent(enemy, character, actualDamage, battle);

            // ���������������������߼�
            if (character.Health <= 0)
            {
                _characterCombatService.HandleCharacterDeath(character, battle);
            }
        }

        /// <summary>
        /// δ��������
        /// </summary>
        public enum HitResultType
        {
            Hit,    // ����
            Miss,   // δ����
            Dodge,  // ������
            Block   // ���񵲣������˺���
        }

        /// <summary>
        /// ������ҶԵ��˵��˺�
        /// </summary>
        private int CalculatePlayerDamage(Player character, Enemy enemy)
        {
            // �����ʼ��
            if (!RollForHit(character, enemy, out var hitResult))
            {
                // ����δ�����¼�
                RaiseMissEvent(character, enemy, hitResult, null);
                return 0; // δ���У�û���˺�
            }
            
            // �����˺�
            int baseDamage = character.GetTotalAttackPower();
            
            // TODO: ���������Ӹ����ӵ��˺�����
            // - �����ʺͱ����˺�
            // - ����������
            // - Ԫ���˺��Ϳ���
            // - �˺��ӳɺͼ���buff
            
            return baseDamage;
        }

        /// <summary>
        /// ��鹥���Ƿ�����
        /// </summary>
        private bool RollForHit(Player attacker, Enemy defender, out HitResultType resultType)
        {
            // �������������
            double baseHitChance = 0.7; // 70%�Ļ���������
            
            // ��ȡ�������ܻ��������ͺ͵ȼ�
            int avoidanceRating = defender.AvoidanceRating;
            if (avoidanceRating == 0) // ���û�����ã�ʹ��Ĭ�ϼ���
            {
                switch (defender.Type)
                {
                    case MonsterType.Normal:
                        avoidanceRating = defender.Level * 5;
                        break;
                    case MonsterType.Elite:
                        avoidanceRating = defender.Level * 8;
                        break;
                    case MonsterType.Boss:
                        avoidanceRating = defender.Level * 10;
                        break;
                }
            }
            
            // ��ȡ��ҵ�������
            int accuracyRating = attacker.GetTotalAccuracy();
            
            // ��������������
            double accuracyBonus = (accuracyRating - avoidanceRating) / 100.0;
            double finalHitChance = Math.Clamp(baseHitChance + accuracyBonus, 0.3, 0.99);
            
            // ���ֱ�����ܣ������������ʣ�
            if (new Random().NextDouble() < defender.DodgeChance)
            {
                resultType = HitResultType.Dodge;
                return false;
            }
            
            // �������
            bool hit = new Random().NextDouble() <= finalHitChance;
            resultType = hit ? HitResultType.Hit : HitResultType.Miss;
            return hit;
        }

        /// <summary>
        /// ����δ�����¼�
        /// </summary>
        private void RaiseMissEvent(Player attacker, Enemy target, HitResultType resultType, BattleContext? battle)
        {
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            
            string missType = resultType == HitResultType.Dodge ? "����" : "δ����";
            
            gameStateService?.RaiseCombatEvent(
                GameEventType.AttackMissed,  // ��Ҫ��GameEventType������������
                attacker,
                target,
                null,
                null,
                battle?.Party
            );
        }

        /// <summary>
        /// ������˶���ҵ��˺�
        /// </summary>
        private int CalculateEnemyDamage(Enemy enemy, Player character)
        {
            // �����ʼ�飨�������������ҵ����м�飩
            
            // �����˺�
            int baseDamage = enemy.AttackPower;
            
            // ��鱩��
            bool isCritical = new Random().NextDouble() < enemy.CriticalChance;
            if (isCritical)
            {
                baseDamage = (int)(baseDamage * enemy.CriticalMultiplier);
            }
            
            // ������ҷ��������⣨������ʵ�֣�
            
            return baseDamage;
        }

        /// <summary>
        /// Ӧ���˺�������
        /// </summary>
        private void ApplyDamageToEnemy(Enemy enemy, int damage)
        {
            enemy.Health = Math.Max(0, enemy.Health - damage);
        }

        /// <summary>
        /// Ӧ���˺������
        /// </summary>
        private void ApplyDamageToPlayer(Player character, int damage)
        {
            character.Health = Math.Max(0, character.Health - damage);
        }

        /// <summary>
        /// �����������
        /// </summary>
        private void HandleEnemyDeath(Player killer, Enemy enemy, BattleContext battle)
        {
            // �������������¼�
            RaiseEnemyKilledEvent(killer, enemy, battle);

            // ��ս�����Ƴ�����
            _battleManager.RemoveEnemyFromBattle(battle, enemy);

            // ����ս��Ʒ
            _lootService.DistributeEnemyLoot(killer, enemy, battle);
        }

        /// <summary>
        /// Ϊ���ѡ��Ŀ��
        /// </summary>
        public Enemy? SelectTargetForPlayer(BattleContext battle, Player player)
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
        public Player? SelectTargetForEnemy(BattleContext battle, Enemy enemy)
        {
            // ��ȡ���д������
            var alivePlayers = battle.Players.Where(p => !p.IsDead).ToList();
            if (!alivePlayers.Any())
                return null;

            // ���ݲ���ѡ��Ŀ��
            switch (battle.EnemyTargetStrategy)
            {
                case TargetSelectionStrategy.HighestThreat:
                    // TODO: ʵ����вֵϵͳ
                    return alivePlayers.OrderByDescending(p => CalculateThreatLevel(p)).FirstOrDefault();

                case TargetSelectionStrategy.Random:
                default:
                    return alivePlayers[new Random().Next(alivePlayers.Count)];
            }
        }

        /// <summary>
        /// ���������вֵ
        /// </summary>
        private int CalculateThreatLevel(Player player)
        {
            // ��ʵ�֣����ڹ�����
            // TODO: ʵ�ָ����ӵ���вֵ����
            // - ��ɵ��˺�����
            // - ������
            // - ���⼼��Ч��
            return player.GetTotalAttackPower();
        }

        /// <summary>
        /// ������ݾ�ϵͳ����ҹ�������
        /// </summary>
        public void PlayerAttackEnemy(Player character, Enemy enemy, Party? party)
        {
            // ���һ򴴽���ʱս��������
            var battle = _battleManager.GetBattleContextForPlayer(character.Id);
            if (battle == null)
            {
                // Ϊ���ݾ�ϵͳ������ʱ������
                battle = new BattleContext
                {
                    BattleType = party != null ? BattleType.Party : BattleType.Solo,
                    Party = party,
                    State = BattleState.Active
                };
                battle.Players.Add(character);
                battle.Enemies.Add(enemy);
            }

            ExecutePlayerAttack(character, enemy, battle);
        }

        /// <summary>
        /// ������ݾ�ϵͳ�ĵ��˹������
        /// </summary>
        public void EnemyAttackPlayer(Enemy enemy, Player character)
        {
            // ���һ򴴽���ʱս��������
            var battle = _battleManager.GetBattleContextForPlayer(character.Id);
            if (battle == null)
            {
                // Ϊ���ݾ�ϵͳ������ʱ������
                battle = new BattleContext
                {
                    BattleType = BattleType.Solo,
                    State = BattleState.Active
                };
                battle.Players.Add(character);
                battle.Enemies.Add(enemy);
            }

            ExecuteEnemyAttack(enemy, character, battle);
        }

        #region �¼���������

        private void RaiseDamageEvent(Player attacker, Enemy target, int damage, BattleContext battle)
        {
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseCombatEvent(
                GameEventType.EnemyDamaged,
                attacker,
                target,
                damage,
                null,
                battle.Party
            );
        }

        private void RaisePlayerDamagedEvent(Enemy attacker, Player target, int damage, BattleContext battle)
        {
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseCombatEvent(
                GameEventType.PlayerDamaged,
                target,
                attacker,
                damage,
                null,
                battle.Party
            );
        }

        private void RaiseEnemyKilledEvent(Player killer, Enemy enemy, BattleContext battle)
        {
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseCombatEvent(
                GameEventType.EnemyKilled,
                killer,
                enemy,
                null,
                null,
                battle.Party
            );
        }

        #endregion
    }
}