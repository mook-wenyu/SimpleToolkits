using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleToolkits
{
    /// <summary>
    /// 布局工具类 - 提供Unity布局组件相关的工具方法
    /// </summary>
    public static class LayoutUtils
    {
        #region 布局组件配置
        /// <summary>
        /// 配置RectTransform用于自动尺寸计算
        /// </summary>
        public static void ConfigureForAutoSize(
            RectTransform rectTransform,
            Vector2 minSize,
            Vector2 maxSize,
            bool addLayoutElement = true,
            bool addContentSizeFitter = true)
        {
            if (rectTransform == null) return;

            // 添加LayoutElement
            if (addLayoutElement)
            {
                var layoutElement = rectTransform.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();
                }

                layoutElement.minWidth = minSize.x;
                layoutElement.minHeight = minSize.y;
                layoutElement.preferredWidth = -1; // 自动计算
                layoutElement.preferredHeight = -1; // 自动计算
            }

            // 添加ContentSizeFitter
            if (addContentSizeFitter)
            {
                var contentSizeFitter = rectTransform.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter == null)
                {
                    contentSizeFitter = rectTransform.gameObject.AddComponent<ContentSizeFitter>();
                }

                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        /// <summary>
        /// 配置RectTransform用于固定尺寸
        /// </summary>
        public static void ConfigureForFixedSize(
            RectTransform rectTransform,
            Vector2 fixedSize,
            bool addLayoutElement = true)
        {
            if (rectTransform == null) return;

            // 设置固定尺寸
            rectTransform.sizeDelta = fixedSize;

            // 添加LayoutElement
            if (addLayoutElement)
            {
                var layoutElement = rectTransform.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();
                }

                layoutElement.minWidth = fixedSize.x;
                layoutElement.minHeight = fixedSize.y;
                layoutElement.preferredWidth = fixedSize.x;
                layoutElement.preferredHeight = fixedSize.y;
            }

            // 移除ContentSizeFitter（如果存在）
            var contentSizeFitter = rectTransform.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter != null)
            {
                UnityEngine.Object.Destroy(contentSizeFitter);
            }
        }

        /// <summary>
        /// 配置RectTransform用于混合尺寸（固定宽度，自适应高度）
        /// </summary>
        public static void ConfigureForMixedSize(
            RectTransform rectTransform,
            float fixedWidth,
            float minHeight,
            float maxHeight,
            bool addLayoutElement = true,
            bool addContentSizeFitter = true)
        {
            if (rectTransform == null) return;

            // 添加LayoutElement
            if (addLayoutElement)
            {
                var layoutElement = rectTransform.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();
                }

                layoutElement.minWidth = fixedWidth;
                layoutElement.preferredWidth = fixedWidth;
                layoutElement.minHeight = minHeight;
                layoutElement.preferredHeight = -1; // 自动计算
            }

            // 添加ContentSizeFitter
            if (addContentSizeFitter)
            {
                var contentSizeFitter = rectTransform.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter == null)
                {
                    contentSizeFitter = rectTransform.gameObject.AddComponent<ContentSizeFitter>();
                }

                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }
        #endregion

        #region 布局尺寸计算
        /// <summary>
        /// 计算包含指定RectTransform的布局组的首选尺寸
        /// </summary>
        public static Vector2 CalculateLayoutGroupPreferredSize(RectTransform container)
        {
            if (container == null) return Vector2.zero;

            var layoutGroup = container.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (layoutGroup == null) return Vector2.zero;

            try
            {
                // 强制重建布局
                LayoutRebuilder.ForceRebuildLayoutImmediate(container);
                
                // 获取布局组的首选尺寸
                return GetLayoutGroupPreferredSize(layoutGroup);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LayoutUtils] 布局计算失败: {e.Message}");
                return Vector2.zero;
            }
        }

        /// <summary>
        /// 获取布局组的首选尺寸
        /// </summary>
        private static Vector2 GetLayoutGroupPreferredSize(HorizontalOrVerticalLayoutGroup layoutGroup)
        {
            var rectTransform = layoutGroup.transform as RectTransform;
            if (rectTransform == null) return Vector2.zero;

            // 计算所有子元素的总尺寸
            float totalMinWidth = 0f;
            float totalPreferredWidth = 0f;
            float totalMinHeight = 0f;
            float totalPreferredHeight = 0f;

            foreach (Transform child in layoutGroup.transform)
            {
                var childRect = child as RectTransform;
                if (childRect == null || !childRect.gameObject.activeSelf) continue;

                var layoutElement = childRect.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    totalMinWidth += layoutElement.minWidth;
                    totalPreferredWidth += layoutElement.preferredWidth > 0 ? layoutElement.preferredWidth : layoutElement.minWidth;
                    totalMinHeight += layoutElement.minHeight;
                    totalPreferredHeight += layoutElement.preferredHeight > 0 ? layoutElement.preferredHeight : layoutElement.minHeight;
                }
                else
                {
                    var size = childRect.rect.size;
                    totalMinWidth += size.x;
                    totalPreferredWidth += size.x;
                    totalMinHeight += size.y;
                    totalPreferredHeight += size.y;
                }
            }

            // 添加间距
            var childCount = layoutGroup.transform.childCount;
            var spacing = layoutGroup.spacing;
            var padding = layoutGroup.padding;

            if (layoutGroup is HorizontalLayoutGroup)
            {
                totalMinWidth += spacing * (childCount - 1) + padding.left + padding.right;
                totalPreferredWidth += spacing * (childCount - 1) + padding.left + padding.right;
                totalMinHeight += padding.top + padding.bottom;
                totalPreferredHeight += padding.top + padding.bottom;
            }
            else if (layoutGroup is VerticalLayoutGroup)
            {
                totalMinWidth += padding.left + padding.right;
                totalPreferredWidth += padding.left + padding.right;
                totalMinHeight += spacing * (childCount - 1) + padding.top + padding.bottom;
                totalPreferredHeight += spacing * (childCount - 1) + padding.top + padding.bottom;
            }

            return new Vector2(totalPreferredWidth, totalPreferredHeight);
        }

        /// <summary>
        /// 计算Text组件的首选尺寸（保持向后兼容）
        /// </summary>
        public static Vector2 CalculateTextPreferredSize(Text textComponent, float maxWidth, float maxHeight)
        {
            if (textComponent == null) return Vector2.zero;

            try
            {
                // 简化的Text组件尺寸计算
                var textGenerator = textComponent.cachedTextGeneratorForLayout;
                var settings = textComponent.GetGenerationSettings(new Vector2(maxWidth, maxHeight));
                var width = textGenerator.GetPreferredWidth(textComponent.text, settings);
                var height = textGenerator.GetPreferredHeight(textComponent.text, settings);
                
                return new Vector2(Mathf.Min(width, maxWidth), Mathf.Min(height, maxHeight));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LayoutUtils] Text尺寸计算失败: {e.Message}");
                return Vector2.zero;
            }
        }

        /// <summary>
        /// 计算TextMeshProUGUI组件的首选尺寸
        /// </summary>
        public static Vector2 CalculateTextMeshProPreferredSize(TextMeshProUGUI tmpComponent, float maxWidth, float maxHeight)
        {
            if (tmpComponent == null) return Vector2.zero;

            try
            {
                var preferredValues = tmpComponent.GetPreferredValues(tmpComponent.text, maxWidth, maxHeight);
                return new Vector2(Mathf.Min(preferredValues.x, maxWidth), Mathf.Min(preferredValues.y, maxHeight));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LayoutUtils] TextMeshPro尺寸计算失败: {e.Message}");
                return Vector2.zero;
            }
        }
        #endregion

        #region 布局验证和诊断
        /// <summary>
        /// 验证RectTransform是否配置正确用于自动尺寸计算
        /// </summary>
        public static bool ValidateAutoSizeConfiguration(RectTransform rectTransform, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (rectTransform == null)
            {
                errorMessage = "RectTransform不能为空";
                return false;
            }

            var layoutElement = rectTransform.GetComponent<LayoutElement>();
            var contentSizeFitter = rectTransform.GetComponent<ContentSizeFitter>();

            if (layoutElement == null && contentSizeFitter == null)
            {
                errorMessage = "RectTransform缺少LayoutElement或ContentSizeFitter组件";
                return false;
            }

            if (layoutElement != null)
            {
                if (layoutElement.minWidth <= 0 && layoutElement.minHeight <= 0)
                {
                    errorMessage = "LayoutElement的最小宽度和高度必须大于0";
                    return false;
                }
            }

            if (contentSizeFitter != null)
            {
                if (contentSizeFitter.horizontalFit == ContentSizeFitter.FitMode.Unconstrained &&
                    contentSizeFitter.verticalFit == ContentSizeFitter.FitMode.Unconstrained)
                {
                    errorMessage = "ContentSizeFitter至少需要一个轴设置为PreferredSize";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 获取RectTransform的布局诊断信息
        /// </summary>
        public static string GetLayoutDiagnostics(RectTransform rectTransform)
        {
            if (rectTransform == null) return "RectTransform为空";

            var diagnostics = $"GameObject: {rectTransform.gameObject.name}\n";
            diagnostics += $"SizeDelta: {rectTransform.sizeDelta}\n";
            diagnostics += $"AnchoredPosition: {rectTransform.anchoredPosition}\n";

            var layoutElement = rectTransform.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                diagnostics += $"LayoutElement: Min({layoutElement.minWidth}, {layoutElement.minHeight}), " +
                             $"Preferred({layoutElement.preferredWidth}, {layoutElement.preferredHeight})\n";
            }

            var contentSizeFitter = rectTransform.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter != null)
            {
                diagnostics += $"ContentSizeFitter: Horizontal={contentSizeFitter.horizontalFit}, " +
                             $"Vertical={contentSizeFitter.verticalFit}\n";
            }

            var layoutGroup = rectTransform.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                diagnostics += $"LayoutGroup: {layoutGroup.GetType().Name}, " +
                             $"Spacing={layoutGroup.spacing}, " +
                             $"Padding=({layoutGroup.padding.left}, {layoutGroup.padding.top}, {layoutGroup.padding.right}, {layoutGroup.padding.bottom})\n";
            }

            return diagnostics;
        }
        #endregion

        #region 性能优化
        /// <summary>
        /// 批量重建布局，避免频繁调用
        /// </summary>
        public static void BatchRebuildLayout(IEnumerable<RectTransform> rectTransforms)
        {
            if (rectTransforms == null) return;

            foreach (var rectTransform in rectTransforms)
            {
                if (rectTransform != null)
                {
                    LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
                }
            }

            // 一次性重建所有标记的布局
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// 延迟重建布局，避免在同一帧内多次重建
        /// </summary>
        public static void DelayedRebuildLayout(RectTransform rectTransform, float delay = 0f)
        {
            if (rectTransform == null) return;

            if (delay <= 0f)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
            else
            {
                // 使用协程延迟重建
                var monoBehaviour = rectTransform.GetComponent<MonoBehaviour>();
                if (monoBehaviour != null)
                {
                    monoBehaviour.StartCoroutine(DelayedRebuildCoroutine(rectTransform, delay));
                }
            }
        }

        private static System.Collections.IEnumerator DelayedRebuildCoroutine(RectTransform rectTransform, float delay)
        {
            yield return new WaitForSeconds(delay);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
        #endregion
    }
}