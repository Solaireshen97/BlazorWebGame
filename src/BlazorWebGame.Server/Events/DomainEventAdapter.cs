using System;
using System.Runtime.InteropServices;
using BlazorWebGame.Shared.Events;
using BlazorWebGame.Shared.Models;

namespace BlazorWebGame.Server.Events
{
    /// <summary>
    /// 领域事件类型映射
    /// </summary>
    public static class DomainEventTypes
    {
        // 领域事件类型 (600-699)
        public const ushort SKILL_CAST = 600;
        public const ushort RESOURCE_OVERFLOW = 601;
        public const ushort PLAN_COMPLETED = 602;
        public const ushort SEGMENT_FLUSHED = 603;
        public const ushort DAMAGE_DEALT_DOMAIN = 604;
        public const ushort RESOURCE_GAIN = 605;
        public const ushort ATTACK_TICK = 606;
        public const ushort SPECIAL_PULSE = 607;
    }

    /// <summary>
    /// 领域事件内联数据结构
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SkillCastEventData
    {
        public uint SkillIdHash;      // 4 bytes - 技能ID的哈希
        public ulong CasterId;         // 8 bytes
        public ulong TargetId;         // 8 bytes
        public float ManaCost;         // 4 bytes
        public float EnergyCost;       // 4 bytes
        // Total: 28 bytes (exactly fits!)
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ResourceOverflowEventData
    {
        public uint ResourceIdHash;    // 4 bytes
        public float OverflowAmount;   // 4 bytes
        public uint ConvertedToHash;   // 4 bytes
        public float ConvertedAmount;  // 4 bytes
        public ulong OwnerId;          // 8 bytes
        // Total: 24 bytes
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DomainDamageEventData
    {
        public ulong SourceId;         // 8 bytes
        public ulong TargetId;         // 8 bytes
        public float Amount;           // 4 bytes
        public uint SkillIdHash;       // 4 bytes
        public byte IsCritical;        // 1 byte
        public byte DamageType;        // 1 byte
        // Total: 26 bytes
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AttackTickEventData
    {
        public ulong AttackerId;       // 8 bytes
        public ulong TargetId;         // 8 bytes
        public float AttackSpeed;      // 4 bytes
        public int TickNumber;         // 4 bytes
        // Total: 24 bytes
    }

    /// <summary>
    /// 领域事件适配器 - 将领域事件转换为统一事件
    /// </summary>
    public class DomainEventAdapter
    {
        private readonly UnifiedEventQueue _eventQueue;
        private readonly ILogger<DomainEventAdapter> _logger;

        public DomainEventAdapter(UnifiedEventQueue eventQueue, ILogger<DomainEventAdapter> logger)
        {
            _eventQueue = eventQueue;
            _logger = logger;
        }

        /// <summary>
        /// 发布领域事件
        /// </summary>
        public bool PublishDomainEvent(IDomainEvent domainEvent)
        {
            try
            {
                return domainEvent switch
                {
                    SkillCastEvent e => PublishSkillCast(e),
                    ResourceOverflowEvent e => PublishResourceOverflow(e),
                    DamageEvent e => PublishDamage(e),
                    AttackTickEvent e => PublishAttackTick(e),
                    PlanCompletedEvent e => PublishPlanCompleted(e),
                    ResourceGainEvent e => PublishResourceGain(e),
                    _ => PublishGenericDomainEvent(domainEvent)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish domain event: {EventType}", domainEvent.EventType);
                return false;
            }
        }

        private bool PublishSkillCast(SkillCastEvent e)
        {
            var data = new SkillCastEventData
            {
                SkillIdHash = (uint)e.SkillId.GetHashCode(),
                CasterId = ParseEntityId(e.CasterId),
                TargetId = ParseEntityId(e.TargetId),
                ManaCost = (float)(e.ResourceCosts.GetValueOrDefault("Mana", 0)),
                EnergyCost = (float)(e.ResourceCosts.GetValueOrDefault("Energy", 0))
            };

            return _eventQueue.EnqueueEvent(
                DomainEventTypes.SKILL_CAST,
                data,
                EventPriority.Gameplay,
                data.CasterId,
                data.TargetId
            );
        }

        private bool PublishResourceOverflow(ResourceOverflowEvent e)
        {
            var data = new ResourceOverflowEventData
            {
                ResourceIdHash = (uint)e.ResourceId.GetHashCode(),
                OverflowAmount = (float)e.OverflowAmount,
                ConvertedToHash = (uint)(e.ConvertedTo?.GetHashCode() ?? 0),
                ConvertedAmount = (float)e.ConvertedAmount,
                OwnerId = ParseEntityId(e.AggregateId)
            };

            return _eventQueue.EnqueueEvent(
                DomainEventTypes.RESOURCE_OVERFLOW,
                data,
                EventPriority.Analytics,
                data.OwnerId
            );
        }

        private bool PublishDamage(DamageEvent e)
        {
            var data = new DomainDamageEventData
            {
                SourceId = ParseEntityId(e.SourceId),
                TargetId = ParseEntityId(e.TargetId),
                Amount = (float)e.Amount,
                SkillIdHash = (uint)(e.SkillId?.GetHashCode() ?? 0),
                IsCritical = (byte)(e.IsCritical ? 1 : 0),
                DamageType = 0 // Physical
            };

            return _eventQueue.EnqueueEvent(
                DomainEventTypes.DAMAGE_DEALT_DOMAIN,
                data,
                EventPriority.Gameplay,
                data.SourceId,
                data.TargetId
            );
        }

        private bool PublishAttackTick(AttackTickEvent e)
        {
            var data = new AttackTickEventData
            {
                AttackerId = ParseEntityId(e.AttackerId),
                TargetId = ParseEntityId(e.TargetId),
                AttackSpeed = 1.0f,
                TickNumber = 0
            };

            return _eventQueue.EnqueueEvent(
                DomainEventTypes.ATTACK_TICK,
                data,
                EventPriority.Gameplay,
                data.AttackerId,
                data.TargetId
            );
        }

        private bool PublishPlanCompleted(PlanCompletedEvent e)
        {
            // 简单事件，只使用基本字段
            return _eventQueue.EnqueueEvent(
                DomainEventTypes.PLAN_COMPLETED,
                EventPriority.Analytics,
                ParseEntityId(e.AggregateId)
            );
        }

        private bool PublishResourceGain(ResourceGainEvent e)
        {
            var data = new ResourceOverflowEventData // 复用结构
            {
                ResourceIdHash = (uint)e.ResourceId.GetHashCode(),
                OverflowAmount = (float)e.Amount,
                OwnerId = ParseEntityId(e.OwnerId)
            };

            return _eventQueue.EnqueueEvent(
                DomainEventTypes.RESOURCE_GAIN,
                data,
                EventPriority.Analytics,
                data.OwnerId
            );
        }

        private bool PublishGenericDomainEvent(IDomainEvent e)
        {
            // 对于未特殊处理的领域事件，使用通用状态变更事件
            return _eventQueue.EnqueueEvent(
                GameEventTypes.STATE_CHANGED,
                EventPriority.Analytics,
                ParseEntityId(e.AggregateId)
            );
        }

        private ulong ParseEntityId(string? id)
        {
            if (string.IsNullOrEmpty(id))
                return 0;

            // 如果是GUID，取前8字节
            if (Guid.TryParse(id, out var guid))
            {
                var bytes = guid.ToByteArray();
                return BitConverter.ToUInt64(bytes, 0);
            }

            // 否则使用哈希
            return (ulong)id.GetHashCode();
        }
    }
}