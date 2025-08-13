using System;

/// <summary>
/// 布尔类型枚举
/// 用于精确表示配置合并时的意图，解决bool类型的歧义问题
/// </summary>
public enum BoolType
{
    /// <summary>
    /// 不修改此属性，保持默认值
    /// </summary>
    None,

    /// <summary>
    /// 设置为false
    /// </summary>
    False,

    /// <summary>
    /// 设置为true
    /// </summary>
    True
}

/// <summary>
/// BoolType 的扩展方法和转换操作符
/// </summary>
public static class BoolTypeExtensions
{
    /// <summary>
    /// 从 bool 转换为 BoolType
    /// </summary>
    public static BoolType ToBoolType(this bool value)
    {
        return value ? BoolType.True : BoolType.False;
    }

    /// <summary>
    /// 从 BoolType 转换为 bool（仅当不为None时）
    /// </summary>
    public static bool ToBool(this BoolType value)
    {
        return value switch
        {
            BoolType.True => true,
            BoolType.False => false,
            BoolType.None => throw new InvalidOperationException("Cannot convert BoolType.None to bool"),
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };
    }

    /// <summary>
    /// 检查是否有有效的bool值（不为None）
    /// </summary>
    public static bool HasValue(this BoolType value)
    {
        return value != BoolType.None;
    }
}
