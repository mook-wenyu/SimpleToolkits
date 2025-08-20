namespace SimpleToolkits
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 纵向列表布局：从上到下，x 方向填充为 0。
    /// </summary>
    public sealed class VerticalLayout : IScrollLayout
    {
        public bool IsVertical => true;
        public Vector2 Spacing { get; private set; }
        public RectOffset Padding { get; private set; }
        public int ConstraintCount => 1;
        public bool ControlChildWidth { get; private set; }
        public bool ControlChildHeight { get; private set; }
        /// <summary>是否反向排列（true：索引 0 在底部）。</summary>
        public bool Reverse { get; private set; }

        public VerticalLayout(float spacingY = 4f, float paddingLeft = 0, float paddingTop = 0, float paddingRight = 0, float paddingBottom = 0,
            bool controlChildWidth = true, bool controlChildHeight = false, bool reverse = false)
        {
            Spacing = new Vector2(0, spacingY);
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
                height += itemCount * cellSize.y + (itemCount - 1) * Spacing.y;
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
            var start = Mathf.FloorToInt((y + 0.0001f) / (cellSize.y + Spacing.y));
            start = Mathf.Clamp(start, 0, Mathf.Max(0, itemCount - 1));

            // 结束索引：覆盖视口高度
            var endCover = y + viewportSize.y;
            var end = Mathf.FloorToInt((endCover - Padding.top + 0.0001f) / (cellSize.y + Spacing.y));
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
            var y = -Padding.top - viewIndex * (cellSize.y + Spacing.y);
            float x = Padding.left; // 水平方向靠左
            return new Vector2(x, y);
        }
    }

    /// <summary>
    /// 横向列表布局：从左到右，y 方向填充为 0。
    /// </summary>
    public sealed class HorizontalLayout : IScrollLayout
    {
        public bool IsVertical => false;
        public Vector2 Spacing { get; private set; }
        public RectOffset Padding { get; private set; }
        public int ConstraintCount => 1;
        public bool ControlChildWidth { get; private set; }
        public bool ControlChildHeight { get; private set; }
        /// <summary>是否反向排列（true：索引 0 在最右）。</summary>
        public bool Reverse { get; private set; }

        public HorizontalLayout(float spacingX = 4f, float paddingLeft = 0, float paddingTop = 0, float paddingRight = 0, float paddingBottom = 0,
            bool controlChildWidth = false, bool controlChildHeight = true, bool reverse = false)
        {
            Spacing = new Vector2(spacingX, 0);
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
                width += itemCount * cellSize.x + (itemCount - 1) * Spacing.x;
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
            var start = Mathf.FloorToInt((x + 0.0001f) / (cellSize.x + Spacing.x));
            start = Mathf.Clamp(start, 0, Mathf.Max(0, itemCount - 1));

            var endCover = x + viewportSize.x;
            var end = Mathf.FloorToInt((endCover - Padding.left + 0.0001f) / (cellSize.x + Spacing.x));
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
            var x = Padding.left + viewIndex * (cellSize.x + Spacing.x);
            float y = -Padding.top; // 顶部对齐
            return new Vector2(x, y);
        }
    }

    /// <summary>
    /// 网格布局：按 ConstraintCount 进行换行/换列。IsVertical=true 表示纵向滚动（从上到下换行），false 表示横向滚动（从左到右换列）。
    /// </summary>
    public sealed class GridLayout : IScrollLayout
    {
        public bool IsVertical { get; private set; }
        public Vector2 Spacing { get; private set; }
        public RectOffset Padding { get; private set; }
        public int ConstraintCount { get; private set; }
        public bool ControlChildWidth { get; private set; }
        public bool ControlChildHeight { get; private set; }
        /// <summary>是否反向排列（主轴方向整体反转索引）。</summary>
        public bool Reverse { get; private set; }

        public GridLayout(bool isVertical = true, int constraintCount = 2, float spacingX = 4f, float spacingY = 4f,
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

    /// <summary>
    /// LayoutGroup 兼容桥接工厂：
    /// - 读取 Vertical/Horizontal/GridLayoutGroup 的 spacing/padding/constraint 配置
    /// - 构造对应的 IScrollLayout 实例
    /// - 可选择禁用原生 LayoutGroup，避免与虚拟化的手动定位冲突
    /// </summary>
    public static class ScrollLayoutFactory
    {
        /// <summary>
        /// 从 Content 上的 LayoutGroup 生成布局策略。
        /// </summary>
        /// <param name="content">Content RectTransform</param>
        /// <param name="isVerticalForGrid">Grid 布局的滚动方向（true=纵向滚动）。当 Content 存在 GridLayoutGroup 时需要明确方向。</param>
        /// <param name="disableOriginalLayoutGroup">是否禁用原生 LayoutGroup</param>
        /// <returns>生成的 IScrollLayout；若未找到 LayoutGroup 则返回 null</returns>
        public static IScrollLayout FromLayoutGroup(RectTransform content, bool isVerticalForGrid = true, bool disableOriginalLayoutGroup = true)
        {
            if (content == null) return null;

            var grid = content.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                // 读取约束数
                var constraint = 1;
                switch (grid.constraint)
                {
                    case GridLayoutGroup.Constraint.FixedColumnCount:
                        constraint = Mathf.Max(1, grid.constraintCount);
                        // FixedColumnCount 更偏向纵向滚动（按列数换行）
                        break;
                    case GridLayoutGroup.Constraint.FixedRowCount:
                        constraint = Mathf.Max(1, grid.constraintCount);
                        // FixedRowCount 更偏向横向滚动（按行数换列）
                        break;
                    case GridLayoutGroup.Constraint.Flexible:
                        // 无固定约束时，交由外部指定 isVerticalForGrid 与视口尺寸决定换行换列策略
                        constraint = 1;
                        break;
                }

                // 基于起始角与滚动方向推导反向（仅粗略映射，满足常见需求）
                bool reverse = false;
                if (isVerticalForGrid)
                {
                    // 纵向滚动：Lower 开头视为自底向上
                    reverse = grid.startCorner == GridLayoutGroup.Corner.LowerLeft || grid.startCorner == GridLayoutGroup.Corner.LowerRight;
                }
                else
                {
                    // 横向滚动：Right 开头视为自右向左
                    reverse = grid.startCorner == GridLayoutGroup.Corner.UpperRight || grid.startCorner == GridLayoutGroup.Corner.LowerRight;
                }

                var layout = new GridLayout(
                    isVertical: isVerticalForGrid,
                    constraintCount: constraint,
                    spacingX: grid.spacing.x,
                    spacingY: grid.spacing.y,
                    paddingLeft: grid.padding.left,
                    paddingTop: grid.padding.top,
                    paddingRight: grid.padding.right,
                    paddingBottom: grid.padding.bottom,
                    controlChildWidth: false,
                    controlChildHeight: false,
                    reverse: reverse
                );

                if (disableOriginalLayoutGroup) grid.enabled = false;
                return layout;
            }

            var v = content.GetComponent<VerticalLayoutGroup>();
            if (v != null)
            {
                // 兼容 Unity 2022：VerticalLayoutGroup.reverseArrangement
                bool reverse = false;
                try { reverse = v.reverseArrangement; } catch { reverse = false; }
                var layout = new VerticalLayout(
                    spacingY: v.spacing,
                    paddingLeft: v.padding.left,
                    paddingTop: v.padding.top,
                    paddingRight: v.padding.right,
                    paddingBottom: v.padding.bottom,
                    controlChildWidth: v.childControlWidth,
                    controlChildHeight: v.childControlHeight,
                    reverse: reverse
                );
                if (disableOriginalLayoutGroup) v.enabled = false;
                return layout;
            }

            var h = content.GetComponent<HorizontalLayoutGroup>();
            if (h != null)
            {
                bool reverse = false;
                try { reverse = h.reverseArrangement; } catch { reverse = false; }
                var layout = new HorizontalLayout(
                    spacingX: h.spacing,
                    paddingLeft: h.padding.left,
                    paddingTop: h.padding.top,
                    paddingRight: h.padding.right,
                    paddingBottom: h.padding.bottom,
                    controlChildWidth: h.childControlWidth,
                    controlChildHeight: h.childControlHeight,
                    reverse: reverse
                );
                if (disableOriginalLayoutGroup) h.enabled = false;
                return layout;
            }

            return null;
        }

        /// <summary>
        /// 禁用 Content 上所有 LayoutGroup，避免与虚拟化的手动定位冲突。
        /// </summary>
        public static void DisableAllLayoutGroups(RectTransform content)
        {
            if (content == null) return;
            var v = content.GetComponent<VerticalLayoutGroup>();
            if (v != null) v.enabled = false;
            var h = content.GetComponent<HorizontalLayoutGroup>();
            if (h != null) h.enabled = false;
            var g = content.GetComponent<GridLayoutGroup>();
            if (g != null) g.enabled = false;
        }
    }
}
