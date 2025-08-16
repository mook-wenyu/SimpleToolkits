namespace SimpleToolkits
{
    /// <summary>
    /// UI层级
    /// </summary>
    public enum UILayerType
    {
        /// <summary>
        /// 无效层级 / 在配置合并时表示"不修改此属性，保持默认值"
        /// </summary>
        None,
        /// <summary>
        /// 常驻面板（如地图、主界面）
        /// </summary>
        Bottom,
        /// <summary>
        /// 普通面板（如设置、背包）
        /// </summary>
        Panel,
        /// <summary>
        /// 对话框（如提示框、确认框）
        /// </summary>
        Popup,
        /// <summary>
        /// 前置面板（与Pop类似，但可能有特殊用途）
        /// </summary>
        Front,
        /// <summary>
        /// 顶部导航栏（如标题栏、返回按钮）
        /// </summary>
        Top,
        /// <summary>
        /// 飞行提示（如短暂消息）
        /// </summary>
        FlyTip,
        /// <summary>
        /// 剧情面板（如过场动画）
        /// </summary>
        Plot,
        /// <summary>
        /// 加载界面
        /// </summary>
        Loading,
        /// <summary>
        /// 网络错误提示
        /// </summary>
        NetError
    }
}
