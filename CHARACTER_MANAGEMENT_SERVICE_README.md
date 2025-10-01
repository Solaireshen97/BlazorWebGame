# 角色管理服务实现文档

## 概述

本文档描述了在 Server 项目中新实现的 `ServerCharacterManagementService` 服务，该服务提供了增强的角色管理功能，包括角色花名册管理、角色创建/删除/切换、属性分配等核心功能。

## 系统架构

### 服务层次结构

```
CharacterManagementController (API层)
    ↓
ServerCharacterManagementService (业务逻辑层)
    ↓
ServerCharacterService (基础服务层)
    ↓
IDataStorageService (数据持久化层)
```

### 主要组件

1. **ServerCharacterManagementService** - 核心服务类
   - 位置: `/src/BlazorWebGame.Server/Services/Character/ServerCharacterManagementService.cs`
   - 功能: 提供角色花名册管理、角色创建/删除/切换、属性分配等功能

2. **CharacterManagementController** - API控制器
   - 位置: `/src/BlazorWebGame.Server/Controllers/CharacterManagementController.cs`
   - 功能: 暴露RESTful API端点

## 核心功能

### 1. 角色花名册 (Roster) 管理

花名册系统允许用户管理多个角色槽位：

#### 功能特性
- 默认提供 8 个角色槽位
- 初始解锁 3 个槽位
- 支持槽位解锁机制
- 显示角色摘要信息
- 标记活跃角色

#### API 端点

**获取角色花名册**
```http
GET /api/character-management/roster
Authorization: Bearer {token}

Response:
{
  "success": true,
  "data": {
    "userId": "user-123",
    "maxSlots": 8,
    "unlockedSlots": 3,
    "activeCharacterId": "char-456",
    "slots": [
      {
        "slotIndex": 0,
        "state": "Occupied",
        "character": {
          "id": "char-456",
          "name": "勇者",
          "level": 15,
          "professionName": "Warrior",
          "professionIcon": "⚔️",
          "isOnline": false,
          "lastActiveAt": "2024-01-01T12:00:00Z"
        },
        "lastPlayedAt": "2024-01-01T12:00:00Z"
      },
      {
        "slotIndex": 1,
        "state": "Unlocked"
      },
      {
        "slotIndex": 3,
        "state": "Locked",
        "unlockCondition": "角色等级达到10级"
      }
    ]
  },
  "message": "获取角色花名册成功"
}
```

**解锁角色槽位**
```http
POST /api/character-management/roster/slots/{slotIndex}/unlock
Authorization: Bearer {token}

Response:
{
  "success": true,
  "data": true,
  "message": "槽位解锁成功"
}
```

### 2. 角色创建

#### 功能特性
- 角色名称验证
- 自动分配到可用槽位
- 初始化角色属性和职业
- 触发角色创建事件
- 第一个角色自动设为默认角色

#### API 端点

**创建新角色**
```http
POST /api/character-management/characters
Authorization: Bearer {token}
Content-Type: application/json

Request:
{
  "name": "新角色",
  "startingProfessionId": "Warrior",
  "slotIndex": 0
}

Response:
{
  "success": true,
  "data": {
    "id": "char-789",
    "name": "新角色",
    "level": 1,
    "experience": 0,
    "experienceToNextLevel": 1000,
    "gold": 10000,
    "vitals": {
      "health": 100,
      "maxHealth": 100,
      "mana": 0,
      "maxMana": 100,
      "healthRegen": 0.1,
      "manaRegen": 0.05
    },
    "attributes": {
      "strength": 10,
      "agility": 10,
      "intellect": 10,
      "spirit": 10,
      "stamina": 10,
      "availablePoints": 0,
      "attackPower": 20,
      "armor": 20,
      "criticalChance": 1.0,
      "attackSpeed": 1.1
    },
    "profession": {
      "id": "Warrior",
      "name": "战士",
      "icon": "⚔️",
      "level": 1,
      "experience": 0,
      "experienceToNextLevel": 1000
    }
  },
  "message": "角色创建成功"
}
```

### 3. 角色名称验证

#### 验证规则
- 不能为空
- 长度必须在 2-16 个字符之间
- 只能包含中文、英文、数字和下划线
- 不能使用黑名单中的名称（admin, gm等）
- 不能包含敏感词汇

#### API 端点

**验证角色名称**
```http
POST /api/character-management/validate-name
Authorization: Bearer {token}
Content-Type: application/json

Request:
{
  "name": "测试角色"
}

Response:
{
  "success": true,
  "data": {
    "isValid": true,
    "reason": null
  },
  "message": "角色名称可用"
}
```

### 4. 角色切换

#### 功能特性
- 验证用户拥有该角色
- 更新默认角色设置
- 更新花名册缓存
- 触发角色切换事件

#### API 端点

**切换活跃角色**
```http
POST /api/character-management/characters/{characterId}/switch
Authorization: Bearer {token}

Response:
{
  "success": true,
  "data": {
    "id": "char-456",
    "name": "勇者",
    "level": 15,
    // ... 完整角色信息
  },
  "message": "角色切换成功"
}
```

### 5. 角色删除

#### 功能特性
- 验证用户拥有该角色
- 从数据库删除用户角色关联
- 更新花名册缓存
- 如果删除的是活跃角色，自动选择另一个角色
- 触发角色删除事件

#### API 端点

**删除角色**
```http
DELETE /api/character-management/characters/{characterId}
Authorization: Bearer {token}

Response:
{
  "success": true,
  "data": true,
  "message": "角色删除成功"
}
```

### 6. 属性分配

#### 功能特性
- 验证用户拥有该角色
- 验证属性点有效性
- 应用属性点到角色
- 更新衍生属性
- 触发属性变更事件

#### API 端点

**分配属性点**
```http
POST /api/character-management/characters/{characterId}/attributes/allocate
Authorization: Bearer {token}
Content-Type: application/json

Request:
{
  "points": {
    "strength": 5,
    "agility": 3,
    "stamina": 2
  }
}

Response:
{
  "success": true,
  "data": {
    "strength": 15,
    "agility": 13,
    "intellect": 10,
    "spirit": 10,
    "stamina": 12,
    "availablePoints": 0,
    "attackPower": 30,
    "armor": 30,
    "criticalChance": 1.3,
    "attackSpeed": 1.13
  },
  "message": "属性点分配成功"
}
```

### 7. 获取角色详细信息

#### API 端点

**获取角色详细信息**
```http
GET /api/character-management/characters/{characterId}
Authorization: Bearer {token}

Response:
{
  "success": true,
  "data": {
    "id": "char-456",
    "name": "勇者",
    "level": 15,
    "experience": 14500,
    "experienceToNextLevel": 1000,
    "gold": 25000,
    "vitals": { /* ... */ },
    "attributes": { /* ... */ },
    "profession": { /* ... */ },
    "equipment": { /* ... */ },
    "skills": { /* ... */ },
    "reputations": { /* ... */ },
    "statistics": { /* ... */ },
    "createdAt": "2024-01-01T00:00:00Z",
    "lastActiveAt": "2024-01-01T12:00:00Z"
  },
  "message": "获取角色详细信息成功"
}
```

## 数据模型

### RosterDto (角色花名册)
```csharp
public class RosterDto
{
    public string UserId { get; set; }
    public List<CharacterSlotDto> Slots { get; set; }
    public string? ActiveCharacterId { get; set; }
    public int MaxSlots { get; set; }
    public int UnlockedSlots { get; set; }
}
```

### CharacterSlotDto (角色槽位)
```csharp
public class CharacterSlotDto
{
    public int SlotIndex { get; set; }
    public string State { get; set; } // "Locked" / "Unlocked" / "Occupied"
    public CharacterSummaryDto? Character { get; set; }
    public string? UnlockCondition { get; set; }
    public DateTime? LastPlayedAt { get; set; }
}
```

### CharacterFullDto (完整角色信息)
```csharp
public class CharacterFullDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public int ExperienceToNextLevel { get; set; }
    public int Gold { get; set; }
    public CharacterVitalsDto Vitals { get; set; }
    public CharacterAttributesDto Attributes { get; set; }
    public ProfessionInfoDto Profession { get; set; }
    public EquipmentDto Equipment { get; set; }
    public SkillSystemDto Skills { get; set; }
    public Dictionary<string, ReputationDto> Reputations { get; set; }
    public CharacterStatisticsDto Statistics { get; set; }
    // ... 更多字段
}
```

## 事件系统集成

服务集成了游戏事件系统，在关键操作时触发事件：

### 触发的事件
1. **CharacterCreated** - 角色创建时
2. **ActiveCharacterChanged** - 切换活跃角色时
3. **CharacterStatChanged** - 属性变更时
4. **GenericStateChanged** - 通用状态变更（槽位解锁、角色删除等）

### 事件使用示例
```csharp
// 角色创建事件
_eventManager.Raise(new GameEventArgs(
    GameEventType.CharacterCreated, 
    characterId, 
    new { UserId = userId, CharacterName = characterName }
));

// 属性分配事件
_eventManager.Raise(new GameEventArgs(
    GameEventType.CharacterStatChanged, 
    characterId, 
    new { UserId = userId, AttributesAllocated = request.Points }
));
```

## 依赖服务

### 必需的服务
1. **IDataStorageService** - 数据持久化
   - `GetUserCharactersAsync` - 获取用户角色
   - `CreateUserCharacterAsync` - 创建用户角色关联
   - `DeleteUserCharacterAsync` - 删除用户角色关联
   - `SetDefaultCharacterAsync` - 设置默认角色
   - `UserOwnsCharacterAsync` - 验证角色所有权

2. **ServerCharacterService** - 基础角色服务
   - `CreateCharacterAsync` - 创建角色
   - `GetCharacterDetailsAsync` - 获取角色详情
   - `UserOwnsCharacterAsync` - 验证角色所有权

3. **ServerPlayerAttributeService** - 属性服务
   - `GetTotalAttributes` - 获取总属性
   - `GetTotalAttackPower` - 获取攻击力
   - `UpdateBaseAttributes` - 更新基础属性
   - `GetTotalMaxHealth` - 获取最大生命值

4. **GameEventManager** - 事件管理器
   - `Raise` - 触发游戏事件

## 缓存策略

服务使用 `ConcurrentDictionary` 缓存用户花名册：

```csharp
private readonly ConcurrentDictionary<string, RosterDto> _userRosters = new();
```

### 缓存更新时机
- 获取花名册时（如果缓存不存在）
- 创建角色后
- 删除角色后
- 切换角色后
- 解锁槽位后

## 配置选项

### 槽位配置
```csharp
private const int DEFAULT_MAX_SLOTS = 8;        // 最大槽位数
private const int DEFAULT_UNLOCKED_SLOTS = 3;   // 默认解锁槽位数
```

### 解锁条件
- 槽位 3: 角色等级达到10级
- 槽位 4: 角色等级达到20级
- 槽位 5: 角色等级达到30级
- 槽位 6: 完成特定任务或消耗1000金币
- 槽位 7: 完成特定任务或消耗2000金币

## 安全特性

### 认证和授权
- 所有端点都需要 JWT 认证
- 使用 `[Authorize]` 特性保护控制器
- 从 JWT Token 中提取用户ID

### 权限验证
- 创建角色时验证槽位可用性
- 删除/切换/查看角色时验证用户所有权
- 属性分配时验证角色所有权

### 输入验证
- 角色名称格式验证
- 敏感词过滤
- 黑名单检查
- 属性点有效性验证

## 错误处理

### 常见错误响应

**401 Unauthorized** - 未认证
```json
{
  "success": false,
  "message": "用户未认证"
}
```

**400 Bad Request** - 请求无效
```json
{
  "success": false,
  "message": "角色名称长度必须在2-16个字符之间"
}
```

**404 Not Found** - 资源不存在
```json
{
  "success": false,
  "message": "未找到角色"
}
```

**500 Internal Server Error** - 服务器错误
```json
{
  "success": false,
  "message": "创建角色失败"
}
```

## 日志记录

服务使用 `ILogger` 进行日志记录：

### 日志级别
- **Information**: 成功操作（创建、删除、切换角色等）
- **Warning**: 业务警告（数据加载失败等）
- **Error**: 异常和错误

### 日志示例
```csharp
_logger.LogInformation($"User {userId} created character {characterName} (ID: {characterId})");
_logger.LogWarning($"Failed to get user characters: {result.Message}");
_logger.LogError(ex, $"Error creating character for user {userId}");
```

## 性能优化

### 优化策略
1. **缓存花名册** - 减少数据库查询
2. **异步操作** - 所有数据库操作使用异步方法
3. **延迟加载** - 仅在需要时加载详细信息
4. **批量操作** - 初始化时批量处理槽位

### 注意事项
- 缓存需要在适当的时机更新
- 大量用户时考虑缓存过期策略
- 考虑使用分布式缓存（Redis）

## 扩展功能建议

### 短期扩展
1. 实现槽位解锁条件验证
2. 添加角色复制功能
3. 实现角色转职功能
4. 添加角色外观定制

### 长期扩展
1. 实现角色共享系统
2. 添加角色成就系统
3. 实现角色排行榜
4. 添加角色导入/导出功能

## 测试建议

### 单元测试
- 角色名称验证逻辑
- 属性分配计算
- 槽位解锁条件判断
- DTO 转换逻辑

### 集成测试
- 角色创建流程
- 角色删除流程
- 角色切换流程
- 属性分配流程

### 端到端测试
- 完整的角色生命周期
- 多用户并发场景
- 缓存一致性验证

## 故障排除

### 常见问题

**问题**: 花名册为空或不显示角色
- **原因**: 缓存未更新或数据库查询失败
- **解决**: 检查日志，验证数据库连接，清除缓存重试

**问题**: 无法创建角色
- **原因**: 槽位已满或名称验证失败
- **解决**: 检查可用槽位，验证角色名称规则

**问题**: 角色切换失败
- **原因**: 用户不拥有该角色或数据库更新失败
- **解决**: 验证用户角色关联，检查数据库状态

## 总结

`ServerCharacterManagementService` 提供了一个完整的角色管理解决方案，包括：

✅ 角色花名册管理  
✅ 角色创建、删除、切换  
✅ 角色名称验证  
✅ 属性分配  
✅ 事件系统集成  
✅ 缓存优化  
✅ 安全认证和授权  
✅ 完整的错误处理  
✅ 详细的日志记录  

该服务可以作为游戏角色系统的核心组件，为玩家提供流畅的角色管理体验。
