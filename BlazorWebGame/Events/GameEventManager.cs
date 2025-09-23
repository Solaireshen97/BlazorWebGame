using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Events
{
    /// <summary>
    /// ��Ϸ�¼�������
    /// </summary>
    public class GameEventManager
    {
        // �¼��������ֵ䣬���¼����ͷ���
        private readonly Dictionary<GameEventType, List<Action<GameEventArgs>>> _handlers = new();
        
        /// <summary>
        /// �����¼�
        /// </summary>
        /// <param name="eventType">Ҫ���ĵ��¼�����</param>
        /// <param name="handler">�¼�������</param>
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
        /// ȡ���¼�����
        /// </summary>
        /// <param name="eventType">�¼�����</param>
        /// <param name="handler">������</param>
        /// <returns>�Ƿ�ɹ�ȡ��</returns>
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
        /// �����¼�
        /// </summary>
        /// <param name="args">�¼�����</param>
        public void Raise(GameEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            // �����ָ���¼����͵Ĵ���������������
            if (_handlers.TryGetValue(args.EventType, out var typeHandlers))
            {
                foreach (var handler in typeHandlers.ToList())
                {
                    try
                    {
                        handler(args);
                        
                        // ����¼���ȡ����ֹͣ��һ������
                        if (args.IsCancelled)
                            break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"�����¼� {args.EventType} ʱ����: {ex.Message}");
                    }
                }
            }
            
            // ͬʱ����GenericStateChanged�¼������ǵ�ǰ�¼��������GenericStateChanged
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
                            Console.WriteLine($"����ͨ��״̬�仯�¼�ʱ����: {ex.Message}");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// ��������¼�����
        /// </summary>
        public void ClearAllSubscriptions()
        {
            _handlers.Clear();
        }
        
        /// <summary>
        /// ����ض��¼����͵����ж���
        /// </summary>
        /// <param name="eventType">�¼�����</param>
        public void ClearSubscriptions(GameEventType eventType)
        {
            if (_handlers.ContainsKey(eventType))
            {
                _handlers[eventType].Clear();
            }
        }
    }
}