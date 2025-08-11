
/// <summary>
/// UI面板状态
/// </summary>
public enum UIPanelStateType
{
    /// <summary>
    /// 无效状态
    /// </summary>
    None,
    /// <summary>
    /// 正在加载
    /// </summary>
    Loading,
    /// <summary>
    /// 已加载但未显示
    /// </summary>
    Loaded,
    /// <summary>
    /// 正在显示中
    /// </summary>
    Showing,
    /// <summary>
    /// 已隐藏
    /// </summary>
    Hidden,
    /// <summary>
    /// 已销毁
    /// </summary>
    Destroyed
}
