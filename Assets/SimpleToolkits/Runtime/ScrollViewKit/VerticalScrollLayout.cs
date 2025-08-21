namespace SimpleToolkits
{
    using UnityEngine;

    /// <summary>
    /// 可视化纵向布局组件
    /// </summary>
    [AddComponentMenu("SimpleToolkits/Scroll Layout/Vertical Layout")]
    public class VerticalScrollLayout : ScrollLayoutBehaviour
    {
        [Header("纵向布局设置")]
        [SerializeField] private TextAnchor _childAlignment = TextAnchor.UpperCenter;
        [SerializeField] private bool _reverseArrangement = false;

        public override LayoutType Type => LayoutType.Vertical;
        public override bool IsVertical => true;

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
            if (itemCount <= 0)
                return new Vector2(viewportSize.x, 0);

            float totalHeight = _padding.top + _padding.bottom;
            
            if (sizeProvider.SupportsVariableSize)
            {
                for (int i = 0; i < itemCount; i++)
                {
                    var itemSize = sizeProvider.GetItemSize(i, viewportSize);
                    totalHeight += itemSize.y;
                    if (i > 0) totalHeight += _spacing;
                }
            }
            else
            {
                var avgSize = sizeProvider.GetAverageSize(viewportSize);
                totalHeight += itemCount * avgSize.y + (itemCount - 1) * _spacing;
            }

            return new Vector2(viewportSize.x, totalHeight);
        }

        public override (int first, int last) CalculateVisibleRange(Vector2 contentPosition, Vector2 viewportSize, int itemCount, IScrollSizeProvider sizeProvider)
        {
            if (itemCount <= 0)
                return (-1, -1);

            var scrollY = -contentPosition.y;
            var viewportTop = scrollY - _padding.top;
            var viewportBottom = scrollY + viewportSize.y - _padding.top;

            if (!sizeProvider.SupportsVariableSize)
            {
                var avgSize = sizeProvider.GetAverageSize(viewportSize);
                var itemHeight = avgSize.y + _spacing;

                var first = Mathf.Max(0, Mathf.FloorToInt(viewportTop / itemHeight));
                var last = Mathf.Min(itemCount - 1, Mathf.CeilToInt(viewportBottom / itemHeight));

                return (first, last);
            }
            else
            {
                var first = -1;
                var last = -1;
                var currentY = 0f;

                for (int i = 0; i < itemCount; i++)
                {
                    var itemSize = sizeProvider.GetItemSize(i, viewportSize);
                    var itemTop = currentY;
                    var itemBottom = currentY + itemSize.y;

                    if (itemBottom >= viewportTop && itemTop <= viewportBottom)
                    {
                        if (first == -1) first = i;
                        last = i;
                    }

                    currentY += itemSize.y + _spacing;

                    if (currentY > viewportBottom && last != -1)
                        break;
                }

                return (first, last);
            }
        }

        public override Vector2 CalculateItemPosition(int index, int itemCount, IScrollSizeProvider sizeProvider, Vector2 viewportSize)
        {
            float y = _padding.top;
            
            if (_reverseArrangement)
            {
                var contentSize = CalculateContentSize(itemCount, sizeProvider, viewportSize);
                y = contentSize.y - _padding.bottom;
                
                if (!sizeProvider.SupportsVariableSize)
                {
                    var avgSize = sizeProvider.GetAverageSize(viewportSize);
                    y -= (index + 1) * avgSize.y + index * _spacing;
                }
                else
                {
                    for (int i = index; i >= 0; i--)
                    {
                        var itemSize2 = sizeProvider.GetItemSize(i, viewportSize);
                        y -= itemSize2.y;
                        if (i > 0) y -= _spacing;
                    }
                }
            }
            else
            {
                if (!sizeProvider.SupportsVariableSize)
                {
                    var avgSize = sizeProvider.GetAverageSize(viewportSize);
                    y += index * (avgSize.y + _spacing);
                }
                else
                {
                    for (int i = 0; i < index; i++)
                    {
                        var itemSize2 = sizeProvider.GetItemSize(i, viewportSize);
                        y += itemSize2.y + _spacing;
                    }
                }
            }

            // 应用对齐方式
            float x = _padding.left;
            var effectiveWidth = viewportSize.x - _padding.left - _padding.right;
            var itemSize = sizeProvider.GetItemSize(index, viewportSize);

            switch (_childAlignment)
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    x = _padding.left + (effectiveWidth - itemSize.x) * 0.5f;
                    break;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    x = viewportSize.x - _padding.right - itemSize.x;
                    break;
            }

            return new Vector2(x, -y);
        }

        public override void SetupContent(RectTransform content)
        {
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0, 1);
            content.anchoredPosition = Vector2.zero;
        }
    }
}