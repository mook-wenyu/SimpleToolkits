namespace SimpleToolkits
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

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
    /// 增强的标准变尺寸适配器（合并了原LayoutAutoSizeProvider功能）：
    /// - 继承 BaseVariableSizeAdapter，提供完整的变尺寸支持
    /// - 集成了自动尺寸计算、缓存机制和性能优化
    /// - 通过 ICellBinder 解耦业务逻辑与框架
    /// - 支持基于Unity布局组件的自动尺寸计算
    /// - 智能支持固定和自适应尺寸参数，根据IScrollLayout布局模式自动优化
    /// - 适用于纵向、横向、网格等各种动态尺寸列表场景
    /// </summary>
    public sealed class StandardVariableSizeAdapter : BaseVariableSizeAdapter
    {
        #region 字段和属性
        private readonly ICellBinder _binder;
        private readonly Func<int> _countGetter;
        private readonly Func<int, object> _dataGetter;
        private readonly Action<RectTransform, object> _templateBinder;
        private readonly Dictionary<int, Vector2> _sizeCache = new Dictionary<int, Vector2>();
        private readonly bool _enableCache;
        private readonly int _maxCacheSize;
        #endregion

        #region 构造函数
        /// <summary>
        /// 标准构造函数 - 支持固定和自适应尺寸参数
        /// 根据布局方向自动选择使用固定尺寸还是自适应尺寸
        /// </summary>
        /// <param name="prefab">单元格预制体</param>
        /// <param name="countGetter">数量获取器</param>
        /// <param name="dataGetter">数据获取器</param>
        /// <param name="binder">单元格绑定器</param>
        /// <param name="templateBinder">模板绑定器（用于尺寸计算）</param>
        /// <param name="fixedWidth">固定宽度（≤0表示自适应）</param>
        /// <param name="fixedHeight">固定高度（≤0表示自适应）</param>
        /// <param name="minWidth">最小宽度</param>
        /// <param name="minHeight">最小高度</param>
        /// <param name="maxWidth">最大宽度</param>
        /// <param name="maxHeight">最大高度</param>
        /// <param name="useLayoutGroups">是否使用布局组</param>
        /// <param name="enableCache">是否启用缓存</param>
        /// <param name="maxCacheSize">最大缓存大小</param>
        /// <param name="forceRebuild">是否强制重建布局</param>
        public StandardVariableSizeAdapter(
            RectTransform prefab,
            Func<int> countGetter,
            Func<int, object> dataGetter,
            ICellBinder binder,
            Action<RectTransform, object> templateBinder,
            float fixedWidth = -1f,
            float fixedHeight = -1f,
            float minWidth = 0f,
            float minHeight = 0f,
            float maxWidth = -1f,
            float maxHeight = -1f,
            bool useLayoutGroups = true,
            bool enableCache = true,
            int maxCacheSize = 1000,
            bool forceRebuild = false)
            : base(prefab, countGetter, new Vector2(fixedWidth, fixedHeight), 
                   new Vector2(minWidth, minHeight), new Vector2(maxWidth, maxHeight), useLayoutGroups, forceRebuild)
        {
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
            _countGetter = countGetter ?? throw new ArgumentNullException(nameof(countGetter));
            _dataGetter = dataGetter ?? throw new ArgumentNullException(nameof(dataGetter));
            _templateBinder = templateBinder;
            _enableCache = enableCache;
            _maxCacheSize = maxCacheSize;
        }

        /// <summary>
        /// 基于数据的构造函数 - 支持固定和自适应尺寸参数
        /// </summary>
        public StandardVariableSizeAdapter(
            RectTransform prefab,
            IReadOnlyList<object> dataList,
            ICellBinder binder,
            Action<RectTransform, object> templateBinder,
            float fixedWidth = -1f,
            float fixedHeight = -1f,
            float minWidth = 0f,
            float minHeight = 0f,
            float maxWidth = -1f,
            float maxHeight = -1f,
            bool useLayoutGroups = true,
            bool enableCache = true,
            int maxCacheSize = 1000,
            bool forceRebuild = false)
            : this(prefab, () => dataList.Count, index => dataList[index], binder, templateBinder, fixedWidth, fixedHeight, minWidth, minHeight, maxWidth, maxHeight, useLayoutGroups, enableCache, maxCacheSize, forceRebuild)
        {
        }

        /// <summary>
        /// 兼容构造函数 - 使用外部尺寸提供器
        /// </summary>
        public StandardVariableSizeAdapter(RectTransform prefab, Func<int> countGetter, ICellBinder binder, IVariableSizeAdapter sizeProvider)
            : base(prefab, countGetter)
        {
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
            _countGetter = countGetter ?? throw new ArgumentNullException(nameof(countGetter));
            _externalSizeProvider = sizeProvider ?? throw new ArgumentNullException(nameof(sizeProvider));
        }

        /// <summary>
        /// 兼容构造函数 - 静态数量，使用外部尺寸提供器
        /// </summary>
        public StandardVariableSizeAdapter(RectTransform prefab, int staticCount, ICellBinder binder, IVariableSizeAdapter sizeProvider)
            : this(prefab, () => staticCount, binder, sizeProvider)
        {
        }

        /// <summary>
        /// 纵向布局专用构造函数 - 固定宽度，自适应高度
        /// </summary>
        public static StandardVariableSizeAdapter CreateForVertical(
            RectTransform prefab,
            Func<int> countGetter,
            Func<int, object> dataGetter,
            ICellBinder binder,
            Action<RectTransform, object> templateBinder,
            float fixedWidth,
            float minHeight = 60f,
            float maxHeight = 300f,
            bool enableCache = true,
            int maxCacheSize = 1000,
            bool forceRebuild = false)
        {
            return new StandardVariableSizeAdapter(
                prefab, countGetter, dataGetter, binder, templateBinder,
                fixedWidth, -1f, 0, minHeight, -1f, maxHeight,
                true, enableCache, maxCacheSize, forceRebuild);
        }

        /// <summary>
        /// 横向布局专用构造函数 - 固定高度，自适应宽度
        /// </summary>
        public static StandardVariableSizeAdapter CreateForHorizontal(
            RectTransform prefab,
            Func<int> countGetter,
            Func<int, object> dataGetter,
            ICellBinder binder,
            Action<RectTransform, object> templateBinder,
            float fixedHeight,
            float minWidth = 60f,
            float maxWidth = 300f,
            bool enableCache = true,
            int maxCacheSize = 1000,
            bool forceRebuild = false)
        {
            return new StandardVariableSizeAdapter(
                prefab, countGetter, dataGetter, binder, templateBinder,
                -1f, fixedHeight, minWidth, 0, maxWidth, -1f,
                true, enableCache, maxCacheSize, forceRebuild);
        }

        /// <summary>
        /// 网格布局专用构造函数 - 固定宽高
        /// </summary>
        public static StandardVariableSizeAdapter CreateForGrid(
            RectTransform prefab,
            Func<int> countGetter,
            Func<int, object> dataGetter,
            ICellBinder binder,
            Action<RectTransform, object> templateBinder,
            float fixedWidth,
            float fixedHeight,
            bool enableCache = true,
            int maxCacheSize = 1000,
            bool forceRebuild = false)
        {
            return new StandardVariableSizeAdapter(
                prefab, countGetter, dataGetter, binder, templateBinder,
                fixedWidth, fixedHeight, 0, 0, -1f, -1f,
                true, enableCache, maxCacheSize, forceRebuild);
        }
        #endregion

        #region 私有字段
        private readonly IVariableSizeAdapter _externalSizeProvider;
        private bool UseExternalSizeProvider => _externalSizeProvider != null;
        #endregion

        #region BaseVariableSizeAdapter 抽象方法实现
        protected override int GetItemCount()
        {
            return Mathf.Max(0, _countGetter?.Invoke() ?? 0);
        }

        protected override object GetDataForLayout(int index)
        {
            return _dataGetter?.Invoke(index);
        }

        protected override void OnBind(int index, RectTransform cell)
        {
            _binder.OnBind(index, cell);
        }

        protected override void OnCreated(RectTransform cell)
        {
            _binder.OnCreated(cell);
        }

        protected override void OnRecycled(int index, RectTransform cell)
        {
            _binder.OnRecycled(index, cell);
        }
        #endregion

        #region 尺寸计算相关
        public override Vector2 GetItemSize(int index, Vector2 viewportSize, IScrollLayout layout)
        {
            // 如果使用外部尺寸提供器，委托给它
            if (UseExternalSizeProvider)
            {
                return _externalSizeProvider.GetItemSize(index, viewportSize, layout);
            }

            // 缓存命中
            if (_enableCache && _sizeCache.TryGetValue(index, out var cached))
                return cached;

            var result = base.GetItemSize(index, viewportSize, layout);

            if (_enableCache && result != Vector2.zero)
            {
                CacheSize(index, result);
            }

            return result;
        }

        /// <summary>
        /// 重写布局尺寸计算
        /// </summary>
        protected override Vector2 GetItemSizeInternal(int index, Vector2 viewportSize, IScrollLayout layout)
        {
            // 使用模板测量
            return MeasureWithTemplate(index, viewportSize, layout);
        }

        /// <summary>
        /// 使用模板进行测量
        /// 根据布局方向和固定尺寸参数智能计算单元格尺寸
        /// </summary>
        private Vector2 MeasureWithTemplate(int index, Vector2 viewportSize, IScrollLayout layout)
        {
            if (_template == null)
                return _fixedSize;

            var data = GetDataForLayout(index);
            if (data == null)
                return _fixedSize;

            try
            {
                // 保存模板原始状态
                var originalSize = _template.sizeDelta;
                var originalAnchoredPosition = _template.anchoredPosition;

                // 绑定数据到模板
                if (_templateBinder != null)
                {
                    _templateBinder.Invoke(_template, data);
                }

                // 强制重建布局
                LayoutRebuilder.ForceRebuildLayoutImmediate(_template);

                // 获取布局后的尺寸
                var layoutSize = _template.rect.size;

                // 恢复模板原始状态
                _template.sizeDelta = originalSize;
                _template.anchoredPosition = originalAnchoredPosition;

                // 根据布局方向和固定尺寸参数计算最终尺寸
                return CalculateFinalSize(layoutSize, layout, viewportSize);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StandardVariableSizeAdapter] 布局测量失败: {e.Message}");
                return _fixedSize;
            }
        }

        /// <summary>
        /// 根据布局方向和固定尺寸参数计算最终尺寸
        /// </summary>
        private Vector2 CalculateFinalSize(Vector2 layoutSize, IScrollLayout layout, Vector2 viewportSize)
        {
            float finalWidth = _fixedSize.x > 0 ? _fixedSize.x : layoutSize.x;
            float finalHeight = _fixedSize.y > 0 ? _fixedSize.y : layoutSize.y;

            // 应用最小/最大尺寸限制
            finalWidth = Mathf.Clamp(finalWidth, _minSize.x, _maxSize.x > 0 ? _maxSize.x : float.MaxValue);
            finalHeight = Mathf.Clamp(finalHeight, _minSize.y, _maxSize.y > 0 ? _maxSize.y : float.MaxValue);

            // 根据布局模式进行优化调整
            if (layout.IsVertical)
            {
                // 纵向布局：通常宽度固定，高度自适应
                // 如果没有设置固定宽度，但布局控制子对象宽度，则使用视口宽度
                if (_fixedSize.x <= 0 && layout.ControlChildWidth)
                {
                    finalWidth = layout.ConstraintCount > 1 ? 
                        (viewportSize.x - layout.Padding.left - layout.Padding.right - (layout.ConstraintCount - 1) * layout.Spacing.x) / layout.ConstraintCount :
                        viewportSize.x - layout.Padding.left - layout.Padding.right;
                }
            }
            else
            {
                // 横向布局：通常高度固定，宽度自适应
                // 如果没有设置固定高度，但布局控制子对象高度，则使用视口高度
                if (_fixedSize.y <= 0 && layout.ControlChildHeight)
                {
                    finalHeight = layout.ConstraintCount > 1 ?
                        (viewportSize.y - layout.Padding.top - layout.Padding.bottom - (layout.ConstraintCount - 1) * layout.Spacing.y) / layout.ConstraintCount :
                        viewportSize.y - layout.Padding.top - layout.Padding.bottom;
                }
            }

            return new Vector2(finalWidth, finalHeight);
        }

        /// <summary>
        /// 缓存尺寸
        /// </summary>
        private void CacheSize(int index, Vector2 size)
        {
            if (!_enableCache) return;

            // 清理过期的缓存
            if (_sizeCache.Count >= _maxCacheSize)
            {
                var keysToRemove = new List<int>();
                foreach (var kvp in _sizeCache)
                {
                    if (kvp.Key < index - _maxCacheSize / 2)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                foreach (var key in keysToRemove)
                {
                    _sizeCache.Remove(key);
                }
            }

            _sizeCache[index] = size;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 清理尺寸缓存
        /// </summary>
        public void ClearSizeCache()
        {
            _sizeCache.Clear();
            base.ClearCache();
        }

        /// <summary>
        /// 预热缓存
        /// </summary>
        public void PreheatCache(IScrollLayout layout, Vector2 viewportSize, int startIndex, int count)
        {
            if (!_enableCache) return;

            for (int i = startIndex; i < startIndex + count && i < GetItemCount(); i++)
            {
                GetItemSize(i, viewportSize, layout);
            }
        }
        #endregion
    }
}
