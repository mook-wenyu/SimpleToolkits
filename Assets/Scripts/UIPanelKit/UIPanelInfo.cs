using System;

/// <summary>
/// UI面板配置信息类
/// 存储面板的各种配置参数，用于在注册时设置，打开时使用
/// </summary>
[Serializable]
public class UIPanelInfo
{
    /// <summary>
    /// 面板类型
    /// </summary>
    public Type PanelType { get; set; }

    /// <summary>
    /// UI层级
    /// </summary>
    public UILayerType Layer { get; set; } = UILayerType.Panel;

    /// <summary>
    /// 是否允许多实例
    /// </summary>
    public bool AllowMultiple { get; set; } = false;

    /// <summary>
    /// 是否全屏面板
    /// </summary>
    public bool Fullscreen { get; set; } = false;

    /// <summary>
    /// 是否需要背景遮罩
    /// </summary>
    public bool NeedMask { get; set; } = false;

    /// <summary>
    /// 是否可以点击外部关闭
    /// </summary>
    public bool CloseByOutside { get; set; } = false;

    /// <summary>
    /// 面板显示时的动画类型
    /// </summary>
    public UIPanelAnimType AnimType { get; set; } = UIPanelAnimType.None;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UIPanelInfo(UILayerType layer = UILayerType.Panel, bool allowMultiple = false,
        bool fullscreen = false, bool needMask = false, bool closeByOutside = false,
        UIPanelAnimType animType = UIPanelAnimType.None)
    {
        Layer = layer;
        AllowMultiple = allowMultiple;
        Fullscreen = fullscreen;
        NeedMask = needMask;
        CloseByOutside = closeByOutside;
        AnimType = animType;
    }

    /// <summary>
    /// 默认配置
    /// </summary>
    public static UIPanelInfo Default => new UIPanelInfo();

    /// <summary>
    /// 弹窗配置（带遮罩，可点击外部关闭，缩放动画）
    /// </summary>
    public static UIPanelInfo Popup => new UIPanelInfo(
        layer: UILayerType.Popup,
        needMask: true,
        closeByOutside: true,
        animType: UIPanelAnimType.Scale
    );

    /// <summary>
    /// 复制配置
    /// </summary>
    public UIPanelInfo Clone()
    {
        return new UIPanelInfo(Layer, AllowMultiple, Fullscreen, NeedMask, CloseByOutside, AnimType)
        {
            PanelType = PanelType
        };
    }

    /// <summary>
    /// 转换为字符串（用于调试）
    /// </summary>
    public override string ToString()
    {
        return $"UIPanelInfo[Layer:{Layer}, Multiple:{AllowMultiple}, " +
               $"Fullscreen:{Fullscreen}, Mask:{NeedMask}, CloseOutside:{CloseByOutside}, " +
               $"Anim:{AnimType}]";
    }
}
