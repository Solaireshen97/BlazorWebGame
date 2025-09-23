namespace BlazorWebGame.Services
{
    /// <summary>
    /// �����ṩ�߽ӿڣ����ڷ������������
    /// </summary>
    public interface IServiceProvider
    {
        /// <summary>
        /// ��ȡ����ʵ��
        /// </summary>
        /// <typeparam name="T">��������</typeparam>
        /// <returns>����ʵ�����粻���ڷ���null</returns>
        T? GetService<T>() where T : class;
        
        /// <summary>
        /// ��ȡ����ʵ�����粻�������׳��쳣
        /// </summary>
        /// <typeparam name="T">��������</typeparam>
        /// <returns>����ʵ��</returns>
        T GetRequiredService<T>() where T : class;
    }
    
    /// <summary>
    /// �����ṩ�ߵ�Ĭ��ʵ�֣�ʹ��ServiceLocator
    /// </summary>
    public class ServiceProvider : IServiceProvider
    {
        public T? GetService<T>() where T : class => ServiceLocator.GetService<T>();
        
        public T GetRequiredService<T>() where T : class => ServiceLocator.GetRequiredService<T>();
    }
}