using Microsoft.JSInterop;
using System.Text.Json;

namespace BlazorWebGame.Client.Services;

/// <summary>
/// 统一的服务器配置管理服务
/// </summary>
public class ServerConfigurationService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ServerConfigurationService> _logger;
    private const string STORAGE_KEY = "blazor-game-server-config";
    
    private string _currentServerUrl = "https://localhost:7000"; // 默认值
    
    public event Action<string>? ServerUrlChanged;
    
    public string CurrentServerUrl => _currentServerUrl;

    public ServerConfigurationService(IJSRuntime jsRuntime, ILogger<ServerConfigurationService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// 初始化配置服务，从本地存储加载设置
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var stored = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", STORAGE_KEY);
            if (!string.IsNullOrEmpty(stored))
            {
                var config = JsonSerializer.Deserialize<ServerConfig>(stored);
                if (config != null && !string.IsNullOrEmpty(config.ServerUrl))
                {
                    _currentServerUrl = config.ServerUrl;
                    _logger.LogInformation("从本地存储加载服务器地址: {Url}", _currentServerUrl);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从本地存储加载服务器配置失败，使用默认设置");
        }
    }

    /// <summary>
    /// 设置新的服务器地址
    /// </summary>
    public async Task SetServerUrlAsync(string serverUrl)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            throw new ArgumentException("服务器地址不能为空", nameof(serverUrl));
        }

        // 规范化URL格式
        if (!serverUrl.StartsWith("http://") && !serverUrl.StartsWith("https://"))
        {
            serverUrl = "https://" + serverUrl;
        }

        var oldUrl = _currentServerUrl;
        _currentServerUrl = serverUrl;

        try
        {
            // 保存到本地存储
            var config = new ServerConfig { ServerUrl = serverUrl };
            var json = JsonSerializer.Serialize(config);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", STORAGE_KEY, json);
            
            _logger.LogInformation("服务器地址已更新: {OldUrl} -> {NewUrl}", oldUrl, serverUrl);
            
            // 通知所有订阅者
            ServerUrlChanged?.Invoke(serverUrl);
        }
        catch (Exception ex)
        {
            // 回滚更改
            _currentServerUrl = oldUrl;
            _logger.LogError(ex, "保存服务器配置失败");
            throw new InvalidOperationException("保存服务器配置失败", ex);
        }
    }

    /// <summary>
    /// 获取预设的服务器地址选项
    /// </summary>
    public List<ServerOption> GetPresetServerOptions()
    {
        return new List<ServerOption>
        {
            new("本地开发服务器", "https://localhost:7000"),
            new("本地开发服务器 (HTTP)", "http://localhost:7000"),
            new("本地开发服务器 (7001)", "https://localhost:7001"),
            new("测试服务器", "https://test-server.example.com"),
        };
    }

    /// <summary>
    /// 测试服务器连接
    /// </summary>
    public async Task<bool> TestServerConnectionAsync(string? serverUrl = null)
    {
        var testUrl = serverUrl ?? _currentServerUrl;
        
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            httpClient.BaseAddress = new Uri(testUrl);
            
            var response = await httpClient.GetAsync("/health/simple");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "服务器连接测试失败: {Url}", testUrl);
            return false;
        }
    }

    /// <summary>
    /// 重置为默认配置
    /// </summary>
    public async Task ResetToDefaultAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", STORAGE_KEY);
            await SetServerUrlAsync("https://localhost:7000");
            _logger.LogInformation("服务器配置已重置为默认值");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置服务器配置失败");
            throw;
        }
    }
}

/// <summary>
/// 服务器配置数据模型
/// </summary>
public class ServerConfig
{
    public string ServerUrl { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 服务器选项
/// </summary>
public record ServerOption(string Name, string Url);