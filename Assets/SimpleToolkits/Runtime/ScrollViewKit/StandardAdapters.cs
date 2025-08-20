namespace SimpleToolkits
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 细粒度业务绑定接口：统一生命周期钩子。
    /// 通过实现该接口，可在不改 Adapter 结构的情况下，复用生命周期管理与绑定流程。
    /// </summary>
    public interface ICellBinder
    {
        /// <summary>Cell 首次实例化时调用（每个实例仅一次）。</summary>
        void OnCreated(RectTransform cell);
        /// <summary>索引绑定回调（高频调用）。</summary>
        void OnBind(int index, RectTransform cell);
        /// <summary>Cell 回收时调用，用于解绑与资源回收。</summary>
        void OnRecycled(int index, RectTransform cell);
    }

    /// <summary>
    /// 变尺寸提供者接口。
    /// 返回每个条目在“当前布局与视口尺寸”条件下应当使用的 <see cref="RectTransform.sizeDelta"/>（宽高）。
    /// 注意：
    /// - 主轴尺寸由实现者决定（纵向列表=高度，横向列表=宽度）。
    /// - 跨轴尺寸通常由布局控制（如 VerticalLayout 控制宽度、HorizontalLayout 控制高度），若布局声明控制跨轴，返回值的对应分量可被忽略。
    /// - 实现需保证高性能与可重入，滚动或重建时会被频繁调用，建议做必要的缓存。
    /// </summary>
    public interface ISizeProvider
    {
        /// <summary>
        /// 计算指定索引项的尺寸（返回 sizeDelta）。
        /// </summary>
        /// <param name="index">数据索引（0-based）。</param>
        /// <param name="viewportSize">当前视口大小（像素）。
        /// 用于在启用自动换行或跨轴受限时确定可用测量宽/高。例如纵向列表中文本换行需用到可用宽度。</param>
        /// <param name="layout">当前使用的布局策略（如 VerticalLayout/HorizontalLayout）。
        /// 可通过其属性判断主轴方向、间距、内边距以及是否由布局控制跨轴尺寸。</param>
        /// <returns>返回该项在当前条件下的 <see cref="Vector2"/> 尺寸（sizeDelta）。
        /// 应确保分量为正值；若实现返回非法值，框架将回退到预制体测量值。</returns>
        Vector2 GetItemSize(int index, Vector2 viewportSize, IScrollLayout layout);
    }

    /// <summary>
    /// 标准统一尺寸适配器：
    /// - 继承 BaseScrollAdapter，统一生命周期（Created/Bind/Recycled）
    /// - 通过 ICellBinder 解耦业务与框架
    /// - 支持动态数量（countGetter）与运行期 OverrideCount
    /// </summary>
    public sealed class StandardScrollAdapter : BaseScrollAdapter
    {
        private readonly ICellBinder _binder;
        private readonly Func<int> _countGetter;

        /// <summary>静态数量构造</summary>
        public StandardScrollAdapter(RectTransform prefab, int staticCount, ICellBinder binder)
            : base(prefab, () => staticCount)
        {
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
            _countGetter = () => staticCount;
        }

        /// <summary>动态数量构造</summary>
        public StandardScrollAdapter(RectTransform prefab, Func<int> countGetter, ICellBinder binder)
            : base(prefab, countGetter)
        {
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
            _countGetter = countGetter ?? throw new ArgumentNullException(nameof(countGetter));
        }

        protected override void OnCreated(RectTransform cell)
        {
            _binder.OnCreated(cell);
        }

        protected override void OnBind(int index, RectTransform cell)
        {
            _binder.OnBind(index, cell);
        }

        protected override void OnRecycled(int index, RectTransform cell)
        {
            _binder.OnRecycled(index, cell);
        }

        protected override int GetCount()
        {
            return Mathf.Max(0, _countGetter?.Invoke() ?? 0);
        }
    }

    /// <summary>
    /// 标准变尺寸适配器：
    /// - 继承 BaseVariableSizeAdapter，统一生命周期与尺寸提供
    /// - 通过 ICellBinder + ISizeProvider 解耦业务
    /// - 仅在单列/单行（ConstraintCount==1）的列表场景下使用
    /// </summary>
    public sealed class StandardVariableSizeAdapter : BaseVariableSizeAdapter
    {
        private readonly ICellBinder _binder;
        private readonly ISizeProvider _sizeProvider;
        private readonly Func<int> _countGetter;

        /// <summary>静态数量构造</summary>
        public StandardVariableSizeAdapter(RectTransform prefab, int staticCount, ICellBinder binder, ISizeProvider sizeProvider)
            : base(prefab, () => staticCount)
        {
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
            _sizeProvider = sizeProvider ?? throw new ArgumentNullException(nameof(sizeProvider));
            _countGetter = () => staticCount;
        }

        /// <summary>动态数量构造</summary>
        public StandardVariableSizeAdapter(RectTransform prefab, Func<int> countGetter, ICellBinder binder, ISizeProvider sizeProvider)
            : base(prefab, countGetter)
        {
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
            _sizeProvider = sizeProvider ?? throw new ArgumentNullException(nameof(sizeProvider));
            _countGetter = countGetter ?? throw new ArgumentNullException(nameof(countGetter));
        }

        protected override void OnCreated(RectTransform cell)
        {
            _binder.OnCreated(cell);
        }

        protected override void OnBind(int index, RectTransform cell)
        {
            _binder.OnBind(index, cell);
        }

        protected override void OnRecycled(int index, RectTransform cell)
        {
            _binder.OnRecycled(index, cell);
        }

        public override Vector2 GetItemSize(int index, Vector2 viewportSize, IScrollLayout layout)
        {
            return _sizeProvider.GetItemSize(index, viewportSize, layout);
        }

        protected override int GetCount()
        {
            return Mathf.Max(0, _countGetter?.Invoke() ?? 0);
        }
    }
}
