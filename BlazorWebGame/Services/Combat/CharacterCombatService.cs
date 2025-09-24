using BlazorWebGame.Events;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Battles;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Models.Monsters;
using System.Linq;

namespace BlazorWebGame.Services.Combat
{
    /// <summary>
    /// ��ɫս������ - �����ɫ��ս���е�״̬
    /// </summary>
    public class CharacterCombatService
    {
        private const double RevivalDuration = 2.0;

        /// <summary>
        /// ׼����ɫ����ս��
        /// </summary>
        public void PrepareCharacterForBattle(Player character)
        {
            if (character == null) return;

            // ���õ�ǰ��ս���
            ResetPlayerAction(character);

            // ����Ϊս��״̬
            character.CurrentAction = PlayerActionState.Combat;
            character.AttackCooldown = 0;
        }

        /// <summary>
        /// ������ҵ�ǰ����״̬
        /// </summary>
        private void ResetPlayerAction(Player player)
        {
            if (player == null) return;

            player.CurrentGatheringNode = null;
            player.CurrentRecipe = null;
            player.GatheringCooldown = 0;
            player.CraftingCooldown = 0;
        }

        /// <summary>
        /// ������Ҹ���
        /// </summary>
        public void ProcessPlayerRevival(BattleContext battle, double elapsedSeconds)
        {
            // ֻ���������Զ������ս��
            if (!battle.AllowAutoRevive && battle.BattleType == BattleType.Dungeon)
                return;

            foreach (var player in battle.Players.Where(p => p.IsDead))
            {
                player.RevivalTimeRemaining -= elapsedSeconds;

                if (player.RevivalTimeRemaining <= 0)
                {
                    ReviveCharacter(player);
                }
            }
        }

        /// <summary>
        /// �����ɫ����
        /// </summary>
        public void HandleCharacterDeath(Player character, BattleContext? battleContext = null)
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

            // ������ɫ�����¼�
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.CharacterDeath, character);

            // ����ṩ��ս�������ģ����Խ��ж������������
            if (battleContext != null)
            {
                // ����һ��ս�����������У�ս�������������Ƿ�������Ҷ�����
                // ����AllowAutoRevive���Ծ����Ƿ����ս��
            }
        }

        /// <summary>
        /// ��ɫ����
        /// </summary>
        public void ReviveCharacter(Player character)
        {
            character.IsDead = false;
            character.Health = character.GetTotalMaxHealth();
            character.RevivalTimeRemaining = 0;

            // ������ɫ�����¼�
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.CharacterRevived, character);
        }

        /// <summary>
        /// ����ս��ְҵ
        /// </summary>
        public void SetBattleProfession(Player character, BattleProfession profession)
        {
            if (character != null)
            {
                character.SelectedBattleProfession = profession;
            }
        }

        /// <summary>
        /// Ϊ��ɫ�����µĵ���ʵ�������ھ�ϵͳ���ݣ�
        /// </summary>
        public void SpawnNewEnemyForCharacter(Player character, Enemy enemyTemplate, SkillSystem skillSystem)
        {
            if (character == null || enemyTemplate == null) return;
            
            // ���ҵ���ģ��
            var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemyTemplate.Name) ?? enemyTemplate;
            
            // ��¡����
            character.CurrentEnemy = originalTemplate.Clone();
            
            // ��ʼ�����˼�����ȴ
            if (character.CurrentEnemy != null)
            {
                skillSystem.InitializeEnemySkills(character.CurrentEnemy);
            }
        }

        /// <summary>
        /// �����ϵͳ���Ŷӿ�ʼս��
        /// </summary>
        public void HandlePartyStartCombat(Player character, Enemy enemyTemplate, Party party, List<Player> allCharacters, SkillSystem skillSystem)
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
            if (party.CurrentEnemy != null)
            {
                skillSystem.InitializeEnemySkills(party.CurrentEnemy);
            }

            // �������ж����Ա��ս��״̬
            foreach (var memberId in party.MemberIds)
            {
                var member = allCharacters.FirstOrDefault(c => c.Id == memberId);
                if (member != null && !member.IsDead)
                {
                    PrepareCharacterForBattle(member);
                }
            }
        }

        /// <summary>
        /// �����ϵͳ�ĸ��˿�ʼս��
        /// </summary>
        public void HandleSoloStartCombat(Player character, Enemy enemyTemplate, SkillSystem skillSystem)
        {
            // ����Ѿ��ڴ�ͬһ�����ˣ�����Ҫ���¿�ʼ
            if (character.CurrentAction == PlayerActionState.Combat && character.CurrentEnemy?.Name == enemyTemplate.Name)
                return;

            // ���õ�ǰ״̬
            ResetPlayerAction(character);
            
            // ����ս��״̬
            character.CurrentAction = PlayerActionState.Combat;
            character.AttackCooldown = 0;
            character.CurrentEnemy = enemyTemplate.Clone();
            
            if (character.CurrentEnemy != null)
            {
                skillSystem.InitializeEnemySkills(character.CurrentEnemy);
            }
        }

        /// <summary>
        /// ����ɫ�Ƿ���Բ���ս��
        /// </summary>
        public bool CanCharacterFight(Player character)
        {
            return character != null && !character.IsDead;
        }

        /// <summary>
        /// ��ȡ��ɫ��ս��״̬��Ϣ
        /// </summary>
        public CharacterCombatStatus GetCombatStatus(Player character)
        {
            return new CharacterCombatStatus
            {
                IsInCombat = character.CurrentAction == PlayerActionState.Combat,
                IsDead = character.IsDead,
                RevivalTimeRemaining = character.RevivalTimeRemaining,
                CurrentHealth = character.Health,
                MaxHealth = character.GetTotalMaxHealth(),
                HealthPercentage = (double)character.Health / character.GetTotalMaxHealth()
            };
        }

        /// <summary>
        /// ���½�ɫս��״̬������UI��ʾ��
        /// </summary>
        public void UpdateCharacterCombatUI(Player character)
        {
            // ����UI�����¼�
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.CombatStatusChanged, character);
        }

        /// <summary>
        /// Ӧ��ս����ʼʱ��Ч��
        /// </summary>
        public void ApplyBattleStartEffects(Player character)
        {
            // ���ü�����ȴ�������Ҫ��
            // Ӧ��ս����ʼʱ�ı���Ч��
            // ��鲢Ӧ��װ����ս����ʼЧ��
        }

        /// <summary>
        /// Ӧ��ս������ʱ��Ч��
        /// </summary>
        public void ApplyBattleEndEffects(Player character)
        {
            // ����ս����ص���ʱЧ��
            // Ӧ��ս������ʱ�Ļָ�Ч��
        }

        /// <summary>
        /// ��ȡ��ɫ��������ʱ��
        /// </summary>
        public double GetRevivalDuration(Player character)
        {
            // ��������ʱ��
            double duration = RevivalDuration;

            // ���Ը���װ�������ܵ����ص�������ʱ��
            // ���磺ĳЩװ�����ܼ��ٸ���ʱ��

            return duration;
        }

        /// <summary>
        /// ��鲢�����Զ�����Ʒʹ��
        /// </summary>
        public void ProcessAutoConsumables(Player character)
        {
            var inventoryService = ServiceLocator.GetService<InventoryService>();
            inventoryService?.ProcessAutoConsumables(character);
        }
    }

    /// <summary>
    /// ��ɫս��״̬��Ϣ
    /// </summary>
    public class CharacterCombatStatus
    {
        public bool IsInCombat { get; set; }
        public bool IsDead { get; set; }
        public double RevivalTimeRemaining { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public double HealthPercentage { get; set; }
    }
}