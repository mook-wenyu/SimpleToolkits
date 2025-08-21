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
    /// - 适用于大多数动态尺寸列表场景
    /// </summary>
    public sealed class StandardVariableSizeAdapter : BaseVariableSizeAdapter
    {
        #region 字段和属性
        private readonly ICellBinder _binder;
        private readonly Func<int> _countGetter;
        private readonly Func<int, object> _dataGetter;
        private readonly Action<RectTransform, object> _templateBinder;
        private readonly Func<int, object, Vector2> _customSizeCalculator;
        private readonly Dictionary<int, Vector2> _sizeCache = new Dictionary<int, Vector2>();
        private readonly bool _enableCache;
        private readonly int _maxCacheSize;
        #endregion

        #region 构造函数
        /// <summary>
        /// 完整构造函数 - 支持自动尺寸计算
        /// </summary>
        /// <param name="prefab">预制体RectTransform</param>
        /// <param name="template">模板RectTransform（用于尺寸计算）</param>
        /// <param name="countGetter">数据数量获取器</param>
        /// <param name="dataGetter">数据获取器</param>
        /// <param name="binder">业务绑定器</param>
        /// <param name="templateBinder">模板绑定委托（尺寸测量前调用）</param>
        /// <param name="fixedSize">固定尺寸</param>
        /// <param name="minSize">最小尺寸</param>
        /// <param name="maxSize">最大尺寸</param>
        /// <param name="useLayoutGroups">是否使用布局组件</param>
        /// <param name="enableCache">是否启用尺寸缓存</param>
        /// <param name="maxCacheSize">最大缓存数量</param>
        /// <param name="customSizeCalculator">自定义尺寸计算器</param>
        /// <param name="forceRebuild">是否强制重建布局</param>
        public StandardVariableSizeAdapter(
            RectTransform prefab,
            RectTransform template,
            Func<int> countGetter,
            Func<int, object> dataGetter,
            ICellBinder binder,
            Action<RectTransform, object> templateBinder,
            Vector2 fixedSize,
            Vector2 minSize,
            Vector2 maxSize,
            bool useLayoutGroups = true,
            bool enableCache = true,
            int maxCacheSize = 1000,
            Func<int, object, Vector2> customSizeCalculator = null,
            bool forceRebuild = false)
            : base(prefab, template, countGetter, fixedSize, minSize, maxSize, useLayoutGroups, forceRebuild)
        {
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
            _countGetter = countGetter ?? throw new ArgumentNullException(nameof(countGetter));
            _dataGetter = dataGetter ?? throw new ArgumentNullException(nameof(dataGetter));
            _templateBinder = templateBinder;
            _customSizeCalculator = customSizeCalculator;
            _enableCache = enableCache;
            _maxCacheSize = maxCacheSize;
        }

        /// <summary>
        /// 简化构造函数 - 使用预制体作为模板
        /// </summary>
        public StandardVariableSizeAdapter(
            RectTransform prefab,
            Func<int> countGetter,
            Func<int, object> dataGetter,
            ICellBinder binder,
            Action<RectTransform, object> templateBinder,
            Vector2 fixedSize,
            Vector2 minSize,
            Vector2 maxSize,
            bool useLayoutGroups = true,
            bool enableCache = true,
            int maxCacheSize = 1000,
            Func<int, object, Vector2> customSizeCalculator = null,
            bool forceRebuild = false)
            : this(prefab, prefab, countGetter, dataGetter, binder, templateBinder, fixedSize, minSize, maxSize, useLayoutGroups, enableCache, maxCacheSize, customSizeCalculator, forceRebuild)
        {
        }

        /// <summary>
        /// 简化构造函数 - 固定宽度，自适应高度
        /// </summary>
        public StandardVariableSizeAdapter(
            RectTransform prefab,
            Func<int> countGetter,
            Func<int, object> dataGetter,
            ICellBinder binder,
            Action<RectTransform, object> templateBinder,
            float fixedWidth,
            float minHeight = 60f,
            float maxHeight = 300f,
            bool useLayoutGroups = true,
            bool enableCache = true,
            int maxCacheSize = 1000,
            Func<int, object, Vector2> customSizeCalculator = null,
            bool forceRebuild = false)
            : this(prefab, prefab, countGetter, dataGetter, binder, templateBinder, new Vector2(fixedWidth, -1f), new Vector2(fixedWidth, minHeight), new Vector2(fixedWidth, maxHeight), useLayoutGroups, enableCache, maxCacheSize, customSizeCalculator, forceRebuild)
        {
        }

        /// <summary>
        /// 基于数据的构造函数
        /// </summary>
        public StandardVariableSizeAdapter(
            RectTransform prefab,
            IReadOnlyList<object> dataList,
            ICellBinder binder,
            Action<RectTransform, object> templateBinder,
            float fixedWidth,
            float minHeight = 60f,
            float maxHeight = 300f,
            bool useLayoutGroups = true,
            bool enableCache = true,
            int maxCacheSize = 1000,
            Func<int, object, Vector2> customSizeCalculator = null,
            bool forceRebuild = false)
            : this(prefab, prefab, () => dataList.Count, index => dataList[index], binder, templateBinder, new Vector2(fixedWidth, -1f), new Vector2(fixedWidth, minHeight), new Vector2(fixedWidth, maxHeight), useLayoutGroups, enableCache, maxCacheSize, customSizeCalculator, forceRebuild)
        {
        }

        /// <summary>
        /// 兼容构造函数 - 使用外部尺寸提供器（保持向后兼容）
        /// </summary>
        public StandardVariableSizeAdapter(RectTransform prefab, Func<int> countGetter, ICellBinder binder, IVariableSizeAdapter sizeProvider)
            : base(prefab, prefab, countGetter)
        {
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
            _countGetter = countGetter ?? throw new ArgumentNullException(nameof(countGetter));
            _externalSizeProvider = sizeProvider ?? throw new ArgumentNullException(nameof(sizeProvider));
        }

        /// <summary>
        /// 兼容构造函数 - 静态数量，使用外部尺寸提供器
        /// </summary>
        public StandardVariableSizeAdapter(RectTransform prefab, int staticCount, ICellBinder binder, IVariableSizeAdapter sizeProvider)
            : base(prefab, prefab, () => staticCount)
        {
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
            _countGetter = () => staticCount;
            _externalSizeProvider = sizeProvider ?? throw new ArgumentNullException(nameof(sizeProvider));
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

        protected override Vector2 GetBaseSize(int index, Vector2 viewportSize, IScrollLayout layout)
        {
            // 如果有自定义计算器，优先使用
            if (_customSizeCalculator != null)
            {
                var data = _dataGetter?.Invoke(index);
                var customSize = _customSizeCalculator.Invoke(index, data);
                if (customSize != Vector2.zero)
                    return customSize;
            }

            // 否则返回固定尺寸
            return _fixedSize;
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

            // 否则使用内置的尺寸计算逻辑
            if (index < 0 || index >= GetItemCount())
            {
                return GetFallbackSize(layout);
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
        /// 重写布局尺寸计算，兼容Unity可视化配置的LayoutGroup
        /// </summary>
        protected override Vector2 GetLayoutSize(int index, Vector2 viewportSize, IScrollLayout layout)
        {
            if (UseExternalSizeProvider)
            {
                return _externalSizeProvider.GetItemSize(index, viewportSize, layout);
            }

            // 缓存命中
            if (_enableCache && _sizeCache.TryGetValue(index, out var cached))
                return cached;

            // 模板不可用直接返回 0，交由外层合并与夹取
            if (_template == null)
                return Vector2.zero;

            // 取数据
            var data = GetDataForLayout(index);

            try
            {
                // 保存模板原始状态
                var originalSize = _template.sizeDelta;
                var originalAnchoredPosition = _template.anchoredPosition;
                
                // 检查是否有VerticalLayoutGroup
                var verticalLayoutGroup = _template.GetComponent<VerticalLayoutGroup>();
                var hasLayoutGroup = verticalLayoutGroup != null;

                Vector2 result;
                if (hasLayoutGroup)
                {
                    // 使用兼容Unity LayoutGroup的测量方式
                    result = MeasureWithUnityLayoutGroup(index, viewportSize, layout, data, verticalLayoutGroup);
                }
                else
                {
                    // 使用原有的RT驱动测量方式
                    result = MeasureWithRectTransform(index, viewportSize, layout, data);
                }

                // 恢复模板原始状态
                _template.sizeDelta = originalSize;
                _template.anchoredPosition = originalAnchoredPosition;

                if (_enableCache && result != Vector2.zero)
                {
                    CacheSize(index, result);
                }
                return result;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StandardVariableSizeAdapter] 布局测量失败: {e.Message}");
                return Vector2.zero;
            }
        }

        /// <summary>
        /// 使用Unity LayoutGroup进行测量
        /// </summary>
        private Vector2 MeasureWithUnityLayoutGroup(int index, Vector2 viewportSize, IScrollLayout layout, object data, VerticalLayoutGroup layoutGroup)
        {
            if (_templateBinder != null)
            {
                _templateBinder.Invoke(_template, data);
            }

            // 强制重建布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(_template);

            // 获取布局后的尺寸
            var layoutSize = _template.rect.size;

            // 根据布局方向返回相应的尺寸
            return layout.IsVertical ? new Vector2(_fixedSize.x > 0 ? _fixedSize.x : layoutSize.x, layoutSize.y) 
                                     : new Vector2(layoutSize.x, _fixedSize.y > 0 ? _fixedSize.y : layoutSize.y);
        }

        /// <summary>
        /// 使用RectTransform进行测量
        /// </summary>
        private Vector2 MeasureWithRectTransform(int index, Vector2 viewportSize, IScrollLayout layout, object data)
        {
            if (_templateBinder != null)
            {
                _templateBinder.Invoke(_template, data);
            }

            // 强制重建布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(_template);

            // 获取最终尺寸
            var finalSize = _template.rect.size;

            // 应用尺寸限制
            return ClampSize(finalSize, layout);
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

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public (int cacheCount, int maxCacheSize, double cacheUsage) GetCacheStats()
        {
            return (_sizeCache.Count, _maxCacheSize, (double)_sizeCache.Count / _maxCacheSize);
        }
        #endregion
    }
}
