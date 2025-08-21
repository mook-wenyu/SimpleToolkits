namespace SimpleToolkits
{
    using UnityEngine;

    /// <summary>
    /// 可视化横向布局组件
    /// </summary>
    [AddComponentMenu("SimpleToolkits/Scroll Layout/Horizontal Layout")]
    public class HorizontalScrollLayout : ScrollLayoutBehaviour
    {
        [Header("横向布局设置")]
        [SerializeField] private TextAnchor _childAlignment = TextAnchor.MiddleLeft;
        [SerializeField] private bool _reverseArrangement = false;

        public override LayoutType Type => LayoutType.Horizontal;
        public override bool IsVertical => false;

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
                return new Vector2(0, viewportSize.y);

            float totalWidth = _padding.left + _padding.right;
            
            if (sizeProvider.SupportsVariableSize)
            {
                for (int i = 0; i < itemCount; i++)
                {
                    var itemSize = sizeProvider.GetItemSize(i, viewportSize);
                    totalWidth += itemSize.x;
                    if (i > 0) totalWidth += _spacing;
                }
            }
            else
            {
                var avgSize = sizeProvider.GetAverageSize(viewportSize);
                totalWidth += itemCount * avgSize.x + (itemCount - 1) * _spacing;
            }

            return new Vector2(totalWidth, viewportSize.y);
        }

        public override (int first, int last) CalculateVisibleRange(Vector2 contentPosition, Vector2 viewportSize, int itemCount, IScrollSizeProvider sizeProvider)
        {
            if (itemCount <= 0)
                return (-1, -1);

            var scrollX = -contentPosition.x;
            var viewportLeft = scrollX - _padding.left;
            var viewportRight = scrollX + viewportSize.x - _padding.left;

            if (!sizeProvider.SupportsVariableSize)
            {
                var avgSize = sizeProvider.GetAverageSize(viewportSize);
                var itemWidth = avgSize.x + _spacing;

                var first = Mathf.Max(0, Mathf.FloorToInt(viewportLeft / itemWidth));
                var last = Mathf.Min(itemCount - 1, Mathf.CeilToInt(viewportRight / itemWidth));

                return (first, last);
            }
            else
            {
                var first = -1;
                var last = -1;
                var currentX = 0f;

                for (int i = 0; i < itemCount; i++)
                {
                    var itemSize = sizeProvider.GetItemSize(i, viewportSize);
                    var itemLeft = currentX;
                    var itemRight = currentX + itemSize.x;

                    if (itemRight >= viewportLeft && itemLeft <= viewportRight)
                    {
                        if (first == -1) first = i;
                        last = i;
                    }

                    currentX += itemSize.x + _spacing;

                    if (currentX > viewportRight && last != -1)
                        break;
                }

                return (first, last);
            }
        }

        public override Vector2 CalculateItemPosition(int index, int itemCount, IScrollSizeProvider sizeProvider, Vector2 viewportSize)
        {
            float x = _padding.left;

            if (_reverseArrangement)
            {
                var contentSize = CalculateContentSize(itemCount, sizeProvider, viewportSize);
                x = contentSize.x - _padding.right;
                
                if (!sizeProvider.SupportsVariableSize)
                {
                    var avgSize = sizeProvider.GetAverageSize(viewportSize);
                    x -= (index + 1) * avgSize.x + index * _spacing;
                }
                else
                {
                    for (int i = index; i >= 0; i--)
                    {
                        var itemSize2 = sizeProvider.GetItemSize(i, viewportSize);
                        x -= itemSize2.x;
                        if (i > 0) x -= _spacing;
                    }
                }
            }
            else
            {
                if (!sizeProvider.SupportsVariableSize)
                {
                    var avgSize = sizeProvider.GetAverageSize(viewportSize);
                    x += index * (avgSize.x + _spacing);
                }
                else
                {
                    for (int i = 0; i < index; i++)
                    {
                        var itemSize2 = sizeProvider.GetItemSize(i, viewportSize);
                        x += itemSize2.x + _spacing;
                    }
                }
            }

            // 应用对齐方式
            float y = -_padding.top;
            var effectiveHeight = viewportSize.y - _padding.top - _padding.bottom;
            var itemSize = sizeProvider.GetItemSize(index, viewportSize);

            switch (_childAlignment)
            {
                case TextAnchor.MiddleLeft:
                case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight:
                    y = -_padding.top - (effectiveHeight - itemSize.y) * 0.5f;
                    break;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    y = -viewportSize.y + _padding.bottom + itemSize.y;
                    break;
            }

            return new Vector2(x, y);
        }

        public override void SetupContent(RectTransform content)
        {
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(0, 1);
            content.pivot = new Vector2(0, 1);
            content.anchoredPosition = Vector2.zero;
        }
    }
}