using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using BlazorWebGame.Server.Data;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Server.Services.Data;

/// <summary>
/// 数据库初始化服务 - 负责确保所有必要的数据表都存在
/// </summary>
public class DatabaseInitializationService
{
    private readonly IDbContextFactory<GameDbContext> _contextFactory;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(
        IDbContextFactory<GameDbContext> contextFactory,
        ILogger<DatabaseInitializationService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <summary>
    /// 初始化数据库并确保所有必要的表都存在
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            
            _logger.LogInformation("Starting database initialization...");
            
            // 检查数据库是否存在
            var databaseExists = await context.Database.CanConnectAsync();
            
            if (!databaseExists)
            {
                _logger.LogInformation("Database does not exist, creating new database with all tables...");
                await context.Database.EnsureCreatedAsync();
                _logger.LogInformation("Database and all tables created successfully");
                return;
            }

            _logger.LogInformation("Database exists, checking for missing tables...");
            
            // 数据库存在，检查并创建缺失的表
            await EnsureAllTablesExistAsync(context);
            
            _logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    /// <summary>
    /// 确保所有实体对应的表都存在
    /// </summary>
    private async Task EnsureAllTablesExistAsync(GameDbContext context)
    {
        // 获取所有实体类型
        var entityTypes = context.Model.GetEntityTypes();
        var missingTables = new List<string>();
        
        foreach (var entityType in entityTypes)
        {
            var tableName = entityType.GetTableName();
            if (string.IsNullOrEmpty(tableName))
                continue;
                
            _logger.LogDebug("Checking table: {TableName}", tableName);
            
            // 检查表是否存在
            var tableExists = await TableExistsAsync(context, tableName);
            
            if (!tableExists)
            {
                _logger.LogWarning("Table {TableName} does not exist", tableName);
                missingTables.Add(tableName);
            }
            else
            {
                _logger.LogDebug("Table {TableName} already exists", tableName);
            }
        }

        // 如果有缺失的表，重新创建数据库结构
        if (missingTables.Any())
        {
            _logger.LogInformation("Found {Count} missing tables: {Tables}. Recreating database structure...", 
                missingTables.Count, string.Join(", ", missingTables));
            
            await RecreateDatabase(context);
        }
    }

    /// <summary>
    /// 检查指定表是否存在
    /// </summary>
    private async Task<bool> TableExistsAsync(GameDbContext context, string tableName)
    {
        try
        {
            // 尝试查询表 - 这是最安全的方法
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\" LIMIT 1";
            
            try
            {
                await command.ExecuteScalarAsync();
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking if table {TableName} exists, assuming it doesn't", tableName);
            return false;
        }
    }

    /// <summary>
    /// 重新创建数据库结构（保持现有数据）
    /// </summary>
    private async Task RecreateDatabase(GameDbContext context)
    {
        try
        {
            _logger.LogInformation("Creating missing database tables...");
            
            // 获取当前数据库的创建脚本
            var script = context.Database.GenerateCreateScript();
            
            // 解析脚本并执行创建表的命令
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            try
            {
                // 分割脚本为单独的命令
                var commands = script.Split(new[] { ";\r\n", ";\n", ";" }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var commandText in commands)
                {
                    var trimmedCommand = commandText.Trim();
                    if (string.IsNullOrEmpty(trimmedCommand)) continue;
                    
                    // 只执行 CREATE TABLE 命令
                    if (trimmedCommand.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            using var command = connection.CreateCommand();
                            command.CommandText = trimmedCommand;
                            await command.ExecuteNonQueryAsync();
                            
                            // 从命令中提取表名来记录日志
                            var tableName = ExtractTableNameFromCreateCommand(trimmedCommand);
                            _logger.LogInformation("Created table: {TableName}", tableName);
                        }
                        catch (Exception ex)
                        {
                            // 如果表已存在，这个错误是预期的，可以忽略
                            _logger.LogDebug(ex, "Table creation command failed (may already exist): {Command}", 
                                trimmedCommand.Substring(0, Math.Min(100, trimmedCommand.Length)));
                        }
                    }
                }
                
                _logger.LogInformation("Database table creation process completed");
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create missing database tables");
            
            // 如果个别表创建失败，尝试完整的数据库重建作为后备方案
            _logger.LogWarning("Falling back to complete database recreation...");
            try
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
                _logger.LogInformation("Database fully recreated as fallback");
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Fallback database recreation also failed");
                throw;
            }
        }
    }

    /// <summary>
    /// 从 CREATE TABLE 命令中提取表名
    /// </summary>
    private string ExtractTableNameFromCreateCommand(string createCommand)
    {
        try
        {
            var parts = createCommand.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i].Equals("TABLE", StringComparison.OrdinalIgnoreCase))
                {
                    return parts[i + 1].Trim('"', '[', ']');
                }
            }
        }
        catch
        {
            // 忽略提取错误
        }
        return "Unknown";
    }

    /// <summary>
    /// 获取数据库健康状况
    /// </summary>
    public async Task<Dictionary<string, object>> GetDatabaseHealthAsync()
    {
        var health = new Dictionary<string, object>();
        
        try
        {
            using var context = _contextFactory.CreateDbContext();
            
            // 检查数据库连接
            var canConnect = await context.Database.CanConnectAsync();
            health["CanConnect"] = canConnect;
            
            if (canConnect)
            {
                // 检查每个表的状态
                var entityTypes = context.Model.GetEntityTypes();
                var tableStatus = new Dictionary<string, bool>();
                
                foreach (var entityType in entityTypes)
                {
                    var tableName = entityType.GetTableName();
                    if (!string.IsNullOrEmpty(tableName))
                    {
                        var exists = await TableExistsAsync(context, tableName);
                        tableStatus[tableName] = exists;
                    }
                }
                
                health["Tables"] = tableStatus;
                health["AllTablesExist"] = tableStatus.Values.All(exists => exists);
                health["TotalTables"] = tableStatus.Count;
                health["ExistingTables"] = tableStatus.Values.Count(exists => exists);
            }
            
            health["Status"] = "Healthy";
            health["CheckTime"] = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            health["Status"] = "Unhealthy";
            health["Error"] = ex.Message;
            health["CheckTime"] = DateTime.UtcNow;
            
            _logger.LogError(ex, "Database health check failed");
        }
        
        return health;
    }

    /// <summary>
    /// 验证数据库结构的完整性
    /// </summary>
    public async Task<bool> ValidateDatabaseIntegrityAsync()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            
            // 检查连接
            if (!await context.Database.CanConnectAsync())
            {
                _logger.LogWarning("Cannot connect to database");
                return false;
            }

            // 检查所有表是否存在
            var entityTypes = context.Model.GetEntityTypes();
            foreach (var entityType in entityTypes)
            {
                var tableName = entityType.GetTableName();
                if (!string.IsNullOrEmpty(tableName))
                {
                    var exists = await TableExistsAsync(context, tableName);
                    if (!exists)
                    {
                        _logger.LogWarning("Table {TableName} is missing", tableName);
                        return false;
                    }
                }
            }
            
            _logger.LogInformation("Database integrity validation passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database integrity validation failed");
            return false;
        }
    }
}