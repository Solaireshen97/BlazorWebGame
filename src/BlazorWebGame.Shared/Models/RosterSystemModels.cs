using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 多角色管理系统
/// </summary>
public class Roster
{
    public string UserId { get; private set; }
    public List<RosterSlot> Slots { get; private set; } = new();
    public int UnlockedCount { get; private set; } = 1;
    public string? ActiveCharacterId { get; private set; }

    private const int MaxSlots = 5;

    public Roster(string userId)
    {
        UserId = userId;

        // 初始化所有槽位
        for (int i = 0; i < MaxSlots; i++)
        {
            var slot = new RosterSlot(i + 1)
            {
                State = i == 0 ? SlotState.Unlocked : SlotState.Locked
            };

            if (i > 0)
            {
                // 设置解锁条件
                slot.UnlockConditionExpr = GetSlotUnlockCondition(i + 1);
            }

            Slots.Add(slot);
        }
    }

    private string GetSlotUnlockCondition(int slotNumber)
    {
        return slotNumber switch
        {
            2 => "level >= 10 OR gold >= 1000",
            3 => "level >= 20 AND reputation.adventurer >= 500",
            4 => "level >= 30 AND quest.completed.MainQuest10",
            5 => "level >= 50 AND gold >= 10000",
            _ => "false"
        };
    }

    /// <summary>
    /// 创建新角色
    /// </summary>
    public bool CreateCharacter(string name, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count)
            return false;

        var slot = Slots[slotIndex];
        if (slot.State != SlotState.Unlocked || slot.CharacterId != null)
            return false;

        var character = new Character(name);
        slot.CharacterId = character.Id;
        slot.CharacterName = name;
        slot.State = SlotState.Occupied;

        if (ActiveCharacterId == null)
            ActiveCharacterId = character.Id;

        return true;
    }

    /// <summary>
    /// 切换活跃角色
    /// </summary>
    public bool SwitchActiveCharacter(string characterId)
    {
        var slot = Slots.FirstOrDefault(s => s.CharacterId == characterId);
        if (slot == null || slot.State != SlotState.Occupied)
            return false;

        ActiveCharacterId = characterId;
        return true;
    }

    /// <summary>
    /// 尝试解锁槽位
    /// </summary>
    public bool TryUnlockSlot(int slotIndex, IConditionContext context)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count)
            return false;

        var slot = Slots[slotIndex];
        if (slot.State != SlotState.Locked)
            return false;

        if (string.IsNullOrEmpty(slot.UnlockConditionExpr))
            return false;

        var condition = new ConditionExpr(slot.UnlockConditionExpr);
        if (condition.Evaluate(context))
        {
            slot.State = SlotState.Unlocked;
            UnlockedCount++;
            return true;
        }

        return false;
    }
}

/// <summary>
/// 角色槽位
/// </summary>
public class RosterSlot
{
    public int Index { get; }
    public SlotState State { get; set; } = SlotState.Locked;
    public string? CharacterId { get; set; }
    public string? CharacterName { get; set; }
    public string? UnlockConditionExpr { get; set; }
    public DateTime? LastPlayedAt { get; set; }

    public RosterSlot(int index)
    {
        Index = index;
    }
}

/// <summary>
/// 槽位状态
/// </summary>
public enum SlotState
{
    Locked,     // 未解锁
    Unlocked,   // 已解锁但空闲
    Occupied    // 已占用
}