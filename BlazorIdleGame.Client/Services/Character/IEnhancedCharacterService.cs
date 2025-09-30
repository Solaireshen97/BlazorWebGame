using BlazorWebGame.Shared.DTOs.Character;

namespace BlazorIdleGame.Client.Services.Character
{
    public interface IEnhancedCharacterService
    {
        // 花名册管理
        RosterDto? CurrentRoster { get; }
        CharacterFullDto? ActiveCharacter { get; }

        // 事件
        event EventHandler<RosterDto>? RosterUpdated;
        event EventHandler<CharacterFullDto>? ActiveCharacterUpdated;
        event EventHandler<OfflineProgressDto>? OfflineProgressReceived;
        event EventHandler<CharacterSlotDto>? SlotUnlocked;

        // 花名册操作
        Task<RosterDto?> GetRosterAsync();
        Task<bool> UnlockSlotAsync(int slotIndex);

        // 角色操作
        Task<CharacterFullDto?> CreateCharacterAsync(CreateCharacterRequestDto request);
        Task<ValidateCharacterNameResult> ValidateNameAsync(string name);
        Task<bool> DeleteCharacterAsync(string characterId);
        Task<bool> SwitchCharacterAsync(string characterId);

        // 角色信息
        Task<CharacterFullDto?> GetCharacterDetailsAsync(string characterId);
        Task<CharacterFullDto?> RefreshActiveCharacterAsync();

        // 属性操作
        Task<bool> AllocateAttributePointsAsync(Dictionary<string, int> points);
        Task<bool> ResetAttributesAsync();

        // 离线进度
        Task<OfflineProgressDto?> GetOfflineProgressAsync(string characterId);
    }
}
