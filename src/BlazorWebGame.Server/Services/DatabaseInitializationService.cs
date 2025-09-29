using Microsoft.EntityFrameworkCore;
using BlazorWebGame.Server.Data;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 数据库初始化服务 - 确保数据库架构与模型同步
/// </summary>
public class DatabaseInitializationService
{
    private readonly ConsolidatedGameDbContext _context;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(
        ConsolidatedGameDbContext context, 
        ILogger<DatabaseInitializationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 初始化数据库，确保所有表和列都存在
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("开始数据库架构初始化...");

            // 确保数据库已创建
            await _context.Database.EnsureCreatedAsync();
            
            // 检查并创建 Users 表
            await EnsureUserTableExistsAsync();
            
            // 检查并添加 Players 表的 UserId 列
            await EnsurePlayerUserIdColumnExistsAsync();
            
            _logger.LogInformation("✅ 数据库架构初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 数据库架构初始化失败");
            throw;
        }
    }

    /// <summary>
    /// 确保 Users 表存在
    /// </summary>
    private async Task EnsureUserTableExistsAsync()
    {
        try
        {
            // 检查 Users 表是否存在
            var tableExists = false;
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT name FROM Users LIMIT 1");
                tableExists = true;
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("no such table"))
            {
                tableExists = false;
            }
            
            if (!tableExists)
            {
                _logger.LogInformation("创建 Users 表...");
                
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE Users (
                        Id TEXT PRIMARY KEY,
                        Username TEXT UNIQUE NOT NULL,
                        Email TEXT UNIQUE NOT NULL,
                        PasswordHash TEXT NOT NULL,
                        Salt TEXT NOT NULL,
                        CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        LastLoginAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        IsActive BOOLEAN NOT NULL DEFAULT 1,
                        Roles TEXT NOT NULL DEFAULT 'Player',
                        RefreshToken TEXT,
                        RefreshTokenExpiryTime DATETIME,
                        UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                    )");
                
                // 创建索引
                await _context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_Users_Username ON Users (Username)");
                await _context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_Users_Email ON Users (Email)");
                await _context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_Users_IsActive ON Users (IsActive)");
                await _context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_Users_LastLoginAt ON Users (LastLoginAt)");
                await _context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_Users_CreatedAt ON Users (CreatedAt)");
                
                _logger.LogInformation("✅ Users 表创建完成");
            }
            else
            {
                _logger.LogInformation("Users 表已存在");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建 Users 表失败");
            throw;
        }
    }

    /// <summary>
    /// 确保 Players 表有 UserId 列
    /// </summary>
    private async Task EnsurePlayerUserIdColumnExistsAsync()
    {
        try
        {
            // 检查 UserId 列是否存在
            var columnExists = false;
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT UserId FROM Players LIMIT 1");
                columnExists = true;
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("no such column"))
            {
                columnExists = false;
            }

            if (!columnExists)
            {
                _logger.LogInformation("为 Players 表添加 UserId 列...");
                
                await _context.Database.ExecuteSqlRawAsync("ALTER TABLE Players ADD COLUMN UserId TEXT");
                await _context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_Players_UserId ON Players (UserId)");
                
                _logger.LogInformation("✅ Players 表 UserId 列添加完成");
            }
            else
            {
                _logger.LogInformation("Players 表 UserId 列已存在");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加 Players 表 UserId 列失败");
            throw;
        }
    }

    /// <summary>
    /// 创建测试用户数据
    /// </summary>
    public async Task CreateTestUsersAsync()
    {
        try
        {
            _logger.LogInformation("创建测试用户数据...");

            // 检查是否已有测试用户
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "testuser");
            if (existingUser != null)
            {
                _logger.LogInformation("测试用户已存在，跳过创建");
                return;
            }

            // 使用 UserService 创建测试用户
            var userServiceLogger = new LoggerFactory().CreateLogger<UserService>();
            var userService = new UserService(_context, userServiceLogger);
            
            var testUser = await userService.CreateUserAsync(
                "testuser", 
                "testuser@example.com", 
                "testpass123",
                new List<string> { "Player", "Tester" });

            _logger.LogInformation("✅ 测试用户创建完成: {Username} (ID: {UserId})", 
                testUser.Username, testUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "创建测试用户失败");
        }
    }
}