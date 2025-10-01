# 角色管理服务使用示例

## 概述

本文档提供了如何在客户端使用新的角色管理服务 API 的示例代码。

## 客户端服务示例

### 1. 创建客户端服务类

```csharp
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.DTOs.Character;

namespace BlazorWebGame.Client.Services.Api
{
    /// <summary>
    /// 角色管理API服务
    /// </summary>
    public class CharacterManagementApiService : BaseApiService
    {
        public CharacterManagementApiService(
            ConfigurableHttpClientFactory httpClientFactory, 
            ILogger<CharacterManagementApiService> logger)
            : base(httpClientFactory, logger)
        {
        }

        #region 角色花名册

        /// <summary>
        /// 获取角色花名册
        /// </summary>
        public async Task<ApiResponse<RosterDto>> GetRosterAsync()
        {
            return await GetAsync<RosterDto>("api/character-management/roster");
        }

        /// <summary>
        /// 解锁角色槽位
        /// </summary>
        public async Task<ApiResponse<bool>> UnlockSlotAsync(int slotIndex)
        {
            return await PostAsync<bool>($"api/character-management/roster/slots/{slotIndex}/unlock", null);
        }

        #endregion

        #region 角色管理

        /// <summary>
        /// 创建角色
        /// </summary>
        public async Task<ApiResponse<CharacterFullDto>> CreateCharacterAsync(CreateCharacterRequestDto request)
        {
            return await PostAsync<CharacterFullDto>("api/character-management/characters", request);
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteCharacterAsync(string characterId)
        {
            return await DeleteAsync<bool>($"api/character-management/characters/{characterId}");
        }

        /// <summary>
        /// 切换角色
        /// </summary>
        public async Task<ApiResponse<CharacterFullDto>> SwitchCharacterAsync(string characterId)
        {
            return await PostAsync<CharacterFullDto>(
                $"api/character-management/characters/{characterId}/switch", null);
        }

        /// <summary>
        /// 获取角色详情
        /// </summary>
        public async Task<ApiResponse<CharacterFullDto>> GetCharacterDetailsAsync(string characterId)
        {
            return await GetAsync<CharacterFullDto>(
                $"api/character-management/characters/{characterId}");
        }

        #endregion

        #region 角色验证

        /// <summary>
        /// 验证角色名称
        /// </summary>
        public async Task<ApiResponse<ValidateCharacterNameResult>> ValidateNameAsync(string name)
        {
            var request = new ValidateCharacterNameRequest { Name = name };
            return await PostAsync<ValidateCharacterNameResult>(
                "api/character-management/validate-name", request);
        }

        #endregion

        #region 属性管理

        /// <summary>
        /// 分配属性点
        /// </summary>
        public async Task<ApiResponse<CharacterAttributesDto>> AllocateAttributePointsAsync(
            string characterId, 
            Dictionary<string, int> points)
        {
            var request = new AllocateAttributePointsRequest { Points = points };
            return await PostAsync<CharacterAttributesDto>(
                $"api/character-management/characters/{characterId}/attributes/allocate", request);
        }

        #endregion
    }
}
```

## 使用示例

### 场景1: 显示角色花名册

```csharp
public class CharacterRosterPage : ComponentBase
{
    [Inject] private CharacterManagementApiService CharacterManagementService { get; set; }

    private RosterDto? roster;
    private string errorMessage = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadRosterAsync();
    }

    private async Task LoadRosterAsync()
    {
        var response = await CharacterManagementService.GetRosterAsync();
        if (response.Success && response.Data != null)
        {
            roster = response.Data;
        }
        else
        {
            errorMessage = response.Message;
        }
    }

    private async Task UnlockSlot(int slotIndex)
    {
        var response = await CharacterManagementService.UnlockSlotAsync(slotIndex);
        if (response.Success)
        {
            // 重新加载花名册
            await LoadRosterAsync();
        }
        else
        {
            errorMessage = response.Message;
        }
    }
}
```

### 场景2: 创建新角色

```csharp
public class CreateCharacterPage : ComponentBase
{
    [Inject] private CharacterManagementApiService CharacterManagementService { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }

    private string characterName = string.Empty;
    private string errorMessage = string.Empty;
    private bool isValidating = false;
    private bool isCreating = false;

    private async Task ValidateCharacterName()
    {
        if (string.IsNullOrWhiteSpace(characterName))
        {
            errorMessage = "请输入角色名称";
            return;
        }

        isValidating = true;
        errorMessage = string.Empty;

        try
        {
            var response = await CharacterManagementService.ValidateNameAsync(characterName);
            if (response.Success && response.Data != null)
            {
                if (!response.Data.IsValid)
                {
                    errorMessage = response.Data.Reason ?? "角色名称无效";
                }
            }
            else
            {
                errorMessage = response.Message;
            }
        }
        finally
        {
            isValidating = false;
        }
    }

    private async Task CreateCharacter()
    {
        // 先验证名称
        await ValidateCharacterName();
        if (!string.IsNullOrEmpty(errorMessage))
        {
            return;
        }

        isCreating = true;
        errorMessage = string.Empty;

        try
        {
            var request = new CreateCharacterRequestDto
            {
                Name = characterName,
                StartingProfessionId = "Warrior",
                SlotIndex = 0
            };

            var response = await CharacterManagementService.CreateCharacterAsync(request);
            if (response.Success && response.Data != null)
            {
                // 创建成功，导航到角色列表
                NavigationManager.NavigateTo("/characters");
            }
            else
            {
                errorMessage = response.Message;
            }
        }
        finally
        {
            isCreating = false;
        }
    }
}
```

### 场景3: 切换角色

```csharp
public class CharacterSwitcher : ComponentBase
{
    [Inject] private CharacterManagementApiService CharacterManagementService { get; set; }
    
    [Parameter] public string CharacterId { get; set; } = string.Empty;
    [Parameter] public EventCallback OnCharacterSwitched { get; set; }

    private bool isSwitching = false;
    private string errorMessage = string.Empty;

    private async Task SwitchCharacter()
    {
        if (string.IsNullOrEmpty(CharacterId))
        {
            errorMessage = "未指定角色ID";
            return;
        }

        isSwitching = true;
        errorMessage = string.Empty;

        try
        {
            var response = await CharacterManagementService.SwitchCharacterAsync(CharacterId);
            if (response.Success && response.Data != null)
            {
                // 切换成功，触发回调
                await OnCharacterSwitched.InvokeAsync();
            }
            else
            {
                errorMessage = response.Message;
            }
        }
        finally
        {
            isSwitching = false;
        }
    }
}
```

### 场景4: 删除角色

```csharp
public class CharacterDeleter : ComponentBase
{
    [Inject] private CharacterManagementApiService CharacterManagementService { get; set; }
    
    [Parameter] public string CharacterId { get; set; } = string.Empty;
    [Parameter] public string CharacterName { get; set; } = string.Empty;
    [Parameter] public EventCallback OnCharacterDeleted { get; set; }

    private bool isDeleting = false;
    private bool showConfirmation = false;
    private string errorMessage = string.Empty;

    private void ShowDeleteConfirmation()
    {
        showConfirmation = true;
    }

    private void CancelDelete()
    {
        showConfirmation = false;
    }

    private async Task ConfirmDelete()
    {
        showConfirmation = false;
        isDeleting = true;
        errorMessage = string.Empty;

        try
        {
            var response = await CharacterManagementService.DeleteCharacterAsync(CharacterId);
            if (response.Success)
            {
                // 删除成功，触发回调
                await OnCharacterDeleted.InvokeAsync();
            }
            else
            {
                errorMessage = response.Message;
            }
        }
        finally
        {
            isDeleting = false;
        }
    }
}
```

### 场景5: 分配属性点

```csharp
public class AttributeAllocator : ComponentBase
{
    [Inject] private CharacterManagementApiService CharacterManagementService { get; set; }
    
    [Parameter] public string CharacterId { get; set; } = string.Empty;
    [Parameter] public EventCallback<CharacterAttributesDto> OnAttributesAllocated { get; set; }

    private Dictionary<string, int> pendingAllocation = new()
    {
        { "strength", 0 },
        { "agility", 0 },
        { "intellect", 0 },
        { "spirit", 0 },
        { "stamina", 0 }
    };

    private bool isAllocating = false;
    private string errorMessage = string.Empty;

    private int TotalPendingPoints => pendingAllocation.Values.Sum();

    private void IncrementAttribute(string attribute)
    {
        pendingAllocation[attribute]++;
    }

    private void DecrementAttribute(string attribute)
    {
        if (pendingAllocation[attribute] > 0)
        {
            pendingAllocation[attribute]--;
        }
    }

    private void ResetAllocation()
    {
        foreach (var key in pendingAllocation.Keys.ToList())
        {
            pendingAllocation[key] = 0;
        }
    }

    private async Task AllocatePoints()
    {
        if (TotalPendingPoints == 0)
        {
            errorMessage = "请至少分配1个属性点";
            return;
        }

        isAllocating = true;
        errorMessage = string.Empty;

        try
        {
            // 只发送非零的属性
            var pointsToAllocate = pendingAllocation
                .Where(kvp => kvp.Value > 0)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var response = await CharacterManagementService.AllocateAttributePointsAsync(
                CharacterId, pointsToAllocate);

            if (response.Success && response.Data != null)
            {
                // 分配成功，重置并触发回调
                ResetAllocation();
                await OnAttributesAllocated.InvokeAsync(response.Data);
            }
            else
            {
                errorMessage = response.Message;
            }
        }
        finally
        {
            isAllocating = false;
        }
    }
}
```

## Blazor组件示例

### 角色花名册组件

```razor
@page "/characters/roster"
@using BlazorWebGame.Shared.DTOs.Character

<h3>角色花名册</h3>

@if (roster != null)
{
    <div class="roster-grid">
        @foreach (var slot in roster.Slots)
        {
            <div class="character-slot @slot.State.ToLower()">
                @if (slot.State == "Occupied" && slot.Character != null)
                {
                    <div class="character-card">
                        <div class="character-icon">@slot.Character.ProfessionIcon</div>
                        <div class="character-name">@slot.Character.Name</div>
                        <div class="character-level">Lv. @slot.Character.Level</div>
                        <div class="character-profession">@slot.Character.ProfessionName</div>
                        
                        @if (roster.ActiveCharacterId == slot.Character.Id)
                        {
                            <span class="badge active">活跃</span>
                        }
                        else
                        {
                            <button @onclick="() => SwitchToCharacter(slot.Character.Id)">
                                切换
                            </button>
                        }
                        
                        <button @onclick="() => DeleteCharacter(slot.Character.Id)" class="danger">
                            删除
                        </button>
                    </div>
                }
                else if (slot.State == "Unlocked")
                {
                    <div class="empty-slot">
                        <button @onclick="() => NavigateToCreateCharacter(slot.SlotIndex)">
                            <span class="icon">+</span>
                            <span>创建角色</span>
                        </button>
                    </div>
                }
                else if (slot.State == "Locked")
                {
                    <div class="locked-slot">
                        <span class="icon">🔒</span>
                        <p>@slot.UnlockCondition</p>
                        <button @onclick="() => UnlockSlot(slot.SlotIndex)">
                            解锁
                        </button>
                    </div>
                }
            </div>
        }
    </div>
}
else if (!string.IsNullOrEmpty(errorMessage))
{
    <div class="alert alert-danger">@errorMessage</div>
}
else
{
    <div class="loading">加载中...</div>
}

<style>
    .roster-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
        gap: 1rem;
        padding: 1rem;
    }

    .character-slot {
        border: 2px solid #ddd;
        border-radius: 8px;
        padding: 1rem;
        min-height: 250px;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
    }

    .character-slot.occupied {
        border-color: #28a745;
        background-color: #f8fff9;
    }

    .character-slot.locked {
        border-color: #6c757d;
        background-color: #f0f0f0;
    }

    .character-card {
        text-align: center;
    }

    .character-icon {
        font-size: 3rem;
        margin-bottom: 0.5rem;
    }

    .badge.active {
        background-color: #ffc107;
        color: #000;
        padding: 0.25rem 0.5rem;
        border-radius: 4px;
        font-size: 0.875rem;
    }
</style>
```

## JavaScript互操作示例

如果需要与JavaScript进行交互（例如显示确认对话框）：

```javascript
// wwwroot/js/characterManagement.js

window.characterManagement = {
    confirmDelete: function(characterName) {
        return confirm(`确定要删除角色 "${characterName}" 吗？此操作无法撤销！`);
    },
    
    showSuccessMessage: function(message) {
        // 使用toast或其他UI库显示成功消息
        alert(message);
    },
    
    showErrorMessage: function(message) {
        // 使用toast或其他UI库显示错误消息
        alert(`错误: ${message}`);
    }
};
```

在Blazor组件中使用：

```csharp
[Inject] private IJSRuntime JSRuntime { get; set; }

private async Task DeleteCharacterWithConfirmation(string characterId, string characterName)
{
    var confirmed = await JSRuntime.InvokeAsync<bool>(
        "characterManagement.confirmDelete", characterName);
    
    if (confirmed)
    {
        var response = await CharacterManagementService.DeleteCharacterAsync(characterId);
        if (response.Success)
        {
            await JSRuntime.InvokeVoidAsync(
                "characterManagement.showSuccessMessage", "角色删除成功");
            await LoadRosterAsync();
        }
        else
        {
            await JSRuntime.InvokeVoidAsync(
                "characterManagement.showErrorMessage", response.Message);
        }
    }
}
```

## 最佳实践

### 1. 错误处理
```csharp
private async Task<bool> TryExecuteApiCall(Func<Task<ApiResponse<bool>>> apiCall, string errorContext)
{
    try
    {
        var response = await apiCall();
        if (!response.Success)
        {
            Logger.LogWarning($"{errorContext}: {response.Message}");
            // 显示用户友好的错误消息
            await ShowErrorMessage(response.Message);
            return false;
        }
        return true;
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, errorContext);
        await ShowErrorMessage("操作失败，请稍后重试");
        return false;
    }
}
```

### 2. 加载状态管理
```csharp
private class LoadingState
{
    public bool IsLoading { get; set; }
    public string Message { get; set; } = "加载中...";
}

private LoadingState loadingState = new();

private async Task ExecuteWithLoading(Func<Task> action, string loadingMessage = "处理中...")
{
    loadingState.IsLoading = true;
    loadingState.Message = loadingMessage;
    StateHasChanged();

    try
    {
        await action();
    }
    finally
    {
        loadingState.IsLoading = false;
        StateHasChanged();
    }
}
```

### 3. 缓存角色数据
```csharp
public class CharacterCache
{
    private CharacterFullDto? _cachedCharacter;
    private DateTime _cacheExpiry;
    private readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(5);

    public async Task<CharacterFullDto?> GetCharacterAsync(
        string characterId, 
        Func<Task<ApiResponse<CharacterFullDto>>> fetchFunc)
    {
        if (_cachedCharacter?.Id == characterId && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedCharacter;
        }

        var response = await fetchFunc();
        if (response.Success && response.Data != null)
        {
            _cachedCharacter = response.Data;
            _cacheExpiry = DateTime.UtcNow.Add(_cacheLifetime);
            return _cachedCharacter;
        }

        return null;
    }

    public void InvalidateCache()
    {
        _cachedCharacter = null;
        _cacheExpiry = DateTime.MinValue;
    }
}
```

## 总结

这些示例展示了如何在客户端应用中使用新的角色管理服务API。关键要点：

1. **创建专用的API服务类** - 封装所有HTTP调用
2. **处理所有可能的响应状态** - 成功、失败、错误
3. **提供良好的用户反馈** - 加载状态、错误消息、成功提示
4. **实现确认对话框** - 对于删除等危险操作
5. **缓存数据** - 减少不必要的API调用
6. **错误恢复** - 优雅地处理网络错误和业务错误
