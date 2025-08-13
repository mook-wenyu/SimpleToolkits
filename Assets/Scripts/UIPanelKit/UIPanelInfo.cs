using System;

/// <summary>
/// UI面板配置信息结构体
/// 存储面板的各种配置参数，用于在注册时设置，打开时使用
/// 作为值类型，避免不必要的堆分配，提供更清晰的复制语义
/// </summary>
[Serializable]
public struct UIPanelInfo : IEquatable<UIPanelInfo>
{
    /// <summary>
    /// 面板类型
    /// </summary>
    public Type panelType;

    /// <summary>
    /// UI层级
    /// </summary>
    public UILayerType layer;

    /// <summary>
    /// 是否允许多实例
    /// </summary>
    public BoolType allowMultiple;

    /// <summary>
    /// 是否需要背景遮罩
    /// </summary>
    public BoolType needMask;

    /// <summary>
    /// 是否可以点击外部关闭
    /// </summary>
    public BoolType closeByOutside;

    /// <summary>
    /// 面板显示时的动画类型
    /// </summary>
    public UIPanelAnimType animType;

    public UIPanelInfo(Type panelType = null, UILayerType layer = UILayerType.None,
        BoolType allowMultiple = BoolType.None, BoolType needMask = BoolType.None,
        BoolType closeByOutside = BoolType.None, UIPanelAnimType animType = UIPanelAnimType.None)
    {
        this.panelType = panelType;
        this.layer = layer;
        this.allowMultiple = allowMultiple;
        this.needMask = needMask;
        this.closeByOutside = closeByOutside;
        this.animType = animType;
    }
    
    /// <summary>
    /// 复制配置（对于struct，这实际上是值复制）
    /// </summary>
    public UIPanelInfo Clone()
    {
        return this; // struct 的复制是值复制
    }

    /// <summary>
    /// 从另一个配置对象合并属性
    /// 用于实现部分参数覆盖功能
    /// </summary>
    /// <param name="other">要合并的配置对象</param>
    public void MergeFrom(in UIPanelInfo other)
    {
        // 对于Layer枚举，UILayerType.None表示"不修改此属性"
        if (other.layer != UILayerType.None)
            layer = other.layer;

        // 对于BoolType类型，只有当值不为None时才覆盖
        // 这样可以精确区分用户的意图
        if (other.allowMultiple.HasValue())
            allowMultiple = other.allowMultiple;

        if (other.needMask.HasValue())
            needMask = other.needMask;

        if (other.closeByOutside.HasValue())
            closeByOutside = other.closeByOutside;

        // 对于AnimType枚举，UIPanelAnimType.None表示默认值，不覆盖
        if (other.animType != UIPanelAnimType.None)
            animType = other.animType;
    }

    /// <summary>
    /// 判断两个配置是否相等
    /// </summary>
    public bool Equals(UIPanelInfo other)
    {
        return panelType == other.panelType &&
               layer == other.layer &&
               allowMultiple == other.allowMultiple &&
               needMask == other.needMask &&
               closeByOutside == other.closeByOutside &&
               animType == other.animType;
    }

    /// <summary>
    /// 重写 Equals 方法
    /// </summary>
    public override bool Equals(object obj)
    {
        return obj is UIPanelInfo other && Equals(other);
    }

    /// <summary>
    /// 重写 GetHashCode 方法
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(panelType, layer, allowMultiple, needMask, closeByOutside, animType);
    }

    /// <summary>
    /// 相等运算符重载
    /// </summary>
    public static bool operator ==(UIPanelInfo left, UIPanelInfo right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// 不等运算符重载
    /// </summary>
    public static bool operator !=(UIPanelInfo left, UIPanelInfo right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// 转换为字符串（用于调试）
    /// </summary>
    public override string ToString()
    {
        return $"UIPanelInfo[Layer:{layer}, Multiple:{allowMultiple}, " +
               $"Mask:{needMask}, CloseOutside:{closeByOutside}, " +
               $"Anim:{animType}]";
    }
}
