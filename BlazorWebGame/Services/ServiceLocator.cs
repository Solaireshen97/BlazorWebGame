using System;
using System.Collections.Generic;

namespace BlazorWebGame.Services
{
    /// <summary>
    /// ����λ�������ڻ�ȡ����ʵ����ͳһ���
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();
        private static bool _isInitialized = false;

        /// <summary>
        /// ��ʼ������λ��
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
                throw new InvalidOperationException("ServiceLocator�Ѿ���ʼ��");
                
            _services.Clear();
            _isInitialized = true;
        }

        /// <summary>
        /// ע�����
        /// </summary>
        /// <typeparam name="T">��������</typeparam>
        /// <param name="service">����ʵ��</param>
        /// <param name="overwrite">�Ƿ񸲸�����ע��</param>
        /// <exception cref="ArgumentNullException">����ʵ��Ϊnull</exception>
        /// <exception cref="InvalidOperationException">������ע���Ҳ�������</exception>
        public static void RegisterService<T>(T service, bool overwrite = false) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service), "����ע��null����");

            var type = typeof(T);
            if (_services.ContainsKey(type) && !overwrite)
                throw new InvalidOperationException($"�������� {type.Name} ��ע�ᣬ���踲��������overwrite=true");

            _services[type] = service;
        }

        /// <summary>
        /// ��ȡ��ע��ķ���
        /// </summary>
        /// <typeparam name="T">Ҫ��ȡ�ķ�������</typeparam>
        /// <returns>����ʵ������δע���򷵻�null</returns>
        public static T? GetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return (T)service;
                
            return null;
        }
        
        /// <summary>
        /// ��ȡ��ע��ķ�����δע�����׳��쳣
        /// </summary>
        /// <typeparam name="T">Ҫ��ȡ�ķ�������</typeparam>
        /// <returns>����ʵ��</returns>
        /// <exception cref="InvalidOperationException">����ķ���δע��</exception>
        public static T GetRequiredService<T>() where T : class
        {
            var service = GetService<T>();
            if (service == null)
                throw new InvalidOperationException($"���� {typeof(T).Name} δע��");
                
            return service;
        }
        
        /// <summary>
        /// �������Ƿ���ע��
        /// </summary>
        /// <typeparam name="T">��������</typeparam>
        /// <returns>�Ƿ���ע��</returns>
        public static bool IsRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// �Ƴ�ע��ķ���
        /// </summary>
        /// <typeparam name="T">��������</typeparam>
        /// <returns>�Ƿ�ɹ��Ƴ�</returns>
        public static bool RemoveService<T>() where T : class
        {
            return _services.Remove(typeof(T));
        }
        
        /// <summary>
        /// �������ע��ķ���
        /// </summary>
        public static void ClearAllServices()
        {
            _services.Clear();
        }
    }
}