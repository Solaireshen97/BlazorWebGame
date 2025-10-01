using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BlazorWebGame.Rebuild.Security;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Rebuild.Services.Core;

namespace BlazorWebGame.Rebuild.Validation;

/// <summary>
/// 验证用户是否拥有指定资源的权限
/// </summary>
public class ValidateResourceOwnershipAttribute : ActionFilterAttribute
{
    private readonly string _resourceIdParameterName;
    private readonly ResourceType _resourceType;

    public ValidateResourceOwnershipAttribute(string resourceIdParameterName, ResourceType resourceType = ResourceType.Character)
    {
        _resourceIdParameterName = resourceIdParameterName;
        _resourceType = resourceType;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ValidateResourceOwnershipAttribute>>();
        var authService = context.HttpContext.RequestServices.GetRequiredService<GameAuthenticationService>();
        var userService = context.HttpContext.RequestServices.GetRequiredService<DemoUserService>();

        try
        {
            // 获取用户ID
            var userId = authService.GetUserId(context.HttpContext.User);
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("Resource ownership validation failed: No user ID found in claims");
                context.Result = new UnauthorizedObjectResult(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Authentication required",
                    Timestamp = DateTime.UtcNow
                });
                return;
            }

            // 获取资源ID
            string? resourceId = null;
            
            // 首先尝试从路由参数获取
            if (context.ActionArguments.TryGetValue(_resourceIdParameterName, out var routeValue))
            {
                resourceId = routeValue?.ToString();
            }
            
            // 如果路由参数中没有，尝试从请求体获取
            if (string.IsNullOrEmpty(resourceId))
            {
                resourceId = ExtractResourceIdFromRequestBody(context, _resourceIdParameterName);
            }

            if (string.IsNullOrEmpty(resourceId))
            {
                logger.LogWarning("Resource ownership validation failed: No resource ID found for parameter {ParameterName}", _resourceIdParameterName);
                context.Result = new BadRequestObjectResult(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Resource ID is required",
                    Timestamp = DateTime.UtcNow
                });
                return;
            }

            // 验证资源归属
            bool hasAccess = _resourceType switch
            {
                ResourceType.Character => userService.UserHasCharacter(userId, resourceId),
                ResourceType.Battle => ValidateBattleAccess(context, userId, resourceId),
                _ => false
            };

            if (!hasAccess)
            {
                logger.LogWarning("Resource ownership validation failed: User {UserId} does not have access to {ResourceType} {ResourceId}", 
                    userId, _resourceType, resourceId);
                
                context.Result = new ForbidResult();
                return;
            }

            logger.LogDebug("Resource ownership validated: User {UserId} has access to {ResourceType} {ResourceId}", 
                userId, _resourceType, resourceId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during resource ownership validation");
            context.Result = new StatusCodeResult(500);
            return;
        }

        base.OnActionExecuting(context);
    }

    /// <summary>
    /// 从请求体中提取资源ID
    /// </summary>
    private string? ExtractResourceIdFromRequestBody(ActionExecutingContext context, string parameterName)
    {
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
    /// 验证战斗访问权限
    /// </summary>
    private bool ValidateBattleAccess(ActionExecutingContext context, string userId, string battleId)
    {
        try
        {
            var gameEngine = context.HttpContext.RequestServices.GetRequiredService<GameEngineService>();
            
            if (!Guid.TryParse(battleId, out var battleGuid))
                return false;

            var battleState = gameEngine.GetBattleState(battleGuid);
            if (battleState == null)
                return false;

            // 检查用户是否是战斗的参与者
            return battleState.PartyMemberIds.Contains(userId) || 
                   battleState.CharacterId == userId;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// 资源类型枚举
/// </summary>
public enum ResourceType
{
    Character,
    Battle,
    Party,
    Item
}