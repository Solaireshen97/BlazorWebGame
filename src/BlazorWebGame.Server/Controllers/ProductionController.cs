using BlazorWebGame.Server.Services;
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

    /// <summary>
    /// 获取所有可用的制作配方
    /// </summary>
    [HttpGet("recipes")]
    public ActionResult<ApiResponse<List<RecipeDto>>> GetAvailableRecipes([FromQuery] string profession = "")
    {
        try
        {
            var recipes = _productionService.GetAvailableRecipes(profession);
            return Ok(new ApiResponse<List<RecipeDto>>
            {
                Success = true,
                Message = "获取制作配方成功",
                Data = recipes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available recipes");
            return StatusCode(500, new ApiResponse<List<RecipeDto>>
            {
                Success = false,
                Message = "获取制作配方时发生错误"
            });
        }
    }

    /// <summary>
    /// 开始制作
    /// </summary>
    [HttpPost("crafting/start")]
    public async Task<ActionResult<ApiResponse<CraftingStateDto>>> StartCrafting(StartCraftingRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CharacterId) || string.IsNullOrEmpty(request.RecipeId))
            {
                return BadRequest(new ApiResponse<CraftingStateDto>
                {
                    Success = false,
                    Message = "角色ID和配方ID不能为空"
                });
            }

            var result = await _productionService.StartCraftingAsync(request);
            
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
            _logger.LogError(ex, "Error starting crafting for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<CraftingStateDto>
            {
                Success = false,
                Message = "开始制作时发生错误"
            });
        }
    }

    /// <summary>
    /// 停止制作
    /// </summary>
    [HttpPost("crafting/stop")]
    public async Task<ActionResult<ApiResponse<string>>> StopCrafting(StopCraftingRequest request)
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

            var result = await _productionService.StopCraftingAsync(request);
            
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
            _logger.LogError(ex, "Error stopping crafting for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<string>
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
    /// 停止所有生产活动
    /// </summary>
    [HttpPost("stop-all")]
    public async Task<ActionResult<ApiResponse<string>>> StopAllProductionActivities(StopAllProductionRequest request)
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

            var result = await _productionService.StopAllProductionAsync(request);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping all production activities for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "停止生产活动时发生错误"
            });
        }
    }
}