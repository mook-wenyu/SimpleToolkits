using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleToolkits
{
    /// <summary>
    /// 生产可用的基础统一尺寸适配器基类（非示例代码）。
    /// - 通过继承并实现抽象方法，快速对接 ScrollView
    /// - 支持运行期覆盖数量、可选的回收/创建生命周期回调
    /// - 仅依赖 IScrollAdapter/IRecyclableScrollAdapter 接口
    /// </summary>
    public abstract class BaseScrollAdapter : IScrollAdapter, IRecyclableScrollAdapter
    {
        // 预制体（必须提供）
        private readonly RectTransform _prefab;
        // 数量获取委托（若未提供则使用 GetCount()）
        private readonly Func<int> _countGetter;
        // 运行期覆盖数量（<0 表示未覆盖）
        private int _override = -1;

        protected BaseScrollAdapter(RectTransform prefab, Func<int> countGetter = null)
        {
            _prefab = prefab;
            _countGetter = countGetter;
        }

        /// <summary>必须实现：绑定索引到 Cell。</summary>
        protected abstract void OnBind(int index, RectTransform cell);
        /// <summary>可选：Cell 首次实例化时。</summary>
        protected virtual void OnCreated(RectTransform cell) { }
        /// <summary>可选：Cell 回收时（可清理监听/动画）。</summary>
        protected virtual void OnRecycled(int index, RectTransform cell) { }
        /// <summary>可重写：当未提供 countGetter 时，从派生类返回数量。</summary>
        protected virtual int GetCount() => 0;

        //================ IScrollAdapter ================
        public int Count => _override >= 0 ? _override : (_countGetter != null ? _countGetter() : Mathf.Max(0, GetCount()));
        public void OverrideCount(int count) { _override = count; }
        public RectTransform GetCellPrefab() => _prefab;
        public void BindCell(int index, RectTransform cell) => OnBind(index, cell);

        //================ IRecyclableScrollAdapter ================
        public void OnCellCreated(RectTransform cell) => OnCreated(cell);
        public void OnCellRecycled(int index, RectTransform cell) => OnRecycled(index, cell);
    }

    /// <summary>
    /// 增强的基础变尺寸适配器基类（合并了原AutoSizeProvider功能）。
    /// - 仅在单列/单行布局下使用（ConstraintCount==1）
    /// - 支持基于Unity布局组件的自动尺寸计算
    /// - 提供缓存机制和性能优化
    /// - 实现 GetItemSize 以返回每项尺寸（sizeDelta 语义）
    /// </summary>
    public abstract class BaseVariableSizeAdapter : BaseScrollAdapter, IVariableSizeAdapter
    {
        #region 字段和属性
        protected RectTransform _template;
        protected readonly Vector2 _fixedSize;
        protected readonly Vector2 _minSize;
        protected readonly Vector2 _maxSize;
        protected readonly bool _useLayoutGroups;
        protected readonly bool _forceRebuild;

        // 缓存字段，避免重复获取组件
        protected LayoutElement _layoutElementCache;
        protected ContentSizeFitter _contentSizeFitterCache;
        protected HorizontalOrVerticalLayoutGroup _layoutGroupCache;
        
        // 性能优化：避免频繁重建布局
        private int _lastRebuildFrame = -1;
        private Vector2 _lastTemplateSize;
        private bool _isInitialized = false;
        #endregion

        #region 构造函数
        /// <summary>
        /// 完整构造函数
        /// </summary>
        /// <param name="prefab">预制体RectTransform</param>
        /// <param name="template">模板RectTransform（用于尺寸计算）</param>
        /// <param name="countGetter">数据数量获取器</param>
        /// <param name="fixedSize">固定尺寸（主轴尺寸由布局控制时设置为-1）</param>
        /// <param name="minSize">最小尺寸</param>
        /// <param name="maxSize">最大尺寸</param>
        /// <param name="useLayoutGroups">是否使用布局组件计算尺寸</param>
        /// <param name="forceRebuild">是否强制重建布局</param>
        protected BaseVariableSizeAdapter(
            RectTransform prefab, 
            RectTransform template, 
            Func<int> countGetter = null,
            Vector2 fixedSize = default,
            Vector2 minSize = default,
            Vector2 maxSize = default,
            bool useLayoutGroups = true,
            bool forceRebuild = false)
            : base(prefab, countGetter)
        {
            _template = template ?? throw new ArgumentNullException(nameof(template));
            _fixedSize = fixedSize;
            _minSize = minSize;
            _maxSize = maxSize;
            _useLayoutGroups = useLayoutGroups;
            _forceRebuild = forceRebuild;
            
            InitializeCache();
        }

        /// <summary>
        /// 简化构造函数 - 使用预制体作为模板
        /// </summary>
        protected BaseVariableSizeAdapter(
            RectTransform prefab, 
            Func<int> countGetter = null,
            Vector2 fixedSize = default,
            Vector2 minSize = default,
            Vector2 maxSize = default,
            bool useLayoutGroups = true,
            bool forceRebuild = false)
            : this(prefab, prefab, countGetter, fixedSize, minSize, maxSize, useLayoutGroups, forceRebuild)
        {
        }

        /// <summary>
        /// 简化构造函数 - 使用固定宽度和自适应高度
        /// </summary>
        protected BaseVariableSizeAdapter(
            RectTransform prefab, 
            float fixedWidth, 
            float minHeight = 60f, 
            float maxHeight = 300f,
            bool useLayoutGroups = true,
            bool forceRebuild = false)
            : this(prefab, prefab, null, new Vector2(fixedWidth, -1f), new Vector2(fixedWidth, minHeight), new Vector2(fixedWidth, maxHeight), useLayoutGroups, forceRebuild)
        {
        }
        #endregion

        #region IVariableSizeAdapter 接口实现
        /// <summary>
        /// 获取指定索引项的尺寸
        /// </summary>
        public virtual Vector2 GetItemSize(int index, Vector2 viewportSize, IScrollLayout layout)
        {
            if (index < 0 || index >= GetItemCount())
            {
                return GetFallbackSize(layout);
            }

            var baseSize = GetBaseSize(index, viewportSize, layout);
            var layoutSize = _useLayoutGroups ? GetLayoutSize(index, viewportSize, layout) : Vector2.zero;
            
            // 合并基础尺寸和布局尺寸
            var finalSize = MergeSizes(baseSize, layoutSize, layout);
            
            // 应用尺寸限制
            return ClampSize(finalSize, layout);
        }
        #endregion

        #region 抽象方法 - 子类必须实现
        /// <summary>
        /// 获取数据项总数
        /// </summary>
        protected abstract int GetItemCount();

        /// <summary>
        /// 获取基础尺寸（不包含布局计算的尺寸）
        /// </summary>
        protected abstract Vector2 GetBaseSize(int index, Vector2 viewportSize, IScrollLayout layout);

        /// <summary>
        /// 获取索引对应的数据，用于布局计算
        /// </summary>
        protected abstract object GetDataForLayout(int index);
        #endregion

        #region 虚方法 - 子类可重写
        /// <summary>
        /// 获取后备尺寸（当索引无效时使用）
        /// </summary>
        protected virtual Vector2 GetFallbackSize(IScrollLayout layout)
        {
            var size = _fixedSize;
            if (layout.IsVertical && size.y < 0)
                size.y = _minSize.y;
            else if (!layout.IsVertical && size.x < 0)
                size.x = _minSize.x;
            
            return size;
        }

        /// <summary>
        /// 合并基础尺寸和布局尺寸
        /// </summary>
        protected virtual Vector2 MergeSizes(Vector2 baseSize, Vector2 layoutSize, IScrollLayout layout)
        {
            var result = baseSize;
            
            if (layout.IsVertical)
            {
                // 纵向布局：使用基础宽度，高度使用布局计算或基础值
                if (layoutSize.y > 0)
                    result.y = layoutSize.y;
                else if (result.y < 0)
                    result.y = _minSize.y;
            }
            else
            {
                // 横向布局：使用基础高度，宽度使用布局计算或基础值
                if (layoutSize.x > 0)
                    result.x = layoutSize.x;
                else if (result.x < 0)
                    result.x = _minSize.x;
            }
            
            return result;
        }

        /// <summary>
        /// 应用尺寸限制
        /// </summary>
        protected virtual Vector2 ClampSize(Vector2 size, IScrollLayout layout)
        {
            if (layout.IsVertical)
            {
                size.x = _fixedSize.x > 0 ? _fixedSize.x : size.x;
                size.y = Mathf.Clamp(size.y, _minSize.y, _maxSize.y);
            }
            else
            {
                size.x = Mathf.Clamp(size.x, _minSize.x, _maxSize.x);
                size.y = _fixedSize.y > 0 ? _fixedSize.y : size.y;
            }
            
            return size;
        }

        /// <summary>
        /// 获取布局计算的尺寸
        /// </summary>
        protected virtual Vector2 GetLayoutSize(int index, Vector2 viewportSize, IScrollLayout layout)
        {
            if (!_useLayoutGroups || _template == null)
                return Vector2.zero;

            try
            {
                // 性能优化：避免每帧都重建布局
                if (ShouldRebuildLayout())
                {
                    RebuildLayout();
                }

                return CalculateLayoutSize(index, viewportSize, layout);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BaseVariableSizeAdapter] 布局计算失败: {e.Message}");
                return Vector2.zero;
            }
        }

        /// <summary>
        /// 计算布局尺寸
        /// </summary>
        protected virtual Vector2 CalculateLayoutSize(int index, Vector2 viewportSize, IScrollLayout layout)
        {
            var data = GetDataForLayout(index);
            if (data == null)
                return Vector2.zero;

            // 如果有LayoutElement，优先使用其配置的尺寸
            if (_layoutElementCache != null)
            {
                var layoutSize = GetLayoutElementSize(layout);
                if (layoutSize != Vector2.zero)
                    return layoutSize;
            }

            // 如果有ContentSizeFitter，使用其首选尺寸
            if (_contentSizeFitterCache != null)
            {
                var preferredSize = GetPreferredSize();
                if (preferredSize != Vector2.zero)
                    return preferredSize;
            }

            // 使用模板的当前尺寸
            return _template.rect.size;
        }
        #endregion

        #region 私有方法
        private void InitializeCache()
        {
            if (_template == null) return;

            _layoutElementCache = _template.GetComponent<LayoutElement>();
            _contentSizeFitterCache = _template.GetComponent<ContentSizeFitter>();
            _layoutGroupCache = _template.GetComponent<HorizontalOrVerticalLayoutGroup>();

            _isInitialized = true;
        }

        private bool ShouldRebuildLayout()
        {
            if (!_forceRebuild) return false;
            
            var currentFrame = Time.frameCount;
            if (currentFrame == _lastRebuildFrame) return false;
            
            _lastRebuildFrame = currentFrame;
            return true;
        }

        private void RebuildLayout()
        {
            if (_template == null) return;

            try
            {
                // 强制重建布局
                LayoutRebuilder.ForceRebuildLayoutImmediate(_template);
                
                // 缓存模板尺寸
                _lastTemplateSize = _template.rect.size;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BaseVariableSizeAdapter] 布局重建失败: {e.Message}");
            }
        }

        private Vector2 GetLayoutElementSize(IScrollLayout layout)
        {
            if (_layoutElementCache == null) return Vector2.zero;

            var size = Vector2.zero;
            
            if (layout.IsVertical)
            {
                // 纵向布局：高度优先
                if (_layoutElementCache.minHeight > 0)
                    size.y = _layoutElementCache.minHeight;
                if (_layoutElementCache.preferredHeight > 0)
                    size.y = _layoutElementCache.preferredHeight;
            }
            else
            {
                // 横向布局：宽度优先
                if (_layoutElementCache.minWidth > 0)
                    size.x = _layoutElementCache.minWidth;
                if (_layoutElementCache.preferredWidth > 0)
                    size.x = _layoutElementCache.preferredWidth;
            }

            return size;
        }

        private Vector2 GetPreferredSize()
        {
            if (_contentSizeFitterCache == null) return Vector2.zero;

            // 获取Content Size Fitter的首选尺寸
            var size = Vector2.zero;
            
            // 这里需要根据实际的布局情况计算首选尺寸
            // 由于Unity的限制，我们只能估算
            if (_template != null)
            {
                size = _template.rect.size;
            }

            return size;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 强制重建布局
        /// </summary>
        public void ForceRebuildLayout()
        {
            _lastRebuildFrame = -1; // 重置帧缓存
            RebuildLayout();
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public void ClearCache()
        {
            _lastRebuildFrame = -1;
            _lastTemplateSize = Vector2.zero;
            _isInitialized = false;
        }

        /// <summary>
        /// 更新模板引用
        /// </summary>
        public void UpdateTemplate(RectTransform newTemplate)
        {
            if (newTemplate == null) return;
            
            _template = newTemplate;
            ClearCache();
            InitializeCache();
        }
        #endregion
    }
}
