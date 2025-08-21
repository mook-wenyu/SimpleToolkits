namespace SimpleToolkits
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 纵向列表布局：从上到下，x 方向填充为 0。
    /// </summary>
    public class VerticalLayout : MonoBehaviour, IScrollLayout
    {
        public bool IsVertical => true;
        public float Spacing { get; private set; }
        Vector2 IScrollLayout.Spacing => new Vector2(0, Spacing);
        public RectOffset Padding { get; private set; }
        public int ConstraintCount => 1;
        public bool ControlChildWidth { get; private set; }
        public bool ControlChildHeight { get; private set; }
        /// <summary>是否反向排列（true：索引 0 在底部）。</summary>
        public bool Reverse { get; private set; }

        public VerticalLayout()
        {
            Spacing = 4f;
            Padding = new RectOffset(0, 0, 0, 0);
            ControlChildWidth = true;
            ControlChildHeight = false;
            Reverse = false;
        }

        public void SetLayout(float spacingY = 4f, float paddingLeft = 0, float paddingTop = 0, float paddingRight = 0, float paddingBottom = 0,
            bool controlChildWidth = true, bool controlChildHeight = false, bool reverse = false)
        {
            Spacing = spacingY;
            Padding = new RectOffset((int)paddingLeft, (int)paddingRight, (int)paddingTop, (int)paddingBottom);
            ControlChildWidth = controlChildWidth;
            ControlChildHeight = controlChildHeight;
            Reverse = reverse;
        }

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
            float height = Padding.top + Padding.bottom;
            if (itemCount > 0)
            {
                height += itemCount * cellSize.y + (itemCount - 1) * Spacing;
            }
            // 宽度至少等于视口，以避免拉伸问题
            var width = Mathf.Max(viewportSize.x, Padding.left + Padding.right + cellSize.x);
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
            var y = Padding.top + contentTop;
            var start = Mathf.FloorToInt((y + 0.0001f) / (cellSize.y + Spacing));
            start = Mathf.Clamp(start, 0, Mathf.Max(0, itemCount - 1));

            // 结束索引：覆盖视口高度
            var endCover = y + viewportSize.y;
            var end = Mathf.FloorToInt((endCover - Padding.top + 0.0001f) / (cellSize.y + Spacing));
            end = Mathf.Clamp(end, start, Mathf.Max(0, itemCount - 1));

            // 反向索引映射：将“从上到下的索引”映射为“从下到上的实际索引”
            if (Reverse)
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
            int viewIndex = Reverse ? Mathf.Max(0, itemCount - 1 - index) : index;
            var y = -Padding.top - viewIndex * (cellSize.y + Spacing);
            float x = Padding.left; // 水平方向靠左
            return new Vector2(x, y);
        }
    }

    /// <summary>
    /// 横向列表布局：从左到右，y 方向填充为 0。
    /// </summary>
    public class HorizontalLayout : MonoBehaviour, IScrollLayout
    {
        public bool IsVertical => false;
        public float Spacing { get; private set; }
        Vector2 IScrollLayout.Spacing => new Vector2(Spacing, 0);
        public RectOffset Padding { get; private set; }
        public int ConstraintCount => 1;
        public bool ControlChildWidth { get; private set; }
        public bool ControlChildHeight { get; private set; }
        /// <summary>是否反向排列（true：索引 0 在最右）。</summary>
        public bool Reverse { get; private set; }

        public HorizontalLayout()
        {
            Spacing = 4f;
            Padding = new RectOffset(0, 0, 0, 0);
            ControlChildWidth = false;
            ControlChildHeight = true;
            Reverse = false;
        }

        public void SetLayout(float spacingX = 4f, float paddingLeft = 0, float paddingTop = 0, float paddingRight = 0, float paddingBottom = 0,
            bool controlChildWidth = false, bool controlChildHeight = true, bool reverse = false)
        {
            Spacing = spacingX;
            Padding = new RectOffset((int)paddingLeft, (int)paddingRight, (int)paddingTop, (int)paddingBottom);
            ControlChildWidth = controlChildWidth;
            ControlChildHeight = controlChildHeight;
            Reverse = reverse;
        }

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
            float width = Padding.left + Padding.right;
            if (itemCount > 0)
            {
                width += itemCount * cellSize.x + (itemCount - 1) * Spacing;
            }
            var height = Mathf.Max(viewportSize.y, Padding.top + Padding.bottom + cellSize.y);
            return new Vector2(width, height);
        }

        public void GetVisibleRange(float normalizedPosition, int itemCount, Vector2 viewportSize, Vector2 cellSize, out int first, out int last)
        {
            // horizontalNormalized 0=最左,1=最右
            var contentWidth = ComputeContentSize(itemCount, cellSize, viewportSize).x;
            var maxScroll = Mathf.Max(0, contentWidth - viewportSize.x);
            var contentLeft = normalizedPosition * maxScroll;

            var x = Padding.left + contentLeft;
            var start = Mathf.FloorToInt((x + 0.0001f) / (cellSize.x + Spacing));
            start = Mathf.Clamp(start, 0, Mathf.Max(0, itemCount - 1));

            var endCover = x + viewportSize.x;
            var end = Mathf.FloorToInt((endCover - Padding.left + 0.0001f) / (cellSize.x + Spacing));
            end = Mathf.Clamp(end, start, Mathf.Max(0, itemCount - 1));

            if (Reverse)
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
            int viewIndex = Reverse ? Mathf.Max(0, itemCount - 1 - index) : index;
            var x = Padding.left + viewIndex * (cellSize.x + Spacing);
            float y = -Padding.top; // 顶部对齐
            return new Vector2(x, y);
        }
    }

    /// <summary>
    /// 网格布局：按 ConstraintCount 进行换行/换列。IsVertical=true 表示纵向滚动（从上到下换行），false 表示横向滚动（从左到右换列）。
    /// </summary>
    public class GridLayout : MonoBehaviour, IScrollLayout
    {
        public bool IsVertical { get; private set; }
        public Vector2 Spacing { get; private set; }
        public float SpacingX => Spacing.x;
        public float SpacingY => Spacing.y;
        public RectOffset Padding { get; private set; }
        public int ConstraintCount { get; private set; }
        public bool ControlChildWidth { get; private set; }
        public bool ControlChildHeight { get; private set; }
        /// <summary>是否反向排列（主轴方向整体反转索引）。</summary>
        public bool Reverse { get; private set; }

        public GridLayout()
        {
            IsVertical = true;
            ConstraintCount = 2;
            Spacing = new Vector2(4f, 4f);
            Padding = new RectOffset(0, 0, 0, 0);
            ControlChildWidth = false;
            ControlChildHeight = false;
            Reverse = false;
        }

        public void SetLayout(bool isVertical = true, int constraintCount = 2, float spacingX = 4f, float spacingY = 4f,
            float paddingLeft = 0, float paddingTop = 0, float paddingRight = 0, float paddingBottom = 0,
            bool controlChildWidth = false, bool controlChildHeight = false, bool reverse = false)
        {
            IsVertical = isVertical;
            ConstraintCount = Mathf.Max(1, constraintCount);
            Spacing = new Vector2(spacingX, spacingY);
            Padding = new RectOffset((int)paddingLeft, (int)paddingRight, (int)paddingTop, (int)paddingBottom);
            ControlChildWidth = controlChildWidth;
            ControlChildHeight = controlChildHeight;
            Reverse = reverse;
        }

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
            if (IsVertical)
            {
                var columns = Mathf.Max(1, ConstraintCount);
                var rows = itemCount == 0 ? 0 : Mathf.CeilToInt(itemCount / (float)columns);
                var width = Mathf.Max(viewportSize.x, Padding.left + Padding.right + columns * cellSize.x + Mathf.Max(0, columns - 1) * Spacing.x);
                var height = Padding.top + Padding.bottom + rows * cellSize.y + Mathf.Max(0, rows - 1) * Spacing.y;
                return new Vector2(width, height);
            }
            else
            {
                var rows = Mathf.Max(1, ConstraintCount);
                var columns = itemCount == 0 ? 0 : Mathf.CeilToInt(itemCount / (float)rows);
                var width = Padding.left + Padding.right + columns * cellSize.x + Mathf.Max(0, columns - 1) * Spacing.x;
                var height = Mathf.Max(viewportSize.y, Padding.top + Padding.bottom + rows * cellSize.y + Mathf.Max(0, rows - 1) * Spacing.y);
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

            if (IsVertical)
            {
                var columns = Mathf.Max(1, ConstraintCount);
                var contentSize = ComputeContentSize(itemCount, cellSize, viewportSize);
                var maxScroll = Mathf.Max(0, contentSize.y - viewportSize.y);
                // 与 VerticalLayout 保持一致：verticalNormalizedPosition 1=顶部, 0=底部
                var offset = (1f - normalizedPosition) * maxScroll;

                var startY = Padding.top + offset;
                var startRow = Mathf.FloorToInt((startY + 0.0001f) / (cellSize.y + Spacing.y));
                startRow = Mathf.Max(0, startRow);

                var endCover = startY + viewportSize.y;
                var endRow = Mathf.FloorToInt((endCover - Padding.top + 0.0001f) / (cellSize.y + Spacing.y));
                endRow = Mathf.Max(startRow, endRow);

                var startIndex = Mathf.Clamp(startRow * columns, 0, itemCount - 1);
                var endIndex = Mathf.Clamp(((endRow + 1) * columns) - 1, 0, itemCount - 1);

                if (Reverse)
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
                var rows = Mathf.Max(1, ConstraintCount);
                var contentSize = ComputeContentSize(itemCount, cellSize, viewportSize);
                var maxScroll = Mathf.Max(0, contentSize.x - viewportSize.x);
                var offset = normalizedPosition * maxScroll;

                var startX = Padding.left + offset;
                var startCol = Mathf.FloorToInt((startX + 0.0001f) / (cellSize.x + Spacing.x));
                startCol = Mathf.Max(0, startCol);

                var endCover = startX + viewportSize.x;
                var endCol = Mathf.FloorToInt((endCover - Padding.left + 0.0001f) / (cellSize.x + Spacing.x));
                endCol = Mathf.Max(startCol, endCol);

                var startIndex = Mathf.Clamp(startCol * rows, 0, itemCount - 1);
                var endIndex = Mathf.Clamp(((endCol + 1) * rows) - 1, 0, itemCount - 1);

                if (Reverse)
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
            if (IsVertical)
            {
                var columns = Mathf.Max(1, ConstraintCount);
                int viewIndex = Reverse ? Mathf.Max(0, itemCount - 1 - index) : index;
                var row = Mathf.FloorToInt(viewIndex / (float)columns);
                var col = viewIndex % columns;
                var x = Padding.left + col * (cellSize.x + Spacing.x);
                var y = -Padding.top - row * (cellSize.y + Spacing.y);
                return new Vector2(x, y);
            }
            else
            {
                var rows = Mathf.Max(1, ConstraintCount);
                int viewIndex = Reverse ? Mathf.Max(0, itemCount - 1 - index) : index;
                var col = Mathf.FloorToInt(viewIndex / (float)rows);
                var row = viewIndex % rows;
                var x = Padding.left + col * (cellSize.x + Spacing.x);
                var y = -Padding.top - row * (cellSize.y + Spacing.y);
                return new Vector2(x, y);
            }
        }
    }

  }
