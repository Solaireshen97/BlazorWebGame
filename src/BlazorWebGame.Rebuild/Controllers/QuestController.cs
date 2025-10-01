using BlazorWebGame.Rebuild.Services.Quset;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BlazorWebGame.Rebuild.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestController : ControllerBase
{
    private readonly ServerQuestService _questService;
    private readonly ILogger<QuestController> _logger;

    public QuestController(ServerQuestService questService, ILogger<QuestController> logger)
    {
        _questService = questService;
        _logger = logger;
    }

    /// <summary>
    /// 获取角色任务状态
    /// </summary>
    [HttpGet("status/{characterId}")]
    public ActionResult<ApiResponse<CharacterQuestStatusDto>> GetQuestStatus(string characterId)
    {
        try
        {
            var status = _questService.GetCharacterQuestStatus(characterId);
            return Ok(new ApiResponse<CharacterQuestStatusDto>
            {
                Success = true,
                Data = status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quest status for character {CharacterId}", characterId);
            return StatusCode(500, new ApiResponse<CharacterQuestStatusDto>
            {
                Success = false,
                Message = "获取任务状态时发生错误"
            });
        }
    }

    /// <summary>
    /// 获取每日任务
    /// </summary>
    [HttpGet("daily/{characterId}")]
    public async Task<ActionResult<ApiResponse<List<QuestDto>>>> GetDailyQuests(string characterId)
    {
        try
        {
            var result = await _questService.GetDailyQuestsAsync(characterId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily quests for character {CharacterId}", characterId);
            return StatusCode(500, new ApiResponse<List<QuestDto>>
            {
                Success = false,
                Message = "获取每日任务时发生错误"
            });
        }
    }

    /// <summary>
    /// 获取每周任务
    /// </summary>
    [HttpGet("weekly/{characterId}")]
    public async Task<ActionResult<ApiResponse<List<QuestDto>>>> GetWeeklyQuests(string characterId)
    {
        try
        {
            var result = await _questService.GetWeeklyQuestsAsync(characterId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weekly quests for character {CharacterId}", characterId);
            return StatusCode(500, new ApiResponse<List<QuestDto>>
            {
                Success = false,
                Message = "获取每周任务时发生错误"
            });
        }
    }

    /// <summary>
    /// 接受任务
    /// </summary>
    [HttpPost("accept")]
    public async Task<ActionResult<ApiResponse<bool>>> AcceptQuest([FromBody] AcceptQuestRequest request)
    {
        try
        {
            var result = await _questService.AcceptQuestAsync(request.CharacterId, request.QuestId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting quest for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "接受任务时发生错误"
            });
        }
    }

    /// <summary>
    /// 更新任务进度
    /// </summary>
    [HttpPost("progress")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateQuestProgress([FromBody] UpdateQuestProgressRequest request)
    {
        try
        {
            var result = await _questService.UpdateQuestProgressAsync(
                request.CharacterId, 
                request.QuestId, 
                request.ObjectiveId, 
                request.Progress);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quest progress for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "更新任务进度时发生错误"
            });
        }
    }

    /// <summary>
    /// 完成任务
    /// </summary>
    [HttpPost("complete")]
    public async Task<ActionResult<ApiResponse<List<QuestRewardDto>>>> CompleteQuest([FromBody] CompleteQuestRequest request)
    {
        try
        {
            var result = await _questService.CompleteQuestAsync(request.CharacterId, request.QuestId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing quest for character {CharacterId}", request.CharacterId);
            return StatusCode(500, new ApiResponse<List<QuestRewardDto>>
            {
                Success = false,
                Message = "完成任务时发生错误"
            });
        }
    }

    /// <summary>
    /// 同步任务状态
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<ApiResponse<bool>>> SyncQuestStatus([FromBody] CharacterQuestStatusDto status)
    {
        try
        {
            var result = await _questService.SyncQuestStatusAsync(status.CharacterId, status);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing quest status for character {CharacterId}", status.CharacterId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "同步任务状态时发生错误"
            });
        }
    }
}