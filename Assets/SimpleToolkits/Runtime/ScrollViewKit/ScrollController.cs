namespace SimpleToolkits
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// 高性能滚动控制器 - 纯手动布局，无Unity布局依赖
    /// </summary>
    public class ScrollController : IDisposable
    {
        #region 核心组件
        private readonly ScrollRect _scrollRect;
        private readonly RectTransform _content;
        private readonly IScrollLayout _layout;
        private readonly IScrollSizeProvider _sizeProvider;
        private readonly IScrollAdapter _adapter;
        #endregion

        #region 对象池和管理器
        private readonly ScrollCellPool _cellPool;
        private readonly ActiveCellManager _cellManager;
        #endregion

        #region 状态
        private int _visibleFirst = -1;
        private int _visibleLast = -1;
        private Vector2 _lastContentPosition;
        private Vector2 _lastViewportSize;
        private bool _isRefreshing = false;
        private bool _disposed = false;
        #endregion

        #region 设置
        private const float REFRESH_THRESHOLD = 1f;
        private const int VISIBLE_BUFFER = 2; // 可见范围缓冲
        #endregion

        #region 事件
        public event Action<int, int> OnVisibleRangeChanged;
        public event Action<Vector2> OnScrollPositionChanged;
        #endregion

        #region 属性
        public int VisibleFirst => _visibleFirst;
        public int VisibleLast => _visibleLast;
        public int ItemCount => _adapter?.Count ?? 0;
        public bool IsDisposed => _disposed;
        #endregion

        #region 构造和初始化
        public ScrollController(
            ScrollRect scrollRect,
            RectTransform content,
            IScrollLayout layout,
            IScrollSizeProvider sizeProvider,
            IScrollAdapter adapter,
            int poolSize = 20)
        {
            _scrollRect = scrollRect ?? throw new ArgumentNullException(nameof(scrollRect));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _sizeProvider = sizeProvider ?? throw new ArgumentNullException(nameof(sizeProvider));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

            // 创建对象池和管理器
            _cellPool = new ScrollCellPool(_adapter.GetCellPrefab(), _content, _adapter, poolSize);
            _cellManager = new ActiveCellManager(_cellPool, _adapter);

            Initialize();
        }

        private void Initialize()
        {
            // 设置Content
            _layout.SetupContent(_content);

            // 预热对象池
            _cellPool.Prewarm(10);

            // 绑定滚动事件
            _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);

            // 初始刷新
            RefreshAsync().Forget();
        }
        #endregion

        #region 公共API
        /// <summary>刷新显示</summary>
        public void Refresh()
        {
            if (_disposed) return;
            RefreshAsync().Forget();
        }

        /// <summary>滚动到指定索引</summary>
        public void ScrollToIndex(int index, bool immediate = false)
        {
            if (_disposed || index < 0 || index >= ItemCount) return;

            var viewportSize = _scrollRect.viewport.rect.size;
            var targetPosition = _layout.CalculateItemPosition(index, ItemCount, _sizeProvider, viewportSize);

            if (immediate)
            {
                _content.anchoredPosition = -targetPosition;
            }
            else
            {
                ScrollToPositionAsync(-targetPosition).Forget();
            }
        }

        /// <summary>滚动到顶部</summary>
        public void ScrollToTop(bool immediate = false)
        {
            if (_disposed) return;
            
            var targetPosition = Vector2.zero;
            if (immediate)
            {
                _content.anchoredPosition = targetPosition;
            }
            else
            {
                ScrollToPositionAsync(targetPosition).Forget();
            }
        }

        /// <summary>滚动到底部</summary>
        public void ScrollToBottom(bool immediate = false)
        {
            if (_disposed || ItemCount <= 0) return;

            var viewportSize = _scrollRect.viewport.rect.size;
            var contentSize = _layout.CalculateContentSize(ItemCount, _sizeProvider, viewportSize);
            
            Vector2 targetPosition;
            if (_layout.IsVertical)
            {
                targetPosition = new Vector2(0, contentSize.y - viewportSize.y);
            }
            else
            {
                targetPosition = new Vector2(contentSize.x - viewportSize.x, 0);
            }

            if (immediate)
            {
                _content.anchoredPosition = -targetPosition;
            }
            else
            {
                ScrollToPositionAsync(-targetPosition).Forget();
            }
        }
        #endregion

        #region 内部逻辑
        private void OnScrollValueChanged(Vector2 value)
        {
            if (_disposed) return;

            var currentPosition = _content.anchoredPosition;
            _lastContentPosition = currentPosition;
            
            OnScrollPositionChanged?.Invoke(value);

            // 检查是否需要刷新可见项
            if (!_isRefreshing && ShouldRefreshVisibleItems())
            {
                RefreshVisibleItemsAsync().Forget();
            }
        }

        private bool ShouldRefreshVisibleItems()
        {
            var currentViewportSize = _scrollRect.viewport.rect.size;
            
            // 如果视口大小改变，立即刷新
            if (Vector2.Distance(currentViewportSize, _lastViewportSize) > 1f)
            {
                _lastViewportSize = currentViewportSize;
                return true;
            }

            return true; // 简化：总是检查刷新（可优化为基于阈值）
        }

        private async UniTaskVoid RefreshAsync()
        {
            if (_disposed || _isRefreshing) return;
            _isRefreshing = true;

            try
            {
                await UniTask.Yield(); // 等待一帧确保UI稳定

                // 更新Content尺寸
                UpdateContentSize();

                // 清理现有显示
                _cellManager.Clear();

                // 重置可见范围
                _visibleFirst = -1;
                _visibleLast = -1;

                // 刷新可见项
                await RefreshVisibleItemsAsync();
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private async UniTask RefreshVisibleItemsAsync()
        {
            if (_disposed || ItemCount <= 0) return;

            var viewportSize = _scrollRect.viewport.rect.size;
            var contentPosition = _content.anchoredPosition;

            // 计算可见范围
            var (first, last) = _layout.CalculateVisibleRange(contentPosition, viewportSize, ItemCount, _sizeProvider);

            // 扩展缓冲区
            first = Mathf.Max(0, first - VISIBLE_BUFFER);
            last = Mathf.Min(ItemCount - 1, last + VISIBLE_BUFFER);

            // 如果范围没有变化，不需要更新
            if (first == _visibleFirst && last == _visibleLast)
                return;

            // 回收范围外的Cell
            _cellManager.RecycleOutsideRange(first, last);

            // 创建新的可见Cell
            for (int i = first; i <= last; i++)
            {
                var cell = _cellManager.GetOrCreateCell(i);
                if (cell != null)
                {
                    // 设置位置
                    var itemPosition = _layout.CalculateItemPosition(i, ItemCount, _sizeProvider, viewportSize);
                    cell.anchoredPosition = itemPosition;

                    // 设置尺寸
                    var itemSize = _sizeProvider.GetItemSize(i, viewportSize);
                    cell.sizeDelta = itemSize;
                }

                // 每处理几个Item让出控制权，避免卡顿
                if ((i - first) % 5 == 0)
                {
                    await UniTask.Yield();
                    if (_disposed) return; // 检查是否已销毁
                }
            }

            // 更新可见范围
            var oldFirst = _visibleFirst;
            var oldLast = _visibleLast;
            _visibleFirst = first;
            _visibleLast = last;

            // 触发事件
            if (oldFirst != first || oldLast != last)
            {
                OnVisibleRangeChanged?.Invoke(first, last);
            }
        }

        private void UpdateContentSize()
        {
            if (_disposed) return;

            var viewportSize = _scrollRect.viewport.rect.size;
            var contentSize = _layout.CalculateContentSize(ItemCount, _sizeProvider, viewportSize);

            // 确保Content尺寸合理
            if (_layout.IsVertical)
            {
                contentSize.x = Mathf.Max(contentSize.x, viewportSize.x);
                contentSize.y = Mathf.Max(contentSize.y, 0);
            }
            else
            {
                contentSize.x = Mathf.Max(contentSize.x, 0);
                contentSize.y = Mathf.Max(contentSize.y, viewportSize.y);
            }

            _content.sizeDelta = contentSize;
        }

        private async UniTaskVoid ScrollToPositionAsync(Vector2 targetPosition)
        {
            if (_disposed) return;

            var startPosition = _content.anchoredPosition;
            var duration = 0.3f;
            var elapsed = 0f;

            while (elapsed < duration && !_disposed)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                t = Mathf.SmoothStep(0f, 1f, t);

                _content.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
                await UniTask.Yield();
            }

            if (!_disposed)
            {
                _content.anchoredPosition = targetPosition;
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 解绑事件
            if (_scrollRect != null)
            {
                _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            }

            // 清理管理器和对象池
            _cellManager?.Clear();
            _cellPool?.Clear();

            // 清理尺寸提供器缓存
            if (_sizeProvider is DynamicSizeProviderBehaviour dynamicProvider)
            {
                dynamicProvider.ClearCache();
            }
        }
        #endregion
    }
}