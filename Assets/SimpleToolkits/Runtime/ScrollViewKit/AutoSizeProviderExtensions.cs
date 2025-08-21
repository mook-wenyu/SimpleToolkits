using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleToolkits
{
    /// <summary>
    /// BaseVariableSizeAdapter扩展方法
    /// 提供更便捷的创建和配置方法
    /// </summary>
    public static class BaseVariableSizeAdapterExtensions
    {
        #region 快捷创建方法
        /// <summary>
        /// 创建基于List数据的LayoutAutoSizeProvider
        /// </summary>
        public static LayoutAutoSizeProvider CreateForList<T>(
            RectTransform template,
            List<T> dataList,
            float fixedWidth,
            float minHeight = 60f,
            float maxHeight = 300f,
            bool useLayoutGroups = true,
            bool enableCache = true,
            Func<int, T, Vector2> customSizeCalculator = null)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (dataList == null) throw new ArgumentNullException(nameof(dataList));

            Func<int, object> dataGetter = index => 
                index >= 0 && index < dataList.Count ? dataList[index] : null;

            Func<int, object, Vector2> calculator = null;
            if (customSizeCalculator != null)
            {
                calculator = (index, data) => 
                    data is T typedData ? customSizeCalculator(index, typedData) : Vector2.zero;
            }

            return new LayoutAutoSizeProvider(
                template: template,
                countGetter: () => dataList.Count,
                dataGetter: dataGetter,
                templateBinder: null,
                fixedWidth: fixedWidth,
                minHeight: minHeight,
                maxHeight: maxHeight,
                useLayoutGroups: useLayoutGroups,
                enableCache: enableCache,
                customSizeCalculator: calculator
            );
        }

        /// <summary>
        /// 创建基于数组的LayoutAutoSizeProvider
        /// </summary>
        public static LayoutAutoSizeProvider CreateForArray<T>(
            RectTransform template,
            T[] dataArray,
            float fixedWidth,
            float minHeight = 60f,
            float maxHeight = 300f,
            bool useLayoutGroups = true,
            bool enableCache = true,
            Func<int, T, Vector2> customSizeCalculator = null)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (dataArray == null) throw new ArgumentNullException(nameof(dataArray));

            Func<int, object> dataGetter = index => 
                index >= 0 && index < dataArray.Length ? dataArray[index] : null;

            Func<int, object, Vector2> calculator = null;
            if (customSizeCalculator != null)
            {
                calculator = (index, data) => 
                    data is T typedData ? customSizeCalculator(index, typedData) : Vector2.zero;
            }

            return new LayoutAutoSizeProvider(
                template: template,
                countGetter: () => dataArray.Length,
                dataGetter: dataGetter,
                templateBinder: null,
                fixedWidth: fixedWidth,
                minHeight: minHeight,
                maxHeight: maxHeight,
                useLayoutGroups: useLayoutGroups,
                enableCache: enableCache,
                customSizeCalculator: calculator
            );
        }

        /// <summary>
        /// 创建基于固定数量的LayoutAutoSizeProvider
        /// </summary>
        public static LayoutAutoSizeProvider CreateForFixedCount(
            RectTransform template,
            int count,
            float fixedWidth,
            float minHeight = 60f,
            float maxHeight = 300f,
            bool useLayoutGroups = true,
            bool enableCache = true,
            Func<int, object, Vector2> customSizeCalculator = null)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));

            Func<int, object> dataGetter = index => index; // 简单返回索引作为数据

            return new LayoutAutoSizeProvider(
                template: template,
                countGetter: () => count,
                dataGetter: dataGetter,
                templateBinder: null,
                fixedWidth: fixedWidth,
                minHeight: minHeight,
                maxHeight: maxHeight,
                useLayoutGroups: useLayoutGroups,
                enableCache: enableCache,
                customSizeCalculator: customSizeCalculator
            );
        }
        #endregion

        #region 性能优化扩展
        /// <summary>
        /// 预热LayoutAutoSizeProvider的缓存
        /// </summary>
        public static void PreheatCache(
            this LayoutAutoSizeProvider provider,
            IScrollLayout layout,
            Vector2 viewportSize,
            int? startIndex = null,
            int? count = null)
        {
            if (provider == null || layout == null) return;

            var totalCount = provider.GetType().GetProperty("Count")?.GetValue(provider) as int? ?? 0;
            if (totalCount <= 0) return;

            var start = startIndex ?? 0;
            var end = Math.Min(start + (count ?? totalCount), totalCount);

            // 使用反射调用预热方法
            var method = provider.GetType().GetMethod("PreheatCache");
            method?.Invoke(provider, new object[] { start, end - start, viewportSize, layout });
        }

        /// <summary>
        /// 获取LayoutAutoSizeProvider的缓存统计信息
        /// </summary>
        public static (int cacheCount, int maxCacheSize, float cacheUsage) GetCacheStats(
            this LayoutAutoSizeProvider provider)
        {
            if (provider == null) return (0, 0, 0f);

            // 使用反射调用统计方法
            var method = provider.GetType().GetMethod("GetCacheStats");
            var result = method?.Invoke(provider, null);

            if (result is ValueTuple<int, int, float> stats)
            {
                return stats;
            }

            return (0, 0, 0f);
        }

        /// <summary>
        /// 批量预热多个LayoutAutoSizeProvider
        /// </summary>
        public static void BatchPreheatCache(
            IEnumerable<LayoutAutoSizeProvider> providers,
            IScrollLayout layout,
            Vector2 viewportSize,
            int maxItemsPerProvider = 100)
        {
            if (providers == null || layout == null) return;

            foreach (var provider in providers)
            {
                try
                {
                    provider.PreheatCache(layout, viewportSize, 0, maxItemsPerProvider);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AutoSizeProviderExtensions] 预热缓存失败: {e.Message}");
                }
            }
        }
        #endregion

        #region 诊断和调试
        /// <summary>
        /// 获取LayoutAutoSizeProvider的诊断信息
        /// </summary>
        public static string GetDiagnostics(
            this LayoutAutoSizeProvider provider,
            RectTransform template)
        {
            if (provider == null) return "Provider为空";

            var diagnostics = "=== LayoutAutoSizeProvider 诊断信息 ===\n";
            
            // 获取缓存统计
            var stats = provider.GetCacheStats();
            diagnostics += $"缓存统计: {stats.cacheCount}/{stats.maxCacheSize} ({stats.cacheUsage:P1})\n";

            // 获取模板诊断信息
            if (template != null)
            {
                diagnostics += "\n=== 模板诊断信息 ===\n";
                diagnostics += LayoutUtils.GetLayoutDiagnostics(template);
            }

            return diagnostics;
        }

        /// <summary>
        /// 测试LayoutAutoSizeProvider的性能
        /// </summary>
        public static (double averageTimeMs, int testCount) TestPerformance(
            this LayoutAutoSizeProvider provider,
            IScrollLayout layout,
            Vector2 viewportSize,
            int testCount = 1000)
        {
            if (provider == null || layout == null) return (0, 0);

            var totalCount = provider.GetType().GetProperty("Count")?.GetValue(provider) as int? ?? 0;
            if (totalCount <= 0) return (0, 0);

            var random = new System.Random();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < testCount; i++)
            {
                var index = random.Next(0, totalCount);
                provider.GetItemSize(index, viewportSize, layout);
            }

            stopwatch.Stop();

            var averageTimeMs = stopwatch.Elapsed.TotalMilliseconds / testCount;
            return (averageTimeMs, testCount);
        }
        #endregion

        #region 链式配置
        /// <summary>
        /// 链式配置LayoutAutoSizeProvider
        /// </summary>
        public static LayoutAutoSizeProvider Configure(
            this LayoutAutoSizeProvider provider,
            Action<LayoutAutoSizeProvider> configuration)
        {
            if (provider != null && configuration != null)
            {
                configuration(provider);
            }
            return provider;
        }

        /// <summary>
        /// 启用缓存
        /// </summary>
        public static LayoutAutoSizeProvider WithCache(
            this LayoutAutoSizeProvider provider,
            bool enabled = true,
            int maxCacheSize = 1000)
        {
            if (provider != null)
            {
                // 使用反射设置缓存属性
                var enableCacheField = provider.GetType().GetField("_enableCache", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var maxCacheSizeField = provider.GetType().GetField("_maxCacheSize", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (enableCacheField != null) enableCacheField.SetValue(provider, enabled);
                if (maxCacheSizeField != null) maxCacheSizeField.SetValue(provider, maxCacheSize);
            }
            return provider;
        }

        /// <summary>
        /// 设置自定义尺寸计算器
        /// </summary>
        public static LayoutAutoSizeProvider WithCustomCalculator(
            this LayoutAutoSizeProvider provider,
            Func<int, object, Vector2> calculator)
        {
            if (provider != null)
            {
                var method = provider.GetType().GetMethod("SetCustomSizeCalculator");
                method?.Invoke(provider, new object[] { calculator });
            }
            return provider;
        }
        #endregion
    }
}