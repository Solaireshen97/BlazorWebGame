# 用户账户接口分析文档

## 概述
本文档整理了BlazorWebGame项目中所有涉及用户账户（User）的接口、类、方法和API端点。这些接口分布在不同的层次和模块中，包括认证、数据存储、用户管理等功能。

---

## 1. 核心用户模型

### 1.1 User 领域模型
**文件位置**: `src/BlazorWebGame.Shared/Models/UserModels.cs`

**主要属性**:
- `Id`: 用户唯一标识符
- `Username`: 用户名
- `Email`: 邮箱地址
- `IsActive`: 账户是否激活
- `EmailVerified`: 邮箱是否已验证
- `CreatedAt`: 创建时间
- `UpdatedAt`: 更新时间
- `LastLoginAt`: 最后登录时间
- `LastLoginIp`: 最后登录IP
- `Profile`: 用户档案信息（UserProfile类型）
- `Security`: 用户安全信息（UserSecurity类型）
- `CharacterIds`: 用户拥有的角色ID列表

**主要方法**:
- `User(string username, string email)`: 构造函数，创建新用户
- `UpdateProfile(string? displayName, string? avatar)`: 更新用户档案
- `RecordLogin(string ipAddress)`: 记录登录
- `RecordFailedLogin()`: 记录登录失败
- `VerifyEmail()`: 验证邮箱
- `Activate()`: 激活用户
- `Deactivate()`: 停用用户
- `AddCharacter(string characterId)`: 添加角色
- `RemoveCharacter(string characterId)`: 移除角色
- `HasCharacter(string characterId)`: 检查是否拥有角色

### 1.2 UserProfile 类
**文件位置**: `src/BlazorWebGame.Shared/Models/UserModels.cs`

**主要属性**:
- `DisplayName`: 显示名称
- `Avatar`: 头像URL
- `CustomProperties`: 自定义属性字典

**主要方法**:
- `UpdateProfile(string? displayName, string? avatar)`: 更新档案
- `SetCustomProperty(string key, object value)`: 设置自定义属性
- `GetCustomProperty<T>(string key, T? defaultValue)`: 获取自定义属性

### 1.3 UserSecurity 类
**文件位置**: `src/BlazorWebGame.Shared/Models/UserModels.cs`

**主要属性**:
- `Roles`: 用户角色列表
- `LoginAttempts`: 登录尝试次数
- `LockedUntil`: 账户锁定截止时间
- `LastPasswordChange`: 最后密码修改时间
- `LoginHistory`: 登录历史记录

**主要方法**:
- `RecordSuccessfulLogin()`: 记录成功登录
- `RecordFailedLogin()`: 记录失败登录
- `IsLocked()`: 检查账户是否被锁定
- `Unlock()`: 解锁账户
- `AddRole(string role)`: 添加角色
- `RemoveRole(string role)`: 移除角色
- `HasRole(string role)`: 检查是否拥有角色
- `UpdatePassword()`: 更新密码

### 1.4 UserCharacterRelation 类
**文件位置**: `src/BlazorWebGame.Shared/Models/UserModels.cs`

**主要属性**:
- `Id`: 关联ID
- `UserId`: 用户ID
- `CharacterId`: 角色ID
- `IsActive`: 关联是否激活
- `IsDefault`: 是否为默认角色
- `CreatedAt`: 创建时间
- `LastPlayedAt`: 最后游戏时间

**主要方法**:
- `UserCharacterRelation(string userId, string characterId, bool isDefault)`: 构造函数
- `SetAsDefault()`: 设置为默认角色
- `UnsetAsDefault()`: 取消默认角色
- `Activate()`: 激活关联
- `Deactivate()`: 停用关联
- `UpdateLastPlayed()`: 更新游戏时间

---

## 2. 数据传输对象 (DTOs)

### 2.1 UserStorageDto
**文件位置**: `src/BlazorWebGame.Shared/DTOs/DataStorageDTOs.cs`

**用途**: 用于数据存储层的用户数据传输

**主要属性**:
- `Id`: 用户ID
- `Username`: 用户名
- `Email`: 邮箱
- `IsActive`: 是否激活
- `EmailVerified`: 邮箱是否验证
- `LastLoginAt`: 最后登录时间
- `LastLoginIp`: 最后登录IP
- `LoginAttempts`: 登录尝试次数
- `LockedUntil`: 锁定截止时间
- `CreatedAt`: 创建时间
- `UpdatedAt`: 更新时间
- `Roles`: 角色列表
- `Profile`: 档案信息（字典）

### 2.2 UserInfoDto
**文件位置**: `src/BlazorWebGame.Shared/Interfaces/IAuthApi.cs`

**用途**: 用于API层返回用户信息

**主要属性**:
- `Id`: 用户ID
- `Username`: 用户名
- `Email`: 邮箱
- `Roles`: 角色列表

### 2.3 UserCharacterStorageDto
**文件位置**: `src/BlazorWebGame.Shared/DTOs/DataStorageDTOs.cs`

**用途**: 用户角色关联的数据传输

**主要属性**:
- `Id`: 关联ID
- `UserId`: 用户ID
- `CharacterId`: 角色ID
- `CharacterName`: 角色名称
- `IsActive`: 是否激活
- `IsDefault`: 是否为默认角色
- `LastPlayedAt`: 最后游戏时间
- `CreatedAt`: 创建时间
- `UpdatedAt`: 更新时间

### 2.4 认证相关DTOs
**文件位置**: `src/BlazorWebGame.Shared/DTOs/AuthenticationDTOs.cs`

#### LoginRequest
- `Username`: 用户名
- `Password`: 密码

#### RegisterRequest
- `Username`: 用户名
- `Password`: 密码
- `Email`: 邮箱

#### RefreshTokenRequest
- `RefreshToken`: 刷新令牌
- `UserId`: 用户ID

#### AuthenticationResponse
- `AccessToken`: 访问令牌
- `RefreshToken`: 刷新令牌
- `UserId`: 用户ID
- `Username`: 用户名
- `Roles`: 角色列表

---

## 3. 接口定义

### 3.1 IAuthApi 接口
**文件位置**: `src/BlazorWebGame.Shared/Interfaces/IAuthApi.cs`

**用途**: 定义认证系统的API接口

**方法列表**:

| 方法名 | 参数 | 返回类型 | 描述 |
|--------|------|----------|------|
| `LoginAsync` | `LoginRequest request` | `Task<ApiResponse<string>>` | 用户登录 |
| `RegisterAsync` | `RegisterRequest request` | `Task<ApiResponse<string>>` | 用户注册 |
| `RefreshTokenAsync` | `RefreshTokenRequest request` | `Task<ApiResponse<string>>` | 刷新令牌 |
| `LogoutAsync` | 无 | `Task<ApiResponse<bool>>` | 用户登出 |
| `GetCurrentUserAsync` | 无 | `Task<ApiResponse<UserInfoDto>>` | 获取当前用户信息 |
| `DemoLoginAsync` | 无 | `Task<ApiResponse<string>>` | 演示登录 |

### 3.2 IDataStorageService 接口（用户相关部分）
**文件位置**: `src/BlazorWebGame.Shared/Interfaces/IDataStorageService.cs`

**用途**: 定义数据存储服务接口，包含用户账号和用户角色关联管理

#### 用户账号管理方法

| 方法名 | 参数 | 返回类型 | 描述 |
|--------|------|----------|------|
| `GetUserByUsernameAsync` | `string username` | `Task<UserStorageDto?>` | 根据用户名获取用户 |
| `GetUserByIdAsync` | `string userId` | `Task<UserStorageDto?>` | 根据ID获取用户 |
| `GetUserByEmailAsync` | `string email` | `Task<UserStorageDto?>` | 根据邮箱获取用户 |
| `CreateUserAsync` | `UserStorageDto user, string password` | `Task<ApiResponse<UserStorageDto>>` | 创建新用户 |
| `UpdateUserAsync` | `UserStorageDto user` | `Task<ApiResponse<UserStorageDto>>` | 更新用户数据 |
| `ValidateUserPasswordAsync` | `string userId, string password` | `Task<bool>` | 验证用户密码 |
| `UpdateUserPasswordAsync` | `string userId, string newPassword` | `Task<ApiResponse<bool>>` | 更新用户密码 |
| `UpdateUserLastLoginAsync` | `string userId, string ipAddress` | `Task<ApiResponse<bool>>` | 更新最后登录信息 |
| `LockUserAccountAsync` | `string userId, DateTime lockUntil` | `Task<ApiResponse<bool>>` | 锁定用户账户 |
| `UnlockUserAccountAsync` | `string userId` | `Task<ApiResponse<bool>>` | 解锁用户账户 |

#### 用户角色关联管理方法

| 方法名 | 参数 | 返回类型 | 描述 |
|--------|------|----------|------|
| `CreateUserCharacterAsync` | `string userId, string characterId, string characterName, bool isDefault` | `Task<ApiResponse<UserCharacterStorageDto>>` | 创建用户角色关联 |
| `GetUserCharactersAsync` | `string userId` | `Task<ApiResponse<List<UserCharacterStorageDto>>>` | 获取用户的所有角色 |
| `GetCharacterOwnerAsync` | `string characterId` | `Task<UserCharacterStorageDto?>` | 获取角色的拥有者 |
| `UserOwnsCharacterAsync` | `string userId, string characterId` | `Task<bool>` | 验证用户是否拥有指定角色 |
| `SetDefaultCharacterAsync` | `string userId, string characterId` | `Task<ApiResponse<bool>>` | 设置默认角色 |
| `DeleteUserCharacterAsync` | `string userId, string characterId` | `Task<ApiResponse<bool>>` | 删除用户角色关联 |

---

## 4. 服务层

### 4.1 UserService
**文件位置**: `src/BlazorWebGame.Server/Services/UserService.cs`

**用途**: 处理用户认证、注册和管理的业务逻辑

**依赖**:
- `IDataStorageService _dataStorage`: 数据存储服务
- `ILogger<UserService> _logger`: 日志服务

**公共方法**:

| 方法名 | 参数 | 返回类型 | 描述 |
|--------|------|----------|------|
| `ValidateUserAsync` | `string username, string password` | `Task<UserStorageDto?>` | 验证用户凭据 |
| `RegisterUserAsync` | `string username, string password, string email` | `Task<ApiResponse<UserStorageDto>>` | 注册新用户 |
| `GetUserByIdAsync` | `string userId` | `Task<UserStorageDto?>` | 根据ID获取用户 |
| `UpdateLastLoginAsync` | `string userId, string ipAddress` | `Task<bool>` | 更新最后登录信息 |
| `UserHasRoleAsync` | `string userId, string role` | `Task<bool>` | 检查用户是否拥有指定角色 |
| `UserHasCharacterAsync` | `string userId, string characterId` | `Task<bool>` | 检查用户是否拥有角色 |

**私有方法**:
- `IncrementLoginAttemptsAsync(string userId)`: 增加登录尝试次数
- `ValidateRegistrationInput(...)`: 验证注册输入

**验证规则**:
- 用户名: 3-20个字符，只能包含字母、数字和下划线
- 密码: 至少6个字符
- 邮箱: 标准邮箱格式验证
- 登录失败5次后锁定账户30分钟

---

## 5. API 控制器

### 5.1 AuthController
**文件位置**: `src/BlazorWebGame.Server/Controllers/AuthController.cs`

**用途**: 提供用户认证相关的HTTP API端点

**依赖**:
- `GameAuthenticationService _authService`: 认证服务
- `UserService _userService`: 用户服务
- `ILogger<AuthController> _logger`: 日志服务

**API端点**:

#### POST /api/auth/login
**功能**: 用户登录

**请求体**: `LoginRequest`
```json
{
  "username": "testuser",
  "password": "password123"
}
```

**响应**: `ApiResponse<AuthenticationResponse>`
```json
{
  "success": true,
  "data": {
    "accessToken": "jwt_token",
    "refreshToken": "refresh_token",
    "userId": "user_id",
    "username": "testuser",
    "roles": ["Player"]
  },
  "message": "Login successful",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

#### POST /api/auth/register
**功能**: 用户注册

**请求体**: `RegisterRequest`
```json
{
  "username": "newuser",
  "password": "password123",
  "email": "user@example.com"
}
```

**响应**: `ApiResponse<AuthenticationResponse>`（同登录响应格式）

#### POST /api/auth/refresh
**功能**: 刷新访问令牌

**请求体**: `RefreshTokenRequest`
```json
{
  "refreshToken": "refresh_token",
  "userId": "user_id"
}
```

**响应**: `ApiResponse<AuthenticationResponse>`（同登录响应格式）

#### POST /api/auth/logout
**功能**: 用户登出

**认证**: 需要Bearer Token

**响应**: `ApiResponse<object>`
```json
{
  "success": true,
  "message": "Logout successful",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

#### GET /api/auth/me
**功能**: 获取当前用户信息

**认证**: 需要Bearer Token

**响应**: `ApiResponse<UserInfo>`
```json
{
  "success": true,
  "data": {
    "userId": "user_id",
    "username": "testuser",
    "roles": ["Player"]
  },
  "message": "User information retrieved successfully",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

#### POST /api/auth/demo-login
**功能**: 演示登录（测试用）

**响应**: 生成演示用户的访问令牌

### 5.2 CharacterController（用户相关部分）
**文件位置**: `src/BlazorWebGame.Server/Controllers/CharacterController.cs`

**API端点**:

#### GET /api/character/my
**功能**: 获取当前用户的角色列表

**认证**: 需要Bearer Token

**响应**: `ApiResponse<List<CharacterDto>>`

---

## 6. 安全和认证

### 6.1 GameAuthenticationService
**文件位置**: `src/BlazorWebGame.Server/Security/GameAuthenticationService.cs`

**功能**:
- 生成JWT访问令牌
- 生成刷新令牌
- 验证令牌
- 从Claims中提取用户信息

**关键方法**:
- `GenerateAccessToken(string userId, string username, List<string> roles)`: 生成访问令牌
- `GenerateRefreshToken()`: 生成刷新令牌
- `GetUserId(ClaimsPrincipal user)`: 获取用户ID
- `GetUsername(ClaimsPrincipal user)`: 获取用户名

---

## 7. 数据流图

```
用户请求
    ↓
AuthController (HTTP API)
    ↓
UserService (业务逻辑)
    ↓
IDataStorageService (数据访问)
    ↓
DataStorageService / SqliteDataStorageService (具体实现)
    ↓
数据库 / 内存存储
```

---

## 8. 用户账户相关的测试

### 8.1 UserServiceTests
**文件位置**: `src/BlazorWebGame.Server/Tests/UserServiceTests.cs`

**测试用例**:
- `TestUserRegistration`: 测试用户注册
- `TestUserLogin`: 测试用户登录
- `TestPasswordValidation`: 测试密码验证
- `TestUserRoles`: 测试用户角色

### 8.2 UserCharacterServiceTests
**文件位置**: `src/BlazorWebGame.Server/Tests/UserCharacterServiceTests.cs`

**测试用例**: 测试用户和角色的关联关系

---

## 9. 相关文档

项目中包含以下用户相关的文档：

1. **USER_AUTHENTICATION_SYSTEM_README.md**: 用户认证系统详细文档
2. **USER_CHARACTER_RELATIONSHIP_SYSTEM_README.md**: 用户角色关系系统文档
3. **DATA_STORAGE_SERVICE_README.md**: 数据存储服务文档

---

## 10. 修改建议和注意事项

### 10.1 修改用户接口时需要考虑的层次

修改用户相关功能时，需要按照以下顺序考虑各个层次：

1. **领域模型层** (`UserModels.cs`)
   - 修改User、UserProfile、UserSecurity等核心业务模型
   - 确保业务规则的一致性

2. **DTO层** (`DataStorageDTOs.cs`, `AuthenticationDTOs.cs`)
   - 修改UserStorageDto、UserInfoDto等传输对象
   - 保持与模型的映射关系

3. **接口层** (`IDataStorageService.cs`, `IAuthApi.cs`)
   - 修改接口定义
   - 考虑向后兼容性

4. **服务实现层** (`UserService.cs`, `DataStorageService.cs`)
   - 实现新的业务逻辑
   - 更新验证规则

5. **控制器层** (`AuthController.cs`, `CharacterController.cs`)
   - 更新API端点
   - 修改HTTP请求/响应格式

6. **测试层** (`UserServiceTests.cs`)
   - 添加或更新测试用例

### 10.2 关键约束和依赖

- **密码安全**: 密码使用BCrypt或类似算法加密存储
- **令牌管理**: JWT访问令牌用于API认证
- **账户锁定**: 5次登录失败后锁定30分钟
- **用户角色**: 默认角色为"Player"
- **用户-角色关联**: 通过UserCharacterRelation管理

### 10.3 数据库迁移

如果修改涉及数据库结构变化：
1. 更新实体模型
2. 创建数据库迁移
3. 更新种子数据
4. 测试迁移脚本

---

## 11. 总结

本文档整理了BlazorWebGame项目中所有涉及用户账户的接口和类。主要包括：

- **3个核心领域模型**: User, UserProfile, UserSecurity
- **4个DTO类**: UserStorageDto, UserInfoDto, UserCharacterStorageDto, 认证相关DTOs
- **2个接口**: IAuthApi (6个方法), IDataStorageService (16个用户相关方法)
- **1个核心服务**: UserService (6个公共方法)
- **2个控制器**: AuthController (6个端点), CharacterController (部分端点)

所有这些接口形成了一个完整的用户账户管理系统，支持：
- 用户注册和登录
- JWT令牌认证
- 用户信息管理
- 用户角色管理
- 账户安全（锁定机制）
- 用户-游戏角色关联

在修改这些接口时，需要考虑多个层次的协调一致性，并确保向后兼容性和安全性。
