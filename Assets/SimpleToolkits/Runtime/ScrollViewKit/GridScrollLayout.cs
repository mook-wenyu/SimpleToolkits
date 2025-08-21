namespace SimpleToolkits
{
    using UnityEngine;

    /// <summary>
    /// 可视化网格布局组件
    /// </summary>
    [AddComponentMenu("SimpleToolkits/Scroll Layout/Grid Layout")]
    public class GridScrollLayout : ScrollLayoutBehaviour
    {
        [Header("网格布局设置")]
        [SerializeField] private Vector2 _cellSize = new Vector2(100, 100);
        [SerializeField] private int _constraintCount = 2;
        [SerializeField] private GridAxis _axis = GridAxis.Vertical;
        [SerializeField] private TextAnchor _childAlignment = TextAnchor.UpperLeft;
        [SerializeField] private bool _reverseArrangement = false;

        public override LayoutType Type => LayoutType.Grid;
        public override bool IsVertical => _axis == GridAxis.Vertical;

        public Vector2 CellSize
        {
            get => _cellSize;
            set
            {
                if (_cellSize != value)
                {
                    _cellSize = value;
                    SetDirtyAndUpdate();
                }
            }
        }

        public int ConstraintCount
        {
            get => _constraintCount;
            set
            {
                if (_constraintCount != value)
                {
                    _constraintCount = Mathf.Max(1, value);
                    SetDirtyAndUpdate();
                }
            }
        }

        public GridAxis Axis
        {
            get => _axis;
            set
            {
                if (_axis != value)
                {
                    _axis = value;
                    SetDirtyAndUpdate();
                }
            }
        }

        public TextAnchor ChildAlignment
        {
            get => _childAlignment;
            set
            {
                if (_childAlignment != value)
                {
                    _childAlignment = value;
                    SetDirtyAndUpdate();
                }
            }
        }

        public bool ReverseArrangement
        {
            get => _reverseArrangement;
            set
            {
                if (_reverseArrangement != value)
                {
                    _reverseArrangement = value;
                    SetDirtyAndUpdate();
                }
            }
        }

        public override Vector2 CalculateContentSize(int itemCount, IScrollSizeProvider sizeProvider, Vector2 viewportSize)
        {
            if (itemCount <= 0 || _constraintCount <= 0)
                return IsVertical ? new Vector2(viewportSize.x, 0) : new Vector2(0, viewportSize.y);

            if (IsVertical)
            {
                var rowCount = Mathf.CeilToInt((float)itemCount / _constraintCount);
                var totalHeight = _padding.top + _padding.bottom + rowCount * _cellSize.y + (rowCount - 1) * _spacing;
                return new Vector2(viewportSize.x, totalHeight);
            }
            else
            {
                var columnCount = Mathf.CeilToInt((float)itemCount / _constraintCount);
                var totalWidth = _padding.left + _padding.right + columnCount * _cellSize.x + (columnCount - 1) * _spacing;
                return new Vector2(totalWidth, viewportSize.y);
            }
        }

        public override (int first, int last) CalculateVisibleRange(Vector2 contentPosition, Vector2 viewportSize, int itemCount, IScrollSizeProvider sizeProvider)
        {
            if (itemCount <= 0 || _constraintCount <= 0)
                return (-1, -1);

            if (IsVertical)
            {
                var scrollY = -contentPosition.y;
                var viewportTop = scrollY - _padding.top;
                var viewportBottom = scrollY + viewportSize.y - _padding.top;

                var cellHeight = _cellSize.y + _spacing;
                var firstRow = Mathf.Max(0, Mathf.FloorToInt(viewportTop / cellHeight));
                var lastRow = Mathf.CeilToInt(viewportBottom / cellHeight);

                var first = firstRow * _constraintCount;
                var last = Mathf.Min(itemCount - 1, (lastRow + 1) * _constraintCount - 1);

                return (first, last);
            }
            else
            {
                var scrollX = -contentPosition.x;
                var viewportLeft = scrollX - _padding.left;
                var viewportRight = scrollX + viewportSize.x - _padding.left;

                var cellWidth = _cellSize.x + _spacing;
                var firstColumn = Mathf.Max(0, Mathf.FloorToInt(viewportLeft / cellWidth));
                var lastColumn = Mathf.CeilToInt(viewportRight / cellWidth);

                var first = firstColumn * _constraintCount;
                var last = Mathf.Min(itemCount - 1, (lastColumn + 1) * _constraintCount - 1);

                return (first, last);
            }
        }

        public override Vector2 CalculateItemPosition(int index, int itemCount, IScrollSizeProvider sizeProvider, Vector2 viewportSize)
        {
            if (IsVertical)
            {
                var row = index / _constraintCount;
                var column = index % _constraintCount;

                if (_reverseArrangement)
                {
                    var totalRows = Mathf.CeilToInt((float)itemCount / _constraintCount);
                    row = totalRows - 1 - row;
                    column = _constraintCount - 1 - column;
                }

                var x = _padding.left + column * (_cellSize.x + _spacing);
                var y = _padding.top + row * (_cellSize.y + _spacing);

                return new Vector2(x, -y);
            }
            else
            {
                var column = index / _constraintCount;
                var row = index % _constraintCount;

                if (_reverseArrangement)
                {
                    var totalColumns = Mathf.CeilToInt((float)itemCount / _constraintCount);
                    column = totalColumns - 1 - column;
                    row = _constraintCount - 1 - row;
                }

                var x = _padding.left + column * (_cellSize.x + _spacing);
                var y = _padding.top + row * (_cellSize.y + _spacing);

                return new Vector2(x, -y);
            }
        }

        public override void SetupContent(RectTransform content)
        {
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = IsVertical ? new Vector2(1, 1) : new Vector2(0, 1);
            content.pivot = new Vector2(0, 1);
            content.anchoredPosition = Vector2.zero;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _constraintCount = Mathf.Max(1, _constraintCount);
            _cellSize.x = Mathf.Max(1, _cellSize.x);
            _cellSize.y = Mathf.Max(1, _cellSize.y);
        }
    }
}