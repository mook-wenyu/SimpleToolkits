using System;
using System.Collections.Generic;

namespace SimpleToolkits
{
    /// <summary>
    /// 基于 ThreadSafePool 封装的集合池，支持对 List/HashSet/Dictionary 等实现了 ICollection 的集合进行复用。
    /// </summary>
    /// <typeparam name="TCollection">集合类型，必须为引用类型且实现 ICollection<TItem>，并具有无参构造</typeparam>
    /// <typeparam name="TItem">集合元素类型</typeparam>
    public static class CollectionPool<TCollection, TItem>
    where TCollection : class, ICollection<TItem>, new()
    {
        /// <summary>
        /// 内部线程安全对象池。获取时不做处理，释放时自动 Clear()。
        /// </summary>
        private static readonly ThreadSafePool<TCollection> _pools =
            new(
                createFunc: static () => new TCollection(),
                onGet: null,
                onRelease: static c => c.Clear(),
                onDestroy: null,
                collectionCheck: true,
                defaultCapacity: 5,
                maxSize: 10000);

        /// <summary>
        /// 获取一个集合实例（不带句柄）。使用完成后需手动调用 Release(collection)。
        /// </summary>
        public static TCollection Get()
        {
            return _pools.Get();
        }

        /// <summary>
        /// 获取一个集合实例，并返回可释放句柄。推荐使用，确保作用域结束自动回收到池。
        /// 使用方式：
        /// using (CollectionPool&lt;List&lt;T&gt;, T&gt;.Get(out var list)) { /* 使用 list */ }
        /// </summary>
        public static PooledCollection Get(out TCollection collection)
        {
            collection = _pools.Get();
            return new PooledCollection(collection);
        }

        /// <summary>
        /// 释放集合到池中。释放时会自动调用集合的 Clear()，无需手动清理。
        /// </summary>
        public static void Release(TCollection collection)
        {
            _pools.Release(collection);
        }

        /// <summary>
        /// 预热池，提前创建指定数量的集合实例。
        /// </summary>
        public static void Prewarm(int count)
        {
            _pools.Prewarm(count);
        }

        /// <summary>
        /// 清空池中所有缓存对象。
        /// </summary>
        public static void Clear()
        {
            _pools.Clear();
        }

        /// <summary>
        /// 可释放的集合句柄，离开作用域时自动回收集合。
        /// </summary>
        public readonly struct PooledCollection : IDisposable
        {
            private readonly TCollection _collection;

            /// <summary>
            /// 获取当前句柄持有的集合实例。
            /// </summary>
            public TCollection Collection => _collection;

            internal PooledCollection(TCollection collection)
            {
                _collection = collection;
            }

            /// <summary>
            /// 隐式转换，便于直接将句柄当作集合使用。
            /// </summary>
            public static implicit operator TCollection(PooledCollection pooled) => pooled._collection;

            /// <summary>
            /// 释放时自动回收到池。
            /// </summary>
            public void Dispose()
            {
                if (_collection != null)
                {
                    _pools.Release(_collection);
                }
            }
        }
    }
}
