using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleToolkits
{
    /// <summary>
    /// StandardVariableSizeAdapter扩展方法
    /// 提供更便捷的创建和配置方法
    /// </summary>
    public static class StandardVariableSizeAdapterExtensions
    {
        #region 快捷创建方法
        /// <summary>
        /// 创建基于List数据的StandardVariableSizeAdapter - 支持固定和自适应尺寸参数
        /// </summary>
        public static StandardVariableSizeAdapter CreateForList<T>(
            RectTransform template,
            List<T> dataList,
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
            Func<int, T, Vector2> customSizeCalculator = null)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (dataList == null) throw new ArgumentNullException(nameof(dataList));
            if (binder == null) throw new ArgumentNullException(nameof(binder));

            Func<int, object> dataGetter = index => 
                index >= 0 && index < dataList.Count ? dataList[index] : null;

            Func<int, object, Vector2> sizeCalculator = null;
            if (customSizeCalculator != null)
            {
                sizeCalculator = (index, data) => 
                {
                    if (data is T typedData)
                        return customSizeCalculator.Invoke(index, typedData);
                    return Vector2.zero;
                };
            }

            return new StandardVariableSizeAdapter(
                template,
                () => dataList.Count,
                dataGetter,
                binder,
                templateBinder,
                fixedWidth,
                fixedHeight,
                minWidth,
                minHeight,
                maxWidth,
                maxHeight,
                useLayoutGroups,
                enableCache,
                1000,
                sizeCalculator
            );
        }

        /// <summary>
        /// 创建基于数组的StandardVariableSizeAdapter - 支持固定和自适应尺寸参数
        /// </summary>
        public static StandardVariableSizeAdapter CreateForArray<T>(
            RectTransform template,
            T[] dataArray,
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
            Func<int, T, Vector2> customSizeCalculator = null)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (dataArray == null) throw new ArgumentNullException(nameof(dataArray));
            if (binder == null) throw new ArgumentNullException(nameof(binder));

            Func<int, object> dataGetter = index => 
                index >= 0 && index < dataArray.Length ? dataArray[index] : null;

            Func<int, object, Vector2> sizeCalculator = null;
            if (customSizeCalculator != null)
            {
                sizeCalculator = (index, data) => 
                {
                    if (data is T typedData)
                        return customSizeCalculator.Invoke(index, typedData);
                    return Vector2.zero;
                };
            }

            return new StandardVariableSizeAdapter(
                template,
                () => dataArray.Length,
                dataGetter,
                binder,
                templateBinder,
                fixedWidth,
                fixedHeight,
                minWidth,
                minHeight,
                maxWidth,
                maxHeight,
                useLayoutGroups,
                enableCache,
                1000,
                sizeCalculator
            );
        }

        /// <summary>
        /// 创建基于固定数量的StandardVariableSizeAdapter - 支持固定和自适应尺寸参数
        /// </summary>
        public static StandardVariableSizeAdapter CreateForFixedCount(
            RectTransform template,
            int count,
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
            Func<int, object, Vector2> customSizeCalculator = null)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (binder == null) throw new ArgumentNullException(nameof(binder));

            Func<int, object> dataGetter = index => index; // 简单返回索引作为数据

            return new StandardVariableSizeAdapter(
                template,
                () => count,
                dataGetter,
                binder,
                templateBinder,
                fixedWidth,
                fixedHeight,
                minWidth,
                minHeight,
                maxWidth,
                maxHeight,
                useLayoutGroups,
                enableCache,
                1000,
                customSizeCalculator
            );
        }
        #endregion

        #region 缓存管理
        /// <summary>
        /// 预热StandardVariableSizeAdapter的缓存
        /// </summary>
        public static void PreheatCache(
            this StandardVariableSizeAdapter provider,
            IScrollLayout layout,
            Vector2 viewportSize,
            int startIndex,
            int count)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            try
            {
                provider.PreheatCache(layout, viewportSize, startIndex, count);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StandardVariableSizeAdapterExtensions] 预热缓存失败: {e.Message}");
            }
        }

        
        /// <summary>
        /// 批量预热多个StandardVariableSizeAdapter
        /// </summary>
        public static void PreheatCache(
            this IEnumerable<StandardVariableSizeAdapter> providers,
            IScrollLayout layout,
            Vector2 viewportSize,
            int startIndex,
            int count)
        {
            if (providers == null) throw new ArgumentNullException(nameof(providers));

            foreach (var provider in providers)
            {
                try
                {
                    provider.PreheatCache(layout, viewportSize, startIndex, count);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[StandardVariableSizeAdapterExtensions] 预热缓存失败: {e.Message}");
                }
            }
        }
        #endregion
    }
}