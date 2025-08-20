using System;
using System.Collections.Generic;

namespace SimpleToolkits
{
    /// <summary>
    /// List 集合便捷池
    /// </summary>
    public static class ListPool<T>
    {
        /// <summary>
        /// 获取一个 List 实例（需手动 Release）
        /// </summary>
        public static List<T> Get()
        {
            return CollectionPool<List<T>, T>.Get();
        }

        /// <summary>
        /// 获取一个 List 实例并返回可释放句柄
        /// </summary>
        public static CollectionPool<List<T>, T>.PooledCollection Get(out List<T> list)
        {
            return CollectionPool<List<T>, T>.Get(out list);
        }

        /// <summary>
        /// 回收 List（内部自动 Clear）
        /// </summary>
        public static void Release(List<T> list)
        {
            CollectionPool<List<T>, T>.Release(list);
        }

        /// <summary>
        /// 预热池
        /// </summary>
        public static void Prewarm(int count)
        {
            CollectionPool<List<T>, T>.Prewarm(count);
        }

        /// <summary>
        /// 清空池
        /// </summary>
        public static void Clear()
        {
            CollectionPool<List<T>, T>.Clear();
        }
    }

    /// <summary>
    /// HashSet 集合便捷池
    /// </summary>
    public static class HashSetPool<T>
    {
        public static HashSet<T> Get()
        {
            return CollectionPool<HashSet<T>, T>.Get();
        }

        public static CollectionPool<HashSet<T>, T>.PooledCollection Get(out HashSet<T> set)
        {
            return CollectionPool<HashSet<T>, T>.Get(out set);
        }

        public static void Release(HashSet<T> set)
        {
            CollectionPool<HashSet<T>, T>.Release(set);
        }

        public static void Prewarm(int count)
        {
            CollectionPool<HashSet<T>, T>.Prewarm(count);
        }

        public static void Clear()
        {
            CollectionPool<HashSet<T>, T>.Clear();
        }
    }

    /// <summary>
    /// Dictionary 集合便捷池
    /// 注意：Dictionary<TKey, TValue> 实现的是 ICollection<KeyValuePair<TKey, TValue>>
    /// </summary>
    public static class DictionaryPool<TKey, TValue>
    {
        public static Dictionary<TKey, TValue> Get()
        {
            return CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>.Get();
        }

        public static CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>.PooledCollection Get(out Dictionary<TKey, TValue> dict)
        {
            return CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>.Get(out dict);
        }

        public static void Release(Dictionary<TKey, TValue> dict)
        {
            CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>.Release(dict);
        }

        public static void Prewarm(int count)
        {
            CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>.Prewarm(count);
        }

        public static void Clear()
        {
            CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>.Clear();
        }
    }

    /// <summary>
    /// LinkedList 集合便捷池
    /// </summary>
    public static class LinkedListPool<T>
    {
        public static LinkedList<T> Get()
        {
            return CollectionPool<LinkedList<T>, T>.Get();
        }

        public static CollectionPool<LinkedList<T>, T>.PooledCollection Get(out LinkedList<T> list)
        {
            return CollectionPool<LinkedList<T>, T>.Get(out list);
        }

        public static void Release(LinkedList<T> list)
        {
            CollectionPool<LinkedList<T>, T>.Release(list);
        }

        public static void Prewarm(int count)
        {
            CollectionPool<LinkedList<T>, T>.Prewarm(count);
        }

        public static void Clear()
        {
            CollectionPool<LinkedList<T>, T>.Clear();
        }
    }

    /// <summary>
    /// Queue 集合便捷池（Queue<T> 未实现 ICollection<T>，因此直接基于 ThreadSafePool 实现）
    /// </summary>
    public static class QueuePool<T>
    {
        /// <summary>
        /// 内部线程安全池，回收时自动 Clear()
        /// </summary>
        private static readonly ThreadSafePool<Queue<T>> s_Pool =
            new ThreadSafePool<Queue<T>>(
                createFunc: static () => new Queue<T>(),
                onGet: null,
                onRelease: static q => q.Clear(),
                onDestroy: null,
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 1024);

        /// <summary>
        /// 获取一个 Queue 实例（需手动 Release）
        /// </summary>
        public static Queue<T> Get()
        {
            return s_Pool.Get();
        }

        /// <summary>
        /// 获取一个 Queue 实例并返回可释放句柄
        /// </summary>
        public static PooledQueue Get(out Queue<T> queue)
        {
            queue = s_Pool.Get();
            return new PooledQueue(queue);
        }

        /// <summary>
        /// 回收 Queue（内部自动 Clear）
        /// </summary>
        public static void Release(Queue<T> queue)
        {
            s_Pool.Release(queue);
        }

        /// <summary>
        /// 预热池
        /// </summary>
        public static void Prewarm(int count)
        {
            s_Pool.Prewarm(count);
        }

        /// <summary>
        /// 清空池
        /// </summary>
        public static void Clear()
        {
            s_Pool.Clear();
        }

        /// <summary>
        /// 可释放的 Queue 句柄
        /// </summary>
        public readonly struct PooledQueue : IDisposable
        {
            private readonly Queue<T> _queue;

            /// <summary>
            /// 当前句柄持有的 Queue 实例
            /// </summary>
            public Queue<T> Queue => _queue;

            internal PooledQueue(Queue<T> queue)
            {
                _queue = queue;
            }

            /// <summary>
            /// 隐式转换，便于当作 Queue<T> 使用
            /// </summary>
            public static implicit operator Queue<T>(PooledQueue pooled) => pooled._queue;

            public void Dispose()
            {
                if (_queue != null)
                {
                    s_Pool.Release(_queue);
                }
            }
        }
    }

    /// <summary>
    /// Stack 集合便捷池（Stack<T> 未实现 ICollection<T>，因此直接基于 ThreadSafePool 实现）
    /// </summary>
    public static class StackPool<T>
    {
        /// <summary>
        /// 内部线程安全池，回收时自动 Clear()
        /// </summary>
        private static readonly ThreadSafePool<Stack<T>> s_Pool =
            new ThreadSafePool<Stack<T>>(
                createFunc: static () => new Stack<T>(),
                onGet: null,
                onRelease: static s => s.Clear(),
                onDestroy: null,
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 1024);

        /// <summary>
        /// 获取一个 Stack 实例（需手动 Release）
        /// </summary>
        public static Stack<T> Get()
        {
            return s_Pool.Get();
        }

        /// <summary>
        /// 获取一个 Stack 实例并返回可释放句柄
        /// </summary>
        public static PooledStack Get(out Stack<T> stack)
        {
            stack = s_Pool.Get();
            return new PooledStack(stack);
        }

        /// <summary>
        /// 回收 Stack（内部自动 Clear）
        /// </summary>
        public static void Release(Stack<T> stack)
        {
            s_Pool.Release(stack);
        }

        /// <summary>
        /// 预热池
        /// </summary>
        public static void Prewarm(int count)
        {
            s_Pool.Prewarm(count);
        }

        /// <summary>
        /// 清空池
        /// </summary>
        public static void Clear()
        {
            s_Pool.Clear();
        }

        /// <summary>
        /// 可释放的 Stack 句柄
        /// </summary>
        public readonly struct PooledStack : IDisposable
        {
            private readonly Stack<T> _stack;

            /// <summary>
            /// 当前句柄持有的 Stack 实例
            /// </summary>
            public Stack<T> Stack => _stack;

            internal PooledStack(Stack<T> stack)
            {
                _stack = stack;
            }

            /// <summary>
            /// 隐式转换，便于当作 Stack<T> 使用
            /// </summary>
            public static implicit operator Stack<T>(PooledStack pooled) => pooled._stack;

            public void Dispose()
            {
                if (_stack != null)
                {
                    s_Pool.Release(_stack);
                }
            }
        }
    }
}
