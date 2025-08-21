namespace SimpleToolkits.Internal
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 内部滚动控制器：负责计算可见范围、复用 Cell、定位与滚动。
    /// 约定：使用统一单元格尺寸（由预制体测量），确保高性能。
    /// </summary>
    internal sealed class ScrollController : IDisposable
    {
        private readonly ScrollRect _scrollRect;
        private readonly RectTransform _content;
        private readonly IScrollAdapter _adapter;
        private readonly IScrollLayout _layout;
        private readonly Action<int, int> _onVisibleRangeChanged; // 可选回调：可见范围变化
        private readonly IRecyclableScrollAdapter _recyclable;    // 可选回收/创建回调

        // 缓存
        private RectTransform _cellPrefab;
        private Vector2 _cellSize;     // 统一单元格尺寸
        private Vector2 _viewportSize; // 视口尺寸
        private Vector2 _contentSize;  // 内容尺寸

        // 变高/变宽虚拟化支持
        private IVariableSizeAdapter _varAdapter; // 可选
        private bool _useVariable;                // 仅当实现接口且 ConstraintCount==1 时启用
        private Vector2[] _itemSizes;             // 每项尺寸缓存
        private float[] _starts;                  // 主轴起点（包含 padding 与间距）
        private float[] _ends;                    // 主轴终点 = start + mainSize

        // 复用池与活动单元
        private readonly Stack<RectTransform> _pool = new(64);
        private readonly Dictionary<int, RectTransform> _active = new(128);
        private int _first = -1; // 当前可见范围
        private int _last = -1;

        internal ScrollController(ScrollRect scrollRect, RectTransform content, IScrollAdapter adapter, IScrollLayout layout, Action<int, int> onVisibleRangeChanged = null)
        {
            _scrollRect = scrollRect;
            _content = content;
            _adapter = adapter;
            _layout = layout;
            _varAdapter = adapter as IVariableSizeAdapter;
            _useVariable = _varAdapter != null && layout != null && layout.ConstraintCount == 1; // 列表场景才支持变高/变宽
            _onVisibleRangeChanged = onVisibleRangeChanged;
            _recyclable = adapter as IRecyclableScrollAdapter;
        }

        /// <summary>
        /// 重建布局与可见项。
        /// </summary>
        internal void Rebuild(bool resetPosition)
        {
            // 设置布局锚点
            var viewport = _scrollRect.viewport != null ? _scrollRect.viewport : _scrollRect.GetComponent<RectTransform>();
            _layout.Setup(viewport, _content);

            // 测量视口与单元格尺寸
            _viewportSize = viewport.rect.size;
            _cellPrefab = _cellPrefab ?? _adapter.GetCellPrefab();
            _cellSize = MeasureCellSize(_cellPrefab);

            // 计算 Content 尺寸
            if (_useVariable)
            {
                BuildVariableCaches();
            }
            else
            {
                _contentSize = _layout.ComputeContentSize(_adapter.Count, _cellSize, _viewportSize);
            }
            SetContentSize(_content, _contentSize);

            if (resetPosition)
            {
                // 纵向回到顶部，横向回到最左
                SetNormalizedPosition(_layout.IsVertical ? 1f : 0f);
            }

            // 立即刷新可见项
            UpdateVisibleImmediate();
        }

        /// <summary>
        /// 滚动变化时调用。
        /// </summary>
        internal void OnScroll()
        {
            UpdateVisibleImmediate();
        }

        /// <summary>
        /// 强制刷新一次（用于启用时或动画后）。
        /// </summary>
        internal void ForceUpdate()
        {
            UpdateVisibleImmediate();
        }

        /// <summary>
        /// 计算使索引 index 与视口指定对齐的 normalizedPosition。
        /// align01: 0=开始对齐(上/左), 0.5=居中, 1=末端对齐(下/右)
        /// </summary>
        internal float CalculateScrollPositionFor(int index, float align01)
        {
            index = Mathf.Clamp(index, 0, Mathf.Max(0, _adapter.Count - 1));
            if (_adapter.Count <= 0) return GetNormalizedPosition();

            // 目标 item 的锚点位置
            Vector2 itemPos;
            Vector2 itemSize;
            if (_useVariable)
            {
                GetItemPositionAndSizeVariable(index, out itemPos, out itemSize);
            }
            else
            {
                itemPos = _layout.GetItemAnchoredPosition(index, _adapter.Count, _cellSize);
                itemSize = _cellSize;
            }

            float contentScrollable;
            float targetOffset; // 相对于起点(上/左)的偏移
            if (_layout.IsVertical)
            {
                // 顶部为 0 偏移（Content 以顶部为 pivot），itemPos.y 为负数
                var itemTop = -itemPos.y; // 距离内容顶部
                var alignOffset = align01 * (_viewportSize.y - itemSize.y);
                targetOffset = Mathf.Clamp(itemTop - alignOffset, 0f, Mathf.Max(0f, _contentSize.y - _viewportSize.y));
                contentScrollable = Mathf.Max(0, _contentSize.y - _viewportSize.y);
            }
            else
            {
                var itemLeft = itemPos.x; // 距离内容左侧
                var alignOffset = align01 * (_viewportSize.x - itemSize.x);
                targetOffset = Mathf.Clamp(itemLeft - alignOffset, 0f, Mathf.Max(0f, _contentSize.x - _viewportSize.x));
                contentScrollable = Mathf.Max(0, _contentSize.x - _viewportSize.x);
            }

            if (contentScrollable <= 0f) return GetNormalizedPosition();
            // 注意：verticalNormalizedPosition 1=顶部、0=底部
            if (_layout.IsVertical)
                return 1f - Mathf.Clamp01(targetOffset / contentScrollable);
            else
                return Mathf.Clamp01(targetOffset / contentScrollable);
        }

        internal float GetNormalizedPosition()
        {
            return _layout.IsVertical ? _scrollRect.verticalNormalizedPosition : _scrollRect.horizontalNormalizedPosition;
        }

        internal void SetNormalizedPosition(float v)
        {
            v = Mathf.Clamp01(v);
            if (_layout.IsVertical)
                _scrollRect.verticalNormalizedPosition = v;
            else
                _scrollRect.horizontalNormalizedPosition = v;
        }

        /// <summary>
        /// 释放所有资源与实例。
        /// </summary>
        public void Dispose()
        {
            foreach (var kv in _active)
            {
                if (kv.Value != null)
                    UnityEngine.Object.Destroy(kv.Value.gameObject);
            }
            _active.Clear();

            while (_pool.Count > 0)
            {
                var rt = _pool.Pop();
                if (rt != null) UnityEngine.Object.Destroy(rt.gameObject);
            }
        }

        private void UpdateVisibleImmediate()
        {
            var count = _adapter.Count;
            if (count <= 0)
            {
                RecycleAll();
                _first = _last = -1;
                return;
            }

            // 计算新的可见范围
            var norm = GetNormalizedPosition();
            int nFirst, nLast;
            if (_useVariable)
            {
                GetVisibleRangeVariable(norm, out nFirst, out nLast);
            }
            else
            {
                _layout.GetVisibleRange(norm, count, _viewportSize, _cellSize, out nFirst, out nLast);
            }
            if (nFirst < 0 || nLast < 0 || nFirst > nLast)
            {
                RecycleAll();
                _first = _last = -1;
                return;
            }

            // 回收不再可见的项
            if (_first != -1)
            {
                var toRecycle = ListCache<int>.Get();
                foreach (var kv in _active)
                {
                    var idx = kv.Key;
                    if (idx < nFirst || idx > nLast)
                        toRecycle.Add(idx);
                }
                foreach (var t in toRecycle)
                {
                    RecycleIndex(t);
                }
                ListCache<int>.Release(toRecycle);
            }

            // 激活需要的项
            for (var i = nFirst; i <= nLast; i++)
            {
                if (_active.ContainsKey(i))
                {
                    // 已存在，仅确保位置正确
                    var cell = _active[i];
                    PositionCell(i, cell);
                }
                else
                {
                    var cell = GetOrCreateCell();
                    _active[i] = cell;
                    PositionCell(i, cell);
                    SafeBind(i, cell);
                }
            }

            var changed = (_first != nFirst) || (_last != nLast);
            _first = nFirst;
            _last = nLast;
            if (changed && _onVisibleRangeChanged != null)
            {
                _onVisibleRangeChanged(_first, _last);
            }
        }

        private void SafeBind(int index, RectTransform cell)
        {
            try
            {
                _adapter.BindCell(index, cell);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void PositionCell(int index, RectTransform cell)
        {
            Vector2 pos;
            Vector2 itemSize2;
            if (_useVariable)
            {
                GetItemPositionAndSizeVariable(index, out pos, out itemSize2);
            }
            else
            {
                pos = _layout.GetItemAnchoredPosition(index, _adapter.Count, _cellSize);
                itemSize2 = _cellSize;
            }
            cell.anchoredPosition = pos;
            // 调整 sizeDelta，保证跨轴填充（可选）
            var size = cell.sizeDelta;
            if (_layout.IsVertical)
            {
                // 主轴：高度使用 per-item 或统一尺寸
                size.y = itemSize2.y;
                // 跨轴：根据标志决定是否填充内容宽度
                if (_layout.ControlChildWidth)
                {
                    var contentWidth = Mathf.Max(0f, _contentSize.x - _layout.Padding.left - _layout.Padding.right);
                    size.x = contentWidth;
                }
                else
                {
                    size.x = itemSize2.x; // 使用 per-item/测量宽度
                }
            }
            else
            {
                // 主轴：宽度使用 per-item 或统一尺寸
                size.x = itemSize2.x;
                // 跨轴：根据标志决定是否填充内容高度
                if (_layout.ControlChildHeight)
                {
                    var contentHeight = Mathf.Max(0f, _contentSize.y - _layout.Padding.top - _layout.Padding.bottom);
                    size.y = contentHeight;
                }
                else
                {
                    size.y = itemSize2.y; // 使用 per-item/测量高度
                }
            }
            cell.sizeDelta = size;
        }

        private RectTransform GetOrCreateCell()
        {
            var rt = _pool.Count > 0 ? _pool.Pop() : CreateCell();
            rt.gameObject.SetActive(true);
            return rt;
        }

        private RectTransform CreateCell()
        {
            var go = UnityEngine.Object.Instantiate(_cellPrefab.gameObject, _content);
            var rt = go.GetComponent<RectTransform>();
            // 为稳定性，强制设定 sizeDelta 为测量尺寸（内部可再被 LayoutElement/ContentSizeFitter 覆盖）
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            // 初始 size 先用测量尺寸，后续 PositionCell 会按跨轴策略拉伸
            rt.sizeDelta = _cellSize;
            // 生命周期：首次实例化回调
            try { _recyclable?.OnCellCreated(rt); } catch (Exception ex) { Debug.LogException(ex); }
            return rt;
        }

        private void RecycleIndex(int index)
        {
            if (_active.TryGetValue(index, out var rt))
            {
                _active.Remove(index);
                // 生命周期：回收回调
                try { _recyclable?.OnCellRecycled(index, rt); } catch (Exception ex) { Debug.LogException(ex); }
                rt.gameObject.SetActive(false);
                _pool.Push(rt);
            }
        }

        private void RecycleAll()
        {
            if (_active.Count == 0) return;
            foreach (var kv in _active)
            {
                var rt = kv.Value;
                if (rt == null) continue;
                // 尽最大努力通知回收（索引已知）
                try { _recyclable?.OnCellRecycled(kv.Key, rt); } catch (Exception ex) { Debug.LogException(ex); }
                rt.gameObject.SetActive(false);
                _pool.Push(rt);
            }
            _active.Clear();
        }

        //================ 外部查询/操作 ================
        internal bool TryGetActiveCell(int index, out RectTransform cell)
        {
            return _active.TryGetValue(index, out cell);
        }

        internal void RebindIndex(int index)
        {
            if (_active.TryGetValue(index, out var cell))
            {
                SafeBind(index, cell);
            }
        }

        internal void ForEachVisible(Action<int, RectTransform> action)
        {
            if (action == null) return;
            foreach (var kv in _active)
            {
                try { action(kv.Key, kv.Value); } catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        private static void SetContentSize(RectTransform content, Vector2 size)
        {
            // Content 锚点已由布局设置，这里根据锚点是否拉伸来设置 sizeDelta：
            // - 如果该轴锚点为 0..1（拉伸），sizeDelta 表示相对父级的“差值”，通常应为 0
            // - 如果非拉伸，则 sizeDelta 表示绝对尺寸
            var sd = content.sizeDelta;

            // 需要父级（视口）尺寸来计算拉伸时的差值
            var parentRT = content.parent as RectTransform;
            var parentSize = parentRT != null ? parentRT.rect.size : Vector2.zero;

            // 水平轴处理
            if (Mathf.Approximately(content.anchorMin.x, 0f) && Mathf.Approximately(content.anchorMax.x, 1f))
            {
                // 拉伸：sizeDelta.x 为与父级宽度的差值，使内容可超出视口
                sd.x = size.x - parentSize.x;
            }
            else
            {
                sd.x = size.x; // 非拉伸：绝对宽度
            }

            // 垂直轴处理
            if (Mathf.Approximately(content.anchorMin.y, 0f) && Mathf.Approximately(content.anchorMax.y, 1f))
            {
                // 拉伸：sizeDelta.y 为与父级高度的差值
                sd.y = size.y - parentSize.y;
            }
            else
            {
                sd.y = size.y; // 非拉伸：绝对高度
            }

            content.sizeDelta = sd;
        }

        private static Vector2 MeasureCellSize(RectTransform prefab)
        {
            // 优先 LayoutElement 的 preferred，退化到 sizeDelta 或 rect
            var le = prefab.GetComponent<LayoutElement>();
            float w = 0, h = 0;
            if (le != null)
            {
                if (le.preferredWidth > 0) w = le.preferredWidth;
                else if (le.minWidth > 0) w = le.minWidth;
                if (le.preferredHeight > 0) h = le.preferredHeight;
                else if (le.minHeight > 0) h = le.minHeight;
            }
            if (w <= 0) w = prefab.sizeDelta.x != 0 ? prefab.sizeDelta.x : prefab.rect.width;
            if (h <= 0) h = prefab.sizeDelta.y != 0 ? prefab.sizeDelta.y : prefab.rect.height;
            if (w <= 0) w = 100; // 兜底，避免 0 尺寸
            if (h <= 0) h = 100;
            return new Vector2(w, h);
        }

        //===================== 可变尺寸实现 =====================
        private void BuildVariableCaches()
        {
            var count = _adapter.Count;
            if (count < 0) count = 0;
            _itemSizes = (_itemSizes == null || _itemSizes.Length != count) ? new Vector2[count] : _itemSizes;
            _starts = (_starts == null || _starts.Length != count) ? new float[count] : _starts;
            _ends = (_ends == null || _ends.Length != count) ? new float[count] : _ends;

            var mainSum = 0f;
            var crossMax = 0f;
            var spacing = _layout.IsVertical ? _layout.Spacing.y : _layout.Spacing.x;
            float padMainStart = _layout.IsVertical ? _layout.Padding.top : _layout.Padding.left;
            float padMainEnd = _layout.IsVertical ? _layout.Padding.bottom : _layout.Padding.right;

            for (var i = 0; i < count; i++)
            {
                // 保护性获取项尺寸，异常与非法值回退到测量尺寸
                Vector2 sz;
                try
                {
                    sz = _varAdapter.GetItemSize(i, _viewportSize, _layout);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    sz = _cellSize;
                }
                if (sz.x <= 0) sz.x = _cellSize.x;
                if (sz.y <= 0) sz.y = _cellSize.y;
                _itemSizes[i] = sz;

                _starts[i] = padMainStart + mainSum + (i > 0 ? i * spacing : 0f);
                var main = _layout.IsVertical ? sz.y : sz.x;
                _ends[i] = _starts[i] + main;
                mainSum += main;

                var cross = _layout.IsVertical ? sz.x : sz.y;
                if (cross > crossMax) crossMax = cross;
            }

            if (_layout.IsVertical)
            {
                var mainSize = padMainStart + padMainEnd + mainSum + Mathf.Max(0, count - 1) * spacing;
                var crossSize = _layout.ControlChildWidth ? _viewportSize.x : (_layout.Padding.left + _layout.Padding.right + crossMax);
                _contentSize = new Vector2(crossSize, mainSize);
            }
            else
            {
                var mainSize = padMainStart + padMainEnd + mainSum + Mathf.Max(0, count - 1) * spacing;
                var crossSize = _layout.ControlChildHeight ? _viewportSize.y : (_layout.Padding.top + _layout.Padding.bottom + crossMax);
                _contentSize = new Vector2(mainSize, crossSize);
            }
        }

        private void GetVisibleRangeVariable(float normalized, out int first, out int last)
        {
            first = last = -1;
            var count = _adapter.Count;
            if (count <= 0 || _starts == null || _ends == null) return;

            var viewportMain = _layout.IsVertical ? _viewportSize.y : _viewportSize.x;
            var contentMain = _layout.IsVertical ? _contentSize.y : _contentSize.x;
            var scrollable = Mathf.Max(0f, contentMain - viewportMain);
            var offset = _layout.IsVertical ? (1f - Mathf.Clamp01(normalized)) * scrollable : Mathf.Clamp01(normalized) * scrollable;

            var viewStart = offset;
            var viewEnd = offset + viewportMain;

            // 二分找到第一个 end > viewStart（按自然顺序：从上到下/从左到右）
            int lo = 0, hi = count - 1, resFirst = count;
            while (lo <= hi)
            {
                var mid = (lo + hi) >> 1;
                if (_ends[mid] > viewStart)
                {
                    resFirst = mid;
                    hi = mid - 1;
                }
                else lo = mid + 1;
            }

            // 二分找到最后一个 start < viewEnd（自然顺序）
            lo = 0; hi = count - 1; var resLast = -1;
            while (lo <= hi)
            {
                var mid = (lo + hi) >> 1;
                if (_starts[mid] < viewEnd)
                {
                    resLast = mid;
                    lo = mid + 1;
                }
                else hi = mid - 1;
            }

            if (resFirst <= resLast && resFirst < count && resLast >= 0)
            {
                // 基础可见范围（自然顺序）
                var nf = Mathf.Max(0, resFirst);
                var nl = Mathf.Min(count - 1, resLast);

                // 向外扩展一个缓冲，降低边界抖动与频繁回收/创建
                nf = Mathf.Max(0, nf - 1);
                nl = Mathf.Min(count - 1, nl + 1);

                if (_layout.Reverse)
                {
                    // 镜像成逻辑索引区间
                    first = count - 1 - nl;
                    last = count - 1 - nf;
                    if (first > last)
                    {
                        var tmp = first; first = last; last = tmp;
                    }
                    first = Mathf.Clamp(first, 0, count - 1);
                    last = Mathf.Clamp(last, 0, count - 1);
                }
                else
                {
                    first = nf;
                    last = nl;
                }
            }
        }

        private void GetItemPositionAndSizeVariable(int index, out Vector2 pos, out Vector2 size)
        {
            // 反向排列时映射到“自然顺序”索引再取缓存
            int m = index;
            int count = _itemSizes?.Length ?? 0;
            if (_layout.Reverse && count > 0)
            {
                m = Mathf.Clamp(count - 1 - index, 0, count - 1);
            }
            size = (m >= 0 && m < (_itemSizes?.Length ?? 0)) ? _itemSizes[m] : _cellSize;
            var mainStart = (m >= 0 && m < (_starts?.Length ?? 0)) ? _starts[m] : 0f;
            if (_layout.IsVertical)
            {
                // 顶部为 0，向下为正偏移；anchoredPosition.y 顶部为 0，向下为负
                var y = -mainStart;
                float x = _layout.Padding.left;
                pos = new Vector2(x, y);
            }
            else
            {
                var x = _layout.Padding.left + mainStart; // 左到右
                float y = -_layout.Padding.top;
                pos = new Vector2(x, y);
            }
        }
    }

    /// <summary>
    /// 简单的 List 缓存，避免临时分配。
    /// </summary>
    internal static class ListCache<T>
    {
        private static readonly Stack<List<T>> _pool = new();
        public static List<T> Get()
        {
            return _pool.Count > 0 ? _pool.Pop() : new List<T>(64);
        }
        public static void Release(List<T> list)
        {
            list.Clear();
            _pool.Push(list);
        }
    }
}
