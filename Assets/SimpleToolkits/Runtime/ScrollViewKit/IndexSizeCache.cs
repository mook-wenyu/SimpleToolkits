namespace SimpleToolkits
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 按索引缓存尺寸的简单容器（主线程使用）。
    /// - 用于变尺寸列表：业务侧在数据就绪后写入每项尺寸，适配器通过委托读取。
    /// - 推荐在写入后调用 ScrollView.InvalidateAllSizes(true) 重建缓存并保持滚动位置。
    /// </summary>
    public sealed class IndexSizeCache
    {
        // 使用 List 以减少装箱与字典开销，并提供顺序局部性
        private readonly List<Vector2> _sizes = new(128);

        /// <summary>
        /// 设置某索引的尺寸（x=宽，y=高）。不会触发布局，请在批量更新后手动调用 InvalidateAllSizes。
        /// </summary>
        public void Set(int index, Vector2 size)
        {
            if (index < 0) return;
            Ensure(index);
            _sizes[index] = size;
        }

        /// <summary>
        /// 读取某索引的尺寸；若未设置则返回 Vector2.zero。
        /// </summary>
        public Vector2 Get(int index)
        {
            if (index < 0 || index >= _sizes.Count) return Vector2.zero;
            return _sizes[index];
        }

        /// <summary>
        /// 清空缓存。
        /// </summary>
        public void Clear()
        {
            _sizes.Clear();
        }

        private void Ensure(int index)
        {
            // 扩容并填充为零
            while (_sizes.Count <= index)
            {
                _sizes.Add(Vector2.zero);
            }
        }
    }
}
