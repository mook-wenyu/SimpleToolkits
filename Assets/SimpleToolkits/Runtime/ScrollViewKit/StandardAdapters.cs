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
    /// - 通过 ICellBinder + IVariableSizeAdapter 解耦业务
    /// - 仅在单列/单行（ConstraintCount==1）的列表场景下使用
    /// </summary>
    public sealed class StandardVariableSizeAdapter : BaseVariableSizeAdapter
    {
        private readonly ICellBinder _binder;
        private readonly IVariableSizeAdapter _sizeProvider;
        private readonly Func<int> _countGetter;

        /// <summary>静态数量构造</summary>
        public StandardVariableSizeAdapter(RectTransform prefab, int staticCount, ICellBinder binder, IVariableSizeAdapter sizeProvider)
            : base(prefab, () => staticCount)
        {
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
            _sizeProvider = sizeProvider ?? throw new ArgumentNullException(nameof(sizeProvider));
            _countGetter = () => staticCount;
        }

        /// <summary>动态数量构造</summary>
        public StandardVariableSizeAdapter(RectTransform prefab, Func<int> countGetter, ICellBinder binder, IVariableSizeAdapter sizeProvider)
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
