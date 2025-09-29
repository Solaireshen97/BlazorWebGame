using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Events
{
    /// <summary>
    /// 游戏事件管理器
    /// </summary>
    public class GameEventManager
    {
        // 事件处理器字典，按事件类型分组
        private readonly Dictionary<GameEventType, List<Action<GameEventArgs>>> _handlers = new();
        
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="eventType">要订阅的事件类型</param>
        /// <param name="handler">事件处理器</param>
        public void Subscribe(GameEventType eventType, Action<GameEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Action<GameEventArgs>>();
                _handlers[eventType] = handlers;
            }
            
            handlers.Add(handler);
        }
        
        /// <summary>
        /// 取消事件订阅
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="handler">处理器</param>
        /// <returns>是否成功取消</returns>
        public bool Unsubscribe(GameEventType eventType, Action<GameEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                return handlers.Remove(handler);
            }
            
            return false;
        }
        
        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="args">事件参数</param>
        public void Raise(GameEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            // 处理特定事件类型的处理器并发送消息
            if (_handlers.TryGetValue(args.EventType, out var typeHandlers))
            {
                foreach (var handler in typeHandlers.ToList())
                {
                    try
                    {
                        handler(args);
                        
                        // 如果事件被取消则停止进一步处理
                        if (args.IsCancelled)
                            break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"处理事件 {args.EventType} 时出错: {ex.Message}");
                    }
                }
            }
            
            // 同时触发GenericStateChanged事件，除非当前事件本身就是GenericStateChanged
            if (args.EventType != GameEventType.GenericStateChanged)
            {
                if (_handlers.TryGetValue(GameEventType.GenericStateChanged, out var genericHandlers))
                {
                    foreach (var handler in genericHandlers.ToList())
                    {
                        try
                        {
                            handler(args);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"处理通用状态变化事件时出错: {ex.Message}");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 清除所有事件订阅
        /// </summary>
        public void ClearAllSubscriptions()
        {
            _handlers.Clear();
        }
        
        /// <summary>
        /// 清除特定事件类型的所有订阅
        /// </summary>
        /// <param name="eventType">事件类型</param>
        public void ClearSubscriptions(GameEventType eventType)
        {
            if (_handlers.ContainsKey(eventType))
            {
                _handlers[eventType].Clear();
            }
        }
    }
}