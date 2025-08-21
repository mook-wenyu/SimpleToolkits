namespace SimpleToolkits
{
    using UnityEngine;

    /// <summary>
    /// 数据适配器接口：提供单元格预制体与数据绑定。
    /// </summary>
    public interface IScrollAdapter
    {
        /// <summary>当前数据条目数量。</summary>
        int Count { get; }

        /// <summary>覆盖数据条目数量（可选）。</summary>
        void OverrideCount(int count);

        /// <summary>提供用于实例化的单一 Cell 预制体（需包含 RectTransform）。</summary>
        RectTransform GetCellPrefab();

        /// <summary>在 Cell 可见或需要刷新时回调，进行数据绑定。</summary>
        /// <param name="index">数据索引</param>
        /// <param name="cell">Cell RectTransform</param>
        void BindCell(int index, RectTransform cell);
    }

    /// <summary>
    /// 变尺寸适配器接口：提供动态尺寸计算功能。
    /// 当适配器实现此接口时，滚动器将按每项主轴尺寸进行虚拟化（前缀和+二分查找）。
    /// 注意：出于性能考虑，主轴尺寸应快速可得，且在刷新周期内尽量稳定。
    /// </summary>
    public interface IVariableSizeAdapter
    {
        /// <summary>
        /// 计算指定索引项的尺寸（返回 sizeDelta）。
        /// </summary>
        /// <param name="index">数据索引（0-based）。</param>
        /// <param name="viewportSize">当前视口大小（像素）。</param>
        /// <param name="layout">当前使用的布局策略。</param>
        /// <returns>返回该项在当前条件下的尺寸（sizeDelta）。</returns>
        Vector2 GetItemSize(int index, Vector2 viewportSize, IScrollLayout layout);
    }

    /// <summary>
    /// 布局策略接口：定义方向、间距、内边距以及索引->位置/范围的计算。
    /// 为保证高性能，建议使用统一单元格尺寸（由预制体测量或外部指定）。
    /// </summary>
    public interface IScrollLayout
    {
        /// <summary>是否纵向滚动（true=纵向，false=横向）。</summary>
        bool IsVertical { get; }

        /// <summary>单元格之间的间距（x=横轴间距，y=纵轴间距）。</summary>
        Vector2 Spacing { get; }

        /// <summary>内容内边距。</summary>
        RectOffset Padding { get; }

        /// <summary>网格专用：每行/列的项数（非网格布局可返回1）。</summary>
        int ConstraintCount { get; }

        /// <summary>控制子对象宽度：为 true 时框架会按布局跨轴拉伸 Cell 的宽度。</summary>
        bool ControlChildWidth { get; }

        /// <summary>控制子对象高度：为 true 时框架会按布局跨轴拉伸 Cell 的高度。</summary>
        bool ControlChildHeight { get; }

        /// <summary>是否反向排列（主轴方向索引镜像，例如纵向：0 在底部；横向：0 在最右）。</summary>
        bool Reverse { get; }

        /// <summary>在初始化时配置 Content 的锚点与 Pivot。</summary>
        void Setup(RectTransform viewport, RectTransform content);

        /// <summary>根据视口尺寸与单元格统一尺寸，计算 Content 总尺寸。</summary>
        Vector2 ComputeContentSize(int itemCount, Vector2 cellSize, Vector2 viewportSize);

        /// <summary>根据滚动位置与视口尺寸，计算应显示的索引范围（闭区间）。</summary>
        void GetVisibleRange(float normalizedPosition, int itemCount, Vector2 viewportSize, Vector2 cellSize, out int first, out int last);

        /// <summary>返回指定索引在 Content 下的 anchoredPosition（左上为原点）。</summary>
        Vector2 GetItemAnchoredPosition(int index, int itemCount, Vector2 cellSize);
    }

    /// <summary>
    /// 可选：可回收适配器接口，接收 Cell 创建与回收的生命周期回调。
    /// - 非必需：未实现时框架不会调用任何回收回调。
    /// - 用途：在回收时释放资源（如监听、动画、临时纹理），在创建时做一次性初始化。
    /// </summary>
    public interface IRecyclableScrollAdapter
    {
        /// <summary>当框架首次实例化一个 Cell 时调用（每个实例仅调用一次）。</summary>
        void OnCellCreated(RectTransform cell);
        /// <summary>当某索引对应的 Cell 被回收进入池时调用。</summary>
        void OnCellRecycled(int index, RectTransform cell);
    }
}
