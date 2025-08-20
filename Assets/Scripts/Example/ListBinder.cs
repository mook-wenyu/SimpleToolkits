using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleToolkits
{
    /// <summary>
    /// 示例列表的业务 Binder：
    /// - 在 OnCreated 中缓存组件，避免绑定阶段的反复 GetComponent
    /// - 在 OnBind 中设置显示与状态
    /// - 在 OnRecycled 中做必要的复位
    /// </summary>
    internal sealed class ListBinder : ICellBinder
    {
        // 依赖：ScrollView 用于局部刷新/重建；地址解析用于异步加载 Sprite
        private readonly ScrollView _scrollView;
        private readonly Func<int, string> _addressGetter; // 可为空，为空则不加载图片
        private bool _singleSelect = true; // 单选模式（默认）：点击切换唯一选中

        public ListBinder(ScrollView scrollView = null, Func<int, string> addressGetter = null, bool singleSelect = true)
        {
            _scrollView = scrollView;
            _addressGetter = addressGetter;
            _singleSelect = singleSelect;
        }

        private sealed class Cache
        {
            public Image Image;
            public TextMeshProUGUI Text;
            public Vector2 DefaultSize;
            public CellEvents Events;              // 指针事件转发组件
            public CancellationTokenSource Cts;    // 异步任务取消
            public CancellationTokenSource PressCts; // 长按检测取消
            public int CurrentIndex;               // 当前绑定索引
            public bool Selected;                  // 是否选中
            public string CurrentAddress;          // 当前加载的地址（避免错绑）
        }

        // 仅存储激活中的 Cell 缓存；回收时移除，避免长期占用
        private readonly Dictionary<RectTransform, Cache> _caches = new();
        // 选中项集合（示例：允许多选）
        private readonly HashSet<int> _selection = new();

        public void OnCreated(RectTransform cell)
        {
            var cache = new Cache
            {
                Image = cell.GetComponent<Image>(),
                Text = cell.GetComponentInChildren<TextMeshProUGUI>(),
                DefaultSize = cell.sizeDelta,
                Events = cell.gameObject.GetComponent<CellEvents>() ?? cell.gameObject.AddComponent<CellEvents>(),
            };
            // 注册指针事件回调（一次性）
            cache.Events.Setup(
                onClick: () => OnCellClick(cell),
                onPointerDown: () => OnCellPointerDown(cell),
                onPointerUp: () => OnCellPointerUp(cell)
            );
            _caches[cell] = cache;
        }

        public void OnBind(int index, RectTransform cell)
        {
            if (!_caches.TryGetValue(cell, out var cache)) return;
            cache.CurrentIndex = index;
            cache.Cts?.Cancel();
            cache.Cts?.Dispose();
            cache.Cts = new CancellationTokenSource();

            if (cache.Text != null) cache.Text.text = index.ToString();

            if (cache.Image != null)
            {
                // 选中态优先展示
                cache.Selected = _selection.Contains(index);
                var baseColor = (index % 2 == 0) ? Color.green : Color.red;
                cache.Image.color = cache.Selected ? Color.yellow : baseColor;
                cell.sizeDelta = cache.Selected ? new Vector2(120, 60) : cache.DefaultSize;
                // 加载图片（如提供地址解析）
                if (_addressGetter != null)
                {
                    cache.CurrentAddress = _addressGetter(index);
                    LoadSpriteAsync(cache, cache.CurrentAddress, cache.Cts.Token).Forget();
                }
            }

            // 启动一个示例异步任务：延迟后轻微闪烁颜色（可被回收或重新绑定时取消）
            BlinkAsync(cell, cache.Cts.Token).Forget();
        }

        public void OnRecycled(int index, RectTransform cell)
        {
            if (_caches.TryGetValue(cell, out var cache))
            {
                // 复位为默认尺寸与颜色，避免下次复用时出现脏状态
                cell.sizeDelta = cache.DefaultSize;
                if (cache.Image != null)
                {
                    cache.Image.color = Color.white;
                    cache.Image.sprite = null; // 清理引用，便于资源回收
                }
                // 取消未完成的异步
                cache.Cts?.Cancel();
                cache.Cts?.Dispose();
                cache.Cts = null;
                cache.PressCts?.Cancel();
                cache.PressCts?.Dispose();
                cache.PressCts = null;
                cache.CurrentIndex = -1;
                cache.CurrentAddress = null;
                cache.Selected = false; // 回收时清理选中标记（不影响全局 _selection 集合）
                // 注意：不要从 _caches 中移除，确保复用时 OnBind 可以取到缓存与已注册事件
            }
        }

        //================ 交互逻辑（点击/长按） ================
        private void OnCellClick(RectTransform cell)
        {
            if (!_caches.TryGetValue(cell, out var cache)) return;
            var index = cache.CurrentIndex;
            if (index < 0) return;

            if (_singleSelect)
            {
                // 单选：若当前未选中，则清空其他；若已选中则取消
                if (!cache.Selected)
                {
                    ClearSelection();
                    cache.Selected = true;
                    _selection.Add(index);
                }
                else
                {
                    cache.Selected = false;
                    _selection.Remove(index);
                }
            }
            else
            {
                // 多选：切换当前
                if (cache.Selected)
                {
                    cache.Selected = false;
                    _selection.Remove(index);
                }
                else
                {
                    cache.Selected = true;
                    _selection.Add(index);
                }
            }

            // 即时刷新当前 Cell 的选中可视
            if (cache.Image != null)
            {
                var baseColor = (index % 2 == 0) ? Color.green : Color.red;
                cache.Image.color = cache.Selected ? Color.yellow : baseColor;
                cell.sizeDelta = cache.Selected ? new Vector2(120, 60) : cache.DefaultSize;
            }
            // 通知视图局部刷新（如果传入了 ScrollView 引用且当前可见）
            _scrollView?.RebindItem(index);
            Debug.Log($"点击了第 {index} 项");
        }

        private void OnCellPointerDown(RectTransform cell)
        {
            if (!_caches.TryGetValue(cell, out var cache)) return;
            // 为长按检测单独创建 CTS，避免影响图片加载等绑定任务
            cache.PressCts?.Cancel();
            cache.PressCts?.Dispose();
            cache.PressCts = new CancellationTokenSource();
            var token = cache.PressCts.Token;
            // 长按 500ms 触发
            LongPressAsync(cell, 0.5f, token).Forget();
        }

        private void OnCellPointerUp(RectTransform cell)
        {
            if (!_caches.TryGetValue(cell, out var cache)) return;
            // 仅取消长按检测，不要取消绑定任务的 CTS
            cache.PressCts?.Cancel();
            cache.PressCts?.Dispose();
            cache.PressCts = null;
        }

        //================ 异步任务示例 ================
        private async UniTaskVoid LongPressAsync(RectTransform cell, float holdSeconds, CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(holdSeconds), cancellationToken: token);
                if (!_caches.TryGetValue(cell, out var cache)) return;
                // 长按反馈：蓝色闪烁一次
                if (cache.Image != null)
                {
                    var old = cache.Image.color;
                    cache.Image.color = Color.cyan;
                    await UniTask.Delay(TimeSpan.FromMilliseconds(120), cancellationToken: token);
                    cache.Image.color = old;
                }
            }
            catch (OperationCanceledException) { }
        }

        private async UniTaskVoid BlinkAsync(RectTransform cell, CancellationToken token)
        {
            try
            {
                // 绑定后 300ms 闪一下，表示异步完成（仅演示 UniTask + 取消）
                await UniTask.Delay(TimeSpan.FromMilliseconds(300), cancellationToken: token);
                if (!_caches.TryGetValue(cell, out var cache)) return;
                if (cache.Image == null) return;

                var old = cache.Image.color;
                cache.Image.color = Color.white;
                await UniTask.Delay(TimeSpan.FromMilliseconds(60), cancellationToken: token);
                cache.Image.color = old;
            }
            catch (OperationCanceledException) { }
        }

        //================ 资源加载示例（地址->Sprite，带取消） ================
        private async UniTaskVoid LoadSpriteAsync(Cache cache, string address, CancellationToken token)
        {
            try
            {
                if (cache == null || cache.Image == null || string.IsNullOrEmpty(address)) return;
                var loader = GKMgr.Instance.GetObject<YooAssetLoader>();
                var sprite = await loader.LoadAssetAsync<Sprite>(address).AttachExternalCancellation(token);
                if (cache.CurrentAddress != address) return; // 已重绑到其他地址
                cache.Image.sprite = sprite;
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        //================ 批量/框选/模式切换与刷新通知 API ================
        /// <summary>设置单选或多选。</summary>
        public void SetSingleSelect(bool single)
        {
            if (_singleSelect == single) return;
            _singleSelect = single;
            if (_singleSelect && _selection.Count > 1)
            {
                // 保留第一个选中
                var first = -1;
                foreach (var i in _selection) { first = i; break; }
                var dirty = new List<int>(_selection);
                _selection.Clear();
                if (first >= 0) _selection.Add(first);
                // 局部刷新受影响项
                if (_scrollView != null)
                {
                    foreach (var idx in dirty) _scrollView.RebindItem(idx);
                }
            }
        }

        /// <summary>清空所有选中，并刷新受影响可见项。</summary>
        public void ClearSelection()
        {
            if (_selection.Count == 0) return;
            var dirty = new List<int>(_selection);
            _selection.Clear();
            if (_scrollView != null)
            {
                foreach (var idx in dirty) _scrollView.RebindItem(idx);
            }
        }

        /// <summary>框选区间 [minIndex, maxIndex]。additive=true 为叠加选择。</summary>
        public void BoxSelect(int minIndex, int maxIndex, bool additive)
        {
            if (minIndex > maxIndex) (minIndex, maxIndex) = (maxIndex, minIndex);
            if (!additive) _selection.Clear();
            for (int i = minIndex; i <= maxIndex; i++) _selection.Add(i);
            if (_scrollView != null)
            {
                for (int i = minIndex; i <= maxIndex; i++) _scrollView.RebindItem(i);
            }
        }

        /// <summary>对所有选中项执行操作。</summary>
        public void ForEachSelected(Action<int> action)
        {
            if (action == null) return;
            foreach (var idx in _selection) action(idx);
        }

        /// <summary>数据变化时，通知单项刷新。</summary>
        public void NotifyItemChanged(int index)
        {
            _scrollView?.RebindItem(index);
        }

        /// <summary>尺寸变化时，触发整体尺寸重建。</summary>
        public void NotifySizesChanged()
        {
            _scrollView?.InvalidateAllSizes(true);
        }
    }
}
