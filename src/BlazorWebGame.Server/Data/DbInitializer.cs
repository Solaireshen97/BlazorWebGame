using Microsoft.EntityFrameworkCore;

namespace BlazorWebGame.Server.Data;

/// <summary>
/// 数据库初始化服务
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// 初始化数据库（应用迁移并种子数据）
    /// </summary>
    public static async Task InitializeAsync(GameDbContext context, ILogger logger, bool seedData = true)
    {
        try
        {
            // 确保数据库已创建
            await context.Database.EnsureCreatedAsync();

            // 应用任何挂起的迁移
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations", pendingMigrations.Count());
                await context.Database.MigrateAsync();
            }

            // 种子数据
            if (seedData && !await context.Players.AnyAsync())
            {
                await SeedDataAsync(context, logger);
            }

            logger.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing database");
            throw;
        }
    }

    /// <summary>
    /// 种子测试数据
    /// </summary>
    private static async Task SeedDataAsync(GameDbContext context, ILogger logger)
    {
        logger.LogInformation("Seeding database with test data");

        // 创建测试玩家
        var testPlayers = new[]
        {
            new PlayerDbEntity
            {
                Id = "player_001",
                Username = "TestPlayer1",
                Email = "test1@blazorwebgame.com",
                PasswordHash = "hashed_password_123", // 在实际应用中应该是真正的哈希密码
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow.AddMinutes(-30),
                IsActive = true,
                MetadataJson = new Dictionary<string, object>
                {
                    ["preferredLanguage"] = "zh-CN",
                    ["theme"] = "dark",
                    ["lastLocation"] = "starter_village"
                }
            },
            new PlayerDbEntity
            {
                Id = "player_002",
                Username = "TestPlayer2",
                Email = "test2@blazorwebgame.com",
                PasswordHash = "hashed_password_456",
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                LastLoginAt = DateTime.UtcNow.AddHours(-2),
                IsActive = true,
                MetadataJson = new Dictionary<string, object>
                {
                    ["preferredLanguage"] = "en-US",
                    ["theme"] = "light",
                    ["lastLocation"] = "forest_camp"
                }
            }
        };

        context.Players.AddRange(testPlayers);

        // 创建测试角色
        var testCharacters = new[]
        {
            new CharacterDbEntity
            {
                Id = "char_001",
                PlayerId = "player_001",
                Name = "勇敢的战士",
                CharacterClass = "Warrior",
                Level = 5,
                Experience = 1250,
                Gold = 500,
                Health = 120,
                MaxHealth = 120,
                Mana = 30,
                MaxMana = 30,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                LastActiveAt = DateTime.UtcNow.AddMinutes(-15),
                IsDead = false,
                AttributesJson = new Dictionary<string, int>
                {
                    ["strength"] = 15,
                    ["agility"] = 10,
                    ["intelligence"] = 8,
                    ["vitality"] = 12
                },
                SkillsJson = new List<string> { "sword_mastery", "shield_bash", "berserker_rage" }
            },
            new CharacterDbEntity
            {
                Id = "char_002",
                PlayerId = "player_001",
                Name = "神秘法师",
                CharacterClass = "Mage",
                Level = 3,
                Experience = 800,
                Gold = 300,
                Health = 80,
                MaxHealth = 80,
                Mana = 100,
                MaxMana = 100,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                LastActiveAt = DateTime.UtcNow.AddHours(-1),
                IsDead = false,
                AttributesJson = new Dictionary<string, int>
                {
                    ["strength"] = 6,
                    ["agility"] = 8,
                    ["intelligence"] = 18,
                    ["vitality"] = 10
                },
                SkillsJson = new List<string> { "fireball", "ice_bolt", "mana_shield" }
            },
            new CharacterDbEntity
            {
                Id = "char_003",
                PlayerId = "player_002",
                Name = "敏捷弓手",
                CharacterClass = "Archer",
                Level = 4,
                Experience = 950,
                Gold = 400,
                Health = 90,
                MaxHealth = 90,
                Mana = 50,
                MaxMana = 50,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                LastActiveAt = DateTime.UtcNow.AddMinutes(-45),
                IsDead = false,
                AttributesJson = new Dictionary<string, int>
                {
                    ["strength"] = 10,
                    ["agility"] = 16,
                    ["intelligence"] = 12,
                    ["vitality"] = 11
                },
                SkillsJson = new List<string> { "precise_shot", "multi_arrow", "eagle_eye" }
            }
        };

        context.Characters.AddRange(testCharacters);

        // 创建测试队伍
        var testTeam = new TeamDbEntity
        {
            Id = "team_001",
            Name = "勇者小队",
            CaptainId = "char_001",
            Description = "一个致力于冒险和探索的勇敢队伍",
            MaxMembers = 4,
            IsPublic = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            MemberIdsJson = new List<string> { "char_001", "char_002" }
        };

        context.Teams.Add(testTeam);

        // 创建测试背包物品
        var testInventoryItems = new[]
        {
            new InventoryItemDbEntity
            {
                Id = Guid.NewGuid(),
                CharacterId = "char_001",
                ItemId = "health_potion",
                ItemName = "生命药水",
                ItemType = "Consumable",
                Rarity = "Common",
                Quantity = 5,
                SlotPosition = 0,
                IsStackable = true,
                AcquiredAt = DateTime.UtcNow.AddDays(-2),
                PropertiesJson = new Dictionary<string, object>
                {
                    ["healAmount"] = 50,
                    ["description"] = "恢复50点生命值的药水"
                }
            },
            new InventoryItemDbEntity
            {
                Id = Guid.NewGuid(),
                CharacterId = "char_001",
                ItemId = "iron_sword",
                ItemName = "铁剑",
                ItemType = "Weapon",
                Rarity = "Common",
                Quantity = 1,
                SlotPosition = 1,
                IsStackable = false,
                AcquiredAt = DateTime.UtcNow.AddDays(-3),
                PropertiesJson = new Dictionary<string, object>
                {
                    ["damage"] = 25,
                    ["durability"] = 100,
                    ["description"] = "一把普通的铁制长剑"
                }
            }
        };

        context.InventoryItems.AddRange(testInventoryItems);

        // 创建测试装备
        var testEquipment = new[]
        {
            new EquipmentDbEntity
            {
                Id = Guid.NewGuid(),
                CharacterId = "char_001",
                Slot = "MainHand",
                ItemId = "iron_sword",
                EquippedAt = DateTime.UtcNow.AddDays(-3)
            },
            new EquipmentDbEntity
            {
                Id = Guid.NewGuid(),
                CharacterId = "char_002",
                Slot = "MainHand",
                ItemId = "wooden_staff",
                EquippedAt = DateTime.UtcNow.AddDays(-2)
            }
        };

        context.Equipment.AddRange(testEquipment);

        // 创建测试任务
        var testQuests = new[]
        {
            new QuestDbEntity
            {
                Id = Guid.NewGuid(),
                CharacterId = "char_001",
                QuestId = "beginner_training",
                QuestName = "新手训练",
                Status = "Active",
                AcceptedAt = DateTime.UtcNow.AddDays(-2),
                ProgressJson = new Dictionary<string, object>
                {
                    ["enemiesKilled"] = 3,
                    ["targetKills"] = 5,
                    ["description"] = "击败5个哥布林来完成训练"
                }
            },
            new QuestDbEntity
            {
                Id = Guid.NewGuid(),
                CharacterId = "char_002",
                QuestId = "magic_studies",
                QuestName = "魔法研究",
                Status = "Completed",
                AcceptedAt = DateTime.UtcNow.AddDays(-1),
                CompletedAt = DateTime.UtcNow.AddHours(-6),
                ProgressJson = new Dictionary<string, object>
                {
                    ["spellsCast"] = 10,
                    ["targetCasts"] = 10,
                    ["description"] = "施放10个火球术"
                }
            }
        };

        context.Quests.AddRange(testQuests);

        // 创建测试战斗记录
        var testBattleRecords = new[]
        {
            new BattleRecordDbEntity
            {
                Id = Guid.NewGuid(),
                CharacterId = "char_001",
                EnemyId = "goblin_warrior",
                BattleType = "PvE",
                StartTime = DateTime.UtcNow.AddHours(-2),
                EndTime = DateTime.UtcNow.AddHours(-2).AddMinutes(3),
                Duration = TimeSpan.FromMinutes(3),
                Result = "Victory",
                RewardsJson = new Dictionary<string, object>
                {
                    ["experience"] = 120,
                    ["gold"] = 50,
                    ["items"] = new[] { "health_potion" }
                }
            },
            new BattleRecordDbEntity
            {
                Id = Guid.NewGuid(),
                CharacterId = "char_002",
                EnemyId = "forest_slime",
                BattleType = "PvE",
                StartTime = DateTime.UtcNow.AddHours(-1),
                EndTime = DateTime.UtcNow.AddHours(-1).AddMinutes(2),
                Duration = TimeSpan.FromMinutes(2),
                Result = "Victory",
                RewardsJson = new Dictionary<string, object>
                {
                    ["experience"] = 80,
                    ["gold"] = 30,
                    ["items"] = new[] { "mana_potion" }
                }
            }
        };

        context.BattleRecords.AddRange(testBattleRecords);

        // 保存所有变更
        await context.SaveChangesAsync();

        logger.LogInformation("Successfully seeded database with test data: {PlayerCount} players, {CharacterCount} characters, {TeamCount} teams",
            testPlayers.Length, testCharacters.Length, 1);
    }
}