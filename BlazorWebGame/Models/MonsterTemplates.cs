namespace BlazorWebGame.Models
{
    /// <summary>
    /// �ṩһ����̬�Ĺ���ģ���б�
    /// </summary>
    public static class MonsterTemplates
    {
        public static List<Enemy> All { get; } = new List<Enemy>
        {
            new Enemy("ʷ��ķ", 30, 3, 0.4, 1, 5),
            new Enemy("�粼��", 50, 5, 0.5, 5, 15),
            new Enemy("Ұ��", 80, 8, 0.8, 10, 25)
            // �Ժ������������Ӹ����ǿ�Ĺ���
        };
    }
}