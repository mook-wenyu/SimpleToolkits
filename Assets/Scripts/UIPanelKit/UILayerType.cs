
/// <summary>
/// UI层级
/// </summary>
public enum UILayerType
{
    /// <summary>
    /// 常驻面板（如地图、主界面）
    /// </summary>
    Bottom = 0,
    /// <summary>
    /// 普通面板（如设置、背包）
    /// </summary>
    Panel = 1,
    /// <summary>
    /// 对话框（如提示框、确认框）
    /// </summary>
    Popup = 2,
    /// <summary>
    /// 前置面板（与Pop类似，但可能有特殊用途）
    /// </summary>
    Front = 3,
    /// <summary>
    /// 顶部导航栏（如标题栏、返回按钮）
    /// </summary>
    Top = 4,
    /// <summary>
    /// 飞行提示（如短暂消息）
    /// </summary>
    FlyTip = 5,
    /// <summary>
    /// 剧情面板（如过场动画）
    /// </summary>
    Plot = 6,
    /// <summary>
    /// 加载界面
    /// </summary>
    Loading = 7,
    /// <summary>
    /// 网络错误提示
    /// </summary>
    NetError = 8,
}
