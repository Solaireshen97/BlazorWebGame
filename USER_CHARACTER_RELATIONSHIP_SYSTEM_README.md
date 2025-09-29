# 用户角色关联系统实现文档

## 概述

本文档描述了BlazorWebGame项目中用户与游戏角色关联系统的完整设计和实现，该系统建立了用户账户与游戏角色之间的可靠数据关联，确保角色数据的安全性和访问控制。

## 系统架构

### 核心组件

```
用户角色关联系统架构:

BlazorWebGame.Shared/
├── Models/
│   └── DataStorageModels.cs          # UserCharacterEntity实体模型
├── DTOs/
│   └── DataStorageDTOs.cs           # UserCharacterStorageDto传输对象
└── Interfaces/
    └── IDataStorageService.cs       # 用户角色关联接口定义

BlazorWebGame.Server/
├── Data/
│   └── GameDbContext.cs             # UserCharacters数据表配置
├── Services/
│   ├── ServerCharacterService.cs    # 增强的角色管理服务
│   ├── UserService.cs               # 增强的用户服务
│   ├── DataStorageService.cs        # 内存存储实现
│   └── SqliteDataStorageService.cs  # SQLite存储实现
├── Controllers/
│   └── CharacterController.cs       # 增强的角色API控制器
└── Tests/
    └── UserCharacterServiceTests.cs # 用户角色关联测试
```

## 数据模型

### UserCharacterEntity (用户角色关联实体)

```csharp
public class UserCharacterEntity : BaseEntity
{
    public string UserId { get; set; } = string.Empty;        // 用户ID
    public string CharacterId { get; set; } = string.Empty;   // 角色ID
    public string CharacterName { get; set; } = string.Empty; // 角色名称
    public bool IsActive { get; set; } = true;                // 关联是否活跃
    public bool IsDefault { get; set; } = false;              // 是否为默认角色
    public DateTime LastPlayedAt { get; set; }                // 最后游玩时间
}
```

### 数据库表结构

```sql
CREATE TABLE UserCharacters (
    Id NVARCHAR(100) PRIMARY KEY,
    UserId NVARCHAR(100) NOT NULL,
    CharacterId NVARCHAR(100) NOT NULL, 
    CharacterName NVARCHAR(50),
    IsActive BOOLEAN NOT NULL DEFAULT 1,
    IsDefault BOOLEAN NOT NULL DEFAULT 0,
    LastPlayedAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME2 NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 索引
CREATE INDEX IX_UserCharacters_UserId ON UserCharacters(UserId);
CREATE INDEX IX_UserCharacters_CharacterId ON UserCharacters(CharacterId);
CREATE UNIQUE INDEX IX_UserCharacters_UserId_CharacterId ON UserCharacters(UserId, CharacterId);
CREATE INDEX IX_UserCharacters_IsActive ON UserCharacters(IsActive);
CREATE INDEX IX_UserCharacters_IsDefault ON UserCharacters(IsDefault);
```

## API 接口

### 用户角色管理接口

#### 获取用户的角色列表
```http
GET /api/character/my
Authorization: Bearer <jwt_token>
```

**响应:**
```json
{
    "success": true,
    "data": [
        {
            "id": "char-123",
            "name": "英勇战士",
            "health": 100,
            "maxHealth": 100,
            "gold": 5000,
            "selectedBattleProfession": "Warrior",
            "lastUpdated": "2024-01-01T12:00:00Z"
        }
    ],
    "message": "用户角色列表获取成功"
}
```

#### 创建新角色
```http
POST /api/character
Authorization: Bearer <jwt_token>
Content-Type: application/json

{
    "name": "新角色名称"
}
```

**响应:**
```json
{
    "success": true,
    "data": {
        "id": "char-456",
        "name": "新角色名称",
        "health": 100,
        "maxHealth": 100,
        "gold": 10000,
        "selectedBattleProfession": "Warrior"
    },
    "message": "角色创建成功"
}
```

#### 获取角色详细信息
```http
GET /api/character/{characterId}
Authorization: Bearer <jwt_token>
```

**注意**: 只能访问自己拥有的角色，管理员用户可以访问任何角色。

### 数据存储服务接口

```csharp
public interface IDataStorageService
{
    // 用户角色关联管理
    Task<ApiResponse<UserCharacterStorageDto>> CreateUserCharacterAsync(string userId, string characterId, string characterName, bool isDefault = false);
    Task<ApiResponse<List<UserCharacterStorageDto>>> GetUserCharactersAsync(string userId);
    Task<UserCharacterStorageDto?> GetCharacterOwnerAsync(string characterId);
    Task<bool> UserOwnsCharacterAsync(string userId, string characterId);
    Task<ApiResponse<bool>> SetDefaultCharacterAsync(string userId, string characterId);
    Task<ApiResponse<bool>> DeleteUserCharacterAsync(string userId, string characterId);
}
```

## 核心功能实现

### 1. 角色创建与用户关联

```csharp
// ServerCharacterService.cs
public async Task<CharacterDto> CreateCharacterAsync(CreateCharacterRequest request, string? userId = null)
{
    var characterId = Guid.NewGuid().ToString();
    var character = new CharacterDetailsDto { /* 初始化角色数据 */ };
    
    // 如果提供了用户ID，创建用户角色关联
    if (!string.IsNullOrEmpty(userId))
    {
        var userCharacters = await _dataStorage.GetUserCharactersAsync(userId);
        var isFirstCharacter = !userCharacters.Success || userCharacters.Data?.Count == 0;
        
        await _dataStorage.CreateUserCharacterAsync(userId, characterId, request.Name, isFirstCharacter);
    }
    
    return character;
}
```

### 2. 角色所有权验证

```csharp
// UserService.cs
public async Task<bool> UserHasCharacterAsync(string userId, string characterId)
{
    return await _dataStorage.UserOwnsCharacterAsync(userId, characterId);
}

// DataStorageService.cs
public async Task<bool> UserOwnsCharacterAsync(string userId, string characterId)
{
    // 管理员可以访问任何角色
    if (_users.TryGetValue(userId, out var user))
    {
        var roles = JsonSerializer.Deserialize<List<string>>(user.RolesJson) ?? new List<string>();
        if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            return true;
    }
    
    return _characterToUser.TryGetValue(characterId, out var ownerId) && ownerId == userId;
}
```

### 3. 角色访问控制

```csharp
// CharacterController.cs
[HttpGet("{characterId}")]
[Authorize]
public async Task<ActionResult<ApiResponse<CharacterDetailsDto>>> GetCharacterDetails(string characterId)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    // 验证用户是否拥有该角色
    var ownsCharacter = await _characterService.UserOwnsCharacterAsync(userId, characterId);
    if (!ownsCharacter)
    {
        return Forbid();
    }
    
    // 获取角色详细信息
    var character = await _characterService.GetCharacterDetailsAsync(characterId);
    return Ok(new ApiResponse<CharacterDetailsDto> { Success = true, Data = character });
}
```

## 安全特性

### 1. 访问控制
- **身份验证**: 所有角色相关API都需要JWT令牌认证
- **所有权验证**: 用户只能访问自己拥有的角色
- **管理员权限**: 管理员用户可以访问任何角色（用于管理功能）

### 2. 数据完整性
- **唯一约束**: 用户-角色组合在数据库中是唯一的
- **软删除**: 删除角色关联时使用软删除，保留历史数据
- **默认角色管理**: 确保用户只有一个默认角色

### 3. 并发安全
- **乐观锁**: 使用UpdatedAt字段进行并发控制
- **事务处理**: 关键操作使用数据库事务确保一致性

## 测试覆盖

### 自动化测试套件

系统包含完整的测试覆盖 (`UserCharacterServiceTests.cs`):

#### 1. 用户和角色创建测试
```csharp
private static async Task TestUserAndCharacterCreation(UserService userService, ServerCharacterService characterService, ILogger logger)
{
    // 创建用户
    var registrationResult = await userService.RegisterUserAsync("gameuser", "password123", "gameuser@example.com");
    
    // 创建角色并关联到用户
    var characterRequest = new CreateCharacterRequest { Name = "TestHero" };
    var character = await characterService.CreateCharacterAsync(characterRequest, user.Id);
    
    // 验证角色创建成功
    Assert.NotNull(character);
    Assert.Equal("TestHero", character.Name);
}
```

#### 2. 角色所有权验证测试
- 测试用户拥有自己创建的角色
- 测试其他用户无法访问不属于自己的角色
- 测试管理员可以访问任何角色

#### 3. 用户角色列表测试
- 测试获取用户的所有角色
- 测试角色列表的正确排序（默认角色优先）

#### 4. 默认角色设置测试
- 测试设置默认角色
- 测试默认角色的唯一性

#### 5. 角色访问权限控制测试
- 测试API端点的权限验证
- 测试JWT令牌认证

### 运行测试

测试在开发环境下自动运行：

```bash
cd src/BlazorWebGame.Server
dotnet run
```

**测试输出示例:**
```
[09:11:05 INF] [Program] Starting User-Character relationship tests...
[09:11:05 INF] [Program] ✓ User and character creation test passed
[09:11:05 INF] [Program] ✓ Character ownership test passed
[09:11:05 INF] [Program] ✓ User character list test passed
[09:11:05 INF] [Program] ✓ Default character setting test passed
[09:11:05 INF] [Program] ✓ Character access control test passed
[09:11:05 INF] [Program] All User-Character relationship tests passed successfully!
```

## 数据存储实现

### 双存储架构支持

系统同时支持两种存储实现：

#### 1. 内存存储 (DataStorageService)
- **用途**: 开发和测试
- **特点**: 快速响应，数据不持久化
- **索引**: 使用ConcurrentDictionary提供快速查找

#### 2. SQLite存储 (SqliteDataStorageService)
- **用途**: 生产环境
- **特点**: 数据持久化，支持复杂查询
- **事务**: 使用Entity Framework事务确保数据一致性

### 数据库迁移

系统会自动检测并创建缺失的数据表：

```csharp
// DatabaseInitializationService会自动创建UserCharacters表
[09:11:03 WRN] Table UserCharacters does not exist
[09:11:03 INF] Found 1 missing tables: UserCharacters. Recreating database structure...
[09:11:03 INF] Created table: UserCharacters
```

## 性能优化

### 1. 数据库索引策略
```sql
-- 主要查询路径优化
CREATE INDEX IX_UserCharacters_UserId ON UserCharacters(UserId);           -- 按用户查询角色
CREATE INDEX IX_UserCharacters_CharacterId ON UserCharacters(CharacterId); -- 按角色查询拥有者
CREATE INDEX IX_UserCharacters_IsActive ON UserCharacters(IsActive);       -- 活跃角色过滤
```

### 2. 缓存策略
- **内存索引**: 在DataStorageService中维护快速查找索引
- **连接池**: SQLite使用连接池减少连接开销

### 3. 查询优化
- **批量操作**: 支持批量查询用户角色
- **分页支持**: 为大量角色数据提供分页查询
- **选择性查询**: 只返回必要的字段数据

## 部署和维护

### 配置选项

在 `appsettings.json` 中配置数据存储类型：

```json
{
  "GameServer": {
    "DataStorageType": "SQLite",  // 或 "InMemory"
    "EnableDevelopmentTests": true
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=gamedata_dev.db"
  }
}
```

### 监控和日志

系统提供详细的日志记录：

```csharp
_logger.LogInformation($"Created user-character relationship: {SafeLogId(userId)} -> {SafeLogId(characterId)}");
_logger.LogError(ex, $"Error creating user-character relationship: {SafeLogId(userId)} -> {SafeLogId(characterId)}");
```

### 数据备份

- **SQLite数据库**: 定期备份 `gamedata_dev.db` 文件
- **用户数据导出**: 使用IDataStorageService的导出功能
- **增量备份**: 基于UpdatedAt字段进行增量数据同步

## 扩展功能

### 已实现功能
- ✅ 用户角色关联创建和管理
- ✅ 角色所有权验证
- ✅ 默认角色设置
- ✅ 软删除支持
- ✅ 完整的API权限控制
- ✅ 双存储架构支持
- ✅ 全面的测试覆盖
- ✅ 自动数据库迁移

### 未来扩展方向
- 🔄 角色共享功能（好友间角色共享）
- 🔄 角色转移功能（账户间角色转移）
- 🔄 角色备份和恢复
- 🔄 角色统计和分析
- 🔄 多服务器角色同步
- 🔄 角色模板系统

## 故障排除

### 常见问题

**Q: 角色创建后用户无法访问**
A: 检查用户角色关联是否正确创建，查看UserCharacters表中的数据。

**Q: 管理员无法访问其他用户的角色**
A: 确认用户角色中包含"Admin"角色，检查JWT令牌中的角色信息。

**Q: 数据库连接失败**
A: 检查SQLite数据库文件权限，确认连接字符串配置正确。

**Q: 角色重复创建**
A: 检查UniqueIndex约束，确保用户-角色组合的唯一性。

### 调试技巧

1. **启用详细日志**: 设置日志级别为Debug查看详细信息
2. **检查数据库状态**: 直接查询UserCharacters表验证数据
3. **测试API端点**: 使用Swagger UI测试角色相关API
4. **验证JWT令牌**: 解码JWT令牌检查用户ID和角色信息

## 总结

本用户角色关联系统成功实现了：

1. **数据安全**: 通过数据库关联确保角色数据的安全访问
2. **权限控制**: 完整的API级别权限控制和验证
3. **可扩展性**: 支持双存储架构，易于扩展和维护
4. **测试覆盖**: 全面的自动化测试确保系统稳定性
5. **文档完整**: 详细的实现文档和API说明

该系统为游戏的用户体验和数据安全提供了坚实的基础，支持游戏的长期发展和功能扩展。

---

*文档版本: 1.0.0*  
*最后更新: 2024年12月*  
*作者: BlazorWebGame开发团队*