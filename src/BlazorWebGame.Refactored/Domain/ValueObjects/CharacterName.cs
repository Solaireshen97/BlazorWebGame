using System.Text.RegularExpressions;

namespace BlazorWebGame.Refactored.Domain.ValueObjects;

/// <summary>
/// 操作结果泛型版本
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string Error { get; }

    private Result(bool isSuccess, T? value, string error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, string.Empty);
    public static Result<T> Failure(string error) => new(false, default, error);
}

/// <summary>
/// 角色名称值对象
/// </summary>
public sealed class CharacterName : IEquatable<CharacterName>
{
    public string Value { get; }
    
    private CharacterName(string value)
    {
        Value = value;
    }
    
    public static Result<CharacterName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<CharacterName>.Failure("Character name cannot be empty");
        
        if (value.Length < 3 || value.Length > 20)
            return Result<CharacterName>.Failure("Character name must be between 3 and 20 characters");
        
        if (!IsValidFormat(value))
            return Result<CharacterName>.Failure("Character name contains invalid characters");
        
        return Result<CharacterName>.Success(new CharacterName(value));
    }
    
    private static bool IsValidFormat(string value)
    {
        // 只允许字母、数字和部分特殊字符
        return Regex.IsMatch(value, @"^[a-zA-Z0-9_\-\u4e00-\u9fa5]+$");
    }
    
    public bool Equals(CharacterName? other)
    {
        if (other is null) return false;
        return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }
    
    public override bool Equals(object? obj) => obj is CharacterName other && Equals(other);
    public override int GetHashCode() => Value.ToLowerInvariant().GetHashCode();
    public override string ToString() => Value;
    
    public static implicit operator string(CharacterName name) => name.Value;
}

/// <summary>
/// 用户ID值对象
/// </summary>
public sealed class UserId : IEquatable<UserId>
{
    public Guid Value { get; }
    
    private UserId(Guid value)
    {
        Value = value;
    }
    
    public static UserId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty");
        
        return new UserId(value);
    }
    
    public static UserId CreateNew() => new(Guid.NewGuid());
    
    public bool Equals(UserId? other)
    {
        if (other is null) return false;
        return Value.Equals(other.Value);
    }
    
    public override bool Equals(object? obj) => obj is UserId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
}