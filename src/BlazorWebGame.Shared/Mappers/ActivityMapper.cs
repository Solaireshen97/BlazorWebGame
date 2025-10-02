using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlazorWebGame.Shared.Mappers
{
    /// <summary>
    /// �ϵͳӳ�乤���� - ������ģ�ͺ�DTO֮��ת��
    /// </summary>
    public static class ActivityMapper
    {
        /// <summary>
        /// ���ϵͳת��Ϊ��λDTO�б�
        /// </summary>
        public static List<ActivitySlotDto> ToDto(this ActivitySystem activitySystem)
        {
            return activitySystem.Slots.Select(slot => new ActivitySlotDto
            {
                SlotIndex = slot.Index,
                CurrentPlan = slot.CurrentPlan != null ? ToPlanDto(slot.CurrentPlan) : null
            }).ToList();
        }
        
        /// <summary>
        /// ����ƻ�ת��ΪDTO
        /// </summary>
        public static ActivityPlanDto ToPlanDto(this ActivityPlan plan)
        {
            return new ActivityPlanDto
            {
                Id = plan.Id,
                Type = plan.Type,
                State = plan.State.ToString(),
                Limit = new LimitSpecDto
                {
                    Type = plan.Limit.Type.ToString(),
                    Value = plan.Limit.TargetValue
                },
                Payload = new Dictionary<string, object>(plan.Payload),
                StartedAt = plan.StartedAt,
                CompletedAt = plan.CompletedAt,
                Progress = plan.Progress
            };
        }
        
        /// <summary>
        /// ��ȡ��ƻ��б�
        /// </summary>
        public static List<ActivityPlanDto> GetActivePlansDto(this ActivitySystem activitySystem)
        {
            return activitySystem.GetActivePlans().Select(ToPlanDto).ToList();
        }
        
        /// <summary>
        /// ��DTO�ָ��ϵͳ״̬
        /// </summary>
        public static void RestoreFromDto(this ActivitySystem activitySystem, List<ActivitySlotDto> activitySlots)
        {
            if (activitySlots == null || activitySlots.Count == 0)
                return;
                
            // ��յ�ǰ��λ
            foreach (var slot in activitySystem.Slots)
            {
                var currentPlanProperty = typeof(ActivitySlot).GetProperty("CurrentPlan", 
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    
                if (currentPlanProperty?.CanWrite == true)
                    currentPlanProperty.SetValue(slot, null);
            }
            
            // �ָ���λ����
            foreach (var slotDto in activitySlots.Where(s => s.SlotIndex >= 0 && s.SlotIndex < activitySystem.Slots.Count))
            {
                var slot = activitySystem.Slots[slotDto.SlotIndex];
                
                if (slotDto.CurrentPlan != null)
                {
                    var plan = FromPlanDto(slotDto.CurrentPlan);
                    activitySystem.AddPlan(plan, slotDto.SlotIndex);
                    
                    // �ָ��ƻ�״̬
                    if (slotDto.CurrentPlan.State == PlanState.Running.ToString() && slotDto.CurrentPlan.StartedAt.HasValue)
                    {
                        plan.Start();
                        
                        // ʹ�÷������ý���
                        SetPrivateProperty(plan, "Progress", slotDto.CurrentPlan.Progress);
                    }
                    else if (slotDto.CurrentPlan.State == PlanState.Completed.ToString() && slotDto.CurrentPlan.CompletedAt.HasValue)
                    {
                        plan.Complete();
                    }
                }
            }
        }
        
        /// <summary>
        /// ��DTO������ƻ�
        /// </summary>
        public static ActivityPlan FromPlanDto(ActivityPlanDto dto)
        {
            // ������������
            Enum.TryParse<LimitType>(dto.Limit.Type, out var limitType);
            
            var limit = new LimitSpec
            {
                Type = limitType,
                TargetValue = dto.Limit.Value,
                Remaining = dto.Limit.Value
            };
            
            var plan = new ActivityPlan(dto.Type, limit);
            
            // ʹ�÷�������˽������
            SetPrivateProperty(plan, "Id", dto.Id);
            SetPrivateProperty(plan, "Payload", new Dictionary<string, object>(dto.Payload));
            SetPrivateProperty(plan, "StartedAt", dto.StartedAt);
            SetPrivateProperty(plan, "CompletedAt", dto.CompletedAt);
            SetPrivateProperty(plan, "Progress", dto.Progress);
            
            // ����״̬
            Enum.TryParse<PlanState>(dto.State, out var state);
            SetPrivateProperty(plan, "State", state);
            
            return plan;
        }
        
        /// <summary>
        /// ��������������˽������
        /// </summary>
        private static void SetPrivateProperty<T>(object obj, string propertyName, T value)
        {
            var property = obj.GetType().GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, value);
            }
            else
            {
                var field = obj.GetType().GetField(propertyName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                    
                field?.SetValue(obj, value);
            }
        }
    }
}