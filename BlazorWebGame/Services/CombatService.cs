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
    /// 战斗系统服务，负责处理所有与战斗相关的逻辑
    /// </summary>
    public class CombatService
    {
        private readonly InventoryService _inventoryService;
        private List<Player> _allCharacters;
        private const double RevivalDuration = 2;
        private Dictionary<Guid, BattleContext> _activeBattles = new();
        /// <summary>
        /// 状态变更事件
        /// </summary>
        public event Action? OnStateChanged;

        public CombatService(InventoryService inventoryService, List<Player> allCharacters)
        {
            _inventoryService = inventoryService;
            _allCharacters = allCharacters;
        }

        /// <summary>
        /// 获取活跃战斗上下文
        /// </summary>
        public BattleContext? GetBattleContextForPlayer(string playerId)
        {
            return _activeBattles.Values.FirstOrDefault(b => b.Players.Any(p => p.Id == playerId));
        }

        /// <summary>
        /// 获取活跃战斗上下文
        /// </summary>
        public BattleContext? GetBattleContextForParty(Guid partyId)
        {
            return _activeBattles.Values.FirstOrDefault(b => b.Party?.Id == partyId);
        }

        /// <summary>
        /// 处理所有活跃战斗
        /// </summary>
        public void ProcessAllBattles(double elapsedSeconds)
        {
            var battlesToRemove = new List<Guid>();

            foreach (var battle in _activeBattles.Values)
            {
                ProcessBattle(battle, elapsedSeconds);

                // 检查战斗是否完成
                if (battle.State == BattleState.Completed)
                {
                    battlesToRemove.Add(battle.Id);
                }
            }

            // 移除已完成的战斗
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
        /// 处理单个战斗
        /// </summary>
        private void ProcessBattle(BattleContext battle, double elapsedSeconds)
        {
            if (battle.State != BattleState.Active)
                return;

            // 处理玩家攻击
            foreach (var player in battle.Players.Where(p => !p.IsDead))
            {
                ProcessPlayerAttack(battle, player, elapsedSeconds);
            }

            // 处理敌人攻击
            foreach (var enemy in battle.Enemies.ToList()) // 使用ToList以避免迭代时集合被修改
            {
                ProcessEnemyAttack(battle, enemy, elapsedSeconds);
            }

            // 检查战斗状态
            CheckBattleStatus(battle);
        }

        /// <summary>
        /// 处理玩家攻击
        /// </summary>
        private void ProcessPlayerAttack(BattleContext battle, Player player, double elapsedSeconds)
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
                    PlayerAttackEnemy(player, targetEnemy, battle.Party);
                }

                // 重置冷却
                player.AttackCooldown += 1.0 / player.AttacksPerSecond;
            }
        }

        /// <summary>
        /// 处理敌人攻击
        /// </summary>
        private void ProcessEnemyAttack(BattleContext battle, Enemy enemy, double elapsedSeconds)
        {
            enemy.EnemyAttackCooldown -= elapsedSeconds;
            if (enemy.EnemyAttackCooldown <= 0)
            {
                // 选择目标
                var targetPlayer = SelectTargetForEnemy(battle, enemy);
                if (targetPlayer != null)
                {
                    // 执行攻击
                    EnemyAttackPlayer(enemy, targetPlayer);
                }

                // 重置冷却
                enemy.EnemyAttackCooldown += 1.0 / enemy.AttacksPerSecond;
            }
        }

        /// <summary>
        /// 为玩家选择目标
        /// </summary>
        private Enemy? SelectTargetForPlayer(BattleContext battle, Player player)
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
        private Player? SelectTargetForEnemy(BattleContext battle, Enemy enemy)
        {
            // 获取所有存活的玩家
            var alivePlayers = battle.Players.Where(p => !p.IsDead).ToList();
            if (!alivePlayers.Any())
                return null;

            // 根据策略选择目标
            switch (battle.EnemyTargetStrategy)
            {
                case TargetSelectionStrategy.HighestThreat:
                    // 这里简单实现，后续可以增加玩家威胁值计算
                    return alivePlayers.OrderByDescending(p => p.GetTotalAttackPower()).FirstOrDefault();

                case TargetSelectionStrategy.Random:
                default:
                    return alivePlayers[new Random().Next(alivePlayers.Count)];
            }
        }

        /// <summary>
        /// 检查战斗状态
        /// </summary>
        private void CheckBattleStatus(BattleContext battle)
        {
            if (battle.IsCompleted)
            {
                battle.State = BattleState.Completed;

                // 如果玩家获胜，处理奖励
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
        /// 处理战斗胜利
        /// </summary>
        private void HandleBattleVictory(BattleContext battle)
        {
            // 副本战斗胜利处理
            if (battle.BattleType == BattleType.Dungeon && !string.IsNullOrEmpty(battle.DungeonId))
            {
                var dungeon = DungeonData.GetDungeonById(battle.DungeonId);
                if (dungeon != null)
                {
                    // 检查是否是最后一波
                    if (battle.WaveNumber >= dungeon.Waves.Count)
                    {
                        // 副本完成奖励
                        DistributeDungeonRewards(battle, dungeon);
                    }
                    else
                    {
                        // 进入下一波
                        PrepareDungeonWave(battle, dungeon, battle.WaveNumber + 1);
                    }
                }
            }
            // 普通战斗胜利处理
            else
            {
                // 根据战斗类型分配奖励
                if (battle.BattleType == BattleType.Party && battle.Party != null)
                {
                    // 重置团队敌人
                    battle.Party.CurrentEnemy = battle.Enemies.FirstOrDefault()?.Clone();
                    if (battle.Party.CurrentEnemy != null)
                    {
                        InitializeEnemySkills(battle.Party.CurrentEnemy);
                    }
                }
                else
                {
                    // 单人战斗，为每个玩家生成新敌人
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

            // 触发战斗完成事件
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.BattleCompleted);
        }

        /// <summary>
        /// 处理战斗失败
        /// </summary>
        private void HandleBattleDefeat(BattleContext battle)
        {
            // 触发战斗失败事件
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.BattleDefeated);
        }

        /// <summary>
        /// 分配副本奖励
        /// </summary>
        private void DistributeDungeonRewards(BattleContext battle, Dungeon dungeon)
        {
            var alivePlayers = battle.Players.Where(p => !p.IsDead).ToList();
            if (!alivePlayers.Any())
                return;

            var random = new Random();

            // 分配每个奖励
            foreach (var reward in dungeon.Rewards)
            {
                // 根据概率决定是否掉落
                if (random.NextDouble() <= reward.DropChance)
                {
                    // 随机选择一名玩家获得物品奖励
                    if (!string.IsNullOrEmpty(reward.ItemId) && reward.ItemQuantity > 0)
                    {
                        var luckyPlayer = alivePlayers[random.Next(alivePlayers.Count)];
                        _inventoryService.AddItemToInventory(luckyPlayer, reward.ItemId, reward.ItemQuantity);
                    }

                    // 所有玩家获得金币和经验
                    foreach (var player in alivePlayers)
                    {
                        // 金币奖励
                        if (reward.Gold > 0)
                        {
                            player.Gold += reward.Gold;
                        }

                        // 经验奖励
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

            // 更新副本完成记录
            foreach (var player in alivePlayers)
            {
                //if (!player.CompletedDungeons.Contains(dungeon.Id))
                //{
                //    player.CompletedDungeons.Add(dungeon.Id);
                //}
            }
        }

        /// <summary>
        /// 准备副本战斗波次
        /// </summary>
        private void PrepareDungeonWave(BattleContext battle, Dungeon dungeon, int waveNumber)
        {
            if (waveNumber <= 0 || waveNumber > dungeon.Waves.Count)
                return;

            var wave = dungeon.Waves[waveNumber - 1];
            battle.WaveNumber = waveNumber;
            battle.Enemies.Clear();

            // 生成波次敌人
            foreach (var spawnInfo in wave.Enemies)
            {
                var template = MonsterTemplates.All.FirstOrDefault(m => m.Name == spawnInfo.EnemyTemplateName);
                if (template != null)
                {
                    for (int i = 0; i < spawnInfo.Count; i++)
                    {
                        var enemy = template.Clone();

                        // 应用等级和属性调整
                        if (spawnInfo.LevelAdjustment != 0)
                        {
                            enemy.Level += spawnInfo.LevelAdjustment;
                            enemy.AttackPower = AdjustStatByLevel(enemy.AttackPower, spawnInfo.LevelAdjustment);
                        }

                        // 应用血量倍率
                        if (spawnInfo.HealthMultiplier != 1.0)
                        {
                            enemy.MaxHealth = (int)(enemy.MaxHealth * spawnInfo.HealthMultiplier);
                            enemy.Health = enemy.MaxHealth;
                        }

                        // 初始化技能冷却
                        InitializeEnemySkills(enemy);

                        // 添加敌人到战斗
                        battle.Enemies.Add(enemy);
                    }
                }
            }

            // 设置状态为活跃
            battle.State = BattleState.Active;

            // 触发新波次事件
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.DungeonWaveStarted, battle.Players.FirstOrDefault());
        }

        /// <summary>
        /// 根据等级调整属性值
        /// </summary>
        private int AdjustStatByLevel(int baseStat, int levelAdjustment)
        {
            // 简单实现：每级提升10%
            return (int)(baseStat * (1 + 0.1 * levelAdjustment));
        }

        /// <summary>
        /// 处理角色的战斗
        /// </summary>
        public void ProcessCombat(Player character, double elapsedSeconds, Party? party)
        {
            // 对于还在使用老系统的战斗，保持兼容
            // 死亡的角色不参与任何战斗计算
            if (character.IsDead)
                return;

            var targetEnemy = party?.CurrentEnemy ?? character.CurrentEnemy;

            if (targetEnemy == null)
                return;

            // 玩家攻击逻辑
            character.AttackCooldown -= elapsedSeconds;
            if (character.AttackCooldown <= 0)
            {
                PlayerAttackEnemy(character, targetEnemy, party);
                character.AttackCooldown += 1.0 / character.AttacksPerSecond;
            }

            // 敌人攻击逻辑
            targetEnemy.EnemyAttackCooldown -= elapsedSeconds;
            if (targetEnemy.EnemyAttackCooldown <= 0)
            {
                Player? playerToAttack = null;
                if (party != null)
                {
                    // 敌人只会选择活着的成员进行攻击
                    var aliveMembers = _allCharacters.Where(c => party.MemberIds.Contains(c.Id) && !c.IsDead).ToList();
                    if (aliveMembers.Any())
                    {
                        playerToAttack = aliveMembers[new Random().Next(aliveMembers.Count)];
                    }
                }
                else
                {
                    playerToAttack = character; // 单人模式
                }

                if (playerToAttack != null)
                {
                    EnemyAttackPlayer(targetEnemy, playerToAttack);
                }

                // 只有当敌人确实攻击了，才重置它的冷却
                if (playerToAttack != null)
                {
                    targetEnemy.EnemyAttackCooldown += 1.0 / targetEnemy.AttacksPerSecond;
                }
            }
        }

        /// <summary>
        /// 玩家攻击敌人
        /// </summary>
        public void PlayerAttackEnemy(Player character, Enemy enemy, Party? party)
        {
            // 应用技能和普通攻击
            ApplyCharacterSkills(character, enemy);

            // 记录原始血量用于计算伤害
            int originalHealth = enemy.Health;
            enemy.Health -= character.GetTotalAttackPower();
            int damageDealt = originalHealth - enemy.Health;

            // 触发敌人受伤事件
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseCombatEvent(
                GameEventType.EnemyDamaged,
                character,
                enemy,
                damageDealt,
                null,
                party
            );

            // 如果敌人血量降至0，处理战利品分配
            if (enemy.Health <= 0)
            {
                // 触发敌人死亡事件
                gameStateService?.RaiseCombatEvent(
                    GameEventType.EnemyKilled,
                    character,
                    enemy,
                    null,
                    null,
                    party
                );

                // 检查是否是新战斗系统中的敌人
                var battle = _activeBattles.Values.FirstOrDefault(b => b.Enemies.Contains(enemy));
                if (battle != null)
                {
                    // 从战斗中移除敌人
                    battle.Enemies.Remove(enemy);

                    // 更新玩家的目标
                    foreach (var playerId in battle.PlayerTargets.Keys.ToList())
                    {
                        if (battle.PlayerTargets[playerId] == enemy.Name)
                        {
                            battle.PlayerTargets.Remove(playerId);
                        }
                    }

                    // 战斗结束检查会在ProcessBattle中进行
                }
                else
                {
                    // 旧战斗系统，使用原来的逻辑
                    var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemy.Name) ?? enemy;

                    if (party != null)
                    {
                        // 团队奖励分配
                        HandlePartyLoot(party, enemy, originalTemplate);
                    }
                    else
                    {
                        // 个人奖励分配
                        HandleSoloLoot(character, enemy, originalTemplate);
                    }
                }
            }
        }

        /// <summary>
        /// 处理团队击败敌人后的战利品分配
        /// </summary>
        private void HandlePartyLoot(Party party, Enemy enemy, Enemy originalTemplate)
        {
            // 获取队伍成员列表
            var partyMembers = _allCharacters.Where(c => party.MemberIds.Contains(c.Id)).ToList();
            if (!partyMembers.Any())
            {
                party.CurrentEnemy = originalTemplate.Clone();
                return;
            }

            var memberCount = partyMembers.Count;
            var random = new Random();

            // 分配金币
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

            // 分配战利品
            foreach (var lootItem in enemy.LootTable)
            {
                if (random.NextDouble() <= lootItem.Value)
                {
                    var luckyMemberForLoot = partyMembers[random.Next(memberCount)];
                    _inventoryService.AddItemToInventory(luckyMemberForLoot, lootItem.Key, 1);
                }
            }

            // 分配经验和任务进度
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

            // 为团队生成新敌人
            party.CurrentEnemy = originalTemplate.Clone();
            InitializeEnemySkills(party.CurrentEnemy);
        }

        /// <summary>
        /// 处理单人击败敌人后的战利品分配
        /// </summary>
        private void HandleSoloLoot(Player character, Enemy enemy, Enemy originalTemplate)
        {
            // 金币奖励
            character.Gold += enemy.GetGoldDropAmount();
            
            // 掉落物品
            var random = new Random();
            foreach (var lootItem in enemy.LootTable)
            {
                if (random.NextDouble() <= lootItem.Value)
                {
                    _inventoryService.AddItemToInventory(character, lootItem.Key, 1);
                }
            }

            // 经验值和任务进度
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

            // 为玩家生成新敌人
            character.CurrentEnemy = originalTemplate.Clone();
            InitializeEnemySkills(character.CurrentEnemy);
        }

        /// <summary>
        /// 敌人攻击玩家
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
        /// 处理角色死亡
        /// </summary>
        public void HandleCharacterDeath(Player character)
        {
            // 如果角色已经死了，就没必要再执行一次死亡逻辑了
            if (character.IsDead) return;

            character.IsDead = true;
            character.Health = 0;
            character.RevivalTimeRemaining = RevivalDuration;

            // 死亡时移除大部分buff，但保留食物buff
            character.ActiveBuffs.RemoveAll(buff =>
            {
                var item = ItemData.GetItemById(buff.SourceItemId);
                return item is Consumable consumable && consumable.Category != ConsumableCategory.Food;
            });
            
            NotifyStateChanged();
        }

        /// <summary>
        /// 角色复活
        /// </summary>
        public void ReviveCharacter(Player character)
        {
            character.IsDead = false;
            character.Health = character.GetTotalMaxHealth();
            character.RevivalTimeRemaining = 0;
            NotifyStateChanged();
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        public void StartCombat(Player character, Enemy enemyTemplate, Party? party)
        {
            if (character == null || enemyTemplate == null) return;

            if (party != null)
            {
                // 团队战斗逻辑
                HandlePartyStartCombat(character, enemyTemplate, party);
            }
            else
            {
                // 个人战斗逻辑
                HandleSoloStartCombat(character, enemyTemplate);
            }

            NotifyStateChanged();
        }

        /// <summary>
        /// 开始副本战斗
        /// </summary>
        public bool StartDungeon(Party party, string dungeonId)
        {
            if (party == null || string.IsNullOrEmpty(dungeonId))
                return false;

            var dungeon = DungeonData.GetDungeonById(dungeonId);
            if (dungeon == null)
                return false;

            // 验证参与人数
            var members = _allCharacters.Where(c => party.MemberIds.Contains(c.Id)).ToList();
            if (members.Count < dungeon.MinPlayers || members.Count > dungeon.MaxPlayers)
                return false;

            // 创建战斗上下文
            var battle = new BattleContext
            {
                BattleType = BattleType.Dungeon,
                Party = party,
                DungeonId = dungeonId,
                State = BattleState.Preparing
            };

            // 添加参与的玩家
            foreach (var member in members)
            {
                if (!member.IsDead)
                {
                    battle.Players.Add(member);

                    // 设置玩家状态
                    member.CurrentAction = PlayerActionState.Combat;
                    member.AttackCooldown = 0;

                    // 重置其他活动
                    member.CurrentGatheringNode = null;
                    member.CurrentRecipe = null;
                    member.GatheringCooldown = 0;
                    member.CraftingCooldown = 0;
                }
            }

            // 准备第一波战斗
            PrepareDungeonWave(battle, dungeon, 1);

            // 添加到活跃战斗列表
            _activeBattles[battle.Id] = battle;

            NotifyStateChanged();
            return true;
        }

        /// <summary>
        /// 开始多对多普通战斗
        /// </summary>
        public bool StartMultiEnemyBattle(Player character, List<Enemy> enemies, Party? party = null)
        {
            if (character == null || enemies == null || !enemies.Any())
                return false;

            // 创建战斗上下文
            var battle = new BattleContext
            {
                BattleType = party != null ? BattleType.Party : BattleType.Solo,
                Party = party,
                State = BattleState.Active
            };

            // 添加玩家
            if (party != null)
            {
                // 团队战斗
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
                // 单人战斗
                battle.Players.Add(character);
                character.CurrentAction = PlayerActionState.Combat;
                character.AttackCooldown = 0;
            }

            // 添加敌人
            foreach (var enemyTemplate in enemies)
            {
                var enemy = enemyTemplate.Clone();
                InitializeEnemySkills(enemy);
                battle.Enemies.Add(enemy);
            }

            // 添加到活跃战斗
            _activeBattles[battle.Id] = battle;

            NotifyStateChanged();
            return true;
        }

        /// <summary>
        /// 处理团队开始战斗
        /// </summary>
        private void HandlePartyStartCombat(Player character, Enemy enemyTemplate, Party party)
        {
            // 只有队长可以发起团队战斗
            if (party.CaptainId != character.Id)
                return;

            // 如果已经在打同一个敌人，则不需要重新开始
            if (party.CurrentEnemy?.Name == enemyTemplate.Name)
                return;

            // 创建敌人副本
            var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemyTemplate.Name) ?? enemyTemplate;
            party.CurrentEnemy = originalTemplate.Clone();
            InitializeEnemySkills(party.CurrentEnemy);

            // 设置所有队伍成员的战斗状态
            foreach (var memberId in party.MemberIds)
            {
                var member = _allCharacters.FirstOrDefault(c => c.Id == memberId);
                if (member != null && !member.IsDead)
                {
                    // 如果成员正在做采集或制作等非战斗、非空闲的活动，重置状态
                    if (member.CurrentAction != PlayerActionState.Idle && member.CurrentAction != PlayerActionState.Combat)
                    {
                        member.CurrentGatheringNode = null;
                        member.CurrentRecipe = null;
                        member.GatheringCooldown = 0;
                        member.CraftingCooldown = 0;
                    }

                    // 设置战斗状态
                    member.CurrentAction = PlayerActionState.Combat;
                    member.AttackCooldown = 0;
                }
            }
        }

        /// <summary>
        /// 处理个人开始战斗
        /// </summary>
        private void HandleSoloStartCombat(Player character, Enemy enemyTemplate)
        {
            // 如果已经在打同一个敌人，则不需要重新开始
            if (character.CurrentAction == PlayerActionState.Combat && character.CurrentEnemy?.Name == enemyTemplate.Name)
                return;

            // 重置当前状态
            character.CurrentGatheringNode = null;
            character.CurrentRecipe = null;
            character.GatheringCooldown = 0;
            character.CraftingCooldown = 0;
            
            // 设置战斗状态
            character.CurrentAction = PlayerActionState.Combat;
            character.CurrentEnemy = enemyTemplate.Clone();
            InitializeEnemySkills(character.CurrentEnemy);
        }

        /// <summary>
        /// 初始化敌人的技能冷却
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
        /// 应用角色技能效果
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

                    // 技能效果处理
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
                    
                    // 技能触发后进入冷却
                    character.SkillCooldowns[skillId] = skill.CooldownRounds;
                }
                else if (cooldown > 0)
                {
                    // 技能冷却时间减少
                    character.SkillCooldowns[skillId] = cooldown - 1;
                }
            }
        }

        /// <summary>
        /// 应用敌人技能效果
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
        /// 装备技能
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
        /// 卸下技能
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
        /// 检查是否有新技能解锁
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
        /// 重置玩家技能冷却
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
        /// 设置战斗职业
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
        /// 更新任务进度
        /// </summary>
        private void UpdateQuestProgress(Player character, QuestType type, string targetId, int amount)
        {
            if (character == null) return;
            
            // 直接使用ServiceLocator获取QuestService
            var questService = ServiceLocator.GetService<QuestService>();
            if (questService != null)
            {
                questService.UpdateQuestProgress(character, type, targetId, amount);
            }
        }

        /// <summary>
        /// 触发状态变更事件
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();

        /// <summary>
        /// 为角色生成新的敌人实例
        /// </summary>
        public void SpawnNewEnemyForCharacter(Player character, Enemy enemyTemplate)
        {
            if (character == null || enemyTemplate == null) return;
            
            // 查找敌人模板
            var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemyTemplate.Name) ?? enemyTemplate;
            
            // 克隆敌人
            character.CurrentEnemy = originalTemplate.Clone();
            
            // 初始化敌人技能冷却
            InitializeEnemySkills(character.CurrentEnemy);
        }

        /// <summary>
        /// 设置所有角色列表
        /// </summary>
        public void SetAllCharacters(List<Player> characters)
        {
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));

            _allCharacters = characters;
        }

        /// <summary>
        /// 副本数据静态类
        /// </summary>
        public static class DungeonData
        {
            /// <summary>
            /// 所有可用副本
            /// </summary>
            public static List<Dungeon> AllDungeons { get; } = new();

            /// <summary>
            /// 通过ID获取副本
            /// </summary>
            public static Dungeon? GetDungeonById(string id)
            {
                return AllDungeons.FirstOrDefault(d => d.Id == id);
            }

            /// <summary>
            /// 初始化副本数据
            /// </summary>
            static DungeonData()
            {
                // 示例副本数据
                AllDungeons.Add(new Dungeon
                {
                    Id = "forest_ruins",
                    Name = "森林遗迹",
                    Description = "一个被遗忘的古代遗迹，现在被各种野生动物和强盗占据。",
                    RecommendedLevel = 5,
                    MinPlayers = 1,
                    MaxPlayers = 3,
                    Waves = new List<DungeonWave>
                {
                    new DungeonWave
                    {
                        WaveNumber = 1,
                        Description = "入口守卫",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "森林狼", Count = 3 },
                            new EnemySpawnInfo { EnemyTemplateName = "强盗", Count = 1 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 2,
                        Description = "内部巡逻",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "强盗", Count = 2 },
                            new EnemySpawnInfo { EnemyTemplateName = "强盗弓箭手", Count = 2 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 3,
                        Description = "最终BOSS",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "强盗头目", Count = 1, IsElite = true, HealthMultiplier = 1.5 }
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

                // 可以继续添加更多副本...
            }
        }
    }
}