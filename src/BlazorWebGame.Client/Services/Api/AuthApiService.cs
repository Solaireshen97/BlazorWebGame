using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Interfaces;
using System.Text.Json;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 认证系统API服务实现
/// </summary>
public class AuthApiService : BaseApiService, IAuthApi
{
    public AuthApiService(ConfigurableHttpClientFactory httpClientFactory, ILogger<AuthApiService> logger)
        : base(httpClientFactory, logger)
    {
    }

    public async Task<ApiResponse<string>> LoginAsync(LoginRequest request)
    {
        return await PostAsync<string>("api/auth/login", request);
    }

    public async Task<ApiResponse<string>> RegisterAsync(RegisterRequest request)
    {
        return await PostAsync<string>("api/auth/register", request);
    }

    public async Task<ApiResponse<string>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        return await PostAsync<string>("api/auth/refresh", request);
    }

    public async Task<ApiResponse<bool>> LogoutAsync()
    {
        return await PostAsync<bool>("api/auth/logout");
    }

    public async Task<ApiResponse<UserInfoDto>> GetCurrentUserAsync()
    {
        return await GetAsync<UserInfoDto>("api/auth/me");
    }

    /// <summary>
    /// 演示登录 - 开发用快速认证方法
    /// </summary>
    public async Task<ApiResponse<string>> DemoLoginAsync()
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.PostAsync("api/auth/demo-login", null);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);
                
                if (tokenResponse.TryGetProperty("token", out var tokenElement))
                {
                    var token = tokenElement.GetString();
                    
                    // 设置认证头到HttpClientFactory的所有实例
                    _httpClientFactory.SetAuthorizationHeader($"Bearer {token}");
                    _logger.LogInformation("Demo authentication successful");
                    
                    return new ApiResponse<string>
                    {
                        Success = true,
                        Data = token,
                        Message = "Demo login successful"
                    };
                }
                
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "No token received in demo login response"
                };
            }
            
            return new ApiResponse<string>
            {
                Success = false,
                Message = $"Demo login failed: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during demo login");
            return new ApiResponse<string>
            {
                Success = false,
                Message = "Demo login error occurred"
            };
        }
    }
}