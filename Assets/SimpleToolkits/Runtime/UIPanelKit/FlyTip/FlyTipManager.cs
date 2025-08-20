using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// FlyTip 管理器：
    /// - 支持并发显示上限、队列等待
    /// - 自动垂直堆叠与重排：上一个消失，下面的自动补位
    /// </summary>
    public class FlyTipManager : MonoBehaviour
    {
        private static readonly float _screenHeight = Screen.height / 2f;

        public int maxVisible = 5;                                       // 最大同时可见条数
        public float spacing = 8f;                                       // 条目间距（Y方向）
        public Vector2 startAnchoredPos = new(0f, _screenHeight - 100f); // 顶部起始锚点位置（基于父层Rect）
        public bool topToBottom = true;                                  // true: 从上到下堆叠；false: 从下到上

        public float moveDuration = 0.2f;     // 位置移动缓动时长
        public Ease moveEase = Ease.OutCubic; // 位置移动补位的缓动

        private UIKit _uiKit;
        private readonly List<FlyTipItemPanel> _active = new();
        private readonly Queue<FlyTipShowRequest> _pending = new();
        // 固定条目高度（首次时缓存），用于快速位置计算
        private float _itemHeight = -1f;

        private struct FlyTipShowRequest
        {
            public FlyTipShowData data;
            public float duration;
        }

        private void Awake()
        {
            _uiKit = GKMgr.Instance.GetObject<UIKit>();
            if (_uiKit == null)
            {
                Debug.LogError("[FlyTipManager] 初始化失败：UIKit 未就绪");
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public async UniTask Init()
        {
            // 预注册 FlyTipItemPanel（允许多实例、无遮罩、不点外关闭、使用 Fade 动画）
            await _uiKit.RegisterPanel<FlyTipItemPanel>(
                layer: UILayerType.FlyTip,
                allowMultiple: true,
                needMask: false,
                closeByOutside: false,
                animType: UIPanelAnimType.Fade,
                preCreateCount: 1
            );
        }

        /// <summary>
        /// 显示一条 FlyTip
        /// </summary>
        public void Show(string text, float duration = 2f)
        {
            var req = new FlyTipShowRequest
            {
                data = new FlyTipShowData {text = text},
                duration = Mathf.Max(0.1f, duration)
            };

            // 有空位直接显示，否则入队
            if (_active.Count < maxVisible)
            {
                ShowInternalAsync(req).Forget();
            }
            else
            {
                _pending.Enqueue(req);
            }
        }

        private async UniTaskVoid ShowInternalAsync(FlyTipShowRequest req)
        {
            // 打开面板（使用注册配置）
            var panel = _uiKit.OpenPanel<FlyTipItemPanel>(req.data);
            if (!panel)
            {
                Debug.LogError("打开 FlyTipItemPanel 失败");
                return;
            }

            // 首次缓存固定高度（物体高度固定，避免后续逐个读取）
            if (_itemHeight <= 0f)
            {
                _itemHeight = panel.Height;
            }

            // 初始布局：计算应放置位置
            var targetPos = CalcPositionForNew();
            var rect = panel.GetRect();
            var startPos = topToBottom ? new Vector2(startAnchoredPos.x, -_screenHeight) : new Vector2(startAnchoredPos.x, _screenHeight);
            panel.SetAnchoredPos(startPos);
            // 位置移动
            Tween.UIAnchoredPosition(rect, startPos, targetPos, moveDuration, moveEase).ToYieldInstruction();

            _active.Add(panel);

            // 统一重排（使用 anchoredPosition 动画补位）
            RelayoutActive();

            // 计时，结束后淡出并移除
            await UniTask.Delay(TimeSpan.FromSeconds(req.duration));

            // 防止已被外部关闭
            if (panel && _active.Contains(panel))
            {
                await HidePanelAsync(panel);
            }
        }

        private async UniTask HidePanelAsync(FlyTipItemPanel panel)
        {
            if (!panel) return;

            // 关闭面板
            await _uiKit.HidePanel(panel, destroy: false);

            // 从活动列表移除并重排剩余项
            _active.Remove(panel);
            RelayoutActive();

            // 若队列中有等待项，拉起一个
            if (_pending.Count > 0)
            {
                var next = _pending.Dequeue();
                ShowInternalAsync(next).Forget();
            }
        }

        /// <summary>
        /// 计算新加入项的目标位置
        /// </summary>
        private Vector2 CalcPositionForNew()
        {
            // 物体高度固定，仅使用 _active.Count 计算位移
            var count = _active.Count; // 已在场的数量
            var step = (_itemHeight > 0f ? _itemHeight : 0f) + spacing;
            var delta = step * count;
            var newY = startAnchoredPos.y + (topToBottom ? -delta : delta);
            return new Vector2(startAnchoredPos.x, newY);
        }

        /// <summary>
        /// 重排所有仍在活跃的条目，使其紧凑堆叠并补位
        /// </summary>
        private void RelayoutActive()
        {
            var currentY = startAnchoredPos.y;
            foreach (var item in _active)
            {
                if (!item) continue;
                var rect = item.GetRect();
                var target = new Vector2(startAnchoredPos.x, currentY);

                // 平滑移动到目标位置
                Tween.UIAnchoredPosition(rect, rect.anchoredPosition, target, moveDuration, moveEase);

                // 计算下一个的Y
                var step = (_itemHeight > 0f ? _itemHeight : item.Height) + spacing;
                currentY += topToBottom ? -step : step;
            }
        }

        /// <summary>
        /// 清空所有正在显示与待显示的 FlyTip
        /// </summary>
        public async UniTask ClearAllAsync()
        {
            _pending.Clear();
            // 拷贝一份，逐个淡出
            var list = new List<FlyTipItemPanel>(_active);
            foreach (var p in list)
            {
                await HidePanelAsync(p);
            }
        }
    }
}
