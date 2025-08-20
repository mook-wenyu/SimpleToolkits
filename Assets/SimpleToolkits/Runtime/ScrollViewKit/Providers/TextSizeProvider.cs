namespace SimpleToolkits
{
    using System;
    using TMPro;
    using UnityEngine;

    /// <summary>
    /// 基于 TextMeshPro 的不定长文本尺寸提供者。
    /// - 垂直列表：主轴=高度，按可用宽度计算首选高度。
    /// - 横向列表：主轴=宽度，按可用高度计算首选宽度（通常较少使用）。
    /// </summary>
    public sealed class TextSizeProvider : ISizeProvider
    {
        private readonly System.Collections.Generic.IReadOnlyList<string> _texts;
        private readonly TextMeshProUGUI _measure; // 隐藏的测量TMP（复制了字体/字号/行距）
        private readonly float _minMain;
        private readonly float _maxMain;
        private readonly float _extraPadding; // 额外内边距（避免文本贴边）

        /// <param name="texts">文本数据源</param>
        /// <param name="measureTMP">用于测量的 TMP（建议隐藏、复制字体参数）</param>
        /// <param name="minMain">主轴最小尺寸（像素）</param>
        /// <param name="maxMain">主轴最大尺寸（像素）</param>
        /// <param name="extraPadding">额外内边距（像素），会加在首选尺寸上</param>
        public TextSizeProvider(System.Collections.Generic.IReadOnlyList<string> texts,
            TextMeshProUGUI measureTMP, float minMain = 24f, float maxMain = 2000f, float extraPadding = 8f)
        {
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
            _measure = measureTMP ?? throw new ArgumentNullException(nameof(measureTMP));
            _minMain = Mathf.Max(0f, Mathf.Min(minMain, maxMain));
            _maxMain = Mathf.Max(0f, Mathf.Max(minMain, maxMain));
            _extraPadding = Mathf.Max(0f, extraPadding);
        }

        public Vector2 GetItemSize(int index, Vector2 viewportSize, IScrollLayout layout)
        {
            string text = (index >= 0 && index < _texts.Count) ? _texts[index] : string.Empty;

            if (layout.IsVertical)
            {
                // 计算可用宽度（跨轴）。若布局控制跨轴宽度，使用内边距后的 viewport 宽度；否则估一个保守宽度
                float availableWidth = layout.ControlChildWidth
                    ? Mathf.Max(0f, viewportSize.x - layout.Padding.left - layout.Padding.right)
                    : Mathf.Max(0f, viewportSize.x * 0.5f);

                // 计算首选高度
                Vector2 pref = _measure.GetPreferredValues(text, availableWidth, 0f);
                float main = Mathf.Clamp(pref.y + _extraPadding, _minMain, _maxMain);
                float cross = layout.ControlChildWidth ? availableWidth : Mathf.Max(0f, pref.x);
                return new Vector2(cross, main);
            }
            else
            {
                // 横向列表较少用到文本换行，这里按可用高度计算首选宽度
                float availableHeight = layout.ControlChildHeight
                    ? Mathf.Max(0f, viewportSize.y - layout.Padding.top - layout.Padding.bottom)
                    : Mathf.Max(0f, viewportSize.y * 0.5f);

                Vector2 pref = _measure.GetPreferredValues(text, 0f, availableHeight);
                float main = Mathf.Clamp(pref.x + _extraPadding, _minMain, _maxMain);
                float cross = layout.ControlChildHeight ? availableHeight : Mathf.Max(0f, pref.y);
                return new Vector2(main, cross);
            }
        }
    }
}
