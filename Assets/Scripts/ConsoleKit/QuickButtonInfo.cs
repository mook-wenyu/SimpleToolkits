/// <summary>
/// 快捷按钮信息结构
/// 用于存储快捷按钮的配置信息，包括显示文本、绑定的命令和参数
/// </summary>
public struct QuickButtonInfo
{
    /// <summary>
    /// 按钮显示文本
    /// </summary>
    public readonly string buttonText;
    
    /// <summary>
    /// 绑定的命令名称
    /// </summary>
    public readonly string commandName;
    
    /// <summary>
    /// 命令参数数组
    /// </summary>
    public readonly string[] args;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="buttonText">按钮显示文本</param>
    /// <param name="commandName">绑定的命令名称</param>
    /// <param name="args">命令参数数组，可以为null</param>
    public QuickButtonInfo(string buttonText, string commandName, string[] args)
    {
        this.buttonText = buttonText;
        this.commandName = commandName;
        this.args = args ?? new string[0];
    }
}
