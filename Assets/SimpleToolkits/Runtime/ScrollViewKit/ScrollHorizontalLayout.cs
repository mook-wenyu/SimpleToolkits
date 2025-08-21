using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 横向列表布局：从左到右，y 方向填充为 0。
    /// 用于ScrollView的横向滚动布局。
    /// </summary>
    public class ScrollHorizontalLayout : MonoBehaviour, IScrollLayout
    {
        [Header("通用布局设置")]
        [Tooltip("内边距：左, 上, 右, 下")]
        public RectOffset padding;
        
        [Tooltip("是否控制子对象的宽度")]
        public bool controlChildWidth = false;
        
        [Tooltip("是否控制子对象的高度")]
        public bool controlChildHeight = true;
        
        [Tooltip("是否反向排列")]
        public bool reverse = false;

        [Header("横向布局设置")]
        [Tooltip("项目之间的水平间距")]
        public float spacing = 4f;

        private void Awake()
        {
            // 初始化RectOffset，避免在构造函数中调用
            padding ??= new RectOffset(0, 0, 0, 0);
        }

        #region IScrollLayout 接口实现
        public bool IsVertical => false;
        public int ConstraintCount => 1;
        public Vector2 Spacing => new Vector2(spacing, 0);
        public bool ControlChildWidth => controlChildWidth;
        public bool ControlChildHeight => controlChildHeight;
        public bool Reverse => reverse;
        RectOffset IScrollLayout.Padding => padding;
        #endregion

        public void Setup(RectTransform viewport, RectTransform content)
        {
            // 左侧对齐
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(0, 1);
            content.pivot = new Vector2(0, 1);
            content.anchoredPosition = Vector2.zero;
        }

        public Vector2 ComputeContentSize(int itemCount, Vector2 cellSize, Vector2 viewportSize)
        {
            float width = padding.left + padding.right;
            if (itemCount > 0)
            {
                width += itemCount * cellSize.x + (itemCount - 1) * spacing;
            }
            var height = Mathf.Max(viewportSize.y, padding.top + padding.bottom + cellSize.y);
            return new Vector2(width, height);
        }

        public void GetVisibleRange(float normalizedPosition, int itemCount, Vector2 viewportSize, Vector2 cellSize, out int first, out int last)
        {
            // horizontalNormalized 0=最左,1=最右
            var contentWidth = ComputeContentSize(itemCount, cellSize, viewportSize).x;
            var maxScroll = Mathf.Max(0, contentWidth - viewportSize.x);
            var contentLeft = normalizedPosition * maxScroll;

            var x = padding.left + contentLeft;
            var start = Mathf.FloorToInt((x + 0.0001f) / (cellSize.x + spacing));
            start = Mathf.Clamp(start, 0, Mathf.Max(0, itemCount - 1));

            var endCover = x + viewportSize.x;
            var end = Mathf.FloorToInt((endCover - padding.left + 0.0001f) / (cellSize.x + spacing));
            end = Mathf.Clamp(end, start, Mathf.Max(0, itemCount - 1));

            if (reverse)
            {
                var f = Mathf.Clamp(start - 1, 0, itemCount - 1);
                var l = Mathf.Clamp(end + 1, 0, itemCount - 1);
                first = itemCount - 1 - l;
                last = itemCount - 1 - f;
                first = Mathf.Clamp(first, 0, itemCount - 1);
                last = Mathf.Clamp(last, 0, itemCount - 1);
            }
            else
            {
                first = Mathf.Clamp(start - 1, 0, itemCount - 1);
                last = Mathf.Clamp(end + 1, 0, itemCount - 1);
            }
        }

        public Vector2 GetItemAnchoredPosition(int index, int itemCount, Vector2 cellSize)
        {
            int viewIndex = reverse ? Mathf.Max(0, itemCount - 1 - index) : index;
            var x = padding.left + viewIndex * (cellSize.x + spacing);
            float y = -padding.top; // 顶部对齐
            return new Vector2(x, y);
        }
    }
}