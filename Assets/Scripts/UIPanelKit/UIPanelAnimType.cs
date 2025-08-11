using UnityEngine;

/// <summary>
/// UI面板动画类型
/// </summary>
public enum UIPanelAnimType
{
    /// <summary>
    /// 无动画
    /// </summary>
    None,
    
    /// <summary>
    /// 淡入淡出
    /// </summary>
    Fade,
    
    /// <summary>
    /// 缩放
    /// </summary>
    Scale,
    
    /// <summary>
    /// 从上方滑入
    /// </summary>
    SlideFromTop,
    
    /// <summary>
    /// 从下方滑入
    /// </summary>
    SlideFromBottom,
    
    /// <summary>
    /// 从左侧滑入
    /// </summary>
    SlideFromLeft,
    
    /// <summary>
    /// 从右侧滑入
    /// </summary>
    SlideFromRight,
    
    /// <summary>
    /// 自定义动画
    /// </summary>
    Custom
} 