using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleToolkits
{
    /// <summary>
    /// 布局自动尺寸提供器 - 具体实现
    /// 支持基于Unity布局组件的自动尺寸计算，适用于大多数场景
    /// </summary>
    public class LayoutAutoSizeProvider : BaseVariableSizeAdapter
    {
        #region 字段和属性
        private Func<int> _countGetter;
        private Func<int, object> _dataGetter;
        private Func<int, object, Vector2> _customSizeCalculator;
        // 将数据应用到模板以进行测量的委托（派生类可设置）
        private readonly Action<RectTransform, object> _applyDataToTemplate;
        private readonly Dictionary<int, Vector2> _sizeCache = new Dictionary<int, Vector2>();
        private readonly bool _enableCache;
        private readonly int _maxCacheSize;
        #endregion

        #region 构造函数
        /// <summary>
        /// 完整构造函数
        /// </summary>
        /// <param name="template">模板RectTransform</param>
        /// <param name="countGetter">数据数量获取器</param>
        /// <param name="dataGetter">数据获取器</param>
        /// <param name="templateBinder">模板绑定委托（尺寸测量前调用）</param>
        /// <param name="fixedSize">固定尺寸</param>
        /// <param name="minSize">最小尺寸</param>
        /// <param name="maxSize">最大尺寸</param>
        /// <param name="useLayoutGroups">是否使用布局组件</param>
        /// <param name="enableCache">是否启用尺寸缓存</param>
        /// <param name="maxCacheSize">最大缓存数量</param>
        /// <param name="customSizeCalculator">自定义尺寸计算器</param>
        /// <param name="forceRebuild">是否强制重建布局</param>
        public LayoutAutoSizeProvider(
            RectTransform template,
            Func<int> countGetter,
            Func<int, object> dataGetter,
            Action<RectTransform, object> templateBinder,
            Vector2 fixedSize,
            Vector2 minSize,
            Vector2 maxSize,
            bool useLayoutGroups = true,
            bool enableCache = true,
            int maxCacheSize = 1000,
            Func<int, object, Vector2> customSizeCalculator = null,
            bool forceRebuild = false)
            : base(template, template, countGetter, fixedSize, minSize, maxSize, useLayoutGroups, forceRebuild)
        {
            _countGetter = countGetter ?? throw new ArgumentNullException(nameof(countGetter));
            _dataGetter = dataGetter ?? throw new ArgumentNullException(nameof(dataGetter));
            _customSizeCalculator = customSizeCalculator;
            _enableCache = enableCache;
            _maxCacheSize = maxCacheSize;
            _applyDataToTemplate = templateBinder;
        }

        /// <summary>
        /// 简化构造函数 - 固定宽度，自适应高度
        /// </summary>
        public LayoutAutoSizeProvider(
            RectTransform template,
            Func<int> countGetter,
            Func<int, object> dataGetter,
            Action<RectTransform, object> templateBinder,
            float fixedWidth,
            float minHeight = 60f,
            float maxHeight = 300f,
            bool useLayoutGroups = true,
            bool enableCache = true,
            int maxCacheSize = 1000,
            Func<int, object, Vector2> customSizeCalculator = null,
            bool forceRebuild = false)
            : base(template, template, countGetter, new Vector2(fixedWidth, -1f), new Vector2(fixedWidth, minHeight), new Vector2(fixedWidth, maxHeight), useLayoutGroups, forceRebuild)
        {
            _countGetter = countGetter ?? throw new ArgumentNullException(nameof(countGetter));
            _dataGetter = dataGetter ?? throw new ArgumentNullException(nameof(dataGetter));
            _customSizeCalculator = customSizeCalculator;
            _enableCache = enableCache;
            _maxCacheSize = maxCacheSize;
            _applyDataToTemplate = templateBinder;
        }

        /// <summary>
        /// 基于数据的构造函数
        /// </summary>
        public LayoutAutoSizeProvider(
            RectTransform template,
            IReadOnlyList<object> dataList,
            Action<RectTransform, object> templateBinder,
            float fixedWidth,
            float minHeight = 60f,
            float maxHeight = 300f,
            bool useLayoutGroups = true,
            bool enableCache = true,
            int maxCacheSize = 1000,
            Func<int, object, Vector2> customSizeCalculator = null,
            bool forceRebuild = false)
            : base(template, fixedWidth, minHeight, maxHeight, useLayoutGroups, forceRebuild)
        {
            if (dataList == null) throw new ArgumentNullException(nameof(dataList));

            _countGetter = () => dataList.Count;
            _dataGetter = index => index >= 0 && index < dataList.Count ? dataList[index] : null;
            _customSizeCalculator = customSizeCalculator;
            _enableCache = enableCache;
            _maxCacheSize = maxCacheSize;
            _applyDataToTemplate = templateBinder;
        }
        #endregion

        #region 重写基类方法
        /// <summary>
        /// 获取数据项总数
        /// </summary>
        protected override int GetItemCount()
        {
            return _countGetter?.Invoke() ?? 0;
        }

        /// <summary>
        /// 获取基础尺寸
        /// </summary>
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

        /// <summary>
        /// 获取索引对应的数据
        /// </summary>
        protected override object GetDataForLayout(int index)
        {
            return _dataGetter?.Invoke(index);
        }

        /// <summary>
        /// 重写布局尺寸计算，兼容Unity可视化配置的LayoutGroup
        /// </summary>
        protected override Vector2 GetLayoutSize(int index, Vector2 viewportSize, IScrollLayout layout)
        {
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
                Debug.LogWarning($"[LayoutAutoSizeProvider] 布局测量失败: {e.Message}");
                return Vector2.zero;
            }
        }

        /// <summary>
        /// 使用Unity LayoutGroup进行尺寸测量
        /// </summary>
        private Vector2 MeasureWithUnityLayoutGroup(int index, Vector2 viewportSize, IScrollLayout layout, object data, VerticalLayoutGroup layoutGroup)
        {
            // 设置模板的可用宽度（保持Unity LayoutGroup的配置）
            if (layout.IsVertical)
            {
                var availWidth = Mathf.Max(0f, viewportSize.x - layout.Padding.left - layout.Padding.right);
                
                // 不直接修改模板尺寸，而是通过LayoutElement控制
                var layoutElement = _template.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = _template.gameObject.AddComponent<LayoutElement>();
                }
                
                // 设置最小和首选宽度，让LayoutGroup计算高度
                layoutElement.minWidth = _minSize.x;
                layoutElement.preferredWidth = _fixedSize.x > 0f ? _fixedSize.x : availWidth;
                layoutElement.minHeight = _minSize.y;
                layoutElement.preferredHeight = -1; // 让LayoutGroup自动计算高度
            }

            // 应用数据到模板
            _applyDataToTemplate?.Invoke(_template, data);

            // 强制重建布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(_template);

            // 获取LayoutUtility计算的首选尺寸
            var preferredW = LayoutUtility.GetPreferredWidth(_template);
            var preferredH = LayoutUtility.GetPreferredHeight(_template);

            // 如果LayoutUtility计算失败，使用LayoutUtils的辅助计算
            if (preferredH <= 0)
            {
                preferredH = CalculateLayoutGroupHeight(_template, layoutGroup);
            }

            // 依据方向处理尺寸
            Vector2 result;
            if (layout.IsVertical)
            {
                var width = _fixedSize.x > 0f ? _fixedSize.x : preferredW;
                width = Mathf.Clamp(width, _minSize.x, _maxSize.x);
                
                var height = Mathf.Clamp(preferredH, _minSize.y, _maxSize.y);
                result = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
            }
            else
            {
                var width = Mathf.Clamp(preferredW, _minSize.x, _maxSize.x);
                var height = _fixedSize.y > 0f ? _fixedSize.y : preferredH;
                height = Mathf.Clamp(height, _minSize.y, _maxSize.y);
                result = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
            }

            return result;
        }

        /// <summary>
        /// 使用RectTransform进行尺寸测量（原有方式）
        /// </summary>
        private Vector2 MeasureWithRectTransform(int index, Vector2 viewportSize, IScrollLayout layout, object data)
        {
            // 设置模板的可用宽/高（保证文本换行与布局正确）
            if (layout.IsVertical)
            {
                var avail = Mathf.Max(0f, viewportSize.x - layout.Padding.left - layout.Padding.right);
                // 设置可用宽度，高度使用minHeight作为基础值
                _template.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, avail);
                _template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _minSize.y);
            }
            else
            {
                var avail = Mathf.Max(0f, viewportSize.y - layout.Padding.top - layout.Padding.bottom);
                // 设置可用高度，宽度使用minWidth作为基础值
                _template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, avail);
                _template.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _minSize.x);
            }

            // 应用数据到模板
            _applyDataToTemplate?.Invoke(_template, data);

            // 强制重建布局，获取首选尺寸
            LayoutRebuilder.ForceRebuildLayoutImmediate(_template);

            // 使用 LayoutUtility 获取首选尺寸
            var preferredW = LayoutUtility.GetPreferredWidth(_template);
            var preferredH = LayoutUtility.GetPreferredHeight(_template);

            // 获取模板的实际rect尺寸作为备选
            var actualRect = _template.rect;
            var actualW = actualRect.width;
            var actualH = actualRect.height;

            // 依据方向分别处理主轴与交叉轴
            Vector2 result;
            if (layout.IsVertical)
            {
                // 纵向列表：主轴=高度。宽度若有固定值优先，否则取首选并夹取
                var width = _fixedSize.x > 0f ? _fixedSize.x : preferredW;
                width = Mathf.Clamp(width, _minSize.x, _maxSize.x);

                // 如果LayoutUtility计算的首选高度为0，则使用其他方法计算高度
                float calculatedHeight;
                if (preferredH > 0)
                {
                    calculatedHeight = Mathf.Clamp(preferredH, _minSize.y, _maxSize.y);
                }
                else
                {
                    // 尝试使用子元素的总高度
                    float totalChildHeight = 0f;
                    for (int i = 0; i < _template.childCount; i++)
                    {
                        var child = _template.GetChild(i);
                        var childRect = child.GetComponent<RectTransform>();
                        if (childRect != null)
                        {
                            totalChildHeight += childRect.rect.height;
                        }
                    }
                    
                    // 如果有子元素高度，使用子元素总高度，否则使用实际高度
                    if (totalChildHeight > 0)
                    {
                        calculatedHeight = Mathf.Clamp(totalChildHeight, _minSize.y, _maxSize.y);
                    }
                    else
                    {
                        calculatedHeight = Mathf.Clamp(actualH, _minSize.y, _maxSize.y);
                    }
                }
                
                var height = calculatedHeight;
                
                result = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
            }
            else
            {
                // 横向列表：主轴=宽度。高度若有固定值优先，否则取首选并夹取
                var width = Mathf.Clamp(preferredW, _minSize.x, _maxSize.x);
                var height = _fixedSize.y > 0f ? _fixedSize.y : preferredH;
                height = Mathf.Clamp(height, _minSize.y, _maxSize.y);
                result = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
            }

            return result;
        }

        /// <summary>
        /// 计算LayoutGroup的高度（当LayoutUtility失败时使用）
        /// </summary>
        private float CalculateLayoutGroupHeight(RectTransform template, VerticalLayoutGroup layoutGroup)
        {
            if (layoutGroup == null) return _minSize.y;

            float totalHeight = 0f;
            int activeChildCount = 0;

            // 计算所有活动子元素的总高度
            for (int i = 0; i < template.childCount; i++)
            {
                var child = template.GetChild(i);
                if (!child.gameObject.activeSelf) continue;

                var childRect = child.GetComponent<RectTransform>();
                if (childRect == null) continue;

                float childHeight = 0f;
                var layoutElement = child.GetComponent<LayoutElement>();

                if (layoutElement != null)
                {
                    // 使用LayoutElement的首选高度
                    childHeight = layoutElement.preferredHeight > 0 ? layoutElement.preferredHeight : layoutElement.minHeight;
                }
                else
                {
                    // 使用子元素的实际高度
                    childHeight = childRect.rect.height;
                }

                if (childHeight > 0)
                {
                    totalHeight += childHeight;
                    activeChildCount++;
                }
            }

            // 添加间距和内边距
            if (activeChildCount > 0)
            {
                totalHeight += layoutGroup.spacing * (activeChildCount - 1);
                totalHeight += layoutGroup.padding.top + layoutGroup.padding.bottom;
            }

            return Mathf.Max(totalHeight, _minSize.y);
        }
        #endregion

        #region 缓存管理
        /// <summary>
        /// 缓存尺寸
        /// </summary>
        private void CacheSize(int index, Vector2 size)
        {
            if (!_enableCache) return;

            // 清理缓存如果超过最大数量
            if (_sizeCache.Count >= _maxCacheSize)
            {
                ClearCache();
            }

            _sizeCache[index] = size;
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        new public void ClearCache()
        {
            base.ClearCache();
            _sizeCache.Clear();
        }

        /// <summary>
        /// 移除特定索引的缓存
        /// </summary>
        public void RemoveCache(int index)
        {
            _sizeCache.Remove(index);
        }

        /// <summary>
        /// 预热缓存 - 预计算指定范围的尺寸
        /// </summary>
        public void PreheatCache(int startIndex, int count, Vector2 viewportSize, IScrollLayout layout)
        {
            if (!_enableCache) return;

            var endIndex = Mathf.Min(startIndex + count, GetItemCount());
            for (var i = startIndex; i < endIndex; i++)
            {
                var size = GetLayoutSize(i, viewportSize, layout);
                if (size != Vector2.zero)
                {
                    CacheSize(i, size);
                }
            }
        }

        /// <summary>
        /// 预热缓存 - 预计算指定范围的尺寸（扩展方法）
        /// </summary>
        public void PreheatCache(IScrollLayout layout, Vector2 viewportSize, int startIndex, int count)
        {
            PreheatCache(startIndex, count, viewportSize, layout);
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 更新数据源
        /// </summary>
        public void UpdateDataSource(Func<int> countGetter, Func<int, object> dataGetter)
        {
            _countGetter = countGetter ?? throw new ArgumentNullException(nameof(countGetter));
            _dataGetter = dataGetter ?? throw new ArgumentNullException(nameof(dataGetter));
            ClearCache();
        }

        /// <summary>
        /// 更新数据源（基于IList）
        /// </summary>
        public void UpdateDataSource<T>(IReadOnlyList<T> dataList) where T : class
        {
            if (dataList == null) throw new ArgumentNullException(nameof(dataList));

            _countGetter = () => dataList.Count;
            _dataGetter = index => index >= 0 && index < dataList.Count ? dataList[index] : null;
            ClearCache();
        }

        /// <summary>
        /// 设置自定义尺寸计算器
        /// </summary>
        public void SetCustomSizeCalculator(Func<int, object, Vector2> calculator)
        {
            _customSizeCalculator = calculator;
            ClearCache();
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public (int cacheCount, int maxCacheSize, float cacheUsage) GetCacheStats()
        {
            return (_sizeCache.Count, _maxCacheSize, (float)_sizeCache.Count / _maxCacheSize);
        }
        #endregion
    }
}
