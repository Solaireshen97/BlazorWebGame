using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlazorWebGame.Shared.Mappers
{
    /// <summary>
    /// 角色模型映射工具类 - 在领域模型和DTO之间转换
    /// </summary>
    public static class CharacterMapper
    {
        /// <summary>
        /// 将角色DTO转换为领域模型
        /// </summary>
        public static Character ToCharacter(this CharacterStorageDto dto)
        {
            // 使用工厂方法创建角色实例
            var character = CharacterFactory.CreateFromStorage(
                dto.Id,
                dto.Name,
                dto.Level,
                dto.Experience,
                dto.Gold,
                dto.CreatedAt,
                dto.UpdatedAt,
                dto.LastActiveAt);

            // 设置在线状态
            if (dto.IsOnline)
                character.GoOnline();
            else
                character.GoOffline();

            // 设置当前区域
            if (!string.IsNullOrEmpty(dto.CurrentRegionId))
                character.SetCurrentRegion(dto.CurrentRegionId);

            // 设置PartyId
            if (dto.PartyId.HasValue)
                character.JoinParty(dto.PartyId.Value);

            // 设置生命值和法力值
            var vitals = character.Vitals;
            CharacterFactory.SetVitals(vitals, dto.Health, dto.MaxHealth, dto.Mana, dto.MaxMana, dto.IsDead, dto.DeathTime);

            // 设置属性
            var attributes = character.Attributes;
            CharacterFactory.SetAttributes(attributes, dto.Strength, dto.Agility, dto.Intellect, dto.Spirit, dto.Stamina, dto.AttributePoints);

            // 设置职业
            var professions = character.Professions;
            CharacterFactory.SetProfessions(professions, dto.ProfessionId, dto.BattleProfessions, dto.GatheringProfessions, dto.ProductionProfessions);

            // 设置物品和装备
            var inventory = character.Inventory;
            CharacterFactory.SetInventory(inventory, dto.Items, dto.EquippedItems);

            // 设置声望
            if (dto.Reputations != null)
            {
                var reputationsField = typeof(Character).GetField("Reputations", BindingFlags.Instance | BindingFlags.NonPublic);
                if (reputationsField != null)
                    reputationsField.SetValue(character, new Dictionary<string, int>(dto.Reputations));
            }

            // 设置消耗品装载
            var consumableLoadout = character.ConsumableLoadout;
            CharacterFactory.SetConsumableLoadout(consumableLoadout, dto.GeneralConsumableSlots, dto.CombatConsumableSlots);

            // 设置任务
            var quests = character.Quests;
            CharacterFactory.SetQuests(quests, dto.ActiveQuestIds, dto.CompletedQuestIds, dto.QuestProgress);

            // 设置技能
            var skillManager = character.SkillManager;
            CharacterFactory.SetSkills(skillManager, dto.LearnedSkills, dto.EquippedSkills);

            // 设置活动系统
            var activitySystem = character.ActivitySystem;
            CharacterFactory.SetActivitySystem(activitySystem, dto.ActivitySlots);

            // 设置离线记录
            if (dto.LastOfflineRecord != null)
            {
                var lastOfflineRecordField = typeof(Character).GetField("LastOfflineRecord", BindingFlags.Instance | BindingFlags.NonPublic);
                lastOfflineRecordField?.SetValue(character, MapOfflineRecord(dto.LastOfflineRecord));
            }

            return character;
        }

        /// <summary>
        /// 使用DTO更新现有的角色实例
        /// </summary>
        /// <param name="character">要更新的角色实例</param>
        /// <param name="dto">包含更新数据的DTO</param>
        public static void UpdateFromDto(this Character character, CharacterStorageDto dto)
        {
            if (character == null || dto == null)
                return;

            // 更新基本属性 (使用反射设置只读属性)
            SetPrivateProperty(character, "Level", dto.Level);
            SetPrivateProperty(character, "Experience", dto.Experience);
            SetPrivateProperty(character, "Gold", dto.Gold);
            SetPrivateProperty(character, "UpdatedAt", DateTime.UtcNow);
            SetPrivateProperty(character, "LastActiveAt", dto.LastActiveAt);

            // 设置在线状态
            if (dto.IsOnline && !character.IsOnline)
                character.GoOnline();
            else if (!dto.IsOnline && character.IsOnline)
                character.GoOffline();

            // 设置当前区域
            if (!string.IsNullOrEmpty(dto.CurrentRegionId) && dto.CurrentRegionId != character.CurrentRegionId)
                character.SetCurrentRegion(dto.CurrentRegionId);

            // 设置PartyId
            if (dto.PartyId.HasValue && dto.PartyId != character.PartyId)
                character.JoinParty(dto.PartyId.Value);
            else if (!dto.PartyId.HasValue && character.PartyId.HasValue)
                SetPrivateProperty<Guid?>(character, "PartyId", null);

            // 设置生命值和法力值
            CharacterFactory.SetVitals(character.Vitals, dto.Health, dto.MaxHealth, dto.Mana, dto.MaxMana, dto.IsDead, dto.DeathTime);

            // 设置属性
            CharacterFactory.SetAttributes(character.Attributes, dto.Strength, dto.Agility, dto.Intellect, dto.Spirit, dto.Stamina, dto.AttributePoints);

            // 设置职业
            CharacterFactory.SetProfessions(character.Professions, dto.ProfessionId, dto.BattleProfessions, dto.GatheringProfessions, dto.ProductionProfessions);

            // 设置物品和装备
            CharacterFactory.SetInventory(character.Inventory, dto.Items, dto.EquippedItems);

            // 设置声望
            if (dto.Reputations != null)
            {
                var reputationsField = typeof(Character).GetField("Reputations", BindingFlags.Instance | BindingFlags.NonPublic);
                if (reputationsField != null)
                    reputationsField.SetValue(character, new Dictionary<string, int>(dto.Reputations));
            }

            // 设置消耗品装载
            CharacterFactory.SetConsumableLoadout(character.ConsumableLoadout, dto.GeneralConsumableSlots, dto.CombatConsumableSlots);

            // 设置任务
            CharacterFactory.SetQuests(character.Quests, dto.ActiveQuestIds, dto.CompletedQuestIds, dto.QuestProgress);

            // 设置技能
            CharacterFactory.SetSkills(character.SkillManager, dto.LearnedSkills, dto.EquippedSkills);

            // 设置活动系统
            CharacterFactory.SetActivitySystem(character.ActivitySystem, dto.ActivitySlots);

            // 设置离线记录
            if (dto.LastOfflineRecord != null)
            {
                var lastOfflineRecordField = typeof(Character).GetField("LastOfflineRecord", BindingFlags.Instance | BindingFlags.NonPublic);
                lastOfflineRecordField?.SetValue(character, MapOfflineRecord(dto.LastOfflineRecord));
            }
        }

        /// <summary>
        /// 设置私有属性辅助方法
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
        /// 将领域模型转换为角色DTO
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

                // 生命值和法力值
                Health = character.Vitals.Health,
                MaxHealth = character.Vitals.MaxHealth,
                Mana = character.Vitals.Mana,
                MaxMana = character.Vitals.MaxMana,
                IsDead = character.Vitals.IsDead,
                DeathTime = character.Vitals.DeathTime,

                // 基础属性
                Strength = character.Attributes.Strength,
                Agility = character.Attributes.Agility,
                Intellect = character.Attributes.Intellect,
                Spirit = character.Attributes.Spirit,
                Stamina = character.Attributes.Stamina,
                AttributePoints = character.Attributes.AttributePoints,

                // 职业信息
                ProfessionId = character.Professions.SelectedBattleProfession,
                BattleProfessions = MapProfessions(character.Professions.BattleProfessions),
                GatheringProfessions = MapProfessions(character.Professions.GatheringProfessions),
                ProductionProfessions = MapProfessions(character.Professions.ProductionProfessions),

                // 背包和装备
                Items = character.Inventory.Items.Select(i => new InventoryItemDto 
                { 
                    ItemId = i.ItemId, 
                    Quantity = i.Quantity 
                }).ToList(),

                EquippedItems = new Dictionary<string, string>(character.Inventory.EquippedItems),

                // 消耗品装载
                GeneralConsumableSlots = MapConsumableSlots(character.ConsumableLoadout.GeneralSlots),
                CombatConsumableSlots = MapConsumableSlots(character.ConsumableLoadout.CombatSlots),

                // 声望
                Reputations = new Dictionary<string, int>(character.Reputations),

                // 任务系统
                ActiveQuestIds = character.Quests.ActiveQuestIds.ToList(),
                CompletedQuestIds = character.Quests.CompletedQuestIds.ToList(),
                QuestProgress = new Dictionary<string, int>(character.Quests.QuestProgress),

                // 技能系统
                LearnedSkills = MapLearnedSkills(character.SkillManager),
                EquippedSkills = MapEquippedSkills(character.SkillManager),

                // 活动系统
                ActivitySlots = MapActivitySlots(character.ActivitySystem),

                // 离线记录
                LastOfflineRecord = character.LastOfflineRecord != null ? 
                    MapToOfflineRecordDto(character.LastOfflineRecord) : null
            };

            return dto;
        }

        #region 辅助映射方法

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
            // 使用专用的 SkillMapper 类
            return skillManager.ToDto();
        }

        private static Dictionary<string, List<string>> MapEquippedSkills(CharacterSkillManager skillManager)
        {
            // 使用专用的 SkillMapper 类
            return skillManager.EquippedSkillsToDto();
        }

        private static List<ActivitySlotDto> MapActivitySlots(ActivitySystem activitySystem)
        {
            // 使用专用的 ActivityMapper 类
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

            // 将活动计划列表转换为有效的 ActivityPlan 对象
            var activePlans = new List<ActivityPlan>();
            foreach (var planDto in dto.ActivePlans)
            {
                // 使用 ActivityMapper 创建有效的 ActivityPlan 对象
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
                // 使用 ActivityMapper 转换活动计划
                ActivePlans = record.ActivePlans.Select(p => ActivityMapper.ToPlanDto(p)).ToList()
            };

            return dto;
        }

        #endregion
    }

    /// <summary>
    /// 角色工厂类 - 用于创建角色实例的特殊情况
    /// </summary>
    public static class CharacterFactory
    {
        /// <summary>
        /// 从存储数据创建角色
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
            // 使用有参构造函数创建角色实例
            var character = new Character(name);

            // 使用反射设置私有属性
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
        /// 设置生命值和法力值
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
        /// 设置基础属性
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
        /// 设置职业信息
        /// </summary>
        internal static void SetProfessions(
            CharacterProfessions professions,
            string selectedProfession,
            Dictionary<string, ProfessionLevelDto> battleProfessions,
            Dictionary<string, ProfessionLevelDto> gatheringProfessions,
            Dictionary<string, ProfessionLevelDto> productionProfessions)
        {
            // 设置选定职业
            professions.SelectBattleProfession(selectedProfession);

            // 遍历并设置战斗职业等级和经验
            foreach (var prof in battleProfessions)
            {
                if (professions.BattleProfessions.TryGetValue(prof.Key, out var charProf))
                {
                    SetPrivateProperty(charProf, "Level", prof.Value.Level);
                    SetPrivateProperty(charProf, "Experience", prof.Value.Experience);
                }
            }

            // 遍历并设置采集职业等级和经验
            foreach (var prof in gatheringProfessions)
            {
                if (professions.GatheringProfessions.TryGetValue(prof.Key, out var charProf))
                {
                    SetPrivateProperty(charProf, "Level", prof.Value.Level);
                    SetPrivateProperty(charProf, "Experience", prof.Value.Experience);
                }
            }

            // 遍历并设置生产职业等级和经验
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
        /// 设置物品和装备
        /// </summary>
        internal static void SetInventory(
            CharacterInventory inventory,
            List<InventoryItemDto> items,
            Dictionary<string, string> equipment)
        {
            // 清空现有物品
            var inventoryType = typeof(CharacterInventory);
            var itemsField = inventoryType.GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);
            if (itemsField != null)
            {
                var itemsList = (List<InventoryItem>)itemsField.GetValue(inventory);
                itemsList.Clear();
                
                // 添加物品
                foreach (var item in items)
                {
                    itemsList.Add(new InventoryItem(item.ItemId, item.Quantity));
                }
            }

            // 清空现有装备
            var equipField = inventoryType.GetField("_equippedItems", BindingFlags.Instance | BindingFlags.NonPublic);
            if (equipField != null)
            {
                var equipDict = (Dictionary<string, string>)equipField.GetValue(inventory);
                equipDict.Clear();
                
                // 添加装备
                foreach (var equip in equipment)
                {
                    equipDict[equip.Key] = equip.Value;
                }
            }
        }

        /// <summary>
        /// 设置消耗品装载
        /// </summary>
        internal static void SetConsumableLoadout(
            ConsumableLoadout loadout,
            List<ConsumableSlotDto> generalSlots,
            List<ConsumableSlotDto> combatSlots)
        {
            // 设置通用槽位
            for (int i = 0; i < Math.Min(generalSlots.Count, loadout.GeneralSlots.Count); i++)
            {
                var slotDto = generalSlots[i];
                var slot = loadout.GeneralSlots[i];
                
                Enum.TryParse<UsePolicy>(slotDto.UsePolicy, out var policy);
                loadout.SetConsumable(slot.SlotId, slotDto.ItemId, policy);
                
                SetPrivateProperty(slot, "LastUsedAt", slotDto.LastUsedAt);
            }

            // 设置战斗槽位
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
        /// 设置任务系统
        /// </summary>
        internal static void SetQuests(
            CharacterQuests quests,
            List<string> activeQuestIds,
            List<string> completedQuestIds,
            Dictionary<string, int> questProgress)
        {
            // 接受活跃任务
            foreach (var questId in activeQuestIds)
            {
                quests.AcceptQuest(questId);
                
                // 设置任务进度
                if (questProgress.TryGetValue(questId, out int progress))
                {
                    quests.UpdateQuestProgress(questId, progress);
                }
            }

            // 设置已完成任务
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
        /// 设置技能系统
        /// </summary>
        internal static void SetSkills(
            CharacterSkillManager skillManager,
            Dictionary<string, LearnedSkillDto> learnedSkills,
            Dictionary<string, List<string>> equippedSkills)
        {
            // 使用专用的 SkillMapper 类
            skillManager.RestoreFromDto(learnedSkills, equippedSkills);
        }

        /// <summary>
        /// 设置活动系统
        /// </summary>
        internal static void SetActivitySystem(
            ActivitySystem activitySystem,
            List<ActivitySlotDto> activitySlots)
        {
            // 使用专用的 ActivityMapper 类
            activitySystem.RestoreFromDto(activitySlots);
        }

        /// <summary>
        /// 设置私有属性辅助方法
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