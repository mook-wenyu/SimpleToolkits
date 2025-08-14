using System;

/// <summary>
/// 命令信息结构
/// 用于存储注册的控制台命令的详细信息
/// </summary>
public struct CommandInfo
{
    /// <summary>
    /// 命令名称
    /// </summary>
    public readonly string name;
    
    /// <summary>
    /// 命令描述
    /// </summary>
    public readonly string description;
    
    /// <summary>
    /// 命令执行方法
    /// </summary>
    public readonly Action<string[]> command;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">命令名称</param>
    /// <param name="description">命令描述</param>
    /// <param name="command">命令执行方法</param>
    public CommandInfo(string name, string description, Action<string[]> command)
    {
        this.name = name;
        this.description = description;
        this.command = command;
    }
}
