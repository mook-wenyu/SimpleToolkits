namespace SimpleToolkits
{
    using UnityEngine;

    /// <summary>
    /// 自适应宽度尺寸提供器组件
    /// </summary>
    [AddComponentMenu("SimpleToolkits/Scroll Size Provider/Fit Width")]
    public class FitWidthSizeProviderBehaviour : ScrollSizeProviderBehaviour
    {
        [Header("自适应宽度设置")]
        [SerializeField] private float _fixedHeight = 60f;
        [SerializeField] private float _widthPadding = 0f;

        public override bool SupportsVariableSize => false;

        public float FixedHeight
        {
            get => _fixedHeight;
            set
            {
                if (_fixedHeight != value)
                {
                    _fixedHeight = Mathf.Max(1, value);
                    SetDirtyAndUpdate();
                }
            }
        }

        public float WidthPadding
        {
            get => _widthPadding;
            set
            {
                if (_widthPadding != value)
                {
                    _widthPadding = Mathf.Max(0, value);
                    SetDirtyAndUpdate();
                }
            }
        }

        public override Vector2 GetItemSize(int index, Vector2 viewportSize)
        {
            return new Vector2(viewportSize.x - _widthPadding, _fixedHeight);
        }

        public override Vector2 GetAverageSize(Vector2 viewportSize)
        {
            return GetItemSize(0, viewportSize);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _fixedHeight = Mathf.Max(1, _fixedHeight);
            _widthPadding = Mathf.Max(0, _widthPadding);
        }
    }
}