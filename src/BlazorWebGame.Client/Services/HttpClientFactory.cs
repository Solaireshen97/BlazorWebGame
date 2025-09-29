namespace BlazorWebGame.Client.Services;

/// <summary>
/// 管理可动态配置服务器地址的 HttpClient 工厂
/// </summary>
public class ConfigurableHttpClientFactory
{
    private readonly ServerConfigurationService _serverConfig;
    private readonly ILogger<ConfigurableHttpClientFactory> _logger;
    private HttpClient? _currentHttpClient;
    private string? _currentAuthHeader; // 存储当前的认证头部

    public ConfigurableHttpClientFactory(
        ServerConfigurationService serverConfig,
        ILogger<ConfigurableHttpClientFactory> logger)
    {
        _serverConfig = serverConfig;
        _logger = logger;
        
        // 订阅服务器地址变更事件
        _serverConfig.ServerUrlChanged += OnServerUrlChanged;
    }

    /// <summary>
    /// 获取配置了当前服务器地址的 HttpClient
    /// </summary>
    public HttpClient GetHttpClient()
    {
        if (_currentHttpClient == null || 
            _currentHttpClient.BaseAddress?.ToString() != _serverConfig.CurrentServerUrl)
        {
            CreateNewHttpClient();
        }
        
        return _currentHttpClient!;
    }

    /// <summary>
    /// 设置认证头部到当前HttpClient
    /// </summary>
    public void SetAuthorizationHeader(string authHeaderValue)
    {
        // 保存认证头部值
        _currentAuthHeader = authHeaderValue;
        
        var httpClient = GetHttpClient();
        if (authHeaderValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeaderValue.Substring("Bearer ".Length);
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authHeaderValue);
        }
        
        _logger.LogInformation("已设置认证头部到 HttpClient");
    }

    /// <summary>
    /// 创建新的 HttpClient 实例
    /// </summary>
    private void CreateNewHttpClient()
    {
        // 清理旧的 HttpClient
        _currentHttpClient?.Dispose();
        
        // 创建新的 HttpClient
        _currentHttpClient = new HttpClient();
        _currentHttpClient.BaseAddress = new Uri(_serverConfig.CurrentServerUrl);
        
        // 设置默认超时
        _currentHttpClient.Timeout = TimeSpan.FromSeconds(30);
        
        // 恢复认证头部（如果有的话）- 直接设置而不调用SetAuthorizationHeader避免递归
        if (!string.IsNullOrEmpty(_currentAuthHeader))
        {
            if (_currentAuthHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = _currentAuthHeader.Substring("Bearer ".Length);
                _currentHttpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _currentHttpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentAuthHeader);
            }
        }
        
        _logger.LogInformation("创建新的 HttpClient，服务器地址: {BaseAddress}", _currentHttpClient.BaseAddress);
    }

    /// <summary>
    /// 服务器地址变更时的回调
    /// </summary>
    private void OnServerUrlChanged(string newUrl)
    {
        _logger.LogInformation("检测到服务器地址变更，将在下次请求时更新 HttpClient");
        // 不在这里立即创建新的 HttpClient，而是在下次 GetHttpClient 调用时创建
        // 这样可以避免在事件处理中创建可能不会被使用的实例
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _serverConfig.ServerUrlChanged -= OnServerUrlChanged;
        _currentHttpClient?.Dispose();
    }
}