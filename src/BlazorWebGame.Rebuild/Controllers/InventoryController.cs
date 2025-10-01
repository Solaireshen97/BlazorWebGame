using BlazorWebGame.Rebuild.Services.Inventory;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BlazorWebGame.Rebuild.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly ServerInventoryService _inventoryService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(ServerInventoryService inventoryService, ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    /// <summary>
    /// 获取角色库存
    /// </summary>
    [HttpGet("{characterId}")]
    public ActionResult<ApiResponse<InventoryDto>> GetInventory(string characterId)
    {
        try
        {
            var inventory = _inventoryService.GetCharacterInventory(characterId);
            return Ok(new ApiResponse<InventoryDto>
            {
                Success = true,
                Data = inventory
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory for character {CharacterId}", characterId);
            return StatusCode(500, new ApiResponse<InventoryDto>
            {
                Success = false,
                Message = "获取库存时发生错误"
            });
        }
    }

    /// <summary>
    /// 添加物品到库存
    /// </summary>
    [HttpPost("add")]
    public async Task<ActionResult<ApiResponse<bool>>> AddItem([FromBody] AddItemRequest request)
    {
        try
        {
            var result = await _inventoryService.AddItemAsync(request.CharacterId, request.ItemId, request.Quantity);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "添加物品时发生错误"
            });
        }
    }

    /// <summary>
    /// 使用物品
    /// </summary>
    [HttpPost("use")]
    public async Task<ActionResult<ApiResponse<bool>>> UseItem([FromBody] UseItemRequest request)
    {
        try
        {
            var result = await _inventoryService.UseItemAsync(request.CharacterId, request.ItemId, request.SlotIndex);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error using item for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "使用物品时发生错误"
            });
        }
    }

    /// <summary>
    /// 装备物品
    /// </summary>
    [HttpPost("equip")]
    public async Task<ActionResult<ApiResponse<bool>>> EquipItem([FromBody] EquipItemRequest request)
    {
        try
        {
            var result = await _inventoryService.EquipItemAsync(request.CharacterId, request.ItemId, request.EquipmentSlot);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error equipping item for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "装备物品时发生错误"
            });
        }
    }

    /// <summary>
    /// 出售物品
    /// </summary>
    [HttpPost("sell")]
    public async Task<ActionResult<ApiResponse<int>>> SellItem([FromBody] SellItemRequest request)
    {
        try
        {
            var result = await _inventoryService.SellItemAsync(request.CharacterId, request.ItemId, request.Quantity);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selling item for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<int>
            {
                Success = false,
                Message = "出售物品时发生错误"
            });
        }
    }

    /// <summary>
    /// 同步库存数据
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<ApiResponse<bool>>> SyncInventory([FromBody] InventoryDto inventory)
    {
        try
        {
            var result = await _inventoryService.SyncInventoryAsync(inventory.CharacterId, inventory);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing inventory for character {CharacterId}", inventory.CharacterId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "同步库存时发生错误"
            });
        }
    }
}