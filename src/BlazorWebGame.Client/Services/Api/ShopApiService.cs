using BlazorWebGame.Shared.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 商店API服务，负责与服务器商店功能通信
/// </summary>
public class ShopApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShopApiService> _logger;

    public ShopApiService(HttpClient httpClient, ILogger<ShopApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有商店物品
    /// </summary>
    public async Task<ApiResponse<List<ShopItemDto>>> GetShopItemsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ShopItemDto>>>("api/shop/items");
            return response ?? new ApiResponse<List<ShopItemDto>>
            {
                Success = false,
                Message = "未能获取响应数据",
                Data = new List<ShopItemDto>()
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "获取商店物品时发生网络错误");
            return new ApiResponse<List<ShopItemDto>>
            {
                Success = false,
                Message = "网络连接错误",
                Data = new List<ShopItemDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取商店物品时发生未知错误");
            return new ApiResponse<List<ShopItemDto>>
            {
                Success = false,
                Message = "获取商店物品失败",
                Data = new List<ShopItemDto>(),
            };
        }
    }

    /// <summary>
    /// 根据分类获取商店物品
    /// </summary>
    public async Task<ApiResponse<List<ShopItemDto>>> GetShopItemsByCategoryAsync(string category)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return new ApiResponse<List<ShopItemDto>>
                {
                    Success = false,
                    Message = "分类名称不能为空",
                    Data = new List<ShopItemDto>()
                };
            }

            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ShopItemDto>>>($"api/shop/items/category/{category}");
            return response ?? new ApiResponse<List<ShopItemDto>>
            {
                Success = false,
                Message = "未能获取响应数据",
                Data = new List<ShopItemDto>()
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "获取分类 {Category} 的商店物品时发生网络错误", category);
            return new ApiResponse<List<ShopItemDto>>
            {
                Success = false,
                Message = "网络连接错误",
                Data = new List<ShopItemDto>(),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类 {Category} 的商店物品时发生未知错误", category);
            return new ApiResponse<List<ShopItemDto>>
            {
                Success = false,
                Message = $"获取分类 '{category}' 的商店物品失败",
                Data = new List<ShopItemDto>(),
            };
        }
    }

    /// <summary>
    /// 获取所有商店分类
    /// </summary>
    public async Task<ApiResponse<List<ShopCategoryDto>>> GetShopCategoriesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ShopCategoryDto>>>("api/shop/categories");
            return response ?? new ApiResponse<List<ShopCategoryDto>>
            {
                Success = false,
                Message = "未能获取响应数据",
                Data = new List<ShopCategoryDto>()
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "获取商店分类时发生网络错误");
            return new ApiResponse<List<ShopCategoryDto>>
            {
                Success = false,
                Message = "网络连接错误",
                Data = new List<ShopCategoryDto>(),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取商店分类时发生未知错误");
            return new ApiResponse<List<ShopCategoryDto>>
            {
                Success = false,
                Message = "获取商店分类失败",
                Data = new List<ShopCategoryDto>(),
            };
        }
    }

    /// <summary>
    /// 购买物品
    /// </summary>
    public async Task<ApiResponse<PurchaseResponseDto>> PurchaseItemAsync(PurchaseRequestDto request)
    {
        try
        {
            if (request == null)
            {
                return new ApiResponse<PurchaseResponseDto>
                {
                    Success = false,
                    Message = "购买请求不能为空"
                };
            }

            var response = await _httpClient.PostAsJsonAsync("api/shop/purchase", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<PurchaseResponseDto>>();
                return result ?? new ApiResponse<PurchaseResponseDto>
                {
                    Success = false,
                    Message = "未能解析响应数据"
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("购买物品请求失败: {StatusCode} - {Content}", response.StatusCode, errorContent);
                
                // 尝试解析错误响应
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ApiResponse<PurchaseResponseDto>>(errorContent);
                    if (errorResponse != null)
                        return errorResponse;
                }
                catch (JsonException)
                {
                    // 无法解析JSON，返回基本错误信息
                }

                return new ApiResponse<PurchaseResponseDto>
                {
                    Success = false,
                    Message = $"购买失败 (HTTP {response.StatusCode})",
                    
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "购买物品时发生网络错误: CharacterId={CharacterId}, ItemId={ItemId}", 
                request?.CharacterId, request?.ItemId);
            return new ApiResponse<PurchaseResponseDto>
            {
                Success = false,
                Message = "网络连接错误",
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "购买物品时发生未知错误: CharacterId={CharacterId}, ItemId={ItemId}", 
                request?.CharacterId, request?.ItemId);
            return new ApiResponse<PurchaseResponseDto>
            {
                Success = false,
                Message = "购买物品失败",
            };
        }
    }

    /// <summary>
    /// 出售物品
    /// </summary>
    public async Task<ApiResponse<SellResponseDto>> SellItemAsync(SellRequestDto request)
    {
        try
        {
            if (request == null)
            {
                return new ApiResponse<SellResponseDto>
                {
                    Success = false,
                    Message = "销售请求不能为空"
                };
            }

            var response = await _httpClient.PostAsJsonAsync("api/shop/sell", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<SellResponseDto>>();
                return result ?? new ApiResponse<SellResponseDto>
                {
                    Success = false,
                    Message = "未能解析响应数据"
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("出售物品请求失败: {StatusCode} - {Content}", response.StatusCode, errorContent);
                
                // 尝试解析错误响应
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ApiResponse<SellResponseDto>>(errorContent);
                    if (errorResponse != null)
                        return errorResponse;
                }
                catch (JsonException)
                {
                    // 无法解析JSON，返回基本错误信息
                }

                return new ApiResponse<SellResponseDto>
                {
                    Success = false,
                    Message = $"出售失败 (HTTP {response.StatusCode})",
                    
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "出售物品时发生网络错误: CharacterId={CharacterId}, ItemId={ItemId}", 
                request?.CharacterId, request?.ItemId);
            return new ApiResponse<SellResponseDto>
            {
                Success = false,
                Message = "网络连接错误",
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "出售物品时发生未知错误: CharacterId={CharacterId}, ItemId={ItemId}", 
                request?.CharacterId, request?.ItemId);
            return new ApiResponse<SellResponseDto>
            {
                Success = false,
                Message = "出售物品失败",
            };
        }
    }
}