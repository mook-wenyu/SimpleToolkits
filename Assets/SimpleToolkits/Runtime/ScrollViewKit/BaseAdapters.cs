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
        private bool _isInitialized = false;
        #endregion

        #region 构造函数
        /// <summary>
        /// 基础构造函数
        /// </summary>
        protected BaseVariableSizeAdapter(
            RectTransform prefab, 
            Func<int> countGetter = null,
            Vector2 fixedSize = default,
            Vector2 minSize = default,
            Vector2 maxSize = default,
            bool useLayoutGroups = true,
            bool forceRebuild = false)
            : base(prefab, countGetter)
        {
            _template = prefab;
            _fixedSize = fixedSize;
            _minSize = minSize;
            _maxSize = maxSize;
            _useLayoutGroups = useLayoutGroups;
            _forceRebuild = forceRebuild;
            
            InitializeCache();
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

            var size = GetItemSizeInternal(index, viewportSize, layout);
            return ClampSize(size, layout);
        }
        #endregion

        #region 抽象方法 - 子类必须实现
        /// <summary>
        /// 获取数据项总数
        /// </summary>
        protected abstract int GetItemCount();

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
        /// 获取项目尺寸（内部实现）
        /// </summary>
        protected virtual Vector2 GetItemSizeInternal(int index, Vector2 viewportSize, IScrollLayout layout)
        {
            if (!_useLayoutGroups || _template == null)
                return _fixedSize;

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
                return _fixedSize;
            }
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
        /// 计算布局尺寸
        /// 根据布局方向和固定尺寸参数智能计算单元格尺寸
        /// </summary>
        protected virtual Vector2 CalculateLayoutSize(int index, Vector2 viewportSize, IScrollLayout layout)
        {
            var data = GetDataForLayout(index);
            if (data == null)
                return _fixedSize;
            
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
                    return OptimizeSizeForLayout(preferredSize, layout, viewportSize);
            }
            
            // 使用模板的当前尺寸并进行布局优化
            var templateSize = _template.rect.size;
            return OptimizeSizeForLayout(templateSize, layout, viewportSize);
        }

        /// <summary>
        /// 根据布局模式优化尺寸
        /// </summary>
        protected virtual Vector2 OptimizeSizeForLayout(Vector2 size, IScrollLayout layout, Vector2 viewportSize)
        {
            float finalWidth = _fixedSize.x > 0 ? _fixedSize.x : size.x;
            float finalHeight = _fixedSize.y > 0 ? _fixedSize.y : size.y;
            
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
