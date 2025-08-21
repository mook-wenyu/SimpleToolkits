namespace SimpleToolkits
{
    using UnityEngine;
    using UnityEngine.UI;
    using System;
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// 高性能可无限滚动的 ScrollView 外观组件
    /// - 支持纵向/横向/网格布局（通过 IScrollLayout 策略）
    /// - 复用单元格，按需创建，超出视区自动回收
    /// - 兼容 LayoutElement/ContentSizeFitter（单元格内部自行决定布局）
    /// - 建议固定主轴尺寸以获得最佳性能，可选尺寸提供器支持变高/变宽
    /// - 协程基于 UniTask
    ///
    /// 用法：
    /// 1) 将本组件挂到包含 ScrollRect 的同级 GameObject
    /// 2) 在 Content 上添加布局组件（VerticalLayout、HorizontalLayout 或 GridLayout）
    /// 3) 调用 Initialize(adapter) 初始化
    /// 4) 调用 Refresh() 或 SetItemCount() 刷新数据
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public sealed class ScrollView : MonoBehaviour
    {
        [Header("必需组件")]
        [SerializeField] private ScrollRect _scrollRect; // 允许在 Inspector 直接绑定，减少运行时查找

        [Header("可选：内容容器，留空则使用 ScrollRect.content")]
        [SerializeField] private RectTransform _content; // 允许在 Inspector 直接绑定

        // 适配器与布局策略
        private IScrollAdapter _adapter;
        private IScrollLayout _layout;

        // 控制器（内部负责复用/定位/计算）
        private Internal.ScrollController _controller;

        // 初始化状态
        private bool _initialized;

        /// <summary>
        /// 是否已初始化。
        /// </summary>
        public bool Initialized => _initialized;

        // 当前可见范围缓存（闭区间），当无可见项时为 -1,-1
        private int _visibleFirst = -1;
        private int _visibleLast = -1;

        /// <summary>
        /// 可见范围变化事件（first,last 为闭区间）。
        /// </summary>
        public event Action<int, int> OnVisibleRangeChanged;

        /// <summary>
        /// 初始化滚动视图。必须在使用前调用一次。
        /// </summary>
        public void Initialize(IScrollAdapter adapter)
        {
            // 参数检查
            if (_scrollRect == null)
            {
                _scrollRect = GetComponentInChildren<ScrollRect>(true);
            }

            if (_scrollRect == null)
            {
                Debug.LogError("[ScrollView] 缺少 ScrollRect 组件");
                return;
            }

            _content = _content != null ? _content : _scrollRect.content;
            if (_content == null)
            {
                Debug.LogError("[ScrollView] 缺少 Content 容器(RectTransform)");
                return;
            }

            _adapter = adapter;
            _layout = _content.GetComponent<IScrollLayout>();
            if (_layout == null)
            {
                Debug.LogError("[ScrollView] Content 上未找到 IScrollLayout 组件，请添加 VerticalLayout、HorizontalLayout 或 GridLayout 组件");
                return;
            }

            // 如果 Content 上仍有启用的 Unity LayoutGroup，则禁用以避免与手动定位冲突
            var hasAnyLayoutGroup = _content.GetComponent<HorizontalLayoutGroup>() != null
                                    || _content.GetComponent<VerticalLayoutGroup>() != null
                                    || _content.GetComponent<GridLayoutGroup>() != null;
            if (hasAnyLayoutGroup)
            {
                Debug.LogWarning("[ScrollView] 检测到 Content 上存在 Unity LayoutGroup，虚拟化运行期将禁用这些组件以避免冲突。");
                var v = _content.GetComponent<VerticalLayoutGroup>();
                if (v != null) v.enabled = false;
                var h = _content.GetComponent<HorizontalLayoutGroup>();
                if (h != null) h.enabled = false;
                var g = _content.GetComponent<GridLayoutGroup>();
                if (g != null) g.enabled = false;
            }

            // ScrollRect 方向设置
            _scrollRect.horizontal = !_layout.IsVertical;
            _scrollRect.vertical = _layout.IsVertical;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // 创建控制器并桥接可见范围变化事件
            _controller = new Internal.ScrollController(
                _scrollRect,
                _content,
                _adapter,
                _layout,
                (first, last) =>
                {
                    _visibleFirst = first;
                    _visibleLast = last;
                    OnVisibleRangeChanged?.Invoke(first, last);
                });

            // 监听滚动与尺寸变化
            _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);

            _initialized = true;

            // 初次布局
            Refresh(true);
        }


        //================ 便捷初始化方法 ================
        /// <summary>
        /// 便捷初始化（固定数量）。
        /// </summary>
        public void Initialize(RectTransform prefab, int count, Action<int, RectTransform> bind)
        {
            if (prefab == null || bind == null)
            {
                Debug.LogError("[ScrollView] Initialize 参数无效");
                return;
            }
            Initialize(new SimpleAdapter(prefab, () => count, bind));
        }

        /// <summary>
        /// 便捷初始化（动态数量委托）。
        /// </summary>
        public void Initialize(RectTransform prefab, Func<int> countGetter, Action<int, RectTransform> bind)
        {
            if (prefab == null || countGetter == null || bind == null)
            {
                Debug.LogError("[ScrollView] Initialize 参数无效");
                return;
            }
            Initialize(new SimpleAdapter(prefab, countGetter, bind));
        }



        /// <summary>
        /// 便捷初始化（变尺寸：固定数量）。仅在单列/单行布局有效。
        /// </summary>
        public void InitializeVariable(RectTransform prefab, int count, Action<int, RectTransform> bind,
            Func<int, Vector2, IScrollLayout, Vector2> sizeGetter, Vector2 fallbackSize = default)
        {
            if (prefab == null || bind == null || sizeGetter == null)
            {
                Debug.LogError("[ScrollView] InitializeVariable 参数无效");
                return;
            }
            Initialize(new SimpleVarAdapter(prefab, () => count, bind, sizeGetter, fallbackSize));
        }

        /// <summary>
        /// 便捷初始化（变尺寸：动态数量）。仅在单列/单行布局有效。
        /// </summary>
        public void InitializeVariable(RectTransform prefab, Func<int> countGetter, Action<int, RectTransform> bind,
            Func<int, Vector2, IScrollLayout, Vector2> sizeGetter, Vector2 fallbackSize = default)
        {
            if (prefab == null || countGetter == null || bind == null || sizeGetter == null)
            {
                Debug.LogError("[ScrollView] InitializeVariable 参数无效");
                return;
            }
            Initialize(new SimpleVarAdapter(prefab, countGetter, bind, sizeGetter, fallbackSize));
        }

        /// <summary>
        /// 获取当前可见范围（闭区间）。当无可见项时返回 (-1,-1)。
        /// </summary>
        public (int first, int last) GetVisibleRange() => (_visibleFirst, _visibleLast);

        /// <summary>
        /// 当前可见项数量（无可见项时为 0）。
        /// </summary>
        public int VisibleCount => (_visibleFirst >= 0 && _visibleLast >= _visibleFirst) ? (_visibleLast - _visibleFirst + 1) : 0;

        /// <summary>
        /// 设置数据总量（通过适配器返回的 Count 优先）。
        /// </summary>
        public void SetItemCount(int count, bool keepPosition = false)
        {
            if (!_initialized) return;
            _adapter.OverrideCount(count);
            Refresh(!keepPosition);
        }

        /// <summary>
        /// 强制刷新：重新计算内容尺寸并重建可视单元格。
        /// </summary>
        public void Refresh(bool resetPosition = false)
        {
            if (!_initialized) return;
            _controller.Rebuild(resetPosition);
        }

        /// <summary>
        /// 失效所有项尺寸并重建（用于变尺寸模式下，运行期项尺寸发生变化）。
        /// </summary>
        /// <param name="keepPosition">是否保留当前滚动位置</param>
        public void InvalidateAllSizes(bool keepPosition = true)
        {
            if (!_initialized) return;
            _controller.Rebuild(!keepPosition);
        }

        /// <summary>
        /// 滚动到指定索引。
        /// </summary>
        /// <param name="index">目标索引</param>
        /// <param name="align01">0=开始对齐，0.5=居中，1=末端对齐</param>
        /// <param name="animated">是否平滑滚动</param>
        /// <param name="duration">动画时长（秒）</param>
        public async UniTask ScrollTo(int index, float align01 = 0f, bool animated = true, float duration = 0.25f)
        {
            if (!_initialized) return;
            var target = _controller.CalculateScrollPositionFor(index, align01);

            if (!animated || duration <= 0f)
            {
                _controller.SetNormalizedPosition(target);
                _controller.ForceUpdate();
                return;
            }

            // 简单插值动画（基于 UniTask）
            float t = 0f;
            var start = _controller.GetNormalizedPosition();
            while (t < 1f)
            {
                t += Mathf.Clamp01(Time.unscaledDeltaTime / duration);
                var v = Mathf.SmoothStep(0f, 1f, t);
                var cur = Mathf.Lerp(start, target, v);
                _controller.SetNormalizedPosition(cur);
                await UniTask.Yield();
            }

            _controller.SetNormalizedPosition(target);
            _controller.ForceUpdate();
        }

        /// <summary>
        /// 在外部布局或屏幕尺寸变化时调用，以触发重新布局。
        /// </summary>
        public void NotifyViewportOrLayoutChanged(bool keepPosition = true)
        {
            if (!_initialized) return;
            _controller.Rebuild(!keepPosition);
        }


        /// <summary>
        /// 滚动到起始位置（顶部/最左）。
        /// </summary>
        public void ScrollToTop()
        {
            if (!_initialized) return;
            _controller.SetNormalizedPosition(_layout.IsVertical ? 1f : 0f);
            _controller.ForceUpdate();
        }

        /// <summary>
        /// 滚动到末尾位置（底部/最右）。
        /// </summary>
        public void ScrollToBottom()
        {
            if (!_initialized) return;
            _controller.SetNormalizedPosition(_layout.IsVertical ? 0f : 1f);
            _controller.ForceUpdate();
        }

        /// <summary>
        /// 尝试获取某索引对应的激活中 Cell（仅当其在可见范围内）。
        /// </summary>
        public bool TryGetActiveCell(int index, out RectTransform cell)
        {
            cell = null;
            if (!_initialized) return false;
            return _controller.TryGetActiveCell(index, out cell);
        }

        /// <summary>
        /// 重新绑定某个索引（当业务数据变更且该项可见时）。
        /// </summary>
        public void RebindItem(int index)
        {
            if (!_initialized) return;
            _controller.RebindIndex(index);
        }

        /// <summary>
        /// 遍历当前可见的所有项。
        /// </summary>
        public void ForEachVisible(Action<int, RectTransform> action)
        {
            if (!_initialized) return;
            _controller.ForEachVisible(action);
        }

        private void OnScrollValueChanged(Vector2 _)
        {
            if (!_initialized) return;
            _controller.OnScroll();
        }

        private void OnEnable()
        {
            if (_initialized) _controller.ForceUpdate();
        }

        private void OnDisable()
        {
            // 暂不做特殊处理，保留位置与池
        }

        private void OnRectTransformDimensionsChange()
        {
            // 当视口或父级尺寸变化时，自动触发重建以适配（保持当前位置）
            if (_initialized && gameObject.activeInHierarchy)
            {
                _controller.Rebuild(resetPosition: false);
            }
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
                _controller.Dispose();
                _controller = null;
            }
            _initialized = false;
        }

        //================ 内部适配器类 ================
        /// <summary>
        /// 简单适配器：统一尺寸。
        /// </summary>
        private sealed class SimpleAdapter : IScrollAdapter
        {
            private readonly RectTransform _prefab;
            private readonly Func<int> _countGetter;
            private readonly Action<int, RectTransform> _bind;
            private int _override = -1;

            public SimpleAdapter(RectTransform prefab, Func<int> countGetter, Action<int, RectTransform> bind)
            {
                _prefab = prefab;
                _countGetter = countGetter;
                _bind = bind;
            }

            public int Count => _override >= 0 ? _override : (_countGetter != null ? _countGetter() : 0);
            public void OverrideCount(int count) => _override = count;
            public RectTransform GetCellPrefab() => _prefab;
            public void BindCell(int index, RectTransform cell) => _bind?.Invoke(index, cell);
        }

        /// <summary>
        /// 简单适配器：变尺寸。
        /// </summary>
        private sealed class SimpleVarAdapter : IScrollAdapter, IVariableSizeAdapter
        {
            private readonly SimpleAdapter _base;
            private readonly Func<int, Vector2, IScrollLayout, Vector2> _sizeGetter;
            private readonly Vector2 _fallback;

            public SimpleVarAdapter(RectTransform prefab, Func<int> countGetter, Action<int, RectTransform> bind,
                Func<int, Vector2, IScrollLayout, Vector2> sizeGetter, Vector2 fallback)
            {
                _base = new SimpleAdapter(prefab, countGetter, bind);
                _sizeGetter = sizeGetter;
                _fallback = fallback;
            }

            public int Count => _base.Count;
            public void OverrideCount(int count) => _base.OverrideCount(count);
            public RectTransform GetCellPrefab() => _base.GetCellPrefab();
            public void BindCell(int index, RectTransform cell) => _base.BindCell(index, cell);

            public Vector2 GetItemSize(int index, Vector2 viewportSize, IScrollLayout layout)
            {
                if (_sizeGetter == null) return _fallback;
                return _sizeGetter(index, viewportSize, layout);
            }
        }
    }
}
