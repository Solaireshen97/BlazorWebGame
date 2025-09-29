# BlazorWebGame 数据存储架构完整分析与优化方案

## 目录
1. [架构概览](#1-架构概览)
2. [客户端数据存储分析](#2-客户端数据存储分析)
3. [服务端数据存储分析](#3-服务端数据存储分析)
4. [问题识别与分析](#4-问题识别与分析)
5. [优化建议与方案](#5-优化建议与方案)
6. [实施步骤指南](#6-实施步骤指南)
7. [最佳实践建议](#7-最佳实践建议)

---

## 1. 架构概览

### 1.1 当前技术栈
- **客户端存储**: Browser localStorage (通过 IJSRuntime)
- **服务端数据库**: SQLite
- **ORM框架**: Entity Framework Core 8.0
- **API框架**: ASP.NET Core 8.0
- **缓存系统**: IMemoryCache
- **通信协议**: SignalR + HTTP API

### 1.2 架构层次结构
```
┌─────────────────────────────────────────────────────────────┐
│                    Blazor Client                             │
├─────────────────────────────────────────────────────────────┤
│ GameStorage (localStorage) ←→ OfflineService                │
│           ↓                                                 │
│ GameStateService ←→ Various Service Classes                 │
└─────────────────────────────────────────────────────────────┘
                            ↑↓ HTTP/SignalR
┌─────────────────────────────────────────────────────────────┐
│                    Server APIs                              │
├─────────────────────────────────────────────────────────────┤
│ Controllers ←→ Business Services                            │
│           ↓                                                 │
│ DataStorageService ←→ SqliteDataStorageService              │
│           ↓                                                 │
│ GameDbContext (Entity Framework)                            │
│           ↓                                                 │
│ SQLite Database                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. 客户端数据存储分析

### 2.1 GameStorage 实现分析

**位置**: `src/BlazorWebGame.Client/Utils/GameStorage.cs`

**功能特点**:
- 基于浏览器 localStorage
- JSON序列化存储
- 简单的错误处理
- 只支持玩家数据存储

**关键代码分析**:
```csharp
public class GameStorage
{
    private readonly IJSRuntime _js;
    private const string PlayerDataKey = "mygame-player-data";
    
    // 优点：简单直接
    public async Task<T?> GetItemAsync<T>(string key)
    {
        var json = await _js.InvokeAsync<string>("localStorage.getItem", key);
        return JsonSerializer.Deserialize<T>(json);
    }
    
    // 缺点：错误处理过于简单，只输出到Console
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading data: {ex.Message}");
        return default;
    }
}
```

**存在问题**:
1. **存储容量限制**: localStorage 通常限制在 5-10MB
2. **数据类型单一**: 只支持玩家数据，缺乏扩展性
3. **错误处理不完善**: 异常只记录到控制台
4. **缺乏版本控制**: 无法处理数据结构变更
5. **安全性不足**: 明文存储敏感数据

### 2.2 OfflineService 分析

**位置**: `src/BlazorWebGame.Client/Services/Api/OfflineService.cs`

**当前状态**: 已被简化为存根类，不再支持离线功能

**问题分析**:
- 离线功能被完全移除，影响用户体验
- 网络中断时用户无法继续游戏
- 缺乏数据同步机制

---

## 3. 服务端数据存储分析

### 3.1 数据库设计分析

**位置**: `src/BlazorWebGame.Server/Data/GameDbContext.cs`

**实体模型**:
```csharp
public DbSet<PlayerEntity> Players { get; set; }
public DbSet<TeamEntity> Teams { get; set; }
public DbSet<ActionTargetEntity> ActionTargets { get; set; }
public DbSet<BattleRecordEntity> BattleRecords { get; set; }
public DbSet<OfflineDataEntity> OfflineData { get; set; }
```

**设计优点**:
1. 清晰的实体分离
2. 合理的索引设计
3. JSON字段支持复杂数据结构
4. 审计字段完整 (CreatedAt, UpdatedAt)

**设计问题**:
1. **JSON字段过度使用**: 影响查询性能和数据完整性
2. **缺乏外键约束**: 数据一致性风险
3. **无分区策略**: 大数据量时性能问题
4. **缺乏软删除**: 数据恢复困难

### 3.2 数据访问层分析

**多重实现并存**:
- `DataStorageService.cs` - 内存存储实现
- `SqliteDataStorageService.cs` - SQLite实现 
- `ConsolidatedDataStorageService.cs` - 统一实现
- `UnifiedDataStorageService.cs` - 另一个统一实现

**问题分析**:
1. **实现重复**: 多个相似的存储服务类
2. **接口不统一**: 不同实现之间存在差异
3. **生命周期混乱**: Singleton vs Scoped 服务混用
4. **缺乏抽象**: 业务逻辑与数据访问耦合

### 3.3 配置管理分析

**位置**: `src/BlazorWebGame.Server/appsettings.json`

**配置结构**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=gamedata.db;Cache=Shared"
  },
  "ConsolidatedDataStorage": {
    "StorageType": "SQLite",
    "EnableCaching": true,
    "EnableBatchOperations": true,
    "SqliteOptimization": {
      "EnableWALMode": true,
      "CacheSize": 10000
    }
  }
}
```

**优点**:
- 配置项齐全
- 支持性能优化参数
- 环境特定配置

**缺点**:
- 配置过于复杂
- 缺乏配置验证
- 敏感信息未加密

---

## 4. 问题识别与分析

### 4.1 架构层面问题

#### 4.1.1 数据一致性问题
- **客户端与服务端数据同步**: 缺乏有效的数据同步机制
- **并发控制**: 多用户同时操作时缺乏冲突解决
- **事务管理**: 复杂操作缺乏事务保护

#### 4.1.2 性能问题
- **N+1查询问题**: Entity Framework 查询未优化
- **缓存策略不当**: 缓存粒度和策略需要优化
- **JSON字段查询**: 无法有效利用数据库索引

#### 4.1.3 可扩展性问题
- **单数据库限制**: SQLite 不支持高并发写入
- **存储容量限制**: localStorage 和 SQLite 都有容量限制
- **水平扩展困难**: 缺乏分布式架构设计

### 4.2 代码质量问题

#### 4.2.1 重复代码
```csharp
// 在多个服务中都有相似的映射代码
private PlayerStorageDto MapToDto(PlayerEntity entity) { ... }
private PlayerEntity MapToEntity(PlayerStorageDto dto) { ... }
```

#### 4.2.2 错误处理不一致
```csharp
// 有些地方只记录到控制台
Console.WriteLine($"Error: {ex.Message}");

// 有些地方使用日志框架
_logger.LogError(ex, "Error occurred");
```

#### 4.2.3 缺乏单元测试
- 大部分数据访问代码缺乏单元测试
- 集成测试覆盖率低
- 缺乏性能测试

### 4.3 安全问题

#### 4.3.1 SQL注入风险
```csharp
// 存在SQL注入风险的代码
await _context.Database.ExecuteSqlRawAsync($"DELETE FROM {tableName}");
```

#### 4.3.2 数据验证不足
- 缺乏输入数据验证
- 没有数据脱敏机制
- 敏感数据明文存储

---

## 5. 优化建议与方案

### 5.1 架构优化方案

#### 5.1.1 采用CQRS模式
```csharp
// 命令和查询分离
public interface IPlayerCommandService
{
    Task<Result> CreatePlayerAsync(CreatePlayerCommand command);
    Task<Result> UpdatePlayerAsync(UpdatePlayerCommand command);
}

public interface IPlayerQueryService  
{
    Task<PlayerDto> GetPlayerAsync(string playerId);
    Task<List<PlayerDto>> GetPlayersAsync(PlayerQuery query);
}
```

#### 5.1.2 实现Repository模式
```csharp
public interface IPlayerRepository
{
    Task<Player> GetByIdAsync(string id);
    Task<Player> SaveAsync(Player player);
    Task<bool> DeleteAsync(string id);
}

public class PlayerRepository : IPlayerRepository
{
    private readonly GameDbContext _context;
    private readonly IMemoryCache _cache;
    
    // 实现缓存策略和数据访问逻辑
}
```

#### 5.1.3 引入领域驱动设计(DDD)
```csharp
// 领域实体
public class Player : AggregateRoot
{
    public PlayerId Id { get; private set; }
    public PlayerName Name { get; private set; }
    public Level Level { get; private set; }
    
    // 业务逻辑封装在领域对象中
    public void LevelUp(Experience experience)
    {
        if (CanLevelUp(experience))
        {
            Level = Level.Increase();
            AddDomainEvent(new PlayerLeveledUpEvent(Id, Level));
        }
    }
}
```

### 5.2 数据库优化方案

#### 5.2.1 数据模型重构
```sql
-- 拆分JSON字段为具体列
CREATE TABLE PlayerAttributes (
    PlayerId TEXT NOT NULL,
    AttributeType TEXT NOT NULL,
    Value INTEGER NOT NULL,
    PRIMARY KEY (PlayerId, AttributeType),
    FOREIGN KEY (PlayerId) REFERENCES Players(Id)
);

-- 添加外键约束
ALTER TABLE Teams ADD CONSTRAINT FK_Teams_Players 
    FOREIGN KEY (CaptainId) REFERENCES Players(Id);
```

#### 5.2.2 索引优化
```sql
-- 复合索引优化查询
CREATE INDEX IX_Players_Level_IsOnline ON Players(Level, IsOnline);
CREATE INDEX IX_BattleRecords_Status_StartedAt ON BattleRecords(Status, StartedAt);

-- 部分索引减少存储开销
CREATE INDEX IX_ActionTargets_Active ON ActionTargets(PlayerId, StartedAt) 
WHERE IsCompleted = 0;
```

#### 5.2.3 分区策略
```csharp
// 按时间分区战斗记录
public class BattleRecordPartitionService
{
    public async Task CreateMonthlyPartition(DateTime month)
    {
        var tableName = $"BattleRecords_{month:yyyyMM}";
        await _context.Database.ExecuteSqlRawAsync($@"
            CREATE TABLE {tableName} (LIKE BattleRecords INCLUDING ALL);
            ALTER TABLE {tableName} ADD CONSTRAINT check_date 
            CHECK (StartedAt >= '{month:yyyy-MM-01}' AND StartedAt < '{month.AddMonths(1):yyyy-MM-01}');
        ");
    }
}
```

### 5.3 缓存优化方案

#### 5.3.1 多层缓存架构
```csharp
public class MultiLevelCacheService
{
    private readonly IMemoryCache _l1Cache;        // 内存缓存
    private readonly IDistributedCache _l2Cache;   // Redis缓存
    private readonly IRepository _repository;      // 数据库
    
    public async Task<T> GetAsync<T>(string key)
    {
        // L1缓存
        if (_l1Cache.TryGetValue(key, out T value))
            return value;
            
        // L2缓存  
        var cachedValue = await _l2Cache.GetAsync(key);
        if (cachedValue != null)
        {
            value = JsonSerializer.Deserialize<T>(cachedValue);
            _l1Cache.Set(key, value, TimeSpan.FromMinutes(5));
            return value;
        }
        
        // 数据库
        value = await _repository.GetAsync<T>(key);
        if (value != null)
        {
            await _l2Cache.SetAsync(key, JsonSerializer.Serialize(value));
            _l1Cache.Set(key, value, TimeSpan.FromMinutes(5));
        }
        
        return value;
    }
}
```

#### 5.3.2 缓存策略优化
```csharp
public class CachePolicy
{
    public static CacheEntryOptions GetPlayerCacheOptions()
    {
        return new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            SlidingExpiration = TimeSpan.FromMinutes(10),
            Priority = CacheItemPriority.High,
            Size = 1
        };
    }
    
    public static CacheEntryOptions GetBattleRecordCacheOptions()
    {
        return new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            Priority = CacheItemPriority.Low,
            Size = 2
        };
    }
}
```

### 5.4 客户端存储优化

#### 5.4.1 IndexedDB替代localStorage
```csharp
public class IndexedDbStorage
{
    private readonly IJSRuntime _js;
    
    public async Task<T> GetAsync<T>(string storeName, string key)
    {
        var result = await _js.InvokeAsync<string>("indexedDbHelper.get", storeName, key);
        return JsonSerializer.Deserialize<T>(result);
    }
    
    public async Task SetAsync<T>(string storeName, string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        await _js.InvokeVoidAsync("indexedDbHelper.set", storeName, key, json);
    }
}
```

#### 5.4.2 数据同步机制
```csharp
public class DataSyncService
{
    private readonly IndexedDbStorage _localStorage;
    private readonly GameApiService _apiService;
    
    public async Task SyncToServer()
    {
        var localData = await _localStorage.GetAsync<PlayerData>("players", "current");
        var serverData = await _apiService.GetPlayerAsync(localData.Id);
        
        if (localData.Version > serverData.Version)
        {
            // 本地更新，推送到服务器
            await _apiService.UpdatePlayerAsync(localData);
        }
        else if (serverData.Version > localData.Version)
        {
            // 服务器更新，拉取到本地
            await _localStorage.SetAsync("players", "current", serverData);
        }
    }
}
```

---

## 6. 实施步骤指南

### 第一阶段: 代码重构 (1-2周)

#### 步骤1: 创建统一接口
```bash
# 1. 创建数据访问接口
mkdir src/BlazorWebGame.Shared/Interfaces/Repositories
touch src/BlazorWebGame.Shared/Interfaces/Repositories/IPlayerRepository.cs
touch src/BlazorWebGame.Shared/Interfaces/Repositories/ITeamRepository.cs
```

#### 步骤2: 实现Repository模式
```bash
# 2. 创建Repository实现
mkdir src/BlazorWebGame.Server/Repositories
touch src/BlazorWebGame.Server/Repositories/PlayerRepository.cs
touch src/BlazorWebGame.Server/Repositories/TeamRepository.cs
```

#### 步骤3: 重构服务层
```bash
# 3. 统一服务实现
rm src/BlazorWebGame.Server/Services/DataStorageService.cs
rm src/BlazorWebGame.Server/Services/UnifiedDataStorageService.cs
# 保留 ConsolidatedDataStorageService.cs，作为主要实现
```

### 第二阶段: 数据库优化 (2-3周)

#### 步骤1: 数据模型重构
```csharp
// 创建迁移脚本
public partial class RefactorPlayerAttributes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 创建PlayerAttributes表
        migrationBuilder.CreateTable(
            name: "PlayerAttributes",
            columns: table => new
            {
                PlayerId = table.Column<string>(nullable: false),
                AttributeType = table.Column<string>(nullable: false),
                Value = table.Column<int>(nullable: false)
            });
            
        // 迁移数据
        migrationBuilder.Sql(@"
            INSERT INTO PlayerAttributes (PlayerId, AttributeType, Value)
            SELECT Id, 'Strength', JSON_EXTRACT(AttributesJson, '$.Strength')
            FROM Players WHERE JSON_EXTRACT(AttributesJson, '$.Strength') IS NOT NULL
        ");
    }
}
```

#### 步骤2: 索引优化
```sql
-- 执行索引优化脚本
-- indexes_optimization.sql

-- 删除冗余索引
DROP INDEX IF EXISTS IX_Players_Name;

-- 创建复合索引
CREATE INDEX IX_Players_Composite ON Players(Level, IsOnline, LastActiveAt);
CREATE INDEX IX_BattleRecords_Composite ON BattleRecords(Status, BattleType, StartedAt);

-- 创建部分索引
CREATE INDEX IX_ActionTargets_Active ON ActionTargets(PlayerId, StartedAt) 
WHERE IsCompleted = 0;
```

#### 步骤3: 查询优化
```csharp
// 使用预编译查询
public static class CompiledQueries
{
    public static readonly Func<GameDbContext, string, Task<Player>> GetPlayerById =
        EF.CompileAsyncQuery((GameDbContext context, string id) =>
            context.Players.Where(p => p.Id == id).First());
            
    public static readonly Func<GameDbContext, int, IAsyncEnumerable<Player>> GetOnlinePlayers =
        EF.CompileAsyncQuery((GameDbContext context, int limit) =>
            context.Players.Where(p => p.IsOnline).Take(limit));
}
```

### 第三阶段: 缓存系统实现 (1-2周)

#### 步骤1: 配置Redis
```bash
# 1. 安装Redis包
dotnet add src/BlazorWebGame.Server package Microsoft.Extensions.Caching.StackExchangeRedis

# 2. 配置连接字符串
# appsettings.json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

#### 步骤2: 实现缓存服务
```csharp
// 注册服务
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

builder.Services.AddScoped<ICacheService, RedisCacheService>();
```

#### 步骤3: 集成缓存到Repository
```csharp
public class CachedPlayerRepository : IPlayerRepository
{
    private readonly IPlayerRepository _repository;
    private readonly ICacheService _cache;
    
    public async Task<Player> GetByIdAsync(string id)
    {
        var cacheKey = $"player:{id}";
        var cached = await _cache.GetAsync<Player>(cacheKey);
        
        if (cached != null)
            return cached;
            
        var player = await _repository.GetByIdAsync(id);
        if (player != null)
        {
            await _cache.SetAsync(cacheKey, player, TimeSpan.FromMinutes(30));
        }
        
        return player;
    }
}
```

### 第四阶段: 客户端优化 (1-2周)

#### 步骤1: 实现IndexedDB存储
```javascript
// wwwroot/js/indexeddb-helper.js
window.indexedDbHelper = {
    async open(dbName, version) {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(dbName, version);
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    },
    
    async get(storeName, key) {
        const db = await this.open('BlazorWebGame', 1);
        const transaction = db.transaction([storeName], 'readonly');
        const store = transaction.objectStore(storeName);
        
        return new Promise((resolve, reject) => {
            const request = store.get(key);
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    }
};
```

#### 步骤2: 实现数据同步
```csharp
public class OfflineDataSyncService
{
    private readonly IndexedDbStorage _localStorage;
    private readonly GameApiService _apiService;
    private readonly ILogger<OfflineDataSyncService> _logger;
    
    public async Task<SyncResult> SyncPlayerData()
    {
        try
        {
            var localPlayer = await _localStorage.GetPlayerAsync();
            var serverPlayer = await _apiService.GetPlayerAsync(localPlayer.Id);
            
            var result = await ResolveConflicts(localPlayer, serverPlayer);
            
            if (result.LocalWins)
            {
                await _apiService.UpdatePlayerAsync(localPlayer);
                _logger.LogInformation("本地数据已同步到服务器");
            }
            else if (result.ServerWins)
            {
                await _localStorage.SavePlayerAsync(serverPlayer);
                _logger.LogInformation("服务器数据已同步到本地");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据同步失败");
            return SyncResult.Failed(ex.Message);
        }
    }
}
```

### 第五阶段: 监控与测试 (1周)

#### 步骤1: 添加性能监控
```csharp
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        await _next(context);
        
        stopwatch.Stop();
        
        if (stopwatch.ElapsedMilliseconds > 1000) // 超过1秒的请求
        {
            _logger.LogWarning("慢请求: {Path} 耗时 {ElapsedMs}ms", 
                context.Request.Path, stopwatch.ElapsedMilliseconds);
        }
    }
}
```

#### 步骤2: 实现健康检查
```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly GameDbContext _context;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            return HealthCheckResult.Healthy("数据库连接正常");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("数据库连接失败", ex);
        }
    }
}
```

#### 步骤3: 编写集成测试
```csharp
[TestClass]
public class PlayerRepositoryIntegrationTests
{
    private GameDbContext _context;
    private IPlayerRepository _repository;
    
    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new GameDbContext(options);
        _repository = new PlayerRepository(_context);
    }
    
    [TestMethod]
    public async Task SavePlayer_ShouldPersistToDatabase()
    {
        // Arrange
        var player = new Player { Id = "test", Name = "Test Player" };
        
        // Act
        await _repository.SaveAsync(player);
        
        // Assert
        var saved = await _repository.GetByIdAsync("test");
        Assert.IsNotNull(saved);
        Assert.AreEqual("Test Player", saved.Name);
    }
}
```

---

## 7. 最佳实践建议

### 7.1 代码规范

#### 7.1.1 命名约定
```csharp
// 接口命名
public interface IPlayerRepository { }     // ✅ 正确
public interface PlayerRepository { }      // ❌ 错误

// 异步方法命名
public async Task<Player> GetPlayerAsync(string id) { }  // ✅ 正确
public async Task<Player> GetPlayer(string id) { }       // ❌ 错误

// 常量命名
public const string PLAYER_CACHE_KEY = "player:{0}";     // ✅ 正确
public const string playerCacheKey = "player:{0}";       // ❌ 错误
```

#### 7.1.2 错误处理
```csharp
// 统一错误处理
public async Task<Result<Player>> GetPlayerAsync(string id)
{
    try
    {
        var player = await _repository.GetByIdAsync(id);
        return Result<Player>.Success(player);
    }
    catch (EntityNotFoundException ex)
    {
        _logger.LogWarning(ex, "Player not found: {PlayerId}", id);
        return Result<Player>.NotFound("玩家不存在");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get player: {PlayerId}", id);
        return Result<Player>.Error("获取玩家数据失败");
    }
}
```

#### 7.1.3 配置管理
```csharp
// 强类型配置
public class DatabaseOptions
{
    public const string SectionName = "Database";
    
    [Required]
    public string ConnectionString { get; set; }
    
    [Range(1, 100)]
    public int CommandTimeout { get; set; } = 30;
    
    public bool EnableRetry { get; set; } = true;
}

// 配置验证
builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### 7.2 性能优化

#### 7.2.1 查询优化
```csharp
// 使用AsNoTracking提高查询性能
public async Task<List<PlayerSummary>> GetPlayerSummariesAsync()
{
    return await _context.Players
        .AsNoTracking()
        .Select(p => new PlayerSummary
        {
            Id = p.Id,
            Name = p.Name,
            Level = p.Level
        })
        .ToListAsync();
}

// 预加载相关数据避免N+1问题
public async Task<List<Player>> GetPlayersWithTeamsAsync()
{
    return await _context.Players
        .Include(p => p.Team)
        .ThenInclude(t => t.Members)
        .ToListAsync();
}
```

#### 7.2.2 缓存策略
```csharp
// 缓存装饰器模式
public class CachedPlayerService : IPlayerService
{
    private readonly IPlayerService _playerService;
    private readonly IMemoryCache _cache;
    
    public async Task<Player> GetPlayerAsync(string id)
    {
        var cacheKey = $"player-{id}";
        
        if (_cache.TryGetValue(cacheKey, out Player cachedPlayer))
        {
            return cachedPlayer;
        }
        
        var player = await _playerService.GetPlayerAsync(id);
        
        _cache.Set(cacheKey, player, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            Priority = CacheItemPriority.High
        });
        
        return player;
    }
}
```

### 7.3 安全性

#### 7.3.1 输入验证
```csharp
// 使用FluentValidation
public class CreatePlayerValidator : AbstractValidator<CreatePlayerRequest>
{
    public CreatePlayerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("玩家名称不能为空")
            .Length(2, 20).WithMessage("玩家名称长度必须在2-20字符之间")
            .Matches(@"^[a-zA-Z0-9\u4e00-\u9fa5]+$").WithMessage("玩家名称只能包含字母、数字和中文");
            
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("邮箱不能为空")
            .EmailAddress().WithMessage("邮箱格式不正确");
    }
}
```

#### 7.3.2 数据脱敏
```csharp
public class PlayerDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    
    [JsonIgnore]
    internal string Email { get; set; }
    
    // 脱敏邮箱显示
    public string MaskedEmail => 
        string.IsNullOrEmpty(Email) ? "" : 
        $"{Email.Substring(0, 2)}***@{Email.Split('@')[1]}";
}
```

### 7.4 测试策略

#### 7.4.1 单元测试
```csharp
[TestClass]
public class PlayerServiceTests
{
    private Mock<IPlayerRepository> _mockRepository;
    private Mock<ILogger<PlayerService>> _mockLogger;
    private PlayerService _service;
    
    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IPlayerRepository>();
        _mockLogger = new Mock<ILogger<PlayerService>>();
        _service = new PlayerService(_mockRepository.Object, _mockLogger.Object);
    }
    
    [TestMethod]
    public async Task GetPlayer_WhenPlayerExists_ReturnsPlayer()
    {
        // Arrange
        var playerId = "test-player";
        var expectedPlayer = new Player { Id = playerId, Name = "Test" };
        _mockRepository.Setup(r => r.GetByIdAsync(playerId))
                      .ReturnsAsync(expectedPlayer);
        
        // Act
        var result = await _service.GetPlayerAsync(playerId);
        
        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(expectedPlayer.Name, result.Data.Name);
    }
}
```

#### 7.4.2 集成测试
```csharp
[TestClass]
public class PlayerIntegrationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public PlayerIntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // 使用内存数据库
                    services.AddDbContext<GameDbContext>(options =>
                        options.UseInMemoryDatabase("TestDb"));
                });
            });
            
        _client = _factory.CreateClient();
    }
    
    [TestMethod]
    public async Task GetPlayer_ReturnsPlayerData()
    {
        // Arrange
        var playerId = "test-player";
        
        // Act
        var response = await _client.GetAsync($"/api/players/{playerId}");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var player = await response.Content.ReadFromJsonAsync<PlayerDto>();
        Assert.IsNotNull(player);
    }
}
```

---

## 总结

本文档提供了 BlazorWebGame 数据存储架构的完整分析和优化方案。通过分阶段的实施计划，可以系统地改进当前架构的问题，提升系统的性能、可维护性和可扩展性。

**关键改进点**:
1. **架构重构**: 采用 Repository 模式和 CQRS 模式
2. **数据库优化**: 重构数据模型，优化索引和查询
3. **缓存系统**: 实现多层缓存架构
4. **客户端存储**: 升级到 IndexedDB，实现数据同步
5. **代码质量**: 统一错误处理，添加测试覆盖

**预期收益**:
- 系统性能提升 60-80%
- 代码可维护性显著改善
- 用户体验优化（离线支持）
- 系统可扩展性增强

建议按照实施步骤指南逐步进行，确保每个阶段都有充分的测试和验证。