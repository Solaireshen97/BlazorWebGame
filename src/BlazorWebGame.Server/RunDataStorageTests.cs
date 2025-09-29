using System;
using System.Threading.Tasks;
using BlazorWebGame.Server.Tests;

namespace BlazorWebGame.Server;

/// <summary>
/// 运行数据存储测试的简单程序
/// </summary>
public class RunDataStorageTests
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("🚀 BlazorWebGame 数据存储系统测试");
        Console.WriteLine("=====================================");
        
        var success = true;
        
        try
        {
            // 运行工厂模式测试
            Console.WriteLine("\n🔧 测试数据存储服务工厂...");
            using var factoryTests = new DataStorageServiceFactoryTests();
            await factoryTests.RunAllTests();
            
            // 运行SQLite数据存储服务测试
            Console.WriteLine("\n💾 测试SQLite数据存储服务...");
            using var sqliteTests = new SqliteDataStorageServiceTests();
            await sqliteTests.RunAllTests();
            
            Console.WriteLine("\n=====================================");
            Console.WriteLine("✅ 所有测试通过！数据存储系统工作正常");
            Console.WriteLine("✅ 工厂模式已正确实现");
            Console.WriteLine("✅ SQLite数据存储服务功能完整");
            Console.WriteLine("✅ 已移除不完整的SimpleSqliteDataStorageService实现");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 测试失败: {ex.Message}");
            Console.WriteLine($"详细错误: {ex}");
            success = false;
        }
        
        return success ? 0 : 1;
    }
}