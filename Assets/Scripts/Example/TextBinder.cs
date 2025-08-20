using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleToolkits
{
    /// <summary>
    /// 文本列表 Binder：用于不定长文本展示。
    /// - 在 OnCreated 缓存组件，避免绑定期 GetComponent 开销。
    /// - 在 OnBind 按索引设置文本内容与基础可视。
    /// </summary>
    internal sealed class TextBinder : ICellBinder
    {
        private readonly IReadOnlyList<string> _texts;

        private sealed class Cache
        {
            public Image Bg;
            public TextMeshProUGUI Tmp;
            public Vector2 DefaultSize;
        }

        private readonly Dictionary<RectTransform, Cache> _caches = new();

        public TextBinder(IReadOnlyList<string> texts)
        {
            _texts = texts;
        }

        public void OnCreated(RectTransform cell)
        {
            var cache = new Cache
            {
                Bg = cell.GetComponent<Image>(),
                Tmp = cell.GetComponentInChildren<TextMeshProUGUI>(true),
                DefaultSize = cell.sizeDelta,
            };
            if (cache.Tmp != null)
            {
                // 基础显示设置
                cache.Tmp.enableWordWrapping = true; // 启用换行
                cache.Tmp.raycastTarget = false;
            }
            _caches[cell] = cache;
        }

        public void OnBind(int index, RectTransform cell)
        {
            if (!_caches.TryGetValue(cell, out var cache)) return;

            // 背景交替色，便于观察
            if (cache.Bg != null)
                cache.Bg.color = (index & 1) == 0 ? new Color(0.90f, 0.95f, 1f, 1f) : new Color(0.95f, 0.90f, 1f, 1f);

            if (cache.Tmp != null)
            {
                var txt = (index >= 0 && index < _texts.Count) ? _texts[index] : string.Empty;
                cache.Tmp.text = txt;
            }
        }

        public void OnRecycled(int index, RectTransform cell)
        {
            if (_caches.TryGetValue(cell, out var cache))
            {
                // 轻量复位（可按需扩展）
                cell.sizeDelta = cache.DefaultSize;
                if (cache.Bg != null)
                {
                    cache.Bg.color = Color.white;
                }
            }
        }
    }
}
