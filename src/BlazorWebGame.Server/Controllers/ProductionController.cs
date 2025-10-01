using BlazorWebGame.Server.Services.Profession;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BlazorWebGame.Server.Controllers;

/// <summary>
/// 生产系统 API 控制器 - 处理采集、制作等生产活动
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductionController : ControllerBase
{
    private readonly ServerProductionService _productionService;
    private readonly ILogger<ProductionController> _logger;

    public ProductionController(ServerProductionService productionService, ILogger<ProductionController> logger)
    {
        _productionService = productionService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有可用的采集节点
    /// </summary>
    [HttpGet("nodes")]
    public ActionResult<ApiResponse<List<GatheringNodeDto>>> GetAvailableNodes([FromQuery] string profession = "")
    {
        try
        {
            var nodes = _productionService.GetAvailableNodes(profession);
            return Ok(new ApiResponse<List<GatheringNodeDto>>
            {
                Success = true,
                Message = "获取采集节点成功",
                Data = nodes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available gathering nodes");
            return StatusCode(500, new ApiResponse<List<GatheringNodeDto>>
            {
                Success = false,
                Message = "获取采集节点时发生错误"
            });
        }
    }

    /// <summary>
    /// 根据ID获取特定采集节点
    /// </summary>
    [HttpGet("nodes/{nodeId}")]
    public ActionResult<ApiResponse<GatheringNodeDto>> GetNodeById(string nodeId)
    {
        try
        {
            var node = _productionService.GetNodeById(nodeId);
            if (node == null)
            {
                return NotFound(new ApiResponse<GatheringNodeDto>
                {
                    Success = false,
                    Message = "采集节点不存在"
                });
            }

            return Ok(new ApiResponse<GatheringNodeDto>
            {
                Success = true,
                Message = "获取采集节点成功",
                Data = node
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gathering node {NodeId}", nodeId);
            return StatusCode(500, new ApiResponse<GatheringNodeDto>
            {
                Success = false,
                Message = "获取采集节点时发生错误"
            });
        }
    }

    /// <summary>
    /// 开始采集
    /// </summary>
    [HttpPost("gathering/start")]
    public async Task<ActionResult<ApiResponse<GatheringStateDto>>> StartGathering(StartGatheringRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CharacterId) || string.IsNullOrEmpty(request.NodeId))
            {
                return BadRequest(new ApiResponse<GatheringStateDto>
                {
                    Success = false,
                    Message = "角色ID和节点ID不能为空"
                });
            }

            var result = await _productionService.StartGatheringAsync(request);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting gathering for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<GatheringStateDto>
            {
                Success = false,
                Message = "开始采集时发生错误"
            });
        }
    }

    /// <summary>
    /// 停止采集
    /// </summary>
    [HttpPost("gathering/stop")]
    public async Task<ActionResult<ApiResponse<string>>> StopGathering(StopGatheringRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CharacterId))
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var result = await _productionService.StopGatheringAsync(request);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping gathering for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "停止采集时发生错误"
            });
        }
    }

    /// <summary>
    /// 获取玩家当前的采集状态
    /// </summary>
    [HttpGet("gathering/state/{characterId}")]
    public ActionResult<ApiResponse<GatheringStateDto>> GetGatheringState(string characterId)
    {
        try
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return BadRequest(new ApiResponse<GatheringStateDto>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var state = _productionService.GetGatheringState(characterId);
            
            return Ok(new ApiResponse<GatheringStateDto>
            {
                Success = true,
                Message = state != null ? "获取采集状态成功" : "玩家当前未在采集",
                Data = state
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gathering state for character {CharacterId}", characterId);
            return StatusCode(500, new ApiResponse<GatheringStateDto>
            {
                Success = false,
                Message = "获取采集状态时发生错误"
            });
        }
    }

    /// <summary>
    /// 获取所有活跃的采集状态（管理用）
    /// </summary>
    [HttpGet("gathering/active")]
    public ActionResult<ApiResponse<List<GatheringStateDto>>> GetActiveGatheringStates()
    {
        try
        {
            var states = _productionService.GetAllActiveGatheringStates();
            return Ok(new ApiResponse<List<GatheringStateDto>>
            {
                Success = true,
                Message = "获取活跃采集状态成功",
                Data = states
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active gathering states");
            return StatusCode(500, new ApiResponse<List<GatheringStateDto>>
            {
                Success = false,
                Message = "获取活跃采集状态时发生错误"
            });
        }
    }

    // ==================== 制作系统 API ====================

    /// <summary>
    /// 获取可用配方列表
    /// </summary>
    [HttpPost("recipes")]
    public ActionResult<ApiResponse<List<RecipeDto>>> GetRecipes(GetRecipesRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CharacterId))
            {
                return BadRequest(new ApiResponse<List<RecipeDto>>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var recipes = _productionService.GetAvailableRecipes(request.CharacterId, request.Profession, request.MaxLevel);
            return Ok(new ApiResponse<List<RecipeDto>>
            {
                Success = true,
                Message = "获取配方列表成功",
                Data = recipes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipes for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<List<RecipeDto>>
            {
                Success = false,
                Message = "获取配方列表时发生错误"
            });
        }
    }

    /// <summary>
    /// 获取指定配方信息
    /// </summary>
    [HttpGet("recipes/{recipeId}")]
    public ActionResult<ApiResponse<RecipeDto>> GetRecipe(string recipeId)
    {
        try
        {
            var recipe = _productionService.GetRecipeById(recipeId);
            if (recipe == null)
            {
                return NotFound(new ApiResponse<RecipeDto>
                {
                    Success = false,
                    Message = "配方不存在"
                });
            }

            return Ok(new ApiResponse<RecipeDto>
            {
                Success = true,
                Message = "获取配方信息成功",
                Data = recipe
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipe {RecipeId}", recipeId);
            return StatusCode(500, new ApiResponse<RecipeDto>
            {
                Success = false,
                Message = "获取配方信息时发生错误"
            });
        }
    }

    /// <summary>
    /// 开始制作
    /// </summary>
    [HttpPost("crafting/start")]
    public async Task<ActionResult<ApiResponse<bool>>> StartCrafting(StartCraftingRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CharacterId) || string.IsNullOrEmpty(request.RecipeId))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "角色ID和配方ID不能为空"
                });
            }

            var result = await _productionService.StartCraftingAsync(request.CharacterId, request.RecipeId, request.Quantity);
            
            if (result.Success)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "开始制作成功",
                    Data = true
                });
            }
            else
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = result.Message,
                    Data = false
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting crafting for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "开始制作时发生错误",
                Data = false
            });
        }
    }

    /// <summary>
    /// 批量制作
    /// </summary>
    [HttpPost("crafting/batch")]
    public async Task<ActionResult<ApiResponse<bool>>> StartBatchCrafting(BatchCraftingRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CharacterId) || !request.Items.Any())
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "角色ID和制作项目不能为空"
                });
            }

            var result = await _productionService.StartBatchCraftingAsync(request.CharacterId, request.Items);
            
            if (result.Success)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "开始批量制作成功",
                    Data = true
                });
            }
            else
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = result.Message,
                    Data = false
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting batch crafting for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "开始批量制作时发生错误",
                Data = false
            });
        }
    }

    /// <summary>
    /// 停止制作
    /// </summary>
    [HttpPost("crafting/stop")]
    public async Task<ActionResult<ApiResponse<CraftingResultDto>>> StopCrafting(StopCraftingRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CharacterId))
            {
                return BadRequest(new ApiResponse<CraftingResultDto>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var result = await _productionService.StopCraftingAsync(request.CharacterId);
            
            return Ok(new ApiResponse<CraftingResultDto>
            {
                Success = result.Success,
                Message = result.Message,
                Data = result.Success ? result.Data : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping crafting for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<CraftingResultDto>
            {
                Success = false,
                Message = "停止制作时发生错误"
            });
        }
    }

    /// <summary>
    /// 获取玩家当前的制作状态
    /// </summary>
    [HttpGet("crafting/state/{characterId}")]
    public ActionResult<ApiResponse<CraftingStateDto>> GetCraftingState(string characterId)
    {
        try
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return BadRequest(new ApiResponse<CraftingStateDto>
                {
                    Success = false,
                    Message = "角色ID不能为空"
                });
            }

            var state = _productionService.GetCraftingState(characterId);
            
            return Ok(new ApiResponse<CraftingStateDto>
            {
                Success = true,
                Message = state != null ? "获取制作状态成功" : "玩家当前未在制作",
                Data = state
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting crafting state for character {CharacterId}", characterId);
            return StatusCode(500, new ApiResponse<CraftingStateDto>
            {
                Success = false,
                Message = "获取制作状态时发生错误"
            });
        }
    }

    /// <summary>
    /// 检查节点解锁状态
    /// </summary>
    [HttpPost("nodes/unlock-status")]
    public ActionResult<ApiResponse<NodeUnlockStatusDto>> CheckNodeUnlockStatus(NodeUnlockCheckRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CharacterId) || string.IsNullOrEmpty(request.NodeId))
            {
                return BadRequest(new ApiResponse<NodeUnlockStatusDto>
                {
                    Success = false,
                    Message = "角色ID和节点ID不能为空"
                });
            }

            var status = _productionService.CheckNodeUnlockStatus(request.CharacterId, request.NodeId);
            return Ok(new ApiResponse<NodeUnlockStatusDto>
            {
                Success = true,
                Message = "检查节点解锁状态成功",
                Data = status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking node unlock status for character {CharacterId}, node {NodeId}", request.CharacterId, request.NodeId);
            return StatusCode(500, new ApiResponse<NodeUnlockStatusDto>
            {
                Success = false,
                Message = "检查节点解锁状态时发生错误"
            });
        }
    }

    /// <summary>
    /// 验证制作材料是否充足
    /// </summary>
    [HttpGet("crafting/materials-check/{characterId}/{recipeId}")]
    public ActionResult<ApiResponse<bool>> CheckCraftingMaterials(string characterId, string recipeId, [FromQuery] int quantity = 1)
    {
        try
        {
            if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(recipeId))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "角色ID和配方ID不能为空"
                });
            }

            var hasMaterials = _productionService.CheckCraftingMaterials(characterId, recipeId, quantity);
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = hasMaterials ? "材料充足" : "材料不足",
                Data = hasMaterials
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking crafting materials for character {CharacterId}, recipe {RecipeId}", characterId, recipeId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "检查制作材料时发生错误"
            });
        }
    }
}