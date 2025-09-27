namespace BlazorWebGame.Refactored.Domain.Entities;

/// <summary>
/// 通知实体
/// </summary>
public class Notification : Entity
{
    public Guid Id { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
    public bool IsRead { get; private set; }
    public string Type { get; private set; } = "info";

    private Notification() { } // For serialization

    public Notification(Guid id, string message, string type = "info")
    {
        Id = id;
        Message = message;
        Type = type;
        Timestamp = DateTime.UtcNow;
        IsRead = false;
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }

    protected override object GetId() => Id;
}