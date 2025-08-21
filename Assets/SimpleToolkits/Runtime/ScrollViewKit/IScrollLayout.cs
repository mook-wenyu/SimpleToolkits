namespace SimpleToolkits
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 滚动布局接口 - 纯手动布局计算，无Unity布局依赖
    /// </summary>
    public interface IScrollLayout
    {
        /// <summary>布局类型</summary>
        LayoutType Type { get; }

        /// <summary>是否纵向滚动</summary>
        bool IsVertical { get; }

        /// <summary>计算Content总尺寸</summary>
        Vector2 CalculateContentSize(int itemCount, IScrollSizeProvider sizeProvider, Vector2 viewportSize);

        /// <summary>计算可见范围</summary>
        (int first, int last) CalculateVisibleRange(Vector2 contentPosition, Vector2 viewportSize, int itemCount, IScrollSizeProvider sizeProvider);

        /// <summary>计算指定索引的位置</summary>
        Vector2 CalculateItemPosition(int index, int itemCount, IScrollSizeProvider sizeProvider, Vector2 viewportSize);

        /// <summary>设置Content的锚点和Pivot</summary>
        void SetupContent(RectTransform content);
    }

    /// <summary>
    /// 尺寸提供器接口 - 支持固定和动态尺寸
    /// </summary>
    public interface IScrollSizeProvider
    {
        /// <summary>获取指定索引的尺寸</summary>
        Vector2 GetItemSize(int index, Vector2 viewportSize);

        /// <summary>是否支持变尺寸</summary>
        bool SupportsVariableSize { get; }

        /// <summary>获取平均尺寸（用于预估计算）</summary>
        Vector2 GetAverageSize(Vector2 viewportSize);
    }

    /// <summary>
    /// 数据适配器接口 - 纯数据绑定，无布局依赖
    /// </summary>
    public interface IScrollAdapter
    {
        /// <summary>数据项数量</summary>
        int Count { get; }

        /// <summary>获取Cell预制体</summary>
        RectTransform GetCellPrefab();

        /// <summary>绑定数据到Cell</summary>
        void BindCell(int index, RectTransform cell);

        /// <summary>Cell创建回调</summary>
        void OnCellCreated(RectTransform cell);

        /// <summary>Cell回收回调</summary>
        void OnCellRecycled(int index, RectTransform cell);
    }

    /// <summary>
    /// 布局类型枚举
    /// </summary>
    public enum LayoutType
    {
        Vertical,   // 纵向
        Horizontal, // 横向  
        Grid        // 网格
    }

    /// <summary>
    /// 网格轴向枚举
    /// </summary>
    public enum GridAxis
    {
        Vertical,   // 纵向：固定列数，行数可变
        Horizontal  // 横向：固定行数，列数可变
    }
}