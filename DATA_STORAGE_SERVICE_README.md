# 数据存储服务 (Data Storage Service)

## 概述

数据存储服务是为离线战斗游戏设计的数据管理系统，能够管理玩家、队伍、当前动作目标和战斗记录。该服务采用内存存储方式，便于后续接入数据库。

## 主要功能

### 1. 玩家数据管理
- 玩家基本信息存储（姓名、等级、经验、生命值、金币等）
- 在线状态管理
- 玩家属性、装备、技能数据
- 批量操作支持

### 2. 队伍数据管理
- 队伍创建和管理
- 成员管理（队长、成员列表）
- 队伍状态跟踪

### 3. 动作目标管理
- 当前动作目标跟踪（战斗、采集、制作等）
- 动作进度管理
- 历史动作记录

### 4. 战斗记录管理
- 战斗开始/结束记录
- 参与者和敌人信息
- 战斗结果和奖励记录
- 队伍战斗历史

### 5. 离线数据支持
- 离线模式数据缓存
- 数据同步管理
- 版本控制

## 技术架构

### 核心组件

```
BlazorWebGame.Shared/
├── Models/
│   └── DataStorageModels.cs          # 数据实体模型
├── DTOs/
│   └── DataStorageDTOs.cs           # 数据传输对象
└── Interfaces/
    └── IDataStorageService.cs       # 服务接口

BlazorWebGame.Server/
├── Services/
│   ├── DataStorageService.cs        # 核心数据存储服务
│   └── DataStorageIntegrationService.cs  # 与现有系统集成
├── Controllers/
│   └── DataStorageController.cs     # REST API 控制器
└── Tests/
    └── DataStorageServiceTests.cs   # 单元测试
```

### 数据模型

#### 基础实体类
```csharp
public abstract class BaseEntity
{
    public string Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### 主要实体

1. **PlayerEntity** - 玩家数据实体
2. **TeamEntity** - 队伍数据实体  
3. **ActionTargetEntity** - 动作目标实体
4. **BattleRecordEntity** - 战斗记录实体
5. **OfflineDataEntity** - 离线数据实体

## API 端点

### 玩家管理
- `GET /api/datastorage/players/{playerId}` - 获取玩家数据
- `POST /api/datastorage/players` - 保存玩家数据
- `DELETE /api/datastorage/players/{playerId}` - 删除玩家数据
- `GET /api/datastorage/players/online` - 获取在线玩家列表
- `POST /api/datastorage/players/batch` - 批量保存玩家数据
- `GET /api/datastorage/players/search` - 搜索玩家

### 队伍管理
- `GET /api/datastorage/teams/{teamId}` - 获取队伍数据
- `GET /api/datastorage/teams/captain/{captainId}` - 根据队长获取队伍
- `GET /api/datastorage/teams/player/{playerId}` - 根据玩家获取队伍
- `POST /api/datastorage/teams` - 保存队伍数据
- `DELETE /api/datastorage/teams/{teamId}` - 删除队伍数据
- `GET /api/datastorage/teams/active` - 获取活跃队伍列表

### 动作目标管理
- `GET /api/datastorage/action-targets/current/{playerId}` - 获取当前动作目标
- `POST /api/datastorage/action-targets` - 保存动作目标
- `POST /api/datastorage/action-targets/{actionTargetId}/complete` - 完成动作目标
- `DELETE /api/datastorage/action-targets/current/{playerId}` - 取消动作目标
- `GET /api/datastorage/action-targets/history/{playerId}` - 获取动作历史

### 战斗记录管理
- `GET /api/datastorage/battle-records/{battleId}` - 获取战斗记录
- `POST /api/datastorage/battle-records` - 保存战斗记录
- `POST /api/datastorage/battle-records/{battleId}/end` - 结束战斗记录
- `GET /api/datastorage/battle-records/player/{playerId}` - 获取玩家战斗历史
- `GET /api/datastorage/battle-records/team/{teamId}` - 获取队伍战斗历史
- `GET /api/datastorage/battle-records/active` - 获取进行中的战斗

### 系统管理
- `GET /api/datastorage/stats` - 获取存储统计信息
- `GET /api/datastorage/health` - 健康检查
- `GET /api/datastorage/export/player/{playerId}` - 导出玩家数据
- `POST /api/datastorage/backup` - 数据备份
- `DELETE /api/datastorage/cleanup` - 清理过期数据

## 使用示例

### 1. 保存玩家数据

```csharp
var playerDto = new PlayerStorageDto
{
    Id = "player123",
    Name = "TestPlayer",
    Level = 10,
    Experience = 5000,
    Health = 100,
    MaxHealth = 120,
    Gold = 1000,
    SelectedBattleProfession = "Warrior",
    IsOnline = true
};

var result = await dataStorageService.SavePlayerAsync(playerDto);
if (result.Success)
{
    Console.WriteLine($"Player saved: {result.Data.Id}");
}
```

### 2. 记录战斗

```csharp
// 记录战斗开始
var battleRecordId = await integrationService.RecordBattleStartAsync(
    battleId: "battle_001",
    participantIds: new List<string> { "player123" },
    battleType: "Normal"
);

// 记录战斗结束
var success = await integrationService.RecordBattleEndAsync(
    battleId: "battle_001",
    status: "Victory",
    results: new Dictionary<string, object> 
    { 
        ["xpGained"] = 500,
        ["goldEarned"] = 100 
    }
);
```

### 3. 管理动作目标

```csharp
// 设置玩家动作目标
await integrationService.SetPlayerActionTargetAsync(
    playerId: "player123",
    targetType: "Enemy",
    targetId: "goblin_001",
    targetName: "Forest Goblin",
    actionType: "Combat",
    duration: 30.0
);

// 清除动作目标
await integrationService.ClearPlayerActionTargetAsync("player123");
```

## 安全特性

### 日志安全
- 防止日志注入攻击
- 用户输入数据安全过滤
- 敏感信息脱敏

### 数据验证
- 输入参数验证
- 数据完整性检查
- 错误处理机制

## 性能优化

### 内存管理
- 使用 `ConcurrentDictionary` 确保线程安全
- 索引优化提高查询性能
- 数据清理机制防止内存泄漏

### 查询优化
- 玩家到队伍映射索引
- 队长到队伍映射索引
- 玩家动作目标索引
- 玩家战斗记录索引

## 数据库集成准备

当前使用内存存储，但设计上已为数据库集成做好准备：

1. **实体模型** - 遵循 Entity Framework 规范
2. **DTO 分离** - 清晰的数据传输层
3. **接口抽象** - 便于切换实现
4. **异步操作** - 支持数据库异步访问

### 数据库迁移建议

```csharp
// Entity Framework 配置示例
public class GameDbContext : DbContext
{
    public DbSet<PlayerEntity> Players { get; set; }
    public DbSet<TeamEntity> Teams { get; set; }
    public DbSet<ActionTargetEntity> ActionTargets { get; set; }
    public DbSet<BattleRecordEntity> BattleRecords { get; set; }
    public DbSet<OfflineDataEntity> OfflineData { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 配置实体关系和约束
        modelBuilder.Entity<PlayerEntity>()
            .HasIndex(p => p.Id)
            .IsUnique();
            
        // 其他配置...
    }
}
```

## 监控和运维

### 健康检查
```csharp
var healthResult = await dataStorageService.HealthCheckAsync();
// 返回系统状态、内存使用情况、索引健康度等信息
```

### 统计信息
```csharp
var stats = await dataStorageService.GetStorageStatsAsync();
// 返回玩家数、队伍数、战斗记录数等统计信息
```

### 数据清理
```csharp
// 清理30天前的过期数据
var cleanedCount = await dataStorageService.CleanupExpiredDataAsync(TimeSpan.FromDays(30));
```

## 测试

### 运行测试
服务包含完整的单元测试，会在开发环境自动运行：

```csharp
// 在 Program.cs 中自动执行
await BlazorWebGame.Server.Tests.DataStorageServiceTests.RunBasicTests(logger);
```

### 测试覆盖
- 玩家数据 CRUD 操作
- 队伍管理功能
- 动作目标生命周期
- 战斗记录管理
- 系统统计和健康检查

## 最佳实践

1. **错误处理** - 所有操作都有完整的错误处理和日志记录
2. **并发安全** - 使用线程安全的数据结构
3. **资源清理** - 定期清理过期数据防止内存泄漏
4. **监控告警** - 通过健康检查接口监控服务状态
5. **数据备份** - 支持数据导出和备份功能

## 扩展计划

1. **分布式缓存** - Redis 集成
2. **数据库支持** - PostgreSQL/MySQL 集成
3. **消息队列** - 异步处理优化
4. **读写分离** - 提高性能
5. **数据分片** - 支持大规模部署

---

**注意**: 当前版本使用内存存储，重启服务会丢失数据。生产部署时建议接入持久化存储。