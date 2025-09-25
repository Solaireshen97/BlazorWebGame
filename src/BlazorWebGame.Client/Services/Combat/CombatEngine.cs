using BlazorWebGame.Events;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Battles;
using BlazorWebGame.Models.Monsters;
using System;
using System.Linq;

namespace BlazorWebGame.Services.Combat
{
    /// <summary>
    /// 战斗引擎 - 负责战斗计算和伤害处理
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
        /// 处理玩家攻击
        /// </summary>
        public void ProcessPlayerAttack(BattleContext battle, Player player, double elapsedSeconds)
        {
            player.AttackCooldown -= elapsedSeconds;
            if (player.AttackCooldown <= 0)
            {
                // 选择目标
                var targetEnemy = SelectTargetForPlayer(battle, player);
                if (targetEnemy != null)
                {
                    // 记录玩家的目标
                    battle.PlayerTargets[player.Id] = targetEnemy.Name;

                    // 执行攻击
                    ExecutePlayerAttack(player, targetEnemy, battle);
                }

                // 重置冷却
                player.AttackCooldown += 1.0 / player.AttacksPerSecond;
            }
        }

        /// <summary>
        /// 处理敌人攻击
        /// </summary>
        public void ProcessEnemyAttack(BattleContext battle, Enemy enemy, double elapsedSeconds)
        {
            enemy.EnemyAttackCooldown -= elapsedSeconds;
            if (enemy.EnemyAttackCooldown <= 0)
            {
                // 选择目标
                var targetPlayer = SelectTargetForEnemy(battle, enemy);
                if (targetPlayer != null)
                {
                    // 执行攻击
                    ExecuteEnemyAttack(enemy, targetPlayer, battle);
                }

                // 重置冷却
                enemy.EnemyAttackCooldown += 1.0 / enemy.AttacksPerSecond;
            }
        }

        /// <summary>
        /// 执行玩家攻击
        /// </summary>
        private void ExecutePlayerAttack(Player character, Enemy enemy, BattleContext battle)
        {
            // 应用技能效果
            _skillSystem.ApplyCharacterSkills(character, enemy);

            // 计算伤害
            var damage = CalculatePlayerDamage(character, enemy);
            
            // 记录原始血量
            int originalHealth = enemy.Health;
            
            // 应用伤害
            ApplyDamageToEnemy(enemy, damage);
            
            // 计算实际造成的伤害
            int actualDamage = originalHealth - enemy.Health;

            // 触发伤害事件
            RaiseDamageEvent(character, enemy, actualDamage, battle);

            // 如果敌人死亡，处理死亡逻辑
            if (enemy.Health <= 0)
            {
                HandleEnemyDeath(character, enemy, battle);
            }
        }

        /// <summary>
        /// 执行敌人攻击
        /// </summary>
        private void ExecuteEnemyAttack(Enemy enemy, Player character, BattleContext battle)
        {
            // 应用技能效果
            _skillSystem.ApplyEnemySkills(enemy, character);

            // 计算伤害
            var damage = CalculateEnemyDamage(enemy, character);
            
            // 记录原始血量
            int originalHealth = character.Health;
            
            // 应用伤害
            ApplyDamageToPlayer(character, damage);
            
            // 计算实际造成的伤害
            int actualDamage = originalHealth - character.Health;

            // 触发伤害事件
            RaisePlayerDamagedEvent(enemy, character, actualDamage, battle);

            // 如果玩家死亡，处理死亡逻辑
            if (character.Health <= 0)
            {
                _characterCombatService.HandleCharacterDeath(character, battle);
            }
        }

        /// <summary>
        /// 未命中类型
        /// </summary>
        public enum HitResultType
        {
            Hit,    // 命中
            Miss,   // 未命中
            Dodge,  // 被闪避
            Block   // 被格挡（部分伤害）
        }

        /// <summary>
        /// 计算玩家对敌人的伤害
        /// </summary>
        private int CalculatePlayerDamage(Player character, Enemy enemy)
        {
            // 命中率检查
            if (!RollForHit(character, enemy, out var hitResult))
            {
                // 触发未命中事件
                RaiseMissEvent(character, enemy, hitResult, null);
                return 0; // 未命中，没有伤害
            }
            
            // 基础伤害
            int baseDamage = character.GetTotalAttackPower();
            
            // TODO: 这里可以添加更复杂的伤害计算
            // - 暴击率和暴击伤害
            // - 防御力减免
            // - 元素伤害和抗性
            // - 伤害加成和减免buff
            
            return baseDamage;
        }

        /// <summary>
        /// 检查攻击是否命中
        /// </summary>
        private bool RollForHit(Player attacker, Enemy defender, out HitResultType resultType)
        {
            // 计算基础命中率
            double baseHitChance = 0.7; // 70%的基础命中率
            
            // 获取怪物闪避基于其类型和等级
            int avoidanceRating = defender.AvoidanceRating;
            if (avoidanceRating == 0) // 如果没有设置，使用默认计算
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
            
            // 获取玩家的命中率
            int accuracyRating = attacker.GetTotalAccuracy();
            
            // 计算最终命中率
            double accuracyBonus = (accuracyRating - avoidanceRating) / 100.0;
            double finalHitChance = Math.Clamp(baseHitChance + accuracyBonus, 0.3, 0.99);
            
            // 检查直接闪避（优先于命中率）
            if (new Random().NextDouble() < defender.DodgeChance)
            {
                resultType = HitResultType.Dodge;
                return false;
            }
            
            // 检查命中
            bool hit = new Random().NextDouble() <= finalHitChance;
            resultType = hit ? HitResultType.Hit : HitResultType.Miss;
            return hit;
        }

        /// <summary>
        /// 触发未命中事件
        /// </summary>
        private void RaiseMissEvent(Player attacker, Enemy target, HitResultType resultType, BattleContext? battle)
        {
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            
            string missType = resultType == HitResultType.Dodge ? "闪避" : "未命中";
            
            gameStateService?.RaiseCombatEvent(
                GameEventType.AttackMissed,  // 需要在GameEventType中添加这个类型
                attacker,
                target,
                null,
                null,
                battle?.Party
            );
        }

        /// <summary>
        /// 计算敌人对玩家的伤害
        /// </summary>
        private int CalculateEnemyDamage(Enemy enemy, Player character)
        {
            // 命中率检查（可以添加类似玩家的命中检查）
            
            // 基础伤害
            int baseDamage = enemy.AttackPower;
            
            // 检查暴击
            bool isCritical = new Random().NextDouble() < enemy.CriticalChance;
            if (isCritical)
            {
                baseDamage = (int)(baseDamage * enemy.CriticalMultiplier);
            }
            
            // 考虑玩家防御力减免（后续可实现）
            
            return baseDamage;
        }

        /// <summary>
        /// 应用伤害到敌人
        /// </summary>
        private void ApplyDamageToEnemy(Enemy enemy, int damage)
        {
            enemy.Health = Math.Max(0, enemy.Health - damage);
        }

        /// <summary>
        /// 应用伤害到玩家
        /// </summary>
        private void ApplyDamageToPlayer(Player character, int damage)
        {
            character.Health = Math.Max(0, character.Health - damage);
        }

        /// <summary>
        /// 处理敌人死亡
        /// </summary>
        private void HandleEnemyDeath(Player killer, Enemy enemy, BattleContext battle)
        {
            // 触发敌人死亡事件
            RaiseEnemyKilledEvent(killer, enemy, battle);

            // 从战斗中移除敌人
            _battleManager.RemoveEnemyFromBattle(battle, enemy);

            // 分配战利品
            _lootService.DistributeEnemyLoot(killer, enemy, battle);
        }

        /// <summary>
        /// 为玩家选择目标
        /// </summary>
        public Enemy? SelectTargetForPlayer(BattleContext battle, Player player)
        {
            // 如果没有敌人，返回null
            if (!battle.Enemies.Any())
                return null;

            // 检查玩家是否已有目标
            if (battle.PlayerTargets.TryGetValue(player.Id, out var targetName))
            {
                var existingTarget = battle.Enemies.FirstOrDefault(e => e.Name == targetName);
                if (existingTarget != null)
                    return existingTarget;
            }

            // 根据策略选择新目标
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
        /// 为敌人选择目标
        /// </summary>
        public Player? SelectTargetForEnemy(BattleContext battle, Enemy enemy)
        {
            // 获取所有存活的玩家
            var alivePlayers = battle.Players.Where(p => !p.IsDead).ToList();
            if (!alivePlayers.Any())
                return null;

            // 根据策略选择目标
            switch (battle.EnemyTargetStrategy)
            {
                case TargetSelectionStrategy.HighestThreat:
                    // TODO: 实现威胁值系统
                    return alivePlayers.OrderByDescending(p => CalculateThreatLevel(p)).FirstOrDefault();

                case TargetSelectionStrategy.Random:
                default:
                    return alivePlayers[new Random().Next(alivePlayers.Count)];
            }
        }

        /// <summary>
        /// 计算玩家威胁值
        /// </summary>
        private int CalculateThreatLevel(Player player)
        {
            // 简单实现：基于攻击力
            // TODO: 实现更复杂的威胁值计算
            // - 造成的伤害总量
            // - 治疗量
            // - 特殊技能效果
            return player.GetTotalAttackPower();
        }

        /// <summary>
        /// 处理兼容旧系统的玩家攻击敌人
        /// </summary>
        public void PlayerAttackEnemy(Player character, Enemy enemy, Party? party)
        {
            // 查找或创建临时战斗上下文
            var battle = _battleManager.GetBattleContextForPlayer(character.Id);
            if (battle == null)
            {
                // 为兼容旧系统创建临时上下文
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
        /// 处理兼容旧系统的敌人攻击玩家
        /// </summary>
        public void EnemyAttackPlayer(Enemy enemy, Player character)
        {
            // 查找或创建临时战斗上下文
            var battle = _battleManager.GetBattleContextForPlayer(character.Id);
            if (battle == null)
            {
                // 为兼容旧系统创建临时上下文
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

        #region 事件触发方法

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