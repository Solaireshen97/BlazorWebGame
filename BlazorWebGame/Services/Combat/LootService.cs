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
    /// ս��Ʒ���� - ������ս����������
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
        /// ����ս��ʤ��
        /// </summary>
        public void HandleBattleVictory(BattleContext battle, BattleFlowService battleFlowService)
        {
            // ����ս��ʤ������
            if (battle.BattleType == BattleType.Dungeon && !string.IsNullOrEmpty(battle.DungeonId))
            {
                var dungeon = DungeonTemplates.GetDungeonById(battle.DungeonId);
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
                        // ������һ������BattleFlowService����
                        battleFlowService.ProcessDungeonNextWave(battle, dungeon, _skillSystem);
                    }
                }
            }
            // ��ͨս��ʤ������
            else
            {
                // ������ͨս������
                DistributeNormalBattleRewards(battle);
            }
        }

        /// <summary>
        /// ����ս��ʧ��
        /// </summary>
        public void HandleBattleDefeat(BattleContext battle)
        {
            // �������������ս��ʧ�ܵ����⴦��
            // ���磺�����ͷ����;ö���ʧ��
        }

        /// <summary>
        /// ������ͨս������
        /// </summary>
        private void DistributeNormalBattleRewards(BattleContext battle)
        {
            // ����ս�����ͷ��佱��
            if (battle.BattleType == BattleType.Party && battle.Party != null)
            {
                // �����Ŷӵ���
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
                // ����ս����Ϊÿ����������µ���
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
                        
                        // ������Ʒ����¼�
                        RaiseItemAcquiredEvent(luckyPlayer, reward.ItemId, reward.ItemQuantity);
                    }

                    // ������һ�ý�Һ;���
                    foreach (var player in alivePlayers)
                    {
                        // ��ҽ���
                        if (reward.Gold > 0)
                        {
                            player.Gold += reward.Gold;
                            RaiseGoldAcquiredEvent(player, reward.Gold);
                        }

                        // ���齱��
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

            // ���¸�����ɼ�¼
            foreach (var player in alivePlayers)
            {
                // ���������Ӹ�����ɼ�¼���߼�
                RaiseDungeonCompletedEvent(player, dungeon);
            }
        }

        /// <summary>
        /// ������ܵ��˺��ս��Ʒ����
        /// </summary>
        public void DistributeEnemyLoot(Player killer, Enemy enemy, BattleContext? battleContext)
        {
            if (battleContext != null)
            {
                // ����ս��ϵͳ�е�ս��Ʒ����
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
                // ���ݾ�ϵͳ��ս��Ʒ����
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
        /// �����Ŷӻ��ܵ��˺��ս��Ʒ����
        /// </summary>
        private void HandlePartyLoot(Party party, Enemy enemy, Player killer)
        {
            var partyMembers = _allCharacters.Where(c => party.MemberIds.Contains(c.Id)).ToList();
            if (!partyMembers.Any())
                return;

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
                    RaiseItemAcquiredEvent(luckyMemberForLoot, lootItem.Key, 1);
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
                    _skillSystem.CheckForNewSkillUnlocks(member, profession, member.GetLevel(profession));
                    RaiseLevelUpEvent(member, profession, member.GetLevel(profession));
                }

                UpdateQuestProgress(member, QuestType.KillMonster, enemy.Name, 1);
                UpdateQuestProgress(member, QuestType.KillMonster, "any", 1);
                member.DefeatedMonsterIds.Add(enemy.Name);
            }

            // Ϊ�Ŷ������µ��ˣ������Ҫ��
            var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemy.Name) ?? enemy;
            party.CurrentEnemy = originalTemplate.Clone();
            if (party.CurrentEnemy != null)
            {
                _skillSystem.InitializeEnemySkills(party.CurrentEnemy);
            }
        }

        /// <summary>
        /// �����˻��ܵ��˺��ս��Ʒ����
        /// </summary>
        private void HandleSoloLoot(Player character, Enemy enemy)
        {
            // ��ҽ���
            var goldAmount = enemy.GetGoldDropAmount();
            character.Gold += goldAmount;
            RaiseGoldAcquiredEvent(character, goldAmount);
            
            // ������Ʒ
            var random = new Random();
            foreach (var lootItem in enemy.LootTable)
            {
                if (random.NextDouble() <= lootItem.Value)
                {
                    _inventoryService.AddItemToInventory(character, lootItem.Key, 1);
                    RaiseItemAcquiredEvent(character, lootItem.Key, 1);
                }
            }

            // ����ֵ���������
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

            // Ϊ��������µ��ˣ������Ҫ��
            var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemy.Name) ?? enemy;
            character.CurrentEnemy = originalTemplate.Clone();
            if (character.CurrentEnemy != null)
            {
                _skillSystem.InitializeEnemySkills(character.CurrentEnemy);
            }
        }

        /// <summary>
        /// ��ȡ������ڵĶ���
        /// </summary>
        private Party? GetPartyForCharacter(string characterId)
        {
            var partyService = ServiceLocator.GetService<PartyService>();
            return partyService?.GetPartyForCharacter(characterId);
        }

        /// <summary>
        /// �����������
        /// </summary>
        private void UpdateQuestProgress(Player character, QuestType type, string targetId, int amount)
        {
            var questService = ServiceLocator.GetService<QuestService>();
            questService?.UpdateQuestProgress(character, type, targetId, amount);
        }

        #region �¼���������

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