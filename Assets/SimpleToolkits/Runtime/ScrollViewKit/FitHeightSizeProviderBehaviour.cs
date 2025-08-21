namespace SimpleToolkits
{
    using UnityEngine;

    /// <summary>
    /// 自适应高度尺寸提供器组件
    /// </summary>
    [AddComponentMenu("SimpleToolkits/Scroll Size Provider/Fit Height")]
    public class FitHeightSizeProviderBehaviour : ScrollSizeProviderBehaviour
    {
        [Header("自适应高度设置")]
        [SerializeField] private float _fixedWidth = 120f;
        [SerializeField] private float _heightPadding = 0f;

        public override bool SupportsVariableSize => false;

        public float FixedWidth
        {
            get => _fixedWidth;
            set
            {
                if (_fixedWidth != value)
                {
                    _fixedWidth = Mathf.Max(1, value);
                    SetDirtyAndUpdate();
                }
            }
        }

        public float HeightPadding
        {
            get => _heightPadding;
            set
            {
                if (_heightPadding != value)
                {
                    _heightPadding = Mathf.Max(0, value);
                    SetDirtyAndUpdate();
                }
            }
        }

        public override Vector2 GetItemSize(int index, Vector2 viewportSize)
        {
            return new Vector2(_fixedWidth, viewportSize.y - _heightPadding);
        }

        public override Vector2 GetAverageSize(Vector2 viewportSize)
        {
            return GetItemSize(0, viewportSize);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _fixedWidth = Mathf.Max(1, _fixedWidth);
            _heightPadding = Mathf.Max(0, _heightPadding);
        }
    }
}