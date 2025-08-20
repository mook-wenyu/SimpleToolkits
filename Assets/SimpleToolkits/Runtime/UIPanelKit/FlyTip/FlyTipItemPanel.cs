using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleToolkits
{
    /// <summary>
    /// FlyTip 展示参数（由管理器传入）
    /// </summary>
    public struct FlyTipShowData
    {
        public string text;
    }

    /// <summary>
    /// 单条 FlyTip 面板（需要配套同名预制体，挂载于 UILayerType.FlyTip）
    /// 预制体要求：
    /// - 根节点 RectTransform 作为布局节点
    /// - 文本组件为 TextMeshProUGUI
    /// </summary>
    public class FlyTipItemPanel : UIPanelBase
    {
        private Image _bgImg;
        private RectTransform _rect;
        private TextMeshProUGUI _text;

        // 当前配置数据
        internal FlyTipShowData Data { get; private set; }

        // 提供给管理器的高度获取（布局重建后）
        public float Height => _rect ? _rect.rect.height : 0f;

        protected override void OnInit()
        {
            _bgImg = GetComponent<Image>();
            _text = transform.Find("Text").GetComponent<TextMeshProUGUI>();
            if (!_rect) _rect = GetComponent<RectTransform>();
        }

        protected override void OnShow(object args)
        {
            if (args is not FlyTipShowData data) return;
            Data = data;
            if (_text)
            {
                _text.text = data.text;
            }
        }

        protected override void OnHide()
        {
            Data = default(FlyTipShowData);
        }

        /// <summary>
        /// 直接设置位置
        /// </summary>
        public void SetAnchoredPos(Vector2 pos)
        {
            if (!_rect) return;
            _rect.anchoredPosition = pos;
        }

        public RectTransform GetRect() => _rect;
    }
}
