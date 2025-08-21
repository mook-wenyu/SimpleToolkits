namespace SimpleToolkits
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 高性能ScrollView v4.0 - 完全自定义布局系统
    /// 
    /// 核心特性：
    /// - 零Unity布局依赖：完全自实现布局计算
    /// - 高性能：优化的虚拟化滚动和对象池
    /// - 易用性：极简API设计，链式调用
    /// - 扩展性：可插拔的布局、尺寸提供器、适配器
    /// - 稳定性：完善的生命周期管理和资源清理
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollView : MonoBehaviour
    {
        #region 核心组件
        private ScrollRect _scrollRect;
        private RectTransform _content;
        private ScrollController _controller;
        private IScrollAdapter _adapter;
        private IScrollLayout _layout;
        private IScrollSizeProvider _sizeProvider;
        #endregion

        #region 状态
        private bool _initialized = false;
        #endregion

        #region 公共属性
        /// <summary>是否已初始化</summary>
        public bool IsInitialized => _initialized && _controller != null && !_controller.IsDisposed;

        /// <summary>当前数据项数量</summary>
        public int Count => _adapter?.Count ?? 0;

        /// <summary>当前可见的第一个索引</summary>
        public int VisibleFirst => _controller?.VisibleFirst ?? -1;

        /// <summary>当前可见的最后一个索引</summary>
        public int VisibleLast => _controller?.VisibleLast ?? -1;
        #endregion

        #region 事件
        /// <summary>可见范围变化事件</summary>
        public event Action<int, int> OnVisibleRangeChanged;

        /// <summary>滚动位置变化事件</summary>
        public event Action<Vector2> OnScrollPositionChanged;
        #endregion

        #region 组件自动发现和通知监听
        private bool _componentNotificationListenerEnabled = false;
        
        /// <summary>启用组件自动发现和通知监听</summary>
        public void EnableAutoComponentDetection()
        {
            if (_componentNotificationListenerEnabled) return;
            
            _componentNotificationListenerEnabled = true;
            ScrollComponentNotifier.LayoutChanged += OnLayoutComponentChanged;
            ScrollComponentNotifier.SizeProviderChanged += OnSizeProviderComponentChanged;
            
            // 立即检测并使用可用的组件
            TryDetectAndUseComponents();
        }
        
        /// <summary>禁用组件自动发现和通知监听</summary>
        public void DisableAutoComponentDetection()
        {
            if (!_componentNotificationListenerEnabled) return;
            
            _componentNotificationListenerEnabled = false;
            ScrollComponentNotifier.LayoutChanged -= OnLayoutComponentChanged;
            ScrollComponentNotifier.SizeProviderChanged -= OnSizeProviderComponentChanged;
        }
        
        /// <summary>检测并使用可用的组件</summary>
        private void TryDetectAndUseComponents()
        {
            // 在Content上查找布局组件
            var detectedLayout = GetComponent<IScrollLayout>() ?? 
                                _content?.GetComponent<IScrollLayout>() ??
                                GetComponentInChildren<IScrollLayout>();
            
            if (detectedLayout != null && detectedLayout != _layout)
            {
                SetLayout(detectedLayout);
            }
            
            // 在本 GameObject和子对象中查找尺寸提供器
            var detectedSizeProvider = GetComponent<IScrollSizeProvider>() ??
                                      GetComponentInChildren<IScrollSizeProvider>();
            
            if (detectedSizeProvider != null && detectedSizeProvider != _sizeProvider)
            {
                SetSizeProvider(detectedSizeProvider);
            }
        }
        
        /// <summary>布局组件变化时的处理</summary>
        private void OnLayoutComponentChanged(IScrollLayout changedLayout)
        {
            if (!_componentNotificationListenerEnabled || !IsInitialized) return;
            
            // 检查这个变化的布局是否是我们正在使用的
            if (changedLayout == _layout)
            {
                Refresh();
            }
        }
        
        /// <summary>尺寸提供器组件变化时的处理</summary>
        private void OnSizeProviderComponentChanged(IScrollSizeProvider changedSizeProvider)
        {
            if (!_componentNotificationListenerEnabled || !IsInitialized) return;
            
            // 检查这个变化的尺寸提供器是否是我们正在使用的
            if (changedSizeProvider == _sizeProvider)
            {
                Refresh();
            }
        }
        #endregion

        #region 工厂方法
        /// <summary>创建ScrollView构建器</summary>
        public static ScrollViewBuilder Create(ScrollRect scrollRect)
        {
            return new ScrollViewBuilder(scrollRect);
        }

        /// <summary>创建ScrollView构建器 - 自动查找ScrollRect</summary>
        public static ScrollViewBuilder Create(GameObject gameObject)
        {
            var scrollRect = gameObject.GetComponent<ScrollRect>();
            if (scrollRect == null)
                scrollRect = gameObject.GetComponentInChildren<ScrollRect>();
            
            if (scrollRect == null)
                throw new Exception("ScrollRect not found!");
            
            return new ScrollViewBuilder(scrollRect);
        }
        #endregion

        #region 核心API
        /// <summary>初始化ScrollView（内部使用）</summary>
        internal void Initialize(IScrollAdapter adapter, IScrollLayout layout, IScrollSizeProvider sizeProvider, int poolSize)
        {
            if (_initialized) return;

            _scrollRect = GetComponent<ScrollRect>();
            _content = _scrollRect.content;
            _adapter = adapter;
            
            // 设置管理关系
            if (layout is ScrollLayoutBehaviour layoutBehaviour)
            {
                layoutBehaviour.SetManagedBy(this);
            }
            _layout = layout;
            
            if (sizeProvider is ScrollSizeProviderBehaviour sizeProviderBehaviour)
            {
                sizeProviderBehaviour.SetManagedBy(this);
            }
            _sizeProvider = sizeProvider;

            // 创建控制器
            _controller = new ScrollController(_scrollRect, _content, _layout, _sizeProvider, _adapter, poolSize);
            
            // 绑定事件
            _controller.OnVisibleRangeChanged += (first, last) => OnVisibleRangeChanged?.Invoke(first, last);
            _controller.OnScrollPositionChanged += pos => OnScrollPositionChanged?.Invoke(pos);

            _initialized = true;
        }

        /// <summary>刷新数据</summary>
        public void Refresh()
        {
            if (!IsInitialized) return;
            _controller.Refresh();
        }

        /// <summary>获取当前使用的布局</summary>
        public IScrollLayout GetCurrentLayout()
        {
            return _layout;
        }

        /// <summary>获取当前使用的尺寸提供器</summary>
        public IScrollSizeProvider GetCurrentSizeProvider()
        {
            return _sizeProvider;
        }

        /// <summary>设置布局（组件独立工作时使用）</summary>
        public void SetLayout(IScrollLayout layout)
        {
            if (layout == null) return;
            
            // 取消旧布局管理
            if (_layout is ScrollLayoutBehaviour oldLayoutBehaviour)
            {
                oldLayoutBehaviour.SetUnmanaged();
            }
            
            _layout = layout;
            
            // 设置新布局管理
            if (_layout is ScrollLayoutBehaviour newLayoutBehaviour)
            {
                newLayoutBehaviour.SetManagedBy(this);
            }
            
            if (IsInitialized)
            {
                // 重新创建控制器以使用新布局
                RecreateController();
            }
        }

        /// <summary>设置尺寸提供器（组件独立工作时使用）</summary>
        public void SetSizeProvider(IScrollSizeProvider sizeProvider)
        {
            if (sizeProvider == null) return;
            
            // 取消旧尺寸提供器管理
            if (_sizeProvider is ScrollSizeProviderBehaviour oldSizeProviderBehaviour)
            {
                oldSizeProviderBehaviour.SetUnmanaged();
            }
            
            _sizeProvider = sizeProvider;
            
            // 设置新尺寸提供器管理
            if (_sizeProvider is ScrollSizeProviderBehaviour newSizeProviderBehaviour)
            {
                newSizeProviderBehaviour.SetManagedBy(this);
            }
            
            if (IsInitialized)
            {
                // 重新创建控制器以使用新尺寸提供器
                RecreateController();
            }
        }

        /// <summary>尝试自动初始化（当组件独立添加时使用）</summary>
        public void TryAutoInitialize()
        {
            if (_initialized) return;

            // 启用组件自动发现
            EnableAutoComponentDetection();

            // 检查是否有足够的组件来初始化
            if (_layout == null)
            {
                _layout = GetComponent<IScrollLayout>() ?? GetComponentInChildren<IScrollLayout>();
            }

            if (_sizeProvider == null)
            {
                _sizeProvider = GetComponent<IScrollSizeProvider>() ?? GetComponentInChildren<IScrollSizeProvider>();
            }

            // 如果有适配器和基本组件，尝试初始化
            if (_adapter != null && _layout != null && _sizeProvider != null)
            {
                Initialize(_adapter, _layout, _sizeProvider, 20); // 使用默认池大小
            }
        }

        /// <summary>重新创建控制器</summary>
        private void RecreateController()
        {
            if (!_initialized || _adapter == null || _layout == null || _sizeProvider == null) return;

            // 保存当前滚动位置
            var currentPosition = _scrollRect.normalizedPosition;

            // 销毁旧控制器
            _controller?.Dispose();

            // 创建新控制器
            _controller = new ScrollController(_scrollRect, _content, _layout, _sizeProvider, _adapter, 20);
            
            // 绑定事件
            _controller.OnVisibleRangeChanged += (first, last) => OnVisibleRangeChanged?.Invoke(first, last);
            _controller.OnScrollPositionChanged += pos => OnScrollPositionChanged?.Invoke(pos);

            // 恢复滚动位置
            _scrollRect.normalizedPosition = currentPosition;
        }

        /// <summary>滚动到指定索引</summary>
        public void ScrollToIndex(int index, bool immediate = false)
        {
            if (!IsInitialized) return;
            _controller.ScrollToIndex(index, immediate);
        }

        /// <summary>滚动到顶部</summary>
        public void ScrollToTop(bool immediate = false)
        {
            if (!IsInitialized) return;
            _controller.ScrollToTop(immediate);
        }

        /// <summary>滚动到底部</summary>
        public void ScrollToBottom(bool immediate = false)
        {
            if (!IsInitialized) return;
            _controller.ScrollToBottom(immediate);
        }
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            // 自动启用组件发现机制
            EnableAutoComponentDetection();
        }
        
        private void OnDestroy()
        {
            // 清理管理关系
            if (_layout is ScrollLayoutBehaviour layoutBehaviour)
            {
                layoutBehaviour.SetUnmanaged();
            }
            if (_sizeProvider is ScrollSizeProviderBehaviour sizeProviderBehaviour)
            {
                sizeProviderBehaviour.SetUnmanaged();
            }
            
            DisableAutoComponentDetection();
            _controller?.Dispose();
        }
        #endregion
    }

    /// <summary>
    /// 简单数据适配器实现
    /// </summary>
    public class SimpleScrollAdapter<T> : IScrollAdapter
    {
        private readonly IList<T> _data;
        private readonly RectTransform _cellPrefab;
        private readonly Action<int, RectTransform, T> _onBind;
        private readonly Action<RectTransform> _onCellCreated;
        private readonly Action<int, RectTransform> _onCellRecycled;

        public int Count => _data?.Count ?? 0;

        public SimpleScrollAdapter(
            IList<T> data,
            RectTransform cellPrefab,
            Action<int, RectTransform, T> onBind,
            Action<RectTransform> onCellCreated = null,
            Action<int, RectTransform> onCellRecycled = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _cellPrefab = cellPrefab != null ? cellPrefab : throw new ArgumentNullException(nameof(cellPrefab));
            _onBind = onBind ?? throw new ArgumentNullException(nameof(onBind));
            _onCellCreated = onCellCreated;
            _onCellRecycled = onCellRecycled;
        }

        public RectTransform GetCellPrefab() => _cellPrefab;

        public void BindCell(int index, RectTransform cell)
        {
            if (index >= 0 && index < Count)
            {
                _onBind(index, cell, _data[index]);
            }
        }

        public void OnCellCreated(RectTransform cell)
        {
            _onCellCreated?.Invoke(cell);
        }

        public void OnCellRecycled(int index, RectTransform cell)
        {
            _onCellRecycled?.Invoke(index, cell);
        }
    }

    /// <summary>
    /// ScrollView构建器 - 提供链式API
    /// </summary>
    public class ScrollViewBuilder
    {
        private readonly ScrollRect _scrollRect;
        private IScrollAdapter _adapter;
        private IScrollLayout _layout;
        private IScrollSizeProvider _sizeProvider;
        private int _poolSize = 20;

        internal ScrollViewBuilder(ScrollRect scrollRect)
        {
            _scrollRect = scrollRect != null ? scrollRect : throw new ArgumentNullException(nameof(scrollRect));
        }

        /// <summary>设置数据和绑定</summary>
        public ScrollViewBuilder SetData<T>(
            IList<T> data,
            RectTransform cellPrefab,
            Action<int, RectTransform, T> onBind,
            Action<RectTransform> onCellCreated = null,
            Action<int, RectTransform> onCellRecycled = null)
        {
            _adapter = new SimpleScrollAdapter<T>(data, cellPrefab, onBind, onCellCreated, onCellRecycled);
            return this;
        }

        /// <summary>设置自定义适配器</summary>
        public ScrollViewBuilder SetAdapter(IScrollAdapter adapter)
        {
            _adapter = adapter;
            return this;
        }

        /// <summary>设置纵向布局</summary>
        public ScrollViewBuilder SetVerticalLayout(float spacing = 0f, RectOffset padding = null)
        {
            var layout = _scrollRect.gameObject.AddComponent<VerticalScrollLayout>();
            layout.Spacing = spacing;
            layout.Padding = padding ?? new RectOffset();
            _layout = layout;
            return this;
        }

        /// <summary>设置横向布局</summary>
        public ScrollViewBuilder SetHorizontalLayout(float spacing = 0f, RectOffset padding = null)
        {
            var layout = _scrollRect.gameObject.AddComponent<HorizontalScrollLayout>();
            layout.Spacing = spacing;
            layout.Padding = padding ?? new RectOffset();
            _layout = layout;
            return this;
        }

        /// <summary>设置网格布局</summary>
        public ScrollViewBuilder SetGridLayout(Vector2 cellSize, int constraintCount, float spacing = 0f, RectOffset padding = null, GridAxis axis = GridAxis.Vertical)
        {
            var layout = _scrollRect.gameObject.AddComponent<GridScrollLayout>();
            layout.CellSize = cellSize;
            layout.ConstraintCount = constraintCount;
            layout.Spacing = spacing;
            layout.Padding = padding ?? new RectOffset();
            layout.Axis = axis;
            _layout = layout;
            return this;
        }

        /// <summary>自动检测并使用可视化布局组件</summary>
        public ScrollViewBuilder SetLayoutFromBehaviour()
        {
            var layoutBehaviour = _scrollRect.GetComponent<ScrollLayoutBehaviour>();
            if (layoutBehaviour != null)
            {
                _layout = layoutBehaviour;
            }
            return this;
        }

        /// <summary>设置自定义布局</summary>
        public ScrollViewBuilder SetLayout(IScrollLayout layout)
        {
            _layout = layout;
            return this;
        }

        /// <summary>设置固定尺寸</summary>
        public ScrollViewBuilder SetFixedSize(Vector2 fixedSize)
        {
            var provider = _scrollRect.gameObject.AddComponent<FixedSizeProviderBehaviour>();
            provider.FixedSize = fixedSize;
            _sizeProvider = provider;
            return this;
        }

        /// <summary>设置自适应宽度</summary>
        public ScrollViewBuilder SetFitWidth(float fixedHeight, float widthPadding = 0f)
        {
            var provider = _scrollRect.gameObject.AddComponent<FitWidthSizeProviderBehaviour>();
            provider.FixedHeight = fixedHeight;
            provider.WidthPadding = widthPadding;
            _sizeProvider = provider;
            return this;
        }

        /// <summary>设置自适应高度</summary>
        public ScrollViewBuilder SetFitHeight(float fixedWidth, float heightPadding = 0f)
        {
            var provider = _scrollRect.gameObject.AddComponent<FitHeightSizeProviderBehaviour>();
            provider.FixedWidth = fixedWidth;
            provider.HeightPadding = heightPadding;
            _sizeProvider = provider;
            return this;
        }

        /// <summary>设置动态尺寸</summary>
        public ScrollViewBuilder SetDynamicSize(Func<int, Vector2, Vector2> sizeCalculator, Vector2 defaultSize, int maxCacheSize = 1000)
        {
            var provider = _scrollRect.gameObject.AddComponent<DynamicSizeProviderBehaviour>();
            provider.DefaultSize = defaultSize;
            provider.MaxCacheSize = maxCacheSize;
            provider.SetSizeCalculator(sizeCalculator);
            _sizeProvider = provider;
            return this;
        }

        /// <summary>设置自定义尺寸提供器</summary>
        public ScrollViewBuilder SetSizeProvider(IScrollSizeProvider sizeProvider)
        {
            _sizeProvider = sizeProvider;
            return this;
        }

        /// <summary>自动检测并使用可视化尺寸提供器组件</summary>
        public ScrollViewBuilder SetSizeProviderFromBehaviour()
        {
            var sizeProviderBehaviour = _scrollRect.GetComponent<ScrollSizeProviderBehaviour>();
            if (sizeProviderBehaviour != null)
            {
                _sizeProvider = sizeProviderBehaviour;
            }
            return this;
        }

        /// <summary>设置对象池大小</summary>
        public ScrollViewBuilder SetPoolSize(int poolSize)
        {
            _poolSize = poolSize;
            return this;
        }

        /// <summary>构建ScrollView</summary>
        public ScrollView Build()
        {
            if (_adapter == null) throw new ArgumentException("Adapter is required");

            // 自动检测可视化组件
            if (_layout == null)
            {
                var layoutBehaviour = _scrollRect.GetComponent<ScrollLayoutBehaviour>();
                if (layoutBehaviour != null)
                {
                    _layout = layoutBehaviour;
                }
                else
                {
                    // 创建默认的纵向布局
                    var defaultLayout = _scrollRect.gameObject.AddComponent<VerticalScrollLayout>();
                    _layout = defaultLayout;
                }
            }

            if (_sizeProvider == null)
            {
                var sizeProviderBehaviour = _scrollRect.GetComponent<ScrollSizeProviderBehaviour>();
                if (sizeProviderBehaviour != null)
                {
                    _sizeProvider = sizeProviderBehaviour;
                }
                else
                {
                    // 创建默认的自适应宽度提供器
                    var defaultProvider = _scrollRect.gameObject.AddComponent<FitWidthSizeProviderBehaviour>();
                    defaultProvider.FixedHeight = 60f;
                    defaultProvider.WidthPadding = 10f;
                    _sizeProvider = defaultProvider;
                }
            }

            // 获取或添加ScrollView组件
            var scrollView = _scrollRect.GetComponent<ScrollView>();
            if (scrollView == null)
                scrollView = _scrollRect.gameObject.AddComponent<ScrollView>();

            // 初始化
            scrollView.Initialize(_adapter, _layout, _sizeProvider, _poolSize);

            return scrollView;
        }
    }
}