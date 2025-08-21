using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 纵向列表布局：从上到下，x 方向填充为 0。
    /// 用于ScrollView的纵向滚动布局。
    /// </summary>
    [AddComponentMenu("Layout/Scroll Vertical Layout")]
    public class ScrollVerticalLayout : MonoBehaviour, IScrollLayout
    {
        [Header("通用布局设置")]
        [Tooltip("内边距：左, 上, 右, 下")]
        public RectOffset padding;
        
        [Tooltip("是否控制子对象的宽度")]
        public bool controlChildWidth = true;
        
        [Tooltip("是否控制子对象的高度")]
        public bool controlChildHeight = false;
        
        [Tooltip("是否反向排列")]
        public bool reverse = false;

        [Header("纵向布局设置")]
        [Tooltip("项目之间的垂直间距")]
        public float spacing = 4f;

        private void Awake()
        {
            // 初始化RectOffset，避免在构造函数中调用
            if (padding == null)
            {
                padding = new RectOffset(0, 0, 0, 0);
            }
        }

        #region IScrollLayout 接口实现
        public bool IsVertical => true;
        public int ConstraintCount => 1;
        public Vector2 Spacing => new Vector2(0, spacing);
        public bool ControlChildWidth => controlChildWidth;
        public bool ControlChildHeight => controlChildHeight;
        public bool Reverse => reverse;
        RectOffset IScrollLayout.Padding => padding;
        #endregion

        public void Setup(RectTransform viewport, RectTransform content)
        {
            // 顶部对齐
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0, 1);
            content.anchoredPosition = Vector2.zero;
        }

        public Vector2 ComputeContentSize(int itemCount, Vector2 cellSize, Vector2 viewportSize)
        {
            float height = padding.top + padding.bottom;
            if (itemCount > 0)
            {
                height += itemCount * cellSize.y + (itemCount - 1) * spacing;
            }
            // 宽度至少等于视口，以避免拉伸问题
            var width = Mathf.Max(viewportSize.x, padding.left + padding.right + cellSize.x);
            return new Vector2(width, height);
        }

        public void GetVisibleRange(float normalizedPosition, int itemCount, Vector2 viewportSize, Vector2 cellSize, out int first, out int last)
        {
            // normalized 1=顶部, 0=底部
            var contentHeight = ComputeContentSize(itemCount, cellSize, viewportSize).y;
            var maxScroll = Mathf.Max(0, contentHeight - viewportSize.y);
            // 注意：ScrollRect.verticalNormalizedPosition = 1 表示在顶部，0 表示在底部
            var contentTop = (1f - normalizedPosition) * maxScroll; // 相对顶部位移

            // 计算起始索引：跳过 PaddingTop
            var y = padding.top + contentTop;
            var start = Mathf.FloorToInt((y + 0.0001f) / (cellSize.y + spacing));
            start = Mathf.Clamp(start, 0, Mathf.Max(0, itemCount - 1));

            // 结束索引：覆盖视口高度
            var endCover = y + viewportSize.y;
            var end = Mathf.FloorToInt((endCover - padding.top + 0.0001f) / (cellSize.y + spacing));
            end = Mathf.Clamp(end, start, Mathf.Max(0, itemCount - 1));

            // 反向索引映射：将"从上到下的索引"映射为"从下到上的实际索引"
            if (reverse)
            {
                var f = Mathf.Clamp(start - 1, 0, itemCount - 1);
                var l = Mathf.Clamp(end + 1, 0, itemCount - 1);
                // 映射后需要倒转区间
                first = itemCount - 1 - l;
                last = itemCount - 1 - f;
                first = Mathf.Clamp(first, 0, itemCount - 1);
                last = Mathf.Clamp(last, 0, itemCount - 1);
            }
            else
            {
                // 安全向外扩一行以避免边界抖动
                first = Mathf.Clamp(start - 1, 0, itemCount - 1);
                last = Mathf.Clamp(end + 1, 0, itemCount - 1);
            }
        }

        public Vector2 GetItemAnchoredPosition(int index, int itemCount, Vector2 cellSize)
        {
            // 数学镜像：不依赖 contentSize，直接使用 itemCount
            int viewIndex = reverse ? Mathf.Max(0, itemCount - 1 - index) : index;
            var y = -padding.top - viewIndex * (cellSize.y + spacing);
            float x = padding.left; // 水平方向靠左
            return new Vector2(x, y);
        }
    }
}