namespace SimpleToolkits
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 变尺寸辅助方法集合。
    /// - 提供从 <see cref="IndexSizeCache"/> 读取的尺寸提供器工厂
    /// - 提供简单的最小/最大尺寸裁剪工具
    /// </summary>
    public static class VariableSizeHelpers
    {
        /// <summary>
        /// 基于 IndexSizeCache 创建尺寸获取委托。
        /// 典型用法：业务在内容就绪后调用 cache.Set(index, size)，再调用 ScrollView.InvalidateAllSizes(true)。
        /// </summary>
        /// <param name="cache">索引尺寸缓存</param>
        /// <param name="fallback">当缓存未命中时的回退尺寸</param>
        public static Func<int, Vector2, IScrollLayout, Vector2> FromCache(IndexSizeCache cache, Vector2 fallback)
        {
            return (index, viewport, layout) =>
            {
                var s = cache != null ? cache.Get(index) : Vector2.zero;
                // 未设置则回退
                if (s.x <= 0f && s.y <= 0f) return fallback;
                return s;
            };
        }

        /// <summary>
        /// 对尺寸做最小/最大裁剪，返回新的委托。
        /// 注意：这里只裁剪委托返回值，最终控制器仍会根据 ControlChildWidth/Height 覆盖跨轴尺寸。
        /// </summary>
        public static Func<int, Vector2, IScrollLayout, Vector2> Clamp(
            Func<int, Vector2, IScrollLayout, Vector2> inner,
            Vector2 min,
            Vector2 max)
        {
            return (index, viewport, layout) =>
            {
                var s = inner != null ? inner(index, viewport, layout) : Vector2.zero;
                if (min.x > 0f && s.x < min.x) s.x = min.x;
                if (min.y > 0f && s.y < min.y) s.y = min.y;
                if (max.x > 0f && s.x > max.x) s.x = max.x;
                if (max.y > 0f && s.y > max.y) s.y = max.y;
                return s;
            };
        }
    }
}
