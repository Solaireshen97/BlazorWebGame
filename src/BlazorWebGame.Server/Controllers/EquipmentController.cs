using BlazorWebGame.Server.Services.Equipments;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BlazorWebGame.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EquipmentController : ControllerBase
{
    private readonly ServerEquipmentService _equipmentService;
    private readonly ILogger<EquipmentController> _logger;

    public EquipmentController(
        ServerEquipmentService equipmentService,
        ILogger<EquipmentController> logger)
    {
        _equipmentService = equipmentService;
        _logger = logger;
    }

    /// <summary>
    /// 生成装备
    /// </summary>
    [HttpPost("generate")]
    public ActionResult<ApiResponse<EquipmentDto>> GenerateEquipment([FromBody] EquipmentGenerationRequest request)
    {
        try
        {
            _logger.LogDebug("收到装备生成请求: {Request}", request.Name);

            if (string.IsNullOrEmpty(request.Name))
            {
                return BadRequest(new ApiResponse<EquipmentDto>
                {
                    Success = false,
                    Message = "装备名称不能为空"
                });
            }

            if (request.Level <= 0)
            {
                return BadRequest(new ApiResponse<EquipmentDto>
                {
                    Success = false,
                    Message = "装备等级必须大于0"
                });
            }

            var equipment = _equipmentService.GenerateEquipment(request);

            return Ok(new ApiResponse<EquipmentDto>
            {
                Success = true,
                Message = "装备生成成功",
                Data = equipment
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成装备失败: {RequestName}", request.Name);
            return StatusCode(500, new ApiResponse<EquipmentDto>
            {
                Success = false,
                Message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 计算装备价值
    /// </summary>
    [HttpPost("calculate-value")]
    public ActionResult<ApiResponse<int>> CalculateEquipmentValue([FromBody] EquipmentDto equipment)
    {
        try
        {
            var value = _equipmentService.CalculateEquipmentValue(equipment);

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Message = "价值计算成功",
                Data = value
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算装备价值失败: {EquipmentName}", equipment.Name);
            return StatusCode(500, new ApiResponse<int>
            {
                Success = false,
                Message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 根据名称猜测武器类型
    /// </summary>
    [HttpGet("guess-weapon-type/{name}")]
    public ActionResult<ApiResponse<string>> GuessWeaponType(string name)
    {
        try
        {
            var weaponType = _equipmentService.GuessWeaponTypeFromName(name);

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "武器类型猜测成功",
                Data = weaponType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "猜测武器类型失败: {WeaponName}", name);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 根据名称猜测护甲类型
    /// </summary>
    [HttpGet("guess-armor-type/{name}")]
    public ActionResult<ApiResponse<string>> GuessArmorType(string name)
    {
        try
        {
            var armorType = _equipmentService.GuessArmorTypeFromName(name);

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "护甲类型猜测成功",
                Data = armorType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "猜测护甲类型失败: {ArmorName}", name);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 批量生成装备示例
    /// </summary>
    [HttpPost("generate-batch")]
    public ActionResult<ApiResponse<List<EquipmentDto>>> GenerateBatchEquipment([FromBody] List<EquipmentGenerationRequest> requests)
    {
        try
        {
            _logger.LogDebug("收到批量装备生成请求，数量: {Count}", requests.Count);

            if (requests.Count > 50) // 限制批量数量
            {
                return BadRequest(new ApiResponse<List<EquipmentDto>>
                {
                    Success = false,
                    Message = "批量生成数量不能超过50个"
                });
            }

            var equipments = new List<EquipmentDto>();

            foreach (var request in requests)
            {
                try
                {
                    var equipment = _equipmentService.GenerateEquipment(request);
                    equipments.Add(equipment);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "批量生成中单个装备失败: {RequestName}", request.Name);
                    // 继续处理其他装备，不中断整个批量操作
                }
            }

            return Ok(new ApiResponse<List<EquipmentDto>>
            {
                Success = true,
                Message = $"批量生成完成，成功生成 {equipments.Count} 件装备",
                Data = equipments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量生成装备失败");
            return StatusCode(500, new ApiResponse<List<EquipmentDto>>
            {
                Success = false,
                Message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 生成装备示例
    /// </summary>
    [HttpGet("examples")]
    public ActionResult<ApiResponse<List<EquipmentDto>>> GenerateExamples()
    {
        try
        {
            var examples = new List<EquipmentDto>();

            // 生成一些示例装备
            var requests = new List<EquipmentGenerationRequest>
            {
                new EquipmentGenerationRequest
                {
                    Name = "新手铁剑",
                    Level = 1,
                    Slot = "MainHand",
                    Quality = EquipmentQuality.Common,
                    WeaponType = "Sword"
                },
                new EquipmentGenerationRequest
                {
                    Name = "精良皮甲",
                    Level = 5,
                    Slot = "Chest",
                    Quality = EquipmentQuality.Uncommon,
                    ArmorType = "Leather"
                },
                new EquipmentGenerationRequest
                {
                    Name = "稀有法杖",
                    Level = 10,
                    Slot = "MainHand",
                    Quality = EquipmentQuality.Rare,
                    WeaponType = "Staff",
                    IsTwoHanded = true
                },
                new EquipmentGenerationRequest
                {
                    Name = "史诗护身符",
                    Level = 15,
                    Slot = "Trinket1",
                    Quality = EquipmentQuality.Epic
                }
            };

            foreach (var request in requests)
            {
                var equipment = _equipmentService.GenerateEquipment(request);
                examples.Add(equipment);
            }

            return Ok(new ApiResponse<List<EquipmentDto>>
            {
                Success = true,
                Message = "示例装备生成成功",
                Data = examples
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成示例装备失败");
            return StatusCode(500, new ApiResponse<List<EquipmentDto>>
            {
                Success = false,
                Message = "服务器内部错误"
            });
        }
    }
}