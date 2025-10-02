# 战斗系统重构进度文档
# Battle System Refactoring Progress Document

## 概述 / Overview

本文档记录了将战斗系统从使用Player类重构为使用Character领域模型的过程。
This document tracks the refactoring of the battle system from using Player classes to using Character domain models.

## 重构目标 / Refactoring Goals

1. ✅ 使用Character领域模型替代Player类
   - Use Character domain model instead of Player class
   
2. ✅ 使用正确的Battle领域模型而不是DTO
   - Use correct Battle domain models instead of DTOs
   
3. ✅ 完整重构战斗系统使用新的领域模型
   - Complete refactoring of battle system using new domain models

## 架构变更 / Architecture Changes

### 1. 领域模型层 (Shared) / Domain Model Layer

**位置 / Location**: `src/BlazorWebGame.Shared/`

#### Character领域模型 / Character Domain Model
- **文件**: `Models/CharacterModels.cs`
- **说明**: 完整的角色领域模型，包含属性、生命值、技能、背包等
- **Description**: Complete character domain model with attributes, vitals, skills, inventory, etc.

#### Battle领域模型 / Battle Domain Model
- **文件**: `Models/BattleModels.cs`
- **核心类 / Core Classes**:
  - `Battle` - 战斗领域模型
  - `BattleParticipant` - 战斗参与者基类
  - `BattlePlayer` - 玩家参与者
  - `BattleEnemy` - 敌人参与者
  - `BattleResult` - 战斗结果
  - `BattleAction` - 战斗动作记录

#### BattleMapper映射器 / BattleMapper
- **文件**: `Mappers/BattleMapper.cs`
- **核心方法 / Core Methods**:
  - `ToBattlePlayer(Character)` - 将Character转换为BattlePlayer
  - `ToBattleEnemy(Enemy)` - 将Enemy转换为BattleEnemy
  - `ApplyBattleResultToCharacter(Character, BattlePlayer, BattleResult)` - 将战斗结果应用回Character

### 2. 服务层 (Server) / Service Layer

**位置 / Location**: `src/BlazorWebGame.Server/Services/`

#### EnhancedServerCharacterService增强
- **文件**: `Character/EnhancedServerCharacterService.cs`
- **新增方法 / New Methods**:
  ```csharp
  // 获取Character领域模型（用于战斗系统）
  public async Task<Character?> GetCharacterDomainModelAsync(string characterId)
  
  // 保存Character领域模型（战斗结束后保存）
  public async Task<bool> SaveCharacterDomainModelAsync(Character character)
  ```

#### ServerBattleManager重构
- **文件**: `Battle/ServerBattleManager.cs`
- **构造函数变更 / Constructor Changes**:
  - ❌ 旧: 需要List<ServerBattlePlayer>
  - ✅ 新: 使用EnhancedServerCharacterService + GameClock

- **重构方法 / Refactored Methods**:
  1. `GetPlayersForBattleAsync()`:
     - ❌ 旧: 创建简化的Character和BattlePlayer
     - ✅ 新: 从EnhancedServerCharacterService加载完整Character，使用BattleMapper转换

  2. `ApplyBattleResultsAsync()`:
     - ❌ 旧: 只记录日志，无法保存结果（有TODO注释）
     - ✅ 新: 加载Character，使用BattleMapper应用结果，保存到存储

#### Program.cs依赖注入更新
- **文件**: `Program.cs`
- **变更 / Changes**:
  ```csharp
  // 旧的依赖注入
  builder.Services.AddSingleton<ServerBattleManager>(serviceProvider =>
  {
      var allCharacters = new List<ServerBattlePlayer>();
      return new ServerBattleManager(allCharacters, ...);
  });
  
  // 新的依赖注入
  builder.Services.AddSingleton<ServerBattleManager>(serviceProvider =>
  {
      return new ServerBattleManager(
          serviceProvider.GetRequiredService<EnhancedServerCharacterService>(),
          serviceProvider.GetRequiredService<ServerCombatEngine>(),
          serviceProvider.GetRequiredService<ServerBattleFlowService>(),
          serviceProvider.GetRequiredService<ServerSkillSystem>(),
          serviceProvider.GetRequiredService<ServerLootService>(),
          serviceProvider.GetRequiredService<ILogger<ServerBattleManager>>(),
          serviceProvider.GetRequiredService<IHubContext<GameHub>>(),
          serviceProvider.GetRequiredService<GameClock>()
      );
  });
  ```

## 数据流 / Data Flow

### 战斗开始流程 / Battle Start Flow
```
1. 客户端请求开始战斗 / Client requests battle start
   ↓
2. ServerBattleManager.StartBattleAsync(BattleStartRequest)
   ↓
3. GetPlayersForBattleAsync():
   - 调用 EnhancedServerCharacterService.GetCharacterDomainModelAsync()
   - 获取完整的Character领域模型
   - 使用 BattleMapper.ToBattlePlayer() 转换为BattlePlayer
   ↓
4. GenerateEnemiesForBattle():
   - 创建Enemy领域模型
   - 使用 BattleMapper.ToBattleEnemy() 转换为BattleEnemy
   ↓
5. 创建Battle领域模型并添加参与者
   ↓
6. 开始战斗循环 / Start battle loop
```

### 战斗结束流程 / Battle End Flow
```
1. 检测到战斗结束 / Battle end detected
   ↓
2. CalculateBattleRewards():
   - 计算经验值、金币、掉落物品
   - 创建BattleResult
   ↓
3. ApplyBattleResultsAsync():
   - 对每个BattlePlayer:
     a. 调用 EnhancedServerCharacterService.GetCharacterDomainModelAsync()
     b. 使用 BattleMapper.ApplyBattleResultToCharacter() 应用结果
     c. 调用 EnhancedServerCharacterService.SaveCharacterDomainModelAsync()
   ↓
4. 通知客户端战斗结束 / Notify client battle completed
```

## 修复的问题 / Issues Fixed

### 1. ✅ Character加载问题 / Character Loading Issue
- **问题**: ServerBattleManager无法访问Character领域模型
- **Issue**: ServerBattleManager couldn't access Character domain model
- **解决**: 在EnhancedServerCharacterService添加公共方法GetCharacterDomainModelAsync()
- **Solution**: Added public method GetCharacterDomainModelAsync() to EnhancedServerCharacterService

### 2. ✅ 战斗结果保存问题 / Battle Result Saving Issue
- **问题**: 战斗结束后无法保存结果到Character
- **Issue**: Couldn't save battle results to Character after battle
- **解决**: 
  - 添加SaveCharacterDomainModelAsync()方法
  - 使用BattleMapper.ApplyBattleResultToCharacter()应用结果
- **Solution**:
  - Added SaveCharacterDomainModelAsync() method
  - Use BattleMapper.ApplyBattleResultToCharacter() to apply results

### 3. ✅ 依赖注入问题 / Dependency Injection Issue
- **问题**: Program.cs使用过时的构造函数签名
- **Issue**: Program.cs used outdated constructor signature
- **解决**: 更新为使用EnhancedServerCharacterService和GameClock
- **Solution**: Updated to use EnhancedServerCharacterService and GameClock

### 4. ✅ Enemy奖励访问问题 / Enemy Rewards Access Issue
- **问题**: 尝试直接访问enemy.EnemyData.ExperienceReward
- **Issue**: Attempted to directly access enemy.EnemyData.ExperienceReward
- **解决**: 改为访问enemy.EnemyData.Rewards.ExperienceReward
- **Solution**: Changed to access enemy.EnemyData.Rewards.ExperienceReward

### 5. ✅ BattleType枚举问题 / BattleType Enum Issue
- **问题**: 使用未限定的BattleType枚举
- **Issue**: Used unqualified BattleType enum
- **解决**: 使用完全限定名 BlazorWebGame.Shared.Models.BattleType
- **Solution**: Used fully qualified name BlazorWebGame.Shared.Models.BattleType

## 待完成工作 / TODO Items

### 高优先级 / High Priority

1. ⏳ **实现战斗更新逻辑**
   - BattleInstance当前没有Update方法
   - 需要使用ServerCombatEngine来处理战斗逻辑
   - **Implement battle update logic**
     - BattleInstance currently has no Update method
     - Need to use ServerCombatEngine to handle battle logic

2. ⏳ **实现组队战斗支持**
   - GetPlayersForBattleAsync()中的组队逻辑待实现
   - 需要从组队服务获取成员列表
   - **Implement party battle support**
     - Party logic in GetPlayersForBattleAsync() needs implementation
     - Need to get member list from party service

3. ⏳ **完善敌人生成逻辑**
   - GenerateEnemiesForBattle()当前只创建测试敌人
   - 需要根据战斗类型、玩家等级等生成合适的敌人
   - **Improve enemy generation logic**
     - GenerateEnemiesForBattle() currently only creates test enemies
     - Need to generate appropriate enemies based on battle type, player level, etc.

### 中优先级 / Medium Priority

4. ⏳ **区域修饰符支持**
   - 当前regionId设为null，需要从请求中获取
   - **Region modifier support**
     - Currently regionId is set to null, need to get from request

5. ⏳ **战斗DTO转换**
   - NotifyBattleStartedAsync和NotifyBattleCompletedAsync需要转换为DTO再发送
   - **Battle DTO conversion**
     - NotifyBattleStartedAsync and NotifyBattleCompletedAsync need to convert to DTO before sending

### 低优先级 / Low Priority

6. ⏳ **性能优化**
   - 考虑缓存Character以减少数据库访问
   - 批量保存多个玩家的战斗结果
   - **Performance optimization**
     - Consider caching Character to reduce database access
     - Batch save battle results for multiple players

## 测试建议 / Testing Recommendations

### 单元测试 / Unit Tests
1. ✅ BattleMapper转换测试
   - 测试Character → BattlePlayer转换
   - 测试Enemy → BattleEnemy转换
   - 测试战斗结果应用

2. ⏳ EnhancedServerCharacterService测试
   - 测试GetCharacterDomainModelAsync()
   - 测试SaveCharacterDomainModelAsync()
   - 测试缓存逻辑

### 集成测试 / Integration Tests
1. ⏳ 完整战斗流程测试
   - 创建战斗
   - 执行战斗
   - 应用结果
   - 验证Character状态更新

2. ⏳ 并发测试
   - 多个战斗同时进行
   - 同一角色的并发访问

## 变更总结 / Summary of Changes

### 添加的文件 / Added Files
- 无 / None (所有变更在现有文件中)

### 修改的文件 / Modified Files
1. `src/BlazorWebGame.Server/Services/Character/EnhancedServerCharacterService.cs`
   - 添加GetCharacterDomainModelAsync()方法
   - 添加SaveCharacterDomainModelAsync()方法

2. `src/BlazorWebGame.Server/Services/Battle/ServerBattleManager.cs`
   - 更新GetPlayersForBattleAsync()使用Character领域模型
   - 完善ApplyBattleResultsAsync()保存战斗结果
   - 修复Enemy奖励访问
   - 修复BattleType枚举引用

3. `src/BlazorWebGame.Server/Program.cs`
   - 更新ServerBattleManager的依赖注入配置

### 删除的代码 / Removed Code
- 移除了ServerBattleManager中创建简化Character的临时代码
- Removed temporary code that created simplified Character in ServerBattleManager

## 版本历史 / Version History

### v1.0 - 2024年初始重构 / Initial Refactoring
- ✅ 完成Character领域模型到战斗系统的集成
- ✅ 实现完整的战斗结果保存流程
- ✅ 修复所有编译错误
- ✅ 构建成功

## 注意事项 / Notes

1. **缓存策略**: EnhancedServerCharacterService使用30分钟缓存过期策略
   - **Cache Strategy**: EnhancedServerCharacterService uses 30-minute cache expiration

2. **线程安全**: 使用ConcurrentDictionary确保并发访问安全
   - **Thread Safety**: Uses ConcurrentDictionary for concurrent access safety

3. **向后兼容**: 保留了标记为Obsolete的旧方法以保持向后兼容
   - **Backward Compatibility**: Kept old methods marked as Obsolete for backward compatibility

4. **错误处理**: 所有关键操作都包含try-catch和日志记录
   - **Error Handling**: All critical operations include try-catch and logging

## 参考资料 / References

- Character领域模型文档: `src/BlazorWebGame.Shared/Models/CharacterModels.cs`
- Battle领域模型文档: `src/BlazorWebGame.Shared/Models/BattleModels.cs`
- BattleMapper使用指南: `src/BlazorWebGame.Shared/Mappers/BattleMapper.cs`
- 数据存储服务: `DATA_STORAGE_SERVICE_README.md`
- 用户角色关系: `USER_CHARACTER_RELATIONSHIP_SYSTEM_README.md`

---

**文档创建时间 / Document Created**: 2024
**最后更新 / Last Updated**: 2024
**维护者 / Maintainer**: Development Team
