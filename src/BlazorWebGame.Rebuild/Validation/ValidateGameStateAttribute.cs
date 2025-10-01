using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Rebuild.Services.Core;

namespace BlazorWebGame.Rebuild.Validation;

/// <summary>
/// 验证游戏状态的合法性，防止在不合适的状态下执行操作
/// </summary>
public class ValidateGameStateAttribute : ActionFilterAttribute
{
    private readonly GameStateValidationType _validationType;

    public ValidateGameStateAttribute(GameStateValidationType validationType)
    {
        _validationType = validationType;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ValidateGameStateAttribute>>();

        try
        {
            bool isValid = _validationType switch
            {
                GameStateValidationType.BattleCanStart => ValidateBattleCanStart(context),
                GameStateValidationType.BattleIsActive => ValidateBattleIsActive(context),
                GameStateValidationType.CharacterIsAlive => ValidateCharacterIsAlive(context),
                GameStateValidationType.NoActiveBattle => ValidateNoActiveBattle(context),
                _ => false
            };

            if (!isValid)
            {
                logger.LogWarning("Game state validation failed: {ValidationType} for {Method} {Path}", 
                    _validationType, context.HttpContext.Request.Method, context.HttpContext.Request.Path);

                var errorMessage = GetErrorMessage(_validationType);
                context.Result = new BadRequestObjectResult(new ApiResponse<object>
                {
                    Success = false,
                    Message = errorMessage,
                    Timestamp = DateTime.UtcNow
                });
                return;
            }

            logger.LogDebug("Game state validation passed: {ValidationType}", _validationType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during game state validation: {ValidationType}", _validationType);
            context.Result = new StatusCodeResult(500);
            return;
        }

        base.OnActionExecuting(context);
    }

    /// <summary>
    /// 验证是否可以开始战斗
    /// </summary>
    private bool ValidateBattleCanStart(ActionExecutingContext context)
    {
        // 获取请求中的角色ID
        var characterId = ExtractParameterValue(context, "CharacterId");
        if (string.IsNullOrEmpty(characterId))
            return false;

        var gameEngine = context.HttpContext.RequestServices.GetRequiredService<GameEngineService>();
        
        // 检查角色是否已在其他战斗中
        return !gameEngine.IsPlayerInBattleRefresh(characterId);
    }

    /// <summary>
    /// 验证战斗是否处于活跃状态
    /// </summary>
    private bool ValidateBattleIsActive(ActionExecutingContext context)
    {
        var battleId = ExtractParameterValue(context, "BattleId");
        if (string.IsNullOrEmpty(battleId) || !Guid.TryParse(battleId, out var battleGuid))
            return false;

        var gameEngine = context.HttpContext.RequestServices.GetRequiredService<GameEngineService>();
        var battleState = gameEngine.GetBattleState(battleGuid);
        
        return battleState?.IsActive == true;
    }

    /// <summary>
    /// 验证角色是否存活
    /// </summary>
    private bool ValidateCharacterIsAlive(ActionExecutingContext context)
    {
        var characterId = ExtractParameterValue(context, "CharacterId");
        if (string.IsNullOrEmpty(characterId))
            return false;

        // 这里应该查询角色的实际状态
        // 简化实现，假设角色总是存活的
        return true;
    }

    /// <summary>
    /// 验证角色没有活跃的战斗
    /// </summary>
    private bool ValidateNoActiveBattle(ActionExecutingContext context)
    {
        var characterId = ExtractParameterValue(context, "CharacterId");
        if (string.IsNullOrEmpty(characterId))
            return false;

        var gameEngine = context.HttpContext.RequestServices.GetRequiredService<GameEngineService>();
        
        // 检查角色是否在战斗刷新状态中
        return !gameEngine.IsPlayerInBattleRefresh(characterId);
    }

    /// <summary>
    /// 从请求中提取参数值
    /// </summary>
    private string? ExtractParameterValue(ActionExecutingContext context, string parameterName)
    {
        // 首先尝试从路由参数获取
        if (context.ActionArguments.TryGetValue(parameterName.ToLower(), out var routeValue))
        {
            return routeValue?.ToString();
        }

        // 尝试从请求体中的对象属性获取
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg == null) continue;

            var property = arg.GetType().GetProperty(parameterName, 
                System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (property != null)
            {
                return property.GetValue(arg)?.ToString();
            }
        }

        return null;
    }

    /// <summary>
    /// 获取错误消息
    /// </summary>
    private string GetErrorMessage(GameStateValidationType validationType)
    {
        return validationType switch
        {
            GameStateValidationType.BattleCanStart => "Cannot start battle: Character may be in another battle or in cooldown",
            GameStateValidationType.BattleIsActive => "Battle is not active or does not exist",
            GameStateValidationType.CharacterIsAlive => "Character is not alive",
            GameStateValidationType.NoActiveBattle => "Character is already in an active battle",
            _ => "Invalid game state for this operation"
        };
    }
}

/// <summary>
/// 游戏状态验证类型
/// </summary>
public enum GameStateValidationType
{
    /// <summary>
    /// 验证是否可以开始战斗
    /// </summary>
    BattleCanStart,
    
    /// <summary>
    /// 验证战斗是否处于活跃状态
    /// </summary>
    BattleIsActive,
    
    /// <summary>
    /// 验证角色是否存活
    /// </summary>
    CharacterIsAlive,
    
    /// <summary>
    /// 验证角色没有活跃的战斗
    /// </summary>
    NoActiveBattle
}