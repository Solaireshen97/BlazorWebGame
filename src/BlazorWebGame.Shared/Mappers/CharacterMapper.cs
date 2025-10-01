using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlazorWebGame.Shared.Mappers
{
    /// <summary>
    /// ��ɫģ��ӳ�乤���� - ������ģ�ͺ�DTO֮��ת��
    /// </summary>
    public static class CharacterMapper
    {
        /// <summary>
        /// ����ɫDTOת��Ϊ����ģ��
        /// </summary>
        public static Character ToCharacter(this CharacterStorageDto dto)
        {
            // ʹ�ù�������������ɫʵ��
            var character = CharacterFactory.CreateFromStorage(
                dto.Id,
                dto.Name,
                dto.Level,
                dto.Experience,
                dto.Gold,
                dto.CreatedAt,
                dto.UpdatedAt,
                dto.LastActiveAt);

            // ��������״̬
            if (dto.IsOnline)
                character.GoOnline();
            else
                character.GoOffline();

            // ���õ�ǰ����
            if (!string.IsNullOrEmpty(dto.CurrentRegionId))
                character.SetCurrentRegion(dto.CurrentRegionId);

            // ����PartyId
            if (dto.PartyId.HasValue)
                character.JoinParty(dto.PartyId.Value);

            // ��������ֵ�ͷ���ֵ
            var vitals = character.Vitals;
            CharacterFactory.SetVitals(vitals, dto.Health, dto.MaxHealth, dto.Mana, dto.MaxMana, dto.IsDead, dto.DeathTime);

            // ��������
            var attributes = character.Attributes;
            CharacterFactory.SetAttributes(attributes, dto.Strength, dto.Agility, dto.Intellect, dto.Spirit, dto.Stamina, dto.AttributePoints);

            // ����ְҵ
            var professions = character.Professions;
            CharacterFactory.SetProfessions(professions, dto.ProfessionId, dto.BattleProfessions, dto.GatheringProfessions, dto.ProductionProfessions);

            // ������Ʒ��װ��
            var inventory = character.Inventory;
            CharacterFactory.SetInventory(inventory, dto.Items, dto.EquippedItems);

            // ��������
            if (dto.Reputations != null)
            {
                var reputationsField = typeof(Character).GetField("Reputations", BindingFlags.Instance | BindingFlags.NonPublic);
                if (reputationsField != null)
                    reputationsField.SetValue(character, new Dictionary<string, int>(dto.Reputations));
            }

            // ��������Ʒװ��
            var consumableLoadout = character.ConsumableLoadout;
            CharacterFactory.SetConsumableLoadout(consumableLoadout, dto.GeneralConsumableSlots, dto.CombatConsumableSlots);

            // ��������
            var quests = character.Quests;
            CharacterFactory.SetQuests(quests, dto.ActiveQuestIds, dto.CompletedQuestIds, dto.QuestProgress);

            // ���ü���
            var skillManager = character.SkillManager;
            CharacterFactory.SetSkills(skillManager, dto.LearnedSkills, dto.EquippedSkills);

            // ���ûϵͳ
            var activitySystem = character.ActivitySystem;
            CharacterFactory.SetActivitySystem(activitySystem, dto.ActivitySlots);

            // �������߼�¼
            if (dto.LastOfflineRecord != null)
            {
                var lastOfflineRecordField = typeof(Character).GetField("LastOfflineRecord", BindingFlags.Instance | BindingFlags.NonPublic);
                lastOfflineRecordField?.SetValue(character, MapOfflineRecord(dto.LastOfflineRecord));
            }

            return character;
        }

        /// <summary>
        /// ʹ��DTO�������еĽ�ɫʵ��
        /// </summary>
        /// <param name="character">Ҫ���µĽ�ɫʵ��</param>
        /// <param name="dto">�����������ݵ�DTO</param>
        public static void UpdateFromDto(this Character character, CharacterStorageDto dto)
        {
            if (character == null || dto == null)
                return;

            // ���»������� (ʹ�÷�������ֻ������)
            SetPrivateProperty(character, "Level", dto.Level);
            SetPrivateProperty(character, "Experience", dto.Experience);
            SetPrivateProperty(character, "Gold", dto.Gold);
            SetPrivateProperty(character, "UpdatedAt", DateTime.UtcNow);
            SetPrivateProperty(character, "LastActiveAt", dto.LastActiveAt);

            // ��������״̬
            if (dto.IsOnline && !character.IsOnline)
                character.GoOnline();
            else if (!dto.IsOnline && character.IsOnline)
                character.GoOffline();

            // ���õ�ǰ����
            if (!string.IsNullOrEmpty(dto.CurrentRegionId) && dto.CurrentRegionId != character.CurrentRegionId)
                character.SetCurrentRegion(dto.CurrentRegionId);

            // ����PartyId
            if (dto.PartyId.HasValue && dto.PartyId != character.PartyId)
                character.JoinParty(dto.PartyId.Value);
            else if (!dto.PartyId.HasValue && character.PartyId.HasValue)
                SetPrivateProperty<Guid?>(character, "PartyId", null);

            // ��������ֵ�ͷ���ֵ
            CharacterFactory.SetVitals(character.Vitals, dto.Health, dto.MaxHealth, dto.Mana, dto.MaxMana, dto.IsDead, dto.DeathTime);

            // ��������
            CharacterFactory.SetAttributes(character.Attributes, dto.Strength, dto.Agility, dto.Intellect, dto.Spirit, dto.Stamina, dto.AttributePoints);

            // ����ְҵ
            CharacterFactory.SetProfessions(character.Professions, dto.ProfessionId, dto.BattleProfessions, dto.GatheringProfessions, dto.ProductionProfessions);

            // ������Ʒ��װ��
            CharacterFactory.SetInventory(character.Inventory, dto.Items, dto.EquippedItems);

            // ��������
            if (dto.Reputations != null)
            {
                var reputationsField = typeof(Character).GetField("Reputations", BindingFlags.Instance | BindingFlags.NonPublic);
                if (reputationsField != null)
                    reputationsField.SetValue(character, new Dictionary<string, int>(dto.Reputations));
            }

            // ��������Ʒװ��
            CharacterFactory.SetConsumableLoadout(character.ConsumableLoadout, dto.GeneralConsumableSlots, dto.CombatConsumableSlots);

            // ��������
            CharacterFactory.SetQuests(character.Quests, dto.ActiveQuestIds, dto.CompletedQuestIds, dto.QuestProgress);

            // ���ü���
            CharacterFactory.SetSkills(character.SkillManager, dto.LearnedSkills, dto.EquippedSkills);

            // ���ûϵͳ
            CharacterFactory.SetActivitySystem(character.ActivitySystem, dto.ActivitySlots);

            // �������߼�¼
            if (dto.LastOfflineRecord != null)
            {
                var lastOfflineRecordField = typeof(Character).GetField("LastOfflineRecord", BindingFlags.Instance | BindingFlags.NonPublic);
                lastOfflineRecordField?.SetValue(character, MapOfflineRecord(dto.LastOfflineRecord));
            }
        }

        /// <summary>
        /// ����˽�����Ը�������
        /// </summary>
        private static void SetPrivateProperty<T>(object obj, string propertyName, T value)
        {
            var property = obj.GetType().GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (property != null)
            {
                property.SetValue(obj, value);
            }
            else
            {
                var field = obj.GetType().GetField(propertyName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field != null)
                {
                    field.SetValue(obj, value);
                }
            }
        }

        /// <summary>
        /// ������ģ��ת��Ϊ��ɫDTO
        /// </summary>
        public static CharacterStorageDto ToDto(this Character character)
        {
            var dto = new CharacterStorageDto
            {
                Id = character.Id,
                Name = character.Name,
                Level = character.Level,
                Experience = character.Experience,
                Gold = character.Gold,
                IsOnline = character.IsOnline,
                CurrentRegionId = character.CurrentRegionId,
                CreatedAt = character.CreatedAt,
                UpdatedAt = character.UpdatedAt,
                LastActiveAt = character.LastActiveAt,
                PartyId = character.PartyId,

                // ����ֵ�ͷ���ֵ
                Health = character.Vitals.Health,
                MaxHealth = character.Vitals.MaxHealth,
                Mana = character.Vitals.Mana,
                MaxMana = character.Vitals.MaxMana,
                IsDead = character.Vitals.IsDead,
                DeathTime = character.Vitals.DeathTime,

                // ��������
                Strength = character.Attributes.Strength,
                Agility = character.Attributes.Agility,
                Intellect = character.Attributes.Intellect,
                Spirit = character.Attributes.Spirit,
                Stamina = character.Attributes.Stamina,
                AttributePoints = character.Attributes.AttributePoints,

                // ְҵ��Ϣ
                ProfessionId = character.Professions.SelectedBattleProfession,
                BattleProfessions = MapProfessions(character.Professions.BattleProfessions),
                GatheringProfessions = MapProfessions(character.Professions.GatheringProfessions),
                ProductionProfessions = MapProfessions(character.Professions.ProductionProfessions),

                // ������װ��
                Items = character.Inventory.Items.Select(i => new InventoryItemDto 
                { 
                    ItemId = i.ItemId, 
                    Quantity = i.Quantity 
                }).ToList(),

                EquippedItems = new Dictionary<string, string>(character.Inventory.EquippedItems),

                // ����Ʒװ��
                GeneralConsumableSlots = MapConsumableSlots(character.ConsumableLoadout.GeneralSlots),
                CombatConsumableSlots = MapConsumableSlots(character.ConsumableLoadout.CombatSlots),

                // ����
                Reputations = new Dictionary<string, int>(character.Reputations),

                // ����ϵͳ
                ActiveQuestIds = character.Quests.ActiveQuestIds.ToList(),
                CompletedQuestIds = character.Quests.CompletedQuestIds.ToList(),
                QuestProgress = new Dictionary<string, int>(character.Quests.QuestProgress),

                // ����ϵͳ
                LearnedSkills = MapLearnedSkills(character.SkillManager),
                EquippedSkills = MapEquippedSkills(character.SkillManager),

                // �ϵͳ
                ActivitySlots = MapActivitySlots(character.ActivitySystem),

                // ���߼�¼
                LastOfflineRecord = character.LastOfflineRecord != null ? 
                    MapToOfflineRecordDto(character.LastOfflineRecord) : null
            };

            return dto;
        }

        #region ����ӳ�䷽��

        private static Dictionary<string, ProfessionLevelDto> MapProfessions(Dictionary<string, CharacterProfessionLevel> professions)
        {
            return professions.ToDictionary(
                p => p.Key,
                p => new ProfessionLevelDto { Level = p.Value.Level, Experience = p.Value.Experience }
            );
        }

        private static List<ConsumableSlotDto> MapConsumableSlots(List<ConsumableSlot> slots)
        {
            return slots.Select(s => new ConsumableSlotDto
            {
                SlotId = s.SlotId,
                ItemId = s.ItemId,
                UsePolicy = s.Policy.ToString(),
                LastUsedAt = s.LastUsedAt
            }).ToList();
        }

        private static Dictionary<string, LearnedSkillDto> MapLearnedSkills(CharacterSkillManager skillManager)
        {
            // ʹ��ר�õ� SkillMapper ��
            return skillManager.ToDto();
        }

        private static Dictionary<string, List<string>> MapEquippedSkills(CharacterSkillManager skillManager)
        {
            // ʹ��ר�õ� SkillMapper ��
            return skillManager.EquippedSkillsToDto();
        }

        private static List<ActivitySlotDto> MapActivitySlots(ActivitySystem activitySystem)
        {
            // ʹ��ר�õ� ActivityMapper ��
            return activitySystem.ToDto();
        }

        private static OfflineRecord MapOfflineRecord(OfflineRecordDto dto)
        {
            if (dto == null) return null;

            var record = new OfflineRecord
            {
                OfflineAt = dto.OfflineAt,
                CharacterState = dto.CharacterState != null ? new CharacterSnapshot
                {
                    CharacterId = dto.CharacterState.CharacterId,
                    Level = dto.CharacterState.Level,
                    Experience = dto.CharacterState.Experience,
                    Gold = dto.CharacterState.Gold,
                    CurrentRegionId = dto.CharacterState.CurrentRegionId,
                    VitalsSnapshot = new VitalsSnapshot
                    {
                        Health = dto.CharacterState.Health,
                        MaxHealth = dto.CharacterState.MaxHealth,
                        Mana = dto.CharacterState.Mana,
                        MaxMana = dto.CharacterState.MaxMana
                    },
                    AttributesSnapshot = new AttributesSnapshot
                    {
                        Strength = dto.CharacterState.Strength,
                        Agility = dto.CharacterState.Agility,
                        Intellect = dto.CharacterState.Intellect,
                        Spirit = dto.CharacterState.Spirit,
                        Stamina = dto.CharacterState.Stamina
                    },
                    Timestamp = dto.CharacterState.Timestamp
                } : null
            };

            // ����ƻ��б�ת��Ϊ��Ч�� ActivityPlan ����
            var activePlans = new List<ActivityPlan>();
            foreach (var planDto in dto.ActivePlans)
            {
                // ʹ�� ActivityMapper ������Ч�� ActivityPlan ����
                var plan = ActivityMapper.FromPlanDto(planDto);
                activePlans.Add(plan);
            }

            record.ActivePlans = activePlans;
            return record;
        }

        private static OfflineRecordDto MapToOfflineRecordDto(OfflineRecord record)
        {
            if (record == null) return null;

            var dto = new OfflineRecordDto
            {
                OfflineAt = record.OfflineAt,
                CharacterState = record.CharacterState != null ? new CharacterSnapshotDto
                {
                    CharacterId = record.CharacterState.CharacterId,
                    Level = record.CharacterState.Level,
                    Experience = record.CharacterState.Experience,
                    Gold = record.CharacterState.Gold,
                    CurrentRegionId = record.CharacterState.CurrentRegionId,
                    Health = record.CharacterState.VitalsSnapshot?.Health ?? 0,
                    MaxHealth = record.CharacterState.VitalsSnapshot?.MaxHealth ?? 0,
                    Mana = record.CharacterState.VitalsSnapshot?.Mana ?? 0,
                    MaxMana = record.CharacterState.VitalsSnapshot?.MaxMana ?? 0,
                    Strength = record.CharacterState.AttributesSnapshot?.Strength ?? 0,
                    Agility = record.CharacterState.AttributesSnapshot?.Agility ?? 0,
                    Intellect = record.CharacterState.AttributesSnapshot?.Intellect ?? 0,
                    Spirit = record.CharacterState.AttributesSnapshot?.Spirit ?? 0,
                    Stamina = record.CharacterState.AttributesSnapshot?.Stamina ?? 0,
                    Timestamp = record.CharacterState.Timestamp
                } : null,
                // ʹ�� ActivityMapper ת����ƻ�
                ActivePlans = record.ActivePlans.Select(p => ActivityMapper.ToPlanDto(p)).ToList()
            };

            return dto;
        }

        #endregion
    }

    /// <summary>
    /// ��ɫ������ - ���ڴ�����ɫʵ�����������
    /// </summary>
    public static class CharacterFactory
    {
        /// <summary>
        /// �Ӵ洢���ݴ�����ɫ
        /// </summary>
        internal static Character CreateFromStorage(
            string id,
            string name,
            int level,
            int experience,
            int gold,
            DateTime createdAt,
            DateTime updatedAt,
            DateTime lastActiveAt)
        {
            // ʹ���вι��캯��������ɫʵ��
            var character = new Character(name);

            // ʹ�÷�������˽������
            var type = typeof(Character);
            SetPrivateProperty(character, "Id", id);
            SetPrivateProperty(character, "Level", level);
            SetPrivateProperty(character, "Experience", experience);
            SetPrivateProperty(character, "Gold", gold);
            SetPrivateProperty(character, "CreatedAt", createdAt);
            SetPrivateProperty(character, "UpdatedAt", updatedAt);
            SetPrivateProperty(character, "LastActiveAt", lastActiveAt);

            return character;
        }

        /// <summary>
        /// ��������ֵ�ͷ���ֵ
        /// </summary>
        internal static void SetVitals(
            CharacterVitals vitals,
            int health,
            int maxHealth,
            int mana,
            int maxMana,
            bool isDead,
            DateTime? deathTime)
        {
            var type = typeof(CharacterVitals);
            SetPrivateProperty(vitals, "Health", health);
            SetPrivateProperty(vitals, "MaxHealth", maxHealth);
            SetPrivateProperty(vitals, "Mana", mana);
            SetPrivateProperty(vitals, "MaxMana", maxMana);
            SetPrivateProperty(vitals, "IsDead", isDead);
            SetPrivateProperty(vitals, "DeathTime", deathTime);
        }

        /// <summary>
        /// ���û�������
        /// </summary>
        internal static void SetAttributes(
            CharacterAttributes attributes,
            int strength,
            int agility,
            int intellect,
            int spirit,
            int stamina,
            int attributePoints)
        {
            var type = typeof(CharacterAttributes);
            SetPrivateProperty(attributes, "Strength", strength);
            SetPrivateProperty(attributes, "Agility", agility);
            SetPrivateProperty(attributes, "Intellect", intellect);
            SetPrivateProperty(attributes, "Spirit", spirit);
            SetPrivateProperty(attributes, "Stamina", stamina);
            SetPrivateProperty(attributes, "AttributePoints", attributePoints);
        }

        /// <summary>
        /// ����ְҵ��Ϣ
        /// </summary>
        internal static void SetProfessions(
            CharacterProfessions professions,
            string selectedProfession,
            Dictionary<string, ProfessionLevelDto> battleProfessions,
            Dictionary<string, ProfessionLevelDto> gatheringProfessions,
            Dictionary<string, ProfessionLevelDto> productionProfessions)
        {
            // ����ѡ��ְҵ
            professions.SelectBattleProfession(selectedProfession);

            // ����������ս��ְҵ�ȼ��;���
            foreach (var prof in battleProfessions)
            {
                if (professions.BattleProfessions.TryGetValue(prof.Key, out var charProf))
                {
                    SetPrivateProperty(charProf, "Level", prof.Value.Level);
                    SetPrivateProperty(charProf, "Experience", prof.Value.Experience);
                }
            }

            // ���������òɼ�ְҵ�ȼ��;���
            foreach (var prof in gatheringProfessions)
            {
                if (professions.GatheringProfessions.TryGetValue(prof.Key, out var charProf))
                {
                    SetPrivateProperty(charProf, "Level", prof.Value.Level);
                    SetPrivateProperty(charProf, "Experience", prof.Value.Experience);
                }
            }

            // ��������������ְҵ�ȼ��;���
            foreach (var prof in productionProfessions)
            {
                if (professions.ProductionProfessions.TryGetValue(prof.Key, out var charProf))
                {
                    SetPrivateProperty(charProf, "Level", prof.Value.Level);
                    SetPrivateProperty(charProf, "Experience", prof.Value.Experience);
                }
            }
        }

        /// <summary>
        /// ������Ʒ��װ��
        /// </summary>
        internal static void SetInventory(
            CharacterInventory inventory,
            List<InventoryItemDto> items,
            Dictionary<string, string> equipment)
        {
            // ���������Ʒ
            var inventoryType = typeof(CharacterInventory);
            var itemsField = inventoryType.GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);
            if (itemsField != null)
            {
                var itemsList = (List<InventoryItem>)itemsField.GetValue(inventory);
                itemsList.Clear();
                
                // �����Ʒ
                foreach (var item in items)
                {
                    itemsList.Add(new InventoryItem(item.ItemId, item.Quantity));
                }
            }

            // �������װ��
            var equipField = inventoryType.GetField("_equippedItems", BindingFlags.Instance | BindingFlags.NonPublic);
            if (equipField != null)
            {
                var equipDict = (Dictionary<string, string>)equipField.GetValue(inventory);
                equipDict.Clear();
                
                // ���װ��
                foreach (var equip in equipment)
                {
                    equipDict[equip.Key] = equip.Value;
                }
            }
        }

        /// <summary>
        /// ��������Ʒװ��
        /// </summary>
        internal static void SetConsumableLoadout(
            ConsumableLoadout loadout,
            List<ConsumableSlotDto> generalSlots,
            List<ConsumableSlotDto> combatSlots)
        {
            // ����ͨ�ò�λ
            for (int i = 0; i < Math.Min(generalSlots.Count, loadout.GeneralSlots.Count); i++)
            {
                var slotDto = generalSlots[i];
                var slot = loadout.GeneralSlots[i];
                
                Enum.TryParse<UsePolicy>(slotDto.UsePolicy, out var policy);
                loadout.SetConsumable(slot.SlotId, slotDto.ItemId, policy);
                
                SetPrivateProperty(slot, "LastUsedAt", slotDto.LastUsedAt);
            }

            // ����ս����λ
            for (int i = 0; i < Math.Min(combatSlots.Count, loadout.CombatSlots.Count); i++)
            {
                var slotDto = combatSlots[i];
                var slot = loadout.CombatSlots[i];
                
                Enum.TryParse<UsePolicy>(slotDto.UsePolicy, out var policy);
                loadout.SetConsumable(slot.SlotId, slotDto.ItemId, policy);
                
                SetPrivateProperty(slot, "LastUsedAt", slotDto.LastUsedAt);
            }
        }

        /// <summary>
        /// ��������ϵͳ
        /// </summary>
        internal static void SetQuests(
            CharacterQuests quests,
            List<string> activeQuestIds,
            List<string> completedQuestIds,
            Dictionary<string, int> questProgress)
        {
            // ���ܻ�Ծ����
            foreach (var questId in activeQuestIds)
            {
                quests.AcceptQuest(questId);
                
                // �����������
                if (questProgress.TryGetValue(questId, out int progress))
                {
                    quests.UpdateQuestProgress(questId, progress);
                }
            }

            // �������������
            var questsType = typeof(CharacterQuests);
            var completedField = questsType.GetField("_completedQuestIds", BindingFlags.Instance | BindingFlags.NonPublic);
            if (completedField != null)
            {
                var completedList = (List<string>)completedField.GetValue(quests);
                completedList.Clear();
                completedList.AddRange(completedQuestIds);
            }
        }

        /// <summary>
        /// ���ü���ϵͳ
        /// </summary>
        internal static void SetSkills(
            CharacterSkillManager skillManager,
            Dictionary<string, LearnedSkillDto> learnedSkills,
            Dictionary<string, List<string>> equippedSkills)
        {
            // ʹ��ר�õ� SkillMapper ��
            skillManager.RestoreFromDto(learnedSkills, equippedSkills);
        }

        /// <summary>
        /// ���ûϵͳ
        /// </summary>
        internal static void SetActivitySystem(
            ActivitySystem activitySystem,
            List<ActivitySlotDto> activitySlots)
        {
            // ʹ��ר�õ� ActivityMapper ��
            activitySystem.RestoreFromDto(activitySlots);
        }

        /// <summary>
        /// ����˽�����Ը�������
        /// </summary>
        private static void SetPrivateProperty<T>(object obj, string propertyName, T value)
        {
            var property = obj.GetType().GetProperty(propertyName, 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
            if (property != null)
            {
                property.SetValue(obj, value);
            }
            else
            {
                var field = obj.GetType().GetField(propertyName, 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    
                if (field != null)
                {
                    field.SetValue(obj, value);
                }
            }
        }
    }
}