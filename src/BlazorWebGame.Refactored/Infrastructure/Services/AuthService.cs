using BlazorWebGame.Refactored.Application.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlazorWebGame.Refactored.Infrastructure.Services;

/// <summary>
/// 认证服务实现
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<AuthService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private string? _currentUserId;
    private string? _currentUsername;
    private DateTime? _tokenExpiresAt;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_currentUserId) && 
                                   _tokenExpiresAt.HasValue && 
                                   _tokenExpiresAt.Value > DateTime.UtcNow;

    public string? CurrentUserId => _currentUserId;
    public string? CurrentUsername => _currentUsername;

    public AuthService(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        try
        {
            var loginRequest = new
            {
                Username = username,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
                
                if (loginResponse?.Success == true && loginResponse.Data != null)
                {
                    // 存储认证信息
                    await StoreAuthDataAsync(loginResponse.Data);
                    
                    // 设置HTTP客户端默认认证头
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.Data.AccessToken);

                    _logger.LogInformation("User {Username} logged in successfully", username);

                    return new AuthResult
                    {
                        IsSuccess = true,
                        AccessToken = loginResponse.Data.AccessToken,
                        RefreshToken = loginResponse.Data.RefreshToken,
                        UserId = loginResponse.Data.UserId,
                        Username = loginResponse.Data.Username,
                        ExpiresAt = loginResponse.Data.ExpiresAt
                    };
                }
            }

            var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(_jsonOptions);
            return new AuthResult
            {
                IsSuccess = false,
                ErrorMessage = errorResponse?.Message ?? "登录失败"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during login for user {Username}", username);
            return new AuthResult
            {
                IsSuccess = false,
                ErrorMessage = "网络连接失败，请检查网络设置"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for user {Username}", username);
            return new AuthResult
            {
                IsSuccess = false,
                ErrorMessage = "登录过程中发生错误，请重试"
            };
        }
    }

    public async Task<AuthResult> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = await _localStorage.GetItemAsync<string>("refresh_token");
            if (string.IsNullOrEmpty(refreshToken))
            {
                return new AuthResult
                {
                    IsSuccess = false,
                    ErrorMessage = "刷新令牌不存在"
                };
            }

            var refreshRequest = new { RefreshToken = refreshToken };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh", refreshRequest, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var refreshResponse = await response.Content.ReadFromJsonAsync<RefreshResponse>(_jsonOptions);
                
                if (refreshResponse?.Success == true && refreshResponse.Data != null)
                {
                    // 更新访问令牌
                    await _localStorage.SetItemAsync("access_token", refreshResponse.Data.AccessToken);
                    await _localStorage.SetItemAsync("token_expires_at", refreshResponse.Data.ExpiresAt);
                    
                    _tokenExpiresAt = refreshResponse.Data.ExpiresAt;
                    
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refreshResponse.Data.AccessToken);

                    _logger.LogInformation("Token refreshed successfully for user {UserId}", _currentUserId);

                    return new AuthResult
                    {
                        IsSuccess = true,
                        AccessToken = refreshResponse.Data.AccessToken,
                        ExpiresAt = refreshResponse.Data.ExpiresAt
                    };
                }
            }

            return new AuthResult
            {
                IsSuccess = false,
                ErrorMessage = "令牌刷新失败"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token for user {UserId}", _currentUserId);
            return new AuthResult
            {
                IsSuccess = false,
                ErrorMessage = "令牌刷新失败"
            };
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            // 调用服务器登出接口
            if (IsAuthenticated)
            {
                await _httpClient.PostAsync("/api/auth/logout", null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calling logout endpoint");
        }
        finally
        {
            // 清理本地存储
            await ClearAuthDataAsync();
            
            // 清理HTTP客户端认证头
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            _currentUserId = null;
            _currentUsername = null;
            _tokenExpiresAt = null;

            _logger.LogInformation("User logged out");
        }
    }

    public async Task<bool> ValidateTokenAsync()
    {
        try
        {
            if (!IsAuthenticated)
                return false;

            var response = await _httpClient.GetAsync("/api/auth/validate");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return false;
        }
    }

    /// <summary>
    /// 初始化时从本地存储恢复认证状态
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var accessToken = await _localStorage.GetItemAsync<string>("access_token");
            var userId = await _localStorage.GetItemAsync<string>("user_id");
            var username = await _localStorage.GetItemAsync<string>("username");
            var expiresAt = await _localStorage.GetItemAsync<DateTime?>("token_expires_at");

            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(userId) && expiresAt.HasValue)
            {
                _currentUserId = userId;
                _currentUsername = username;
                _tokenExpiresAt = expiresAt;

                if (IsAuthenticated)
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                    
                    _logger.LogInformation("Auth state restored for user {UserId}", userId);
                }
                else
                {
                    // Token已过期，尝试刷新
                    await RefreshTokenAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing auth service");
            await ClearAuthDataAsync();
        }
    }

    private async Task StoreAuthDataAsync(LoginData data)
    {
        await _localStorage.SetItemAsync("access_token", data.AccessToken);
        await _localStorage.SetItemAsync("refresh_token", data.RefreshToken);
        await _localStorage.SetItemAsync("user_id", data.UserId);
        await _localStorage.SetItemAsync("username", data.Username);
        await _localStorage.SetItemAsync("token_expires_at", data.ExpiresAt);

        _currentUserId = data.UserId;
        _currentUsername = data.Username;
        _tokenExpiresAt = data.ExpiresAt;
    }

    private async Task ClearAuthDataAsync()
    {
        await _localStorage.RemoveItemAsync("access_token");
        await _localStorage.RemoveItemAsync("refresh_token");
        await _localStorage.RemoveItemAsync("user_id");
        await _localStorage.RemoveItemAsync("username");
        await _localStorage.RemoveItemAsync("token_expires_at");
    }

    // 内部数据传输对象
    private record LoginResponse
    {
        public bool Success { get; init; }
        public LoginData? Data { get; init; }
        public string? Message { get; init; }
    }

    private record LoginData
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
    }

    private record RefreshResponse
    {
        public bool Success { get; init; }
        public RefreshData? Data { get; init; }
    }

    private record RefreshData
    {
        public string AccessToken { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
    }

    private record ApiErrorResponse
    {
        public string? Message { get; init; }
        public List<string> Errors { get; init; } = new();
    }
}