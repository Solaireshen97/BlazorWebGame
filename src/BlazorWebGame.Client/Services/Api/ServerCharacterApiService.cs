using BlazorWebGame.Shared.DTOs;
using System.Net.Http.Json;

namespace BlazorWebGame.Client.Services.Api
{
    /// <summary>
    /// 客户端角色API服务，与服务端的CharacterController通信
    /// </summary>
    public class ServerCharacterApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ServerCharacterApiService> _logger;

        public ServerCharacterApiService(HttpClient httpClient, ILogger<ServerCharacterApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有角色
        /// </summary>
        public async Task<ApiResponse<List<CharacterDto>>> GetCharactersAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<CharacterDto>>>("api/character");
                return response ?? new ApiResponse<List<CharacterDto>> { Success = false, Message = "未收到响应" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get characters from server");
                return new ApiResponse<List<CharacterDto>>
                {
                    Success = false,
                    Message = "连接服务器失败"
                };
            }
        }

        /// <summary>
        /// 获取角色详细信息
        /// </summary>
        public async Task<ApiResponse<CharacterDetailsDto>> GetCharacterDetailsAsync(string characterId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<CharacterDetailsDto>>($"api/character/{characterId}");
                return response ?? new ApiResponse<CharacterDetailsDto> { Success = false, Message = "未收到响应" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get character details for {CharacterId}", characterId);
                return new ApiResponse<CharacterDetailsDto>
                {
                    Success = false,
                    Message = "连接服务器失败"
                };  
            }
        }

        /// <summary>
        /// 创建新角色
        /// </summary>
        public async Task<ApiResponse<CharacterDto>> CreateCharacterAsync(CreateCharacterRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/character", request);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<CharacterDto>>();
                return result ?? new ApiResponse<CharacterDto> { Success = false, Message = "未收到响应" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create character");
                return new ApiResponse<CharacterDto>
                {
                    Success = false,
                    Message = "连接服务器失败"
                };
            }
        }

        /// <summary>
        /// 添加经验值
        /// </summary>
        public async Task<ApiResponse<bool>> AddExperienceAsync(string characterId, AddExperienceRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/character/{characterId}/experience", request);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                return result ?? new ApiResponse<bool> { Success = false, Message = "未收到响应" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add experience for character {CharacterId}", characterId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "连接服务器失败"
                };
            }
        }

        /// <summary>
        /// 更新角色状态
        /// </summary>
        public async Task<ApiResponse<bool>> UpdateCharacterStatusAsync(string characterId, UpdateCharacterStatusRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/character/{characterId}/status", request);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                return result ?? new ApiResponse<bool> { Success = false, Message = "未收到响应" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update character status for {CharacterId}", characterId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "连接服务器失败"
                };
            }
        }
    }
}