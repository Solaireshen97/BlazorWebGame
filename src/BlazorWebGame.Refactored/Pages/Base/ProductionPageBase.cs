using Microsoft.AspNetCore.Components;
using BlazorWebGame.Refactored.Domain.Entities;
using BlazorWebGame.Refactored.Domain.ValueObjects;
using BlazorWebGame.Refactored.Application.Interfaces;
using BlazorWebGame.Refactored.Application.Services;

namespace BlazorWebGame.Refactored.Pages.Base;

/// <summary>
/// 生产页面基类 - 减少重复代码
/// </summary>
public abstract class ProductionPageBase : ComponentBase, IDisposable
{
    [Inject] protected IGameStateManager GameState { get; set; } = default!;
    [Inject] protected ICharacterService CharacterService { get; set; } = default!;
    [Inject] protected ILogger<ProductionPageBase> Logger { get; set; } = default!;
    
    protected Character? CurrentCharacter => GetCurrentCharacter();
    protected bool IsPickerVisible { get; set; }
    protected string ManagingCategory { get; set; } = string.Empty;
    protected int ManagingSlotId { get; set; }
    protected string ManagingFoodType { get; set; } = string.Empty;
    
    protected abstract ProductionProfession Profession { get; }
    protected abstract string PageTitle { get; }
    
    // 生产类型枚举
    protected enum ProductionProfession
    {
        Tailoring,
        Blacksmithing,
        Alchemy,
        Cooking,
        Enchanting
    }
    
    protected override void OnInitialized()
    {
        // 订阅状态变化事件
        SubscribeToStateChanges();
    }
    
    protected virtual void SubscribeToStateChanges()
    {
        // 子类可以重写此方法以订阅特定的状态变化
    }
    
    protected virtual void HandleStateChanged()
    {
        InvokeAsync(StateHasChanged);
    }
    
    protected double GetCraftingProgress()
    {
        if (CurrentCharacter?.Activities?.Slots == null) return 0;
        
        var craftingActivity = CurrentCharacter.Activities.Slots.Values
            .FirstOrDefault(a => a?.Type == ActivityType.Crafting);
        
        if (craftingActivity == null) return 0;
        
        var totalTime = GetCurrentCraftingTime();
        if (totalTime <= 0) return 0;
        
        // Calculate progress based on activity start time
        var elapsed = DateTime.UtcNow - craftingActivity.StartTime;
        var progress = elapsed.TotalSeconds / totalTime;
        return Math.Clamp(progress * 100, 0, 100);
    }
    
    protected double GetCurrentCraftingTime()
    {
        if (CurrentCharacter?.Activities?.Slots == null) return 0;
        
        var craftingActivity = CurrentCharacter.Activities.Slots.Values
            .FirstOrDefault(a => a?.Type == ActivityType.Crafting);
        
        if (craftingActivity == null) return 60; // Default crafting time
        
        // Get crafting speed bonus from character stats
        double speedBonus = GetCraftingSpeedBonus();
        return 60 / (1 + speedBonus); // Base 60 seconds with speed bonus
    }
    
    protected double GetCraftingSpeedBonus()
    {
        return CurrentCharacter?.Stats.Intelligence * 0.01 ?? 0; // 1% speed per intelligence
    }
    
    protected bool CanAffordRecipe(Recipe recipe)
    {
        if (CurrentCharacter?.Resources == null) return false;
        
        return recipe.RequiredMaterials.All(material => 
            GetItemCountInInventory(material.MaterialId) >= material.Quantity);
    }
    
    protected int GetItemCountInInventory(string itemId)
    {
        return CurrentCharacter?.Resources.Materials.GetValueOrDefault(itemId, 0) ?? 0;
    }
    
    protected void OpenQuickSlotPicker(string category, int slotId, string foodType)
    {
        ManagingCategory = category;
        ManagingSlotId = slotId;
        ManagingFoodType = foodType;
        IsPickerVisible = true;
    }
    
    protected void CloseQuickSlotPicker()
    {
        IsPickerVisible = false;
    }
    
    protected async Task StartCraftingAsync(Recipe recipe)
    {
        try
        {
            if (!CanAffordRecipe(recipe))
            {
                await ShowNotificationAsync("材料不足", NotificationType.Warning);
                return;
            }
            
            // Start crafting through game state manager
            await StartCraftingActivityAsync(recipe);
            await ShowNotificationAsync($"开始制作: {recipe.Name}", NotificationType.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting crafting");
            await ShowNotificationAsync("制作失败", NotificationType.Error);
        }
    }
    
    protected virtual async Task StartCraftingActivityAsync(Recipe recipe)
    {
        // 实现制作活动启动逻辑
        if (CurrentCharacter == null) return;
        
        var parameters = new ActivityParameters();
        parameters.SetValue("recipeId", recipe.RecipeId);
        parameters.SetValue("profession", Profession.ToString());
        
        var result = CurrentCharacter.StartActivity(ActivityType.Crafting, parameters);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Error);
        }
    }
    
    protected async Task ShowNotificationAsync(string message, NotificationType type)
    {
        // 实现通知显示逻辑
        Logger.LogInformation("{NotificationType}: {Message}", type, message);
        await Task.CompletedTask;
    }
    
    protected bool IsCrafting()
    {
        return CurrentCharacter?.Activities?.Slots.Values
            .Any(a => a?.Type == ActivityType.Crafting) ?? false;
    }
    
    protected bool IsBlocked()
    {
        return CurrentCharacter?.Activities?.Slots.Values
            .Any(a => a != null && a.Type != ActivityType.Idle && a.Type != ActivityType.Crafting) ?? false;
    }
    
    protected int GetProfessionLevel()
    {
        // 根据职业类型获取对应的等级
        return CurrentCharacter?.Level ?? 1;
    }
    
    protected int GetProfessionExperience()
    {
        // 根据职业类型获取对应的经验值
        return (int)(CurrentCharacter?.Experience.ToLong() ?? 0);
    }
    
    protected IEnumerable<Recipe> GetAvailableRecipes()
    {
        // 返回当前职业可用的配方
        return GetRecipesForProfession(Profession);
    }
    
    protected virtual IEnumerable<Recipe> GetRecipesForProfession(ProductionProfession profession)
    {
        // 子类可以重写此方法以返回特定职业的配方
        return new List<Recipe>
        {
            new Recipe
            {
                RecipeId = $"{profession}_basic_item",
                Name = $"基础{profession}物品",
                RequiredMaterials = new List<MaterialRequirement>
                {
                    new("wood", 5),
                    new("stone", 3)
                },
                OutputItem = new ItemReward($"{profession}_item", 1),
                CraftTime = 60,
                ExperienceReward = new BigNumber(50),
                RequiredLevel = 1
            }
        };
    }
    
    private Character? GetCurrentCharacter()
    {
        // 从游戏状态管理器获取当前角色
        try
        {
            // 这里需要根据实际的游戏状态管理器实现来调整
            return null; // 临时返回null，需要根据实际实现调整
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting current character");
            return null;
        }
    }
    
    public virtual void Dispose()
    {
        // 清理资源和取消订阅
    }
    
    protected enum NotificationType
    {
        Success,
        Warning,
        Error,
        Info
    }
}