using BlazorIdleGame.Client.Services.Core;
using BlazorWebGame.Shared.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorIdleGame.Client.Services.Character
{
    public class CharacterService : ICharacterService
    {
        private readonly ILogger<CharacterService> _logger;
        private readonly IGameCommunicationService _communicationService;

        public CharacterService(
            ILogger<CharacterService> logger,
            IGameCommunicationService communicationService)
        {
            _logger = logger;
            _communicationService = communicationService;
        }

        public async Task<List<CharacterDto>> GetMyCharactersAsync()
        {
            try
            {
                var response = await _communicationService.GetAsync<ApiResponse<List<CharacterDto>>>("api/character/my");
                return response?.Data ?? new List<CharacterDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色列表失败");
                return new List<CharacterDto>();
            }
        }

        public async Task<CharacterDetailsDto?> GetCharacterDetailsAsync(string characterId)
        {
            try
            {
                var response = await _communicationService.GetAsync<ApiResponse<CharacterDetailsDto>>($"api/character/{characterId}");
                return response?.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色详情失败: {CharacterId}", characterId);
                return null;
            }
        }

        public async Task<CharacterDto?> CreateCharacterAsync(string name)
        {
            try
            {
                var request = new CreateCharacterRequest { Name = name };
                var response = await _communicationService.PostAsync<CreateCharacterRequest, ApiResponse<CharacterDto>>(
                    "api/character/create", request);
                
                return response?.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建角色失败");
                return null;
            }
        }
    }
}