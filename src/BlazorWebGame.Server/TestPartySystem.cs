using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorWebGame.Server;

/// <summary>
/// 组队系统测试类
/// </summary>
public static class TestPartySystem
{
    public static void RunPartyTest(ILogger logger)
    {
        logger.LogInformation("=== 开始组队系统测试 ===");

        try
        {
            // 创建测试服务
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ServerPartyService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var partyService = serviceProvider.GetRequiredService<ServerPartyService>();

            // 测试角色ID
            string player1 = "player-001";
            string player2 = "player-002";
            string player3 = "player-003";

            // 测试1：创建组队
            logger.LogInformation("测试1：创建组队");
            var party = partyService.CreateParty(player1);
            if (party != null)
            {
                logger.LogInformation("✓ 成功创建组队: {PartyId}, 队长: {CaptainId}", party.Id, party.CaptainId);
            }
            else
            {
                logger.LogError("✗ 创建组队失败");
                return;
            }

            // 测试2：加入组队
            logger.LogInformation("测试2：其他玩家加入组队");
            bool joinResult1 = partyService.JoinParty(player2, party.Id);
            bool joinResult2 = partyService.JoinParty(player3, party.Id);

            if (joinResult1 && joinResult2)
            {
                logger.LogInformation("✓ 两个玩家成功加入组队");
            }
            else
            {
                logger.LogError("✗ 玩家加入组队失败: player2={Result1}, player3={Result2}", joinResult1, joinResult2);
            }

            // 测试3：检查组队状态
            logger.LogInformation("测试3：检查组队状态");
            var updatedParty = partyService.GetParty(party.Id);
            if (updatedParty != null)
            {
                logger.LogInformation("✓ 组队成员数量: {MemberCount}", updatedParty.MemberIds.Count);
                logger.LogInformation("  成员列表: {Members}", string.Join(", ", updatedParty.MemberIds));
            }

            // 测试4：检查角色组队信息
            logger.LogInformation("测试4：检查角色组队信息");
            var player1Party = partyService.GetPartyForCharacter(player1);
            var player2Party = partyService.GetPartyForCharacter(player2);
            
            if (player1Party?.Id == party.Id && player2Party?.Id == party.Id)
            {
                logger.LogInformation("✓ 角色组队信息正确");
            }
            else
            {
                logger.LogError("✗ 角色组队信息不正确");
            }

            // 测试5：组队战斗权限检查
            logger.LogInformation("测试5：组队战斗权限检查");
            bool canStart1 = partyService.CanStartPartyBattle(player1); // 队长
            bool canStart2 = partyService.CanStartPartyBattle(player2); // 成员

            if (canStart1 && !canStart2)
            {
                logger.LogInformation("✓ 组队战斗权限检查正确: 队长可以发起，成员不可以");
            }
            else
            {
                logger.LogError("✗ 组队战斗权限检查失败: 队长={CanStart1}, 成员={CanStart2}", canStart1, canStart2);
            }

            // 测试6：离开组队
            logger.LogInformation("测试6：成员离开组队");
            bool leaveResult = partyService.LeaveParty(player2);
            if (leaveResult)
            {
                var partyAfterLeave = partyService.GetParty(party.Id);
                logger.LogInformation("✓ 成员成功离开组队，剩余成员: {MemberCount}", 
                    partyAfterLeave?.MemberIds.Count ?? 0);
            }
            else
            {
                logger.LogError("✗ 成员离开组队失败");
            }

            // 测试7：队长离开（解散组队）
            logger.LogInformation("测试7：队长离开组队（应该解散组队）");
            bool disbandResult = partyService.LeaveParty(player1);
            if (disbandResult)
            {
                var partyAfterDisband = partyService.GetParty(party.Id);
                if (partyAfterDisband == null)
                {
                    logger.LogInformation("✓ 队长离开后组队已解散");
                }
                else
                {
                    logger.LogError("✗ 队长离开后组队仍然存在");
                }
            }

            logger.LogInformation("=== 组队系统测试完成 ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "组队系统测试过程中发生错误");
        }
    }
}