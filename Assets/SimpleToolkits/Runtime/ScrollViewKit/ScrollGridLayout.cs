using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 网格布局：按 ConstraintCount 进行换行/换列。IsVertical=true 表示纵向滚动（从上到下换行），false 表示横向滚动（从左到右换列）。
    /// 用于ScrollView的网格滚动布局。
    /// </summary>
    public class ScrollGridLayout : MonoBehaviour, IScrollLayout
    {
        [Header("通用布局设置")]
        [Tooltip("内边距：左, 上, 右, 下")]
        public RectOffset padding;
        
        [Tooltip("是否控制子对象的宽度")]
        public bool controlChildWidth = false;
        
        [Tooltip("是否控制子对象的高度")]
        public bool controlChildHeight = false;
        
        [Tooltip("是否反向排列")]
        public bool reverse = false;

        [Header("网格设置")]
        [Tooltip("是否为纵向滚动（true：从上到下换行，false：从左到右换列）")]
        public bool isVertical = true;

        [Tooltip("每行/每列的约束数量")]
        public int constraintCount = 2;

        [Header("间距设置")]
        [Tooltip("水平间距")]
        public float spacingX = 4f;

        [Tooltip("垂直间距")]
        public float spacingY = 4f;

        private void Awake()
        {
            // 初始化RectOffset，避免在构造函数中调用
            padding ??= new RectOffset(0, 0, 0, 0);
        }

        #region IScrollLayout 接口实现
        public bool IsVertical => isVertical;
        public int ConstraintCount => Mathf.Max(1, constraintCount);
        public Vector2 Spacing => new Vector2(spacingX, spacingY);
        public bool ControlChildWidth => controlChildWidth;
        public bool ControlChildHeight => controlChildHeight;
        public bool Reverse => reverse;
        RectOffset IScrollLayout.Padding => padding;
        #endregion

        public void Setup(RectTransform viewport, RectTransform content)
        {
            if (IsVertical)
            {
                content.anchorMin = new Vector2(0, 1);
                content.anchorMax = new Vector2(1, 1);
                content.pivot = new Vector2(0, 1);
            }
            else
            {
                content.anchorMin = new Vector2(0, 1);
                content.anchorMax = new Vector2(0, 1);
                content.pivot = new Vector2(0, 1);
            }
            content.anchoredPosition = Vector2.zero;
        }

        public Vector2 ComputeContentSize(int itemCount, Vector2 cellSize, Vector2 viewportSize)
        {
            if (isVertical)
            {
                var columns = Mathf.Max(1, constraintCount);
                var rows = itemCount == 0 ? 0 : Mathf.CeilToInt(itemCount / (float)columns);
                var width = Mathf.Max(viewportSize.x, padding.left + padding.right + columns * cellSize.x + Mathf.Max(0, columns - 1) * spacingX);
                var height = padding.top + padding.bottom + rows * cellSize.y + Mathf.Max(0, rows - 1) * spacingY;
                return new Vector2(width, height);
            }
            else
            {
                var rows = Mathf.Max(1, constraintCount);
                var columns = itemCount == 0 ? 0 : Mathf.CeilToInt(itemCount / (float)rows);
                var width = padding.left + padding.right + columns * cellSize.x + Mathf.Max(0, columns - 1) * spacingX;
                var height = Mathf.Max(viewportSize.y, padding.top + padding.bottom + rows * cellSize.y + Mathf.Max(0, rows - 1) * spacingY);
                return new Vector2(width, height);
            }
        }

        public void GetVisibleRange(float normalizedPosition, int itemCount, Vector2 viewportSize, Vector2 cellSize, out int first, out int last)
        {
            if (itemCount <= 0)
            {
                first = last = -1;
                return;
            }

            if (isVertical)
            {
                var columns = Mathf.Max(1, constraintCount);
                var contentSize = ComputeContentSize(itemCount, cellSize, viewportSize);
                var maxScroll = Mathf.Max(0, contentSize.y - viewportSize.y);
                // 与 VerticalLayout 保持一致：verticalNormalizedPosition 1=顶部, 0=底部
                var offset = (1f - normalizedPosition) * maxScroll;

                var startY = padding.top + offset;
                var startRow = Mathf.FloorToInt((startY + 0.0001f) / (cellSize.y + spacingY));
                startRow = Mathf.Max(0, startRow);

                var endCover = startY + viewportSize.y;
                var endRow = Mathf.FloorToInt((endCover - padding.top + 0.0001f) / (cellSize.y + spacingY));
                endRow = Mathf.Max(startRow, endRow);

                var startIndex = Mathf.Clamp(startRow * columns, 0, itemCount - 1);
                var endIndex = Mathf.Clamp(((endRow + 1) * columns) - 1, 0, itemCount - 1);

                if (reverse)
                {
                    var f = Mathf.Clamp(startIndex - columns, 0, itemCount - 1);
                    var l = Mathf.Clamp(endIndex + columns, 0, itemCount - 1);
                    first = itemCount - 1 - l;
                    last = itemCount - 1 - f;
                    first = Mathf.Clamp(first, 0, itemCount - 1);
                    last = Mathf.Clamp(last, 0, itemCount - 1);
                }
                else
                {
                    first = Mathf.Clamp(startIndex - columns, 0, itemCount - 1);
                    last = Mathf.Clamp(endIndex + columns, 0, itemCount - 1);
                }
            }
            else
            {
                var rows = Mathf.Max(1, constraintCount);
                var contentSize = ComputeContentSize(itemCount, cellSize, viewportSize);
                var maxScroll = Mathf.Max(0, contentSize.x - viewportSize.x);
                var offset = normalizedPosition * maxScroll;

                var startX = padding.left + offset;
                var startCol = Mathf.FloorToInt((startX + 0.0001f) / (cellSize.x + spacingX));
                startCol = Mathf.Max(0, startCol);

                var endCover = startX + viewportSize.x;
                var endCol = Mathf.FloorToInt((endCover - padding.left + 0.0001f) / (cellSize.x + spacingX));
                endCol = Mathf.Max(startCol, endCol);

                var startIndex = Mathf.Clamp(startCol * rows, 0, itemCount - 1);
                var endIndex = Mathf.Clamp(((endCol + 1) * rows) - 1, 0, itemCount - 1);

                if (reverse)
                {
                    var f = Mathf.Clamp(startIndex - rows, 0, itemCount - 1);
                    var l = Mathf.Clamp(endIndex + rows, 0, itemCount - 1);
                    first = itemCount - 1 - l;
                    last = itemCount - 1 - f;
                    first = Mathf.Clamp(first, 0, itemCount - 1);
                    last = Mathf.Clamp(last, 0, itemCount - 1);
                }
                else
                {
                    first = Mathf.Clamp(startIndex - rows, 0, itemCount - 1);
                    last = Mathf.Clamp(endIndex + rows, 0, itemCount - 1);
                }
            }
        }

        public Vector2 GetItemAnchoredPosition(int index, int itemCount, Vector2 cellSize)
        {
            if (isVertical)
            {
                var columns = Mathf.Max(1, constraintCount);
                int viewIndex = reverse ? Mathf.Max(0, itemCount - 1 - index) : index;
                var row = Mathf.FloorToInt(viewIndex / (float)columns);
                var col = viewIndex % columns;
                var x = padding.left + col * (cellSize.x + spacingX);
                var y = -padding.top - row * (cellSize.y + spacingY);
                return new Vector2(x, y);
            }
            else
            {
                var rows = Mathf.Max(1, constraintCount);
                int viewIndex = reverse ? Mathf.Max(0, itemCount - 1 - index) : index;
                var col = Mathf.FloorToInt(viewIndex / (float)rows);
                var row = viewIndex % rows;
                var x = padding.left + col * (cellSize.x + spacingX);
                var y = -padding.top - row * (cellSize.y + spacingY);
                return new Vector2(x, y);
            }
        }
    }
}