namespace SimpleToolkits
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// ScrollView扩展方法 - 提供便捷的工厂方法
    /// </summary>
    public static class ScrollViewExtensions
    {
        #region GameObject扩展方法
        /// <summary>为GameObject创建ScrollView</summary>
        public static ScrollViewBuilder CreateScrollView(this GameObject gameObject)
        {
            return ScrollView.Create(gameObject);
        }

        /// <summary>为ScrollRect创建ScrollView</summary>
        public static ScrollViewBuilder CreateScrollView(this ScrollRect scrollRect)
        {
            return ScrollView.Create(scrollRect);
        }

        /// <summary>为ScrollRect创建ScrollView并自动检测可视化组件</summary>
        public static ScrollViewBuilder CreateScrollViewWithBehaviour(this ScrollRect scrollRect)
        {
            return ScrollView.Create(scrollRect)
                .SetLayoutFromBehaviour()
                .SetSizeProviderFromBehaviour();
        }
        #endregion

        #region 便捷构建方法
        /// <summary>快速创建纵向消息列表</summary>
        public static ScrollView CreateVerticalMessageList<T>(
            this ScrollRect scrollRect,
            IList<T> messages,
            RectTransform messagePrefab,
            Action<int, RectTransform, T> onBind,
            float spacing = 4f,
            float itemHeight = 60f)
        {
            return ScrollView.Create(scrollRect)
                .SetData(messages, messagePrefab, onBind)
                .SetVerticalLayout(spacing, new RectOffset(8, 8, 8, 8))
                .SetFitWidth(itemHeight, 16f)
                .Build();
        }

        /// <summary>快速创建横向图片列表</summary>
        public static ScrollView CreateHorizontalImageList<T>(
            this ScrollRect scrollRect,
            IList<T> images,
            RectTransform imagePrefab,
            Action<int, RectTransform, T> onBind,
            float spacing = 8f,
            float itemWidth = 120f)
        {
            return ScrollView.Create(scrollRect)
                .SetData(images, imagePrefab, onBind)
                .SetHorizontalLayout(spacing, new RectOffset(8, 8, 8, 8))
                .SetFitHeight(itemWidth, 16f)
                .Build();
        }

        /// <summary>快速创建网格商品列表</summary>
        public static ScrollView CreateProductGrid<T>(
            this ScrollRect scrollRect,
            IList<T> products,
            RectTransform productPrefab,
            Action<int, RectTransform, T> onBind,
            int columns = 2,
            Vector2? cellSize = null,
            float spacing = 8f)
        {
            return ScrollView.Create(scrollRect)
                .SetData(products, productPrefab, onBind)
                .SetGridLayout(
                    cellSize ?? new Vector2(150, 200),
                    columns,
                    spacing,
                    new RectOffset(8, 8, 8, 8))
                .Build();
        }

        /// <summary>快速创建动态高度聊天列表</summary>
        public static ScrollView CreateDynamicChatList<T>(
            this ScrollRect scrollRect,
            IList<T> messages,
            RectTransform messagePrefab,
            Action<int, RectTransform, T> onBind,
            Func<int, Vector2, Vector2> sizeCalculator,
            float spacing = 4f)
        {
            return ScrollView.Create(scrollRect)
                .SetData(messages, messagePrefab, onBind)
                .SetVerticalLayout(spacing, new RectOffset(8, 8, 8, 8))
                .SetDynamicSize(sizeCalculator, new Vector2(300, 60))
                .Build();
        }

        /// <summary>使用可视化组件创建ScrollView（推荐）</summary>
        public static ScrollView CreateWithVisualComponents<T>(
            this ScrollRect scrollRect,
            IList<T> data,
            RectTransform cellPrefab,
            Action<int, RectTransform, T> onBind)
        {
            return ScrollView.Create(scrollRect)
                .SetData(data, cellPrefab, onBind)
                .Build(); // 自动检测可视化组件
        }
        #endregion

        #region 实用工具
        /// <summary>批量更新数据并刷新</summary>
        public static void UpdateData<T>(this ScrollView scrollView, IList<T> newData)
        {
            // 注意：这需要适配器支持数据更新
            // 在当前实现中，建议重新构建ScrollView
            scrollView?.Refresh();
        }

        /// <summary>安全滚动到索引（带边界检查）</summary>
        public static void SafeScrollToIndex(this ScrollView scrollView, int index, bool immediate = false)
        {
            if (scrollView == null || !scrollView.IsInitialized) return;
            if (index < 0 || index >= scrollView.Count) return;
            
            scrollView.ScrollToIndex(index, immediate);
        }

        /// <summary>滚动到最新消息（底部）</summary>
        public static void ScrollToLatestMessage(this ScrollView scrollView, bool immediate = false)
        {
            scrollView?.ScrollToBottom(immediate);
        }

        /// <summary>滚动到第一条消息（顶部）</summary>
        public static void ScrollToFirstMessage(this ScrollView scrollView, bool immediate = false)
        {
            scrollView?.ScrollToTop(immediate);
        }
        #endregion
    }

    /// <summary>
    /// ScrollView预设配置
    /// </summary>
    public static class ScrollViewPresets
    {
        /// <summary>聊天消息列表预设</summary>
        public static ScrollViewBuilder ChatMessageList(ScrollRect scrollRect)
        {
            return ScrollView.Create(scrollRect)
                .SetVerticalLayout(4f, new RectOffset(8, 8, 8, 8))
                .SetFitWidth(60f, 16f)
                .SetPoolSize(15);
        }

        /// <summary>商品网格预设</summary>
        public static ScrollViewBuilder ProductGrid(ScrollRect scrollRect, int columns = 2)
        {
            return ScrollView.Create(scrollRect)
                .SetGridLayout(new Vector2(150, 200), columns, 8f, new RectOffset(8, 8, 8, 8))
                .SetPoolSize(columns * 6); // 预估6行
        }

        /// <summary>横向图片轮播预设</summary>
        public static ScrollViewBuilder ImageCarousel(ScrollRect scrollRect)
        {
            return ScrollView.Create(scrollRect)
                .SetHorizontalLayout(8f, new RectOffset(8, 8, 8, 8))
                .SetFitHeight(120f, 16f)
                .SetPoolSize(10);
        }

        /// <summary>设置列表预设</summary>
        public static ScrollViewBuilder SettingsList(ScrollRect scrollRect)
        {
            return ScrollView.Create(scrollRect)
                .SetVerticalLayout(1f, new RectOffset(0, 0, 0, 0))
                .SetFitWidth(50f, 0f)
                .SetPoolSize(20);
        }

        /// <summary>动态内容列表预设（如新闻、帖子）</summary>
        public static ScrollViewBuilder DynamicContentList(ScrollRect scrollRect)
        {
            return ScrollView.Create(scrollRect)
                .SetVerticalLayout(8f, new RectOffset(12, 12, 8, 8))
                .SetFitWidth(120f, 24f) // 默认高度，实际会动态计算
                .SetPoolSize(12);
        }
    }
}