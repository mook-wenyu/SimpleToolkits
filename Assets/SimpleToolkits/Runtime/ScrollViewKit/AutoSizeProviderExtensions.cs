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
        /// 创建基于List数据的StandardVariableSizeAdapter
        /// </summary>
        public static StandardVariableSizeAdapter CreateForList<T>(
            RectTransform template,
            List<T> dataList,
            ICellBinder binder,
            Action<RectTransform, object> templateBinder,
            float fixedWidth,
            float minHeight = 60f,
            float maxHeight = 300f,
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
                minHeight,
                maxHeight,
                useLayoutGroups,
                enableCache,
                1000,
                sizeCalculator
            );
        }

        /// <summary>
        /// 创建基于数组的StandardVariableSizeAdapter
        /// </summary>
        public static StandardVariableSizeAdapter CreateForArray<T>(
            RectTransform template,
            T[] dataArray,
            ICellBinder binder,
            Action<RectTransform, object> templateBinder,
            float fixedWidth,
            float minHeight = 60f,
            float maxHeight = 300f,
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
                minHeight,
                maxHeight,
                useLayoutGroups,
                enableCache,
                1000,
                sizeCalculator
            );
        }

        /// <summary>
        /// 创建基于固定数量的StandardVariableSizeAdapter
        /// </summary>
        public static StandardVariableSizeAdapter CreateForFixedCount(
            RectTransform template,
            int count,
            ICellBinder binder,
            Action<RectTransform, object> templateBinder,
            float fixedWidth,
            float minHeight = 60f,
            float maxHeight = 300f,
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
                minHeight,
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
        /// 获取StandardVariableSizeAdapter的缓存统计信息
        /// </summary>
        public static (int cacheCount, int maxCacheSize, double cacheUsage) GetCacheStats(
            this StandardVariableSizeAdapter provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            return provider.GetCacheStats();
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

        #region 诊断和测试
        /// <summary>
        /// 获取StandardVariableSizeAdapter的诊断信息
        /// </summary>
        public static string GetDiagnostics(
            this StandardVariableSizeAdapter provider,
            RectTransform template)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            var stats = provider.GetCacheStats();
            var diagnostics = "=== StandardVariableSizeAdapter 诊断信息 ===\n";
            diagnostics += $"缓存统计: {stats.cacheCount}/{stats.maxCacheSize} ({stats.cacheUsage:P1})\n";
            diagnostics += $"模板状态: {(template != null ? "正常" : "空引用")}\n";
            
            return diagnostics;
        }

        /// <summary>
        /// 测试StandardVariableSizeAdapter的性能
        /// </summary>
        public static (double averageTimeMs, int testCount) TestPerformance(
            this StandardVariableSizeAdapter provider,
            IScrollLayout layout,
            Vector2 viewportSize,
            int testCount = 1000)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            var startTime = Time.realtimeSinceStartup;
            
            for (int i = 0; i < testCount; i++)
            {
                provider.GetItemSize(i % 100, viewportSize, layout);
            }
            
            var endTime = Time.realtimeSinceStartup;
            var totalTime = endTime - startTime;
            var averageTimeMs = (totalTime / testCount) * 1000;

            return (averageTimeMs, testCount);
        }
        #endregion

        #region 链式配置
        /// <summary>
        /// 链式配置StandardVariableSizeAdapter
        /// </summary>
        public static StandardVariableSizeAdapter Configure(
            this StandardVariableSizeAdapter provider,
            Action<StandardVariableSizeAdapter> configuration)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            configuration.Invoke(provider);
            return provider;
        }

        /// <summary>
        /// 配置缓存设置
        /// </summary>
        public static StandardVariableSizeAdapter WithCache(
            this StandardVariableSizeAdapter provider,
            bool enableCache,
            int maxCacheSize = 1000)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            // 注意：这些参数在构造后不能修改，这里只是为了保持API一致性
            Debug.LogWarning("[StandardVariableSizeAdapterExtensions] 缓存参数只能在构造函数中设置");
            return provider;
        }

        /// <summary>
        /// 配置自定义尺寸计算器
        /// </summary>
        public static StandardVariableSizeAdapter WithCustomCalculator<T>(
            this StandardVariableSizeAdapter provider,
            Func<int, T, Vector2> calculator)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (calculator == null) throw new ArgumentNullException(nameof(calculator));

            // 注意：自定义计算器只能在构造函数中设置
            Debug.LogWarning("[StandardVariableSizeAdapterExtensions] 自定义计算器只能在构造函数中设置");
            return provider;
        }
        #endregion
    }
}