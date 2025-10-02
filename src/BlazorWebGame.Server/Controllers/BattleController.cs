using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BlazorWebGame.Server.Services.Battle;
using BlazorWebGame.Shared.DTOs;
using System.Threading.Tasks;

namespace BlazorWebGame.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BattleController : ControllerBase
    {
        private readonly IBattleService _battleService;
        private readonly ILogger<BattleController> _logger;

        public BattleController(
            IBattleService battleService,
            ILogger<BattleController> logger)
        {
            _battleService = battleService;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBattle([FromBody] CreateBattleRequest request)
        {
            var result = await _battleService.CreateBattleAsync(
                request.CharacterId,
                request.EnemyId,
                request.BattleType ?? "Normal",
                request.RegionId
            );

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("{battleId}/start")]
        public async Task<IActionResult> StartBattle(string battleId)
        {
            var result = await _battleService.StartBattleAsync(battleId);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("{battleId}/skill")]
        public async Task<IActionResult> UseSkill(
            string battleId,
            [FromBody] UseSkillRequest request)
        {
            var result = await _battleService.UseSkillAsync(
                battleId,
                request.CasterId,
                request.SkillId,
                request.TargetId
            );

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpGet("{battleId}/status")]
        public async Task<IActionResult> GetBattleStatus(string battleId)
        {
            var result = await _battleService.GetBattleStatusAsync(battleId);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }

    public class CreateBattleRequest
    {
        public string CharacterId { get; set; } = string.Empty;
        public string EnemyId { get; set; } = string.Empty;
        public string? BattleType { get; set; }
        public string? RegionId { get; set; }
    }

    public class UseSkillRequest
    {
        public string CasterId { get; set; } = string.Empty;
        public string SkillId { get; set; } = string.Empty;
        public string? TargetId { get; set; }
    }
}