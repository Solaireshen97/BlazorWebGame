using BlazorWebGame.Server.Security;
using BlazorWebGame.Server.Validation;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BlazorWebGame.Server.Services.Shop;

namespace BlazorWebGame.Server.Controllers;

/// <summary>
/// 商店控制器，处理所有商店相关的API请求
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ShopController : ControllerBase
{
    private readonly ServerShopService _shopService;
    private readonly GameAuthenticationService _authService;
    private readonly ILogger<ShopController> _logger;

    public ShopController(
        ServerShopService shopService,
        GameAuthenticationService authService,
        ILogger<ShopController> logger)
    {
        _shopService = shopService;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有商店物品
    /// </summary>
    [HttpGet("items")]
    [ProducesResponseType(typeof(ApiResponse<List<ShopItemDto>>), 200)]
    public async Task<ActionResult<ApiResponse<List<ShopItemDto>>>> GetShopItems()
    {
        try
        {
            var items = _shopService.GetShopItems();
            
            return Ok(new ApiResponse<List<ShopItemDto>>
            {
                Success = true,
                Data = items,
                Message = $"成功获取 {items.Count} 个商店物品"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取商店物品时发生错误");
            return StatusCode(500, new ApiResponse<List<ShopItemDto>>
            {
                Success = false,
                Message = "服务器内部错误",
                Errors = { ex.Message }
            });
        }
    }

    /// <summary>
    /// 根据分类获取商店物品
    /// </summary>
    [HttpGet("items/category/{category}")]
    [ProducesResponseType(typeof(ApiResponse<List<ShopItemDto>>), 200)]
    public async Task<ActionResult<ApiResponse<List<ShopItemDto>>>> GetShopItemsByCategory(string category)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return BadRequest(new ApiResponse<List<ShopItemDto>>
                {
                    Success = false,
                    Message = "分类名称不能为空"
                });
            }

            var items = _shopService.GetShopItemsByCategory(category);
            
            return Ok(new ApiResponse<List<ShopItemDto>>
            {
                Success = true,
                Data = items,
                Message = $"成功获取分类 '{category}' 的 {items.Count} 个物品"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类 {Category} 的商店物品时发生错误", category);
            return StatusCode(500, new ApiResponse<List<ShopItemDto>>
            {
                Success = false,
                Message = "服务器内部错误",
                Errors = { ex.Message }
            });
        }
    }

    /// <summary>
    /// 获取所有商店分类
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<List<ShopCategoryDto>>), 200)]
    public async Task<ActionResult<ApiResponse<List<ShopCategoryDto>>>> GetShopCategories()
    {
        try
        {
            var categories = _shopService.GetShopCategories();
            
            return Ok(new ApiResponse<List<ShopCategoryDto>>
            {
                Success = true,
                Data = categories,
                Message = $"成功获取 {categories.Count} 个商店分类"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取商店分类时发生错误");
            return StatusCode(500, new ApiResponse<List<ShopCategoryDto>>
            {
                Success = false,
                Message = "服务器内部错误",
                Errors = { ex.Message }
            });
        }
    }

    /// <summary>
    /// 购买物品
    /// </summary>
    [HttpPost("purchase")]
    [ValidateResourceOwnership("CharacterId", ResourceType.Character)]
    [ProducesResponseType(typeof(ApiResponse<PurchaseResponseDto>), 200)]
    public async Task<ActionResult<ApiResponse<PurchaseResponseDto>>> PurchaseItem([FromBody] PurchaseRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<PurchaseResponseDto>
                {
                    Success = false,
                    Message = "请求数据无效",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            // 验证用户权限
            var currentUserId = _authService.GetUserId(User);
            if (currentUserId != request.CharacterId)
            {
                return Forbid();
            }

            var result = await _shopService.PurchaseItemAsync(request);
            
            if (result.Success)
            {
                _logger.LogInformation("角色 {CharacterId} 成功购买了 {Quantity} 个 {ItemId}", 
                    request.CharacterId, request.Quantity, request.ItemId);
                
                return Ok(new ApiResponse<PurchaseResponseDto>
                {
                    Success = true,
                    Data = result,
                    Message = result.Message
                });
            }
            else
            {
                return BadRequest(new ApiResponse<PurchaseResponseDto>
                {
                    Success = false,
                    Data = result,
                    Message = result.Message
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "购买物品时发生错误: CharacterId={CharacterId}, ItemId={ItemId}", 
                request.CharacterId, request.ItemId);
            
            return StatusCode(500, new ApiResponse<PurchaseResponseDto>
            {
                Success = false,
                Message = "服务器内部错误",
                Errors = { ex.Message }
            });
        }
    }

    /// <summary>
    /// 出售物品
    /// </summary>
    [HttpPost("sell")]
    [ValidateResourceOwnership("CharacterId", ResourceType.Character)]
    [ProducesResponseType(typeof(ApiResponse<SellResponseDto>), 200)]
    public async Task<ActionResult<ApiResponse<SellResponseDto>>> SellItem([FromBody] SellRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<SellResponseDto>
                {
                    Success = false,
                    Message = "请求数据无效",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            // 验证用户权限
            var currentUserId = _authService.GetUserId(User);
            if (currentUserId != request.CharacterId)
            {
                return Forbid();
            }

            var result = await _shopService.SellItemAsync(request);
            
            if (result.Success)
            {
                _logger.LogInformation("角色 {CharacterId} 成功出售了 {Quantity} 个 {ItemId}", 
                    request.CharacterId, request.Quantity, request.ItemId);
                
                return Ok(new ApiResponse<SellResponseDto>
                {
                    Success = true,
                    Data = result,
                    Message = result.Message
                });
            }
            else
            {
                return BadRequest(new ApiResponse<SellResponseDto>
                {
                    Success = false,
                    Data = result,
                    Message = result.Message
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "出售物品时发生错误: CharacterId={CharacterId}, ItemId={ItemId}", 
                request.CharacterId, request.ItemId);
            
            return StatusCode(500, new ApiResponse<SellResponseDto>
            {
                Success = false,
                Message = "服务器内部错误",
                Errors = { ex.Message }
            });
        }
    }
}