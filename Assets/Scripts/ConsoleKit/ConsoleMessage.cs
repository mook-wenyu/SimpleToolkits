using UnityEngine;

/// <summary>
/// 控制台消息结构
/// 用于存储控制台中显示的日志消息信息
/// </summary>
public struct ConsoleMessage
{
    /// <summary>
    /// 消息内容
    /// </summary>
    public readonly string message;
    
    /// <summary>
    /// 堆栈跟踪信息
    /// </summary>
    public readonly string stackTrace;
    
    /// <summary>
    /// 日志类型
    /// </summary>
    public readonly LogType type;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <param name="stackTrace">堆栈跟踪信息</param>
    /// <param name="type">日志类型</param>
    public ConsoleMessage(string message, string stackTrace, LogType type)
    {
        this.message = message;
        this.stackTrace = stackTrace;
        this.type = type;
    }
}
