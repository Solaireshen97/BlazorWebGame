using BlazorWebGame.Server.Services.Character;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.DTOs.Character;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlazorWebGame.Server.Controllers
{
    [ApiController]
    [Route("api/character")]
    public class EnhancedCharacterController : ControllerBase
    {
        private readonly EnhancedServerCharacterService _characterService;
        private readonly ILogger<EnhancedCharacterController> _logger;

        public EnhancedCharacterController(
            EnhancedServerCharacterService characterService,
            ILogger<EnhancedCharacterController> logger)
        {
            _characterService = characterService;
            _logger = logger;
        }

        /// <summary>
        /// ��ȡ��ɫ������
        /// </summary>
        [HttpGet("roster")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<RosterDto>>> GetRoster()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<RosterDto>.Failure("�û�δ��֤"));
                }

                var roster = await _characterService.GetUserRosterAsync(userId);
                return Ok(ApiResponse<RosterDto>.Success(roster, "��ȡ��ɫ������ɹ�"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ��ɫ������ʧ��");
                return StatusCode(500, ApiResponse<RosterDto>.Failure("��ȡ��ɫ������ʧ��"));
            }
        }

        /// <summary>
        /// ������ɫ��λ
        /// </summary>
        [HttpPost("roster/unlock/{slotIndex}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CharacterSlotDto>>> UnlockSlot(int slotIndex)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<CharacterSlotDto>.Failure("�û�δ��֤"));
                }

                var result = await _characterService.UnlockSlotAsync(userId, slotIndex);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ɫ��λʧ�� {SlotIndex}", slotIndex);
                return StatusCode(500, ApiResponse<CharacterSlotDto>.Failure("������ɫ��λʧ��"));
            }
        }

        /// <summary>
        /// ������ɫ
        /// </summary>
        [HttpPost("create")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CharacterFullDto>>> CreateCharacter(CreateCharacterRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<CharacterFullDto>.Failure("�û�δ��֤"));
                }

                var result = await _characterService.CreateCharacterAsync(userId, request);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ɫʧ�� {Name}", request.Name);
                return StatusCode(500, ApiResponse<CharacterFullDto>.Failure("������ɫʧ��"));
            }
        }

        /// <summary>
        /// ��֤��ɫ����
        /// </summary>
        [HttpPost("validate-name")]
        public ActionResult<ApiResponse<ValidateCharacterNameResult>> ValidateName(ValidateCharacterNameRequest request)
        {
            try
            {
                var result = _characterService.ValidateCharacterName(request.Name);
                return Ok(ApiResponse<ValidateCharacterNameResult>.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��֤��ɫ����ʧ�� {Name}", request.Name);
                return StatusCode(500, ApiResponse<ValidateCharacterNameResult>.Failure("��֤��ɫ����ʧ��"));
            }
        }

        /// <summary>
        /// ɾ����ɫ
        /// </summary>
        [HttpPost("{characterId}/delete")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCharacter(string characterId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<bool>.Failure("�û�δ��֤"));
                }

                var result = await _characterService.DeleteCharacterAsync(userId, characterId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ɾ����ɫʧ�� {CharacterId}", characterId);
                return StatusCode(500, ApiResponse<bool>.Failure("ɾ����ɫʧ��"));
            }
        }

        /// <summary>
        /// �л���ɫ
        /// </summary>
        [HttpPost("switch")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CharacterFullDto>>> SwitchCharacter(SwitchCharacterRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<CharacterFullDto>.Failure("�û�δ��֤"));
                }

                var result = await _characterService.SwitchCharacterAsync(userId, request.CharacterId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�л���ɫʧ�� {CharacterId}", request.CharacterId);
                return StatusCode(500, ApiResponse<CharacterFullDto>.Failure("�л���ɫʧ��"));
            }
        }

        /// <summary>
        /// ��ȡ��ɫ����
        /// </summary>
        [HttpGet("{characterId}/details")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CharacterFullDto>>> GetCharacterDetails(string characterId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<CharacterFullDto>.Failure("�û�δ��֤"));
                }

                // ��֤�û��Ƿ�ӵ�иý�ɫ
                bool ownsCharacter = await _characterService.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return Forbid();
                }

                var result = _characterService.GetCharacterDetails(characterId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ��ɫ����ʧ�� {CharacterId}", characterId);
                return StatusCode(500, ApiResponse<CharacterFullDto>.Failure("��ȡ��ɫ����ʧ��"));
            }
        }

        /// <summary>
        /// �������Ե�
        /// </summary>
        [HttpPost("{characterId}/attributes/allocate")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CharacterAttributesDto>>> AllocateAttributePoints(
            string characterId, AllocateAttributePointsRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<CharacterAttributesDto>.Failure("�û�δ��֤"));
                }

                // ��֤�û��Ƿ�ӵ�иý�ɫ
                bool ownsCharacter = await _characterService.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return Forbid();
                }

                var result = _characterService.AllocateAttributePointsAsync(characterId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�������Ե�ʧ�� {CharacterId}", characterId);
                return StatusCode(500, ApiResponse<CharacterAttributesDto>.Failure("�������Ե�ʧ��"));
            }
        }

        /// <summary>
        /// �������Ե�
        /// </summary>
        [HttpPost("{characterId}/attributes/reset")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CharacterAttributesDto>>> ResetAttributes(string characterId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<CharacterAttributesDto>.Failure("�û�δ��֤"));
                }

                // ��֤�û��Ƿ�ӵ�иý�ɫ
                bool ownsCharacter = await _characterService.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return Forbid();
                }

                var result = _characterService.ResetAttributesAsync(characterId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�������Ե�ʧ�� {CharacterId}", characterId);
                return StatusCode(500, ApiResponse<CharacterAttributesDto>.Failure("�������Ե�ʧ��"));
            }
        }

        /// <summary>
        /// ��ȡ���߽���
        /// </summary>
        [HttpGet("{characterId}/offline-progress")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<OfflineProgressDto>>> GetOfflineProgress(string characterId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<OfflineProgressDto>.Failure("�û�δ��֤"));
                }

                // ��֤�û��Ƿ�ӵ�иý�ɫ
                bool ownsCharacter = await _characterService.UserOwnsCharacterAsync(userId, characterId);
                if (!ownsCharacter)
                {
                    return Forbid();
                }

                var result = await _characterService.GetOfflineProgressAsync(characterId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ���߽���ʧ�� {CharacterId}", characterId);
                return StatusCode(500, ApiResponse<OfflineProgressDto>.Failure("��ȡ���߽���ʧ��"));
            }
        }
    }

    /// <summary>
    /// ��ɫ������֤����
    /// </summary>
    public class ValidateCharacterNameRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// �л���ɫ����
    /// </summary>
    public class SwitchCharacterRequest
    {
        public string CharacterId { get; set; } = string.Empty;
    }
}