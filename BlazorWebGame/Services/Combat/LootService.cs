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
    /// 战利品服务 - 负责处理战斗奖励分配
    /// </summary>
    public class LootService
    {
        private readonly InventoryService _inventoryService;
        private readonly SkillSystem _skillSystem;
        private readonly List<Player> _allCharacters;

        public LootService(
            InventoryService inventoryService,
            SkillSystem skillSystem,
            List<Player> allCharacters)
        {
            _inventoryService = inventoryService;
            _skillSystem = skillSystem;
            _allCharacters = allCharacters;
        }

        /// <summary>
        /// 处理战斗胜利
        /// </summary>
        public void HandleBattleVictory(BattleContext battle, BattleFlowService battleFlowService)
        {
            // 副本战斗胜利处理
            if (battle.BattleType == BattleType.Dungeon && !string.IsNullOrEmpty(battle.DungeonId))
            {
                var dungeon = DungeonTemplates.GetDungeonById(battle.DungeonId);
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
                        // 进入下一波（由BattleFlowService处理）
                        battleFlowService.ProcessDungeonNextWave(battle, dungeon, _skillSystem);
                    }
                }
            }
            // 普通战斗胜利处理
            else
            {
                // 分配普通战斗奖励
                DistributeNormalBattleRewards(battle);
            }
        }

        /// <summary>
        /// 处理战斗失败
        /// </summary>
        public void HandleBattleDefeat(BattleContext battle)
        {
            // 可以在这里添加战斗失败的特殊处理
            // 比如：死亡惩罚、耐久度损失等
        }

        /// <summary>
        /// 分配普通战斗奖励
        /// </summary>
        private void DistributeNormalBattleRewards(BattleContext battle)
        {
            // 根据战斗类型分配奖励
            if (battle.BattleType == BattleType.Party && battle.Party != null)
            {
                // 重置团队敌人
                if (battle.Enemies.Any())
                {
                    var enemyTemplate = battle.Enemies.First();
                    battle.Party.CurrentEnemy = enemyTemplate.Clone();
                    if (battle.Party.CurrentEnemy != null)
                    {
                        _skillSystem.InitializeEnemySkills(battle.Party.CurrentEnemy);
                    }
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
                            _skillSystem.InitializeEnemySkills(player.CurrentEnemy);
                        }
                    }
                }
            }
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
                        
                        // 触发物品获得事件
                        RaiseItemAcquiredEvent(luckyPlayer, reward.ItemId, reward.ItemQuantity);
                    }

                    // 所有玩家获得金币和经验
                    foreach (var player in alivePlayers)
                    {
                        // 金币奖励
                        if (reward.Gold > 0)
                        {
                            player.Gold += reward.Gold;
                            RaiseGoldAcquiredEvent(player, reward.Gold);
                        }

                        // 经验奖励
                        if (reward.Experience > 0)
                        {
                            var profession = player.SelectedBattleProfession;
                            var oldLevel = player.GetLevel(profession);
                            player.AddBattleXP(profession, reward.Experience);

                            if (player.GetLevel(profession) > oldLevel)
                            {
                                _skillSystem.CheckForNewSkillUnlocks(player, profession, player.GetLevel(profession));
                                RaiseLevelUpEvent(player, profession, player.GetLevel(profession));
                            }
                        }
                    }
                }
            }

            // 更新副本完成记录
            foreach (var player in alivePlayers)
            {
                // 这里可以添加副本完成记录的逻辑
                RaiseDungeonCompletedEvent(player, dungeon);
            }
        }

        /// <summary>
        /// 处理击败敌人后的战利品分配
        /// </summary>
        public void DistributeEnemyLoot(Player killer, Enemy enemy, BattleContext? battleContext)
        {
            if (battleContext != null)
            {
                // 在新战斗系统中的战利品分配
                if (battleContext.BattleType == BattleType.Party && battleContext.Party != null)
                {
                    HandlePartyLoot(battleContext.Party, enemy, killer);
                }
                else
                {
                    HandleSoloLoot(killer, enemy);
                }
            }
            else
            {
                // 兼容旧系统的战利品分配
                var party = GetPartyForCharacter(killer.Id);
                if (party != null)
                {
                    HandlePartyLoot(party, enemy, killer);
                }
                else
                {
                    HandleSoloLoot(killer, enemy);
                }
            }
        }

        /// <summary>
        /// 处理团队击败敌人后的战利品分配
        /// </summary>
        private void HandlePartyLoot(Party party, Enemy enemy, Player killer)
        {
            var partyMembers = _allCharacters.Where(c => party.MemberIds.Contains(c.Id)).ToList();
            if (!partyMembers.Any())
                return;

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
                    RaiseItemAcquiredEvent(luckyMemberForLoot, lootItem.Key, 1);
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
                    _skillSystem.CheckForNewSkillUnlocks(member, profession, member.GetLevel(profession));
                    RaiseLevelUpEvent(member, profession, member.GetLevel(profession));
                }

                UpdateQuestProgress(member, QuestType.KillMonster, enemy.Name, 1);
                UpdateQuestProgress(member, QuestType.KillMonster, "any", 1);
                member.DefeatedMonsterIds.Add(enemy.Name);
            }

            // 为团队生成新敌人（如果需要）
            var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemy.Name) ?? enemy;
            party.CurrentEnemy = originalTemplate.Clone();
            if (party.CurrentEnemy != null)
            {
                _skillSystem.InitializeEnemySkills(party.CurrentEnemy);
            }
        }

        /// <summary>
        /// 处理单人击败敌人后的战利品分配
        /// </summary>
        private void HandleSoloLoot(Player character, Enemy enemy)
        {
            // 金币奖励
            var goldAmount = enemy.GetGoldDropAmount();
            character.Gold += goldAmount;
            RaiseGoldAcquiredEvent(character, goldAmount);
            
            // 掉落物品
            var random = new Random();
            foreach (var lootItem in enemy.LootTable)
            {
                if (random.NextDouble() <= lootItem.Value)
                {
                    _inventoryService.AddItemToInventory(character, lootItem.Key, 1);
                    RaiseItemAcquiredEvent(character, lootItem.Key, 1);
                }
            }

            // 经验值和任务进度
            var profession = character.SelectedBattleProfession;
            var oldLevel = character.GetLevel(profession);
            character.AddBattleXP(profession, enemy.XpReward);
            
            if (character.GetLevel(profession) > oldLevel)
            {
                _skillSystem.CheckForNewSkillUnlocks(character, profession, character.GetLevel(profession));
                RaiseLevelUpEvent(character, profession, character.GetLevel(profession));
            }

            UpdateQuestProgress(character, QuestType.KillMonster, enemy.Name, 1);
            UpdateQuestProgress(character, QuestType.KillMonster, "any", 1);
            character.DefeatedMonsterIds.Add(enemy.Name);

            // 为玩家生成新敌人（如果需要）
            var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemy.Name) ?? enemy;
            character.CurrentEnemy = originalTemplate.Clone();
            if (character.CurrentEnemy != null)
            {
                _skillSystem.InitializeEnemySkills(character.CurrentEnemy);
            }
        }

        /// <summary>
        /// 获取玩家所在的队伍
        /// </summary>
        private Party? GetPartyForCharacter(string characterId)
        {
            var partyService = ServiceLocator.GetService<PartyService>();
            return partyService?.GetPartyForCharacter(characterId);
        }

        /// <summary>
        /// 更新任务进度
        /// </summary>
        private void UpdateQuestProgress(Player character, QuestType type, string targetId, int amount)
        {
            var questService = ServiceLocator.GetService<QuestService>();
            questService?.UpdateQuestProgress(character, type, targetId, amount);
        }

        #region 事件触发方法

        private void RaiseItemAcquiredEvent(Player player, string itemId, int quantity)
        {
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseItemEvent(
                GameEventType.ItemAcquired,
                player,
                itemId,
                null,
                quantity
            );
        }

        private void RaiseGoldAcquiredEvent(Player player, int amount)
        {
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseItemEvent(
                GameEventType.GoldChanged,
                player,
                null,
                null,
                0,
                amount
            );
        }

        private void RaiseLevelUpEvent(Player player, BattleProfession profession, int newLevel)
        {
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.LevelUp, player);
        }

        private void RaiseDungeonCompletedEvent(Player player, Dungeon dungeon)
        {
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.DungeonCompleted, player);
        }

        #endregion
    }
}