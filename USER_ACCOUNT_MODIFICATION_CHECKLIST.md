# 用户账户接口修改检查清单

## 📋 修改前检查清单

在修改任何用户相关接口之前，请使用此清单确保完整性和一致性。

---

## 🎯 修改范围确认

### 第1步：确定修改类型

- [ ] 添加新字段到用户模型
- [ ] 修改现有字段类型
- [ ] 添加新的用户相关方法
- [ ] 修改现有方法签名
- [ ] 添加新的业务逻辑
- [ ] 修改验证规则
- [ ] 修改安全策略
- [ ] 其他: ___________________

### 第2步：影响范围评估

- [ ] 仅影响内部实现（无需修改接口）
- [ ] 需要修改接口定义
- [ ] 需要修改数据库结构
- [ ] 需要修改API契约（影响客户端）
- [ ] 需要数据迁移

---

## 📝 必须修改的文件清单

### 模型层 (Domain Models)
**位置**: `src/BlazorWebGame.Shared/Models/`

- [ ] **UserModels.cs**
  - [ ] `User` 类
  - [ ] `UserProfile` 类
  - [ ] `UserSecurity` 类
  - [ ] `UserCharacterRelation` 类

### DTO层 (Data Transfer Objects)
**位置**: `src/BlazorWebGame.Shared/DTOs/`

- [ ] **DataStorageDTOs.cs**
  - [ ] `UserStorageDto`
  - [ ] `UserCharacterStorageDto`

- [ ] **AuthenticationDTOs.cs**
  - [ ] `LoginRequest`
  - [ ] `RegisterRequest`
  - [ ] `RefreshTokenRequest`
  - [ ] `AuthenticationResponse`

**位置**: `src/BlazorWebGame.Shared/Interfaces/`

- [ ] **IAuthApi.cs**
  - [ ] `UserInfoDto`

### 接口层 (Interfaces)
**位置**: `src/BlazorWebGame.Shared/Interfaces/`

- [ ] **IAuthApi.cs**
  - [ ] `LoginAsync`
  - [ ] `RegisterAsync`
  - [ ] `RefreshTokenAsync`
  - [ ] `LogoutAsync`
  - [ ] `GetCurrentUserAsync`
  - [ ] `DemoLoginAsync`

- [ ] **IDataStorageService.cs**
  - 用户账号管理:
    - [ ] `GetUserByUsernameAsync`
    - [ ] `GetUserByIdAsync`
    - [ ] `GetUserByEmailAsync`
    - [ ] `CreateUserAsync`
    - [ ] `UpdateUserAsync`
    - [ ] `ValidateUserPasswordAsync`
    - [ ] `UpdateUserPasswordAsync`
    - [ ] `UpdateUserLastLoginAsync`
    - [ ] `LockUserAccountAsync`
    - [ ] `UnlockUserAccountAsync`
  - 用户角色关联:
    - [ ] `CreateUserCharacterAsync`
    - [ ] `GetUserCharactersAsync`
    - [ ] `GetCharacterOwnerAsync`
    - [ ] `UserOwnsCharacterAsync`
    - [ ] `SetDefaultCharacterAsync`
    - [ ] `DeleteUserCharacterAsync`

### 服务层 (Services)
**位置**: `src/BlazorWebGame.Server/Services/`

- [ ] **UserService.cs**
  - [ ] `ValidateUserAsync`
  - [ ] `RegisterUserAsync`
  - [ ] `GetUserByIdAsync`
  - [ ] `UpdateLastLoginAsync`
  - [ ] `UserHasRoleAsync`
  - [ ] `UserHasCharacterAsync`
  - [ ] `IncrementLoginAttemptsAsync`
  - [ ] `ValidateRegistrationInput`

- [ ] **DataStorageService.cs**
  - [ ] 实现 IDataStorageService 的用户相关方法

- [ ] **SqliteDataStorageService.cs**
  - [ ] 实现 IDataStorageService 的用户相关方法

- [ ] **GameAuthenticationService.cs** (如需要)
  - [ ] `GenerateAccessToken`
  - [ ] `GenerateRefreshToken`
  - [ ] Token 相关逻辑

### 控制器层 (Controllers)
**位置**: `src/BlazorWebGame.Server/Controllers/`

- [ ] **AuthController.cs**
  - [ ] `Login` (POST /api/auth/login)
  - [ ] `Register` (POST /api/auth/register)
  - [ ] `RefreshToken` (POST /api/auth/refresh)
  - [ ] `Logout` (POST /api/auth/logout)
  - [ ] `GetCurrentUser` (GET /api/auth/me)
  - [ ] `DemoLogin` (POST /api/auth/demo-login)

- [ ] **CharacterController.cs**
  - [ ] `GetMyCharacters` (GET /api/character/my)

### 数据库层 (Data Access)
**位置**: `src/BlazorWebGame.Server/Data/`

- [ ] **GameDbContext.cs** (如使用EF Core)
  - [ ] 添加/修改实体配置

- [ ] 数据库迁移文件
  - [ ] 创建新迁移
  - [ ] 测试迁移

### 测试层 (Tests)
**位置**: `src/BlazorWebGame.Server/Tests/`

- [ ] **UserServiceTests.cs**
  - [ ] 更新现有测试
  - [ ] 添加新测试用例

- [ ] **UserCharacterServiceTests.cs**
  - [ ] 更新现有测试
  - [ ] 添加新测试用例

### 客户端层 (Client) - 如影响客户端
**位置**: `src/BlazorWebGame.Client/Services/Api/`

- [ ] **AuthApiService.cs**
  - [ ] 更新API调用

---

## ✅ 代码修改检查

### 数据一致性检查

- [ ] User 模型和 UserStorageDto 字段保持同步
- [ ] 所有 DTO 属性都有默认值或可空标记
- [ ] 枚举值在各层保持一致
- [ ] 日期时间统一使用 UTC

### 接口一致性检查

- [ ] 接口定义与实现保持一致
- [ ] 所有实现类都实现了接口的新方法
- [ ] 方法签名在各层保持一致
- [ ] 返回类型统一使用 `ApiResponse<T>` 或 `Task<ApiResponse<T>>`

### 验证规则检查

- [ ] 用户名验证规则: 3-20字符，字母数字下划线
- [ ] 密码验证规则: 至少6个字符
- [ ] 邮箱验证规则: 标准邮箱格式
- [ ] 所有必填字段都有验证
- [ ] 验证错误消息清晰明确

### 安全性检查

- [ ] 密码使用安全算法加密（BCrypt）
- [ ] 不在日志中输出敏感信息
- [ ] API 端点有适当的授权检查
- [ ] JWT 令牌有合理的过期时间
- [ ] 防止 SQL 注入（使用参数化查询）
- [ ] 防止 XSS 攻击
- [ ] 实现了账户锁定机制

### 错误处理检查

- [ ] 所有异步方法都有 try-catch
- [ ] 错误信息对用户友好
- [ ] 详细错误记录到日志
- [ ] 返回适当的 HTTP 状态码
- [ ] 不向客户端暴露内部错误详情

### 性能考虑

- [ ] 数据库查询使用索引
- [ ] 避免 N+1 查询问题
- [ ] 考虑缓存频繁访问的数据
- [ ] 批量操作使用事务
- [ ] 大数据量使用分页

---

## 🧪 测试检查清单

### 单元测试

- [ ] 为新方法添加单元测试
- [ ] 测试正常流程
- [ ] 测试异常情况
- [ ] 测试边界条件
- [ ] 测试验证规则
- [ ] 所有测试通过

### 集成测试

- [ ] 测试完整的登录流程
- [ ] 测试完整的注册流程
- [ ] 测试令牌刷新流程
- [ ] 测试用户-角色关联
- [ ] 测试账户锁定机制
- [ ] 所有集成测试通过

### API测试

- [ ] 使用 Postman/Swagger 测试所有端点
- [ ] 测试有效输入
- [ ] 测试无效输入
- [ ] 测试认证和授权
- [ ] 测试并发请求
- [ ] 记录测试结果

---

## 📖 文档更新检查

### 代码文档

- [ ] 所有公共方法都有 XML 注释
- [ ] 注释描述清晰准确
- [ ] 参数和返回值都有说明
- [ ] 复杂逻辑有解释性注释

### API文档

- [ ] 更新 Swagger/OpenAPI 文档
- [ ] 记录所有端点的请求格式
- [ ] 记录所有端点的响应格式
- [ ] 记录错误响应
- [ ] 提供示例

### 项目文档

- [ ] 更新 USER_ACCOUNT_INTERFACES_ANALYSIS.md
- [ ] 更新 USER_ACCOUNT_INTERFACES_QUICKREF.md
- [ ] 更新 USER_AUTHENTICATION_SYSTEM_README.md
- [ ] 更新 CHANGELOG（如有）
- [ ] 更新 README（如需要）

---

## 🚀 部署前检查

### 代码审查

- [ ] 代码符合项目编码规范
- [ ] 没有硬编码的敏感信息
- [ ] 没有调试代码（Console.WriteLine等）
- [ ] 代码通过静态分析工具检查
- [ ] 代码经过同行评审

### 数据迁移

- [ ] 创建数据库迁移脚本
- [ ] 在开发环境测试迁移
- [ ] 在测试环境测试迁移
- [ ] 准备回滚脚本
- [ ] 备份生产数据库

### 配置检查

- [ ] 检查环境变量配置
- [ ] 检查连接字符串
- [ ] 检查 JWT 密钥配置
- [ ] 检查日志级别配置
- [ ] 检查 CORS 配置

### 兼容性检查

- [ ] 向后兼容性（不破坏现有功能）
- [ ] 旧客户端是否仍能工作
- [ ] 数据格式变更是否平滑迁移
- [ ] API 版本控制（如需要）

---

## 📊 验收标准

### 功能性

- [ ] 所有新功能按预期工作
- [ ] 所有旧功能未受影响
- [ ] 用户体验流畅
- [ ] 错误消息清晰友好

### 非功能性

- [ ] 性能满足要求（响应时间 < 2秒）
- [ ] 安全性满足标准
- [ ] 可维护性良好
- [ ] 代码可读性好

### 质量指标

- [ ] 单元测试覆盖率 > 80%
- [ ] 集成测试通过率 100%
- [ ] 静态代码分析无严重问题
- [ ] 代码审查通过

---

## 🔄 修改流程建议

### 推荐的修改顺序：

1. **设计阶段**
   - [ ] 明确需求
   - [ ] 设计数据模型
   - [ ] 设计接口
   - [ ] 评审设计

2. **实现阶段**
   - [ ] 修改领域模型
   - [ ] 修改 DTO
   - [ ] 修改接口定义
   - [ ] 实现服务层
   - [ ] 实现数据访问层
   - [ ] 实现控制器
   - [ ] 编写测试

3. **测试阶段**
   - [ ] 单元测试
   - [ ] 集成测试
   - [ ] API 测试
   - [ ] 手动测试

4. **文档阶段**
   - [ ] 更新代码注释
   - [ ] 更新 API 文档
   - [ ] 更新项目文档

5. **部署阶段**
   - [ ] 代码审查
   - [ ] 数据库迁移
   - [ ] 部署到测试环境
   - [ ] 验收测试
   - [ ] 部署到生产环境

---

## 📞 需要帮助？

如果在修改过程中遇到问题：

1. **查看相关文档**:
   - USER_ACCOUNT_INTERFACES_ANALYSIS.md - 详细分析
   - USER_ACCOUNT_INTERFACES_QUICKREF.md - 快速参考
   - USER_ACCOUNT_INTERFACES_DIAGRAMS.md - 架构图

2. **查看现有代码**:
   - 参考类似功能的实现
   - 查看测试用例了解用法

3. **团队沟通**:
   - 询问团队成员
   - 提交代码审查请求

---

## 📝 修改记录模板

```markdown
## 修改日期: YYYY-MM-DD
## 修改人: [姓名]
## 相关Issue: #[Issue编号]

### 修改内容
- [ ] 修改项1
- [ ] 修改项2

### 影响范围
- 模型: [列出修改的模型]
- 接口: [列出修改的接口]
- 服务: [列出修改的服务]
- API: [列出修改的端点]

### 测试情况
- 单元测试: ✅ 通过
- 集成测试: ✅ 通过
- API测试: ✅ 通过

### 文档更新
- [x] 代码注释
- [x] API文档
- [x] 项目文档

### 审核情况
- 代码审查: [审查人] - ✅ 通过
- 测试审查: [审查人] - ✅ 通过
```

---

**使用说明**:
1. 在开始修改前，复制此检查清单
2. 逐项检查并标记完成状态
3. 确保所有相关项都已完成
4. 保留检查清单作为修改记录

**版本**: 1.0  
**最后更新**: 2024年  
**维护者**: BlazorWebGame开发团队
