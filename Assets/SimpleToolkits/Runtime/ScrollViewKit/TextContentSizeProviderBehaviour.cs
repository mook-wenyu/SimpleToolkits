namespace SimpleToolkits
{
    using System;
    using UnityEngine;
    using TMPro;

    /// <summary>
    /// 基于文本内容的动态尺寸提供器
    /// </summary>
    [AddComponentMenu("SimpleToolkits/Scroll Size Provider/Text Content Dynamic")]
    public class TextContentSizeProviderBehaviour : ScrollSizeProviderBehaviour
    {
        [Header("文本动态尺寸设置")]
        [SerializeField] private Vector2 _baseSize = new Vector2(300, 60);
        [SerializeField] private float _maxHeight = 200f;
        [SerializeField] private float _widthPadding = 16f;
        [SerializeField] private int _charactersPerLine = 50;
        [SerializeField] private float _lineHeight = 20f;

        [Header("文本样式参考")]
        [SerializeField] private TextMeshProUGUI _textReference;

        public override bool SupportsVariableSize => true;

        public Vector2 BaseSize
        {
            get => _baseSize;
            set
            {
                if (_baseSize != value)
                {
                    _baseSize = value;
                    SetDirtyAndUpdate();
                }
            }
        }

        public float MaxHeight
        {
            get => _maxHeight;
            set
            {
                if (_maxHeight != value)
                {
                    _maxHeight = Mathf.Max(_baseSize.y, value);
                    SetDirtyAndUpdate();
                }
            }
        }

        /// <summary>数据获取函数，需要在外部设置</summary>
        public Func<int, string> GetTextContent { get; set; }

        public override Vector2 GetItemSize(int index, Vector2 viewportSize)
        {
            var width = viewportSize.x - _widthPadding;
            var height = _baseSize.y;

            if (GetTextContent != null)
            {
                var text = GetTextContent(index);
                if (!string.IsNullOrEmpty(text))
                {
                    // 基于文本长度估算高度
                    var characterCount = text.Length;
                    var estimatedLines = Mathf.CeilToInt((float)characterCount / _charactersPerLine);
                    var estimatedHeight = _baseSize.y + (estimatedLines - 1) * _lineHeight;
                    
                    height = Mathf.Clamp(estimatedHeight, _baseSize.y, _maxHeight);

                    // 如果有文本组件引用，使用更精确的计算
                    if (_textReference != null)
                    {
                        height = CalculateTextHeight(text, width);
                    }
                }
            }

            return new Vector2(width, height);
        }

        public override Vector2 GetAverageSize(Vector2 viewportSize)
        {
            return new Vector2(viewportSize.x - _widthPadding, _baseSize.y);
        }

        private float CalculateTextHeight(string text, float width)
        {
            if (_textReference == null) return _baseSize.y;

            // 临时设置文本内容进行测量
            var originalText = _textReference.text;
            var originalWidth = _textReference.rectTransform.sizeDelta.x;

            try
            {
                _textReference.text = text;
                _textReference.rectTransform.sizeDelta = new Vector2(width, _textReference.rectTransform.sizeDelta.y);
                
                // 强制更新文本网格
                _textReference.ForceMeshUpdate();
                
                // 获取首选高度
                var preferredHeight = _textReference.preferredHeight;
                return Mathf.Clamp(preferredHeight, _baseSize.y, _maxHeight);
            }
            finally
            {
                // 恢复原始状态
                _textReference.text = originalText;
                _textReference.rectTransform.sizeDelta = new Vector2(originalWidth, _textReference.rectTransform.sizeDelta.y);
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _baseSize.x = Mathf.Max(1, _baseSize.x);
            _baseSize.y = Mathf.Max(1, _baseSize.y);
            _maxHeight = Mathf.Max(_baseSize.y, _maxHeight);
            _widthPadding = Mathf.Max(0, _widthPadding);
            _charactersPerLine = Mathf.Max(1, _charactersPerLine);
            _lineHeight = Mathf.Max(1, _lineHeight);
        }
    }
}