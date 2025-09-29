using BlazorWebGame.Shared.DTOs;
using BlazorIdleGame.Client.Models.Core;

namespace BlazorIdleGame.Client.Services.Character
{
    public interface ICharacterService
    {
        Task<List<CharacterDto>> GetMyCharactersAsync();
        Task<CharacterDetailsDto?> GetCharacterDetailsAsync(string characterId);
        Task<CharacterDto?> CreateCharacterAsync(string name);
    }
}
