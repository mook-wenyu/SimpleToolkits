namespace SimpleToolkits
{
    using UnityEngine;

    /// <summary>
    /// 固定尺寸提供器组件
    /// </summary>
    [AddComponentMenu("SimpleToolkits/Scroll Size Provider/Fixed Size")]
    public class FixedSizeProviderBehaviour : ScrollSizeProviderBehaviour
    {
        [Header("固定尺寸设置")]
        [SerializeField] private Vector2 _fixedSize = new Vector2(200, 60);

        public override bool SupportsVariableSize => false;

        public Vector2 FixedSize
        {
            get => _fixedSize;
            set
            {
                if (_fixedSize != value)
                {
                    _fixedSize = value;
                    SetDirtyAndUpdate();
                }
            }
        }

        public override Vector2 GetItemSize(int index, Vector2 viewportSize)
        {
            return _fixedSize;
        }

        public override Vector2 GetAverageSize(Vector2 viewportSize)
        {
            return _fixedSize;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _fixedSize.x = Mathf.Max(1, _fixedSize.x);
            _fixedSize.y = Mathf.Max(1, _fixedSize.y);
        }
    }
}