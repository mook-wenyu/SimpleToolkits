namespace SimpleToolkits
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 动态尺寸提供器组件 - 支持每个Item不同尺寸
    /// </summary>
    [AddComponentMenu("SimpleToolkits/Scroll Size Provider/Dynamic Size")]
    public class DynamicSizeProviderBehaviour : ScrollSizeProviderBehaviour
    {
        [Header("动态尺寸设置")]
        [SerializeField] private Vector2 _defaultSize = new Vector2(200, 60);
        [SerializeField] private int _maxCacheSize = 1000;
        [SerializeField] private bool _enableCache = true;

        private Func<int, Vector2, Vector2> _sizeCalculator;
        private Dictionary<int, Vector2> _sizeCache = new Dictionary<int, Vector2>();

        public override bool SupportsVariableSize => true;

        public Vector2 DefaultSize
        {
            get => _defaultSize;
            set
            {
                if (_defaultSize != value)
                {
                    _defaultSize = value;
                    SetDirtyAndUpdate();
                }
            }
        }

        public int MaxCacheSize
        {
            get => _maxCacheSize;
            set
            {
                if (_maxCacheSize != value)
                {
                    _maxCacheSize = Mathf.Max(10, value);
                    SetDirtyAndUpdate();
                }
            }
        }

        public bool EnableCache
        {
            get => _enableCache;
            set
            {
                if (_enableCache != value)
                {
                    _enableCache = value;
                    if (!_enableCache)
                        ClearCache();
                    SetDirtyAndUpdate();
                }
            }
        }

        /// <summary>设置尺寸计算函数</summary>
        public void SetSizeCalculator(Func<int, Vector2, Vector2> sizeCalculator)
        {
            _sizeCalculator = sizeCalculator;
            ClearCache();
            SetDirtyAndUpdate();
        }

        public override Vector2 GetItemSize(int index, Vector2 viewportSize)
        {
            // 如果没有设置计算函数，返回默认尺寸
            if (_sizeCalculator == null)
                return _defaultSize;

            // 尝试从缓存获取
            if (_enableCache && _sizeCache.TryGetValue(index, out var cachedSize))
            {
                return cachedSize;
            }

            // 计算尺寸
            var size = _sizeCalculator(index, viewportSize);

            // 缓存结果（带大小限制）
            if (_enableCache && _sizeCache.Count < _maxCacheSize)
            {
                _sizeCache[index] = size;
            }

            return size;
        }

        public override Vector2 GetAverageSize(Vector2 viewportSize)
        {
            if (!_enableCache || _sizeCache.Count == 0)
                return _defaultSize;

            var totalSize = Vector2.zero;
            foreach (var size in _sizeCache.Values)
            {
                totalSize += size;
            }

            return totalSize / _sizeCache.Count;
        }

        /// <summary>清理缓存</summary>
        public void ClearCache()
        {
            _sizeCache.Clear();
        }

        /// <summary>移除指定索引的缓存</summary>
        public void RemoveCache(int index)
        {
            _sizeCache.Remove(index);
        }

        /// <summary>预热缓存</summary>
        public void WarmupCache(int itemCount, Vector2 viewportSize)
        {
            if (_sizeCalculator == null || !_enableCache) return;

            var warmupCount = Mathf.Min(itemCount, 50); // 限制预热数量
            for (int i = 0; i < warmupCount; i++)
            {
                GetItemSize(i, viewportSize);
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _defaultSize.x = Mathf.Max(1, _defaultSize.x);
            _defaultSize.y = Mathf.Max(1, _defaultSize.y);
            _maxCacheSize = Mathf.Max(10, _maxCacheSize);
        }
    }
}