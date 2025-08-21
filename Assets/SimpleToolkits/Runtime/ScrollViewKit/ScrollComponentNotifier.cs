namespace SimpleToolkits
{
    using System;

    /// <summary>
    /// 滚动组件全局通知系统
    /// 用于解耦组件与ScrollView的直接依赖关系
    /// </summary>
    public static class ScrollComponentNotifier
    {
        /// <summary>布局组件发生变化时触发</summary>
        public static event Action<IScrollLayout> LayoutChanged;
        
        /// <summary>尺寸提供器发生变化时触发</summary>
        public static event Action<IScrollSizeProvider> SizeProviderChanged;

        /// <summary>通知布局组件发生变化</summary>
        public static void NotifyLayoutChanged(IScrollLayout layout)
        {
            LayoutChanged?.Invoke(layout);
        }

        /// <summary>通知尺寸提供器发生变化</summary>
        public static void NotifySizeProviderChanged(IScrollSizeProvider sizeProvider)
        {
            SizeProviderChanged?.Invoke(sizeProvider);
        }
    }
}