using System;
using System.Threading;
using UnityEngine.Pool;

namespace SimpleToolkits
{
    /// <summary>
    /// 基于 Unity ObjectPool<T/> 的线程安全包装器
    /// </summary>
    /// <typeparam name="T">对象类型，必须是引用类型</typeparam>
    public class ThreadSafePool<T> : IThreadSafePool where T : class
    {
        /// <summary>
        /// Unity 原生对象池
        /// </summary>
        private readonly ObjectPool<T> _unityPool;

        /// <summary>
        /// 读写锁，用于保证线程安全
        /// </summary>
        private readonly ReaderWriterLockSlim _lock;

        /// <summary>
        /// 对象池是否已被释放
        /// </summary>
        private volatile bool _disposed;

        /// <summary>
        /// 初始化线程安全对象池
        /// </summary>
        /// <param name="createFunc">对象创建函数</param>
        /// <param name="onGet">获取对象时的回调</param>
        /// <param name="onRelease">释放对象时的回调</param>
        /// <param name="onDestroy">销毁对象时的回调</param>
        /// <param name="collectionCheck">是否启用集合检查</param>
        /// <param name="defaultCapacity">默认容量</param>
        /// <param name="maxSize">最大容量</param>
        public ThreadSafePool(
            Func<T> createFunc,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            bool collectionCheck = true,
            int defaultCapacity = 10,
            int maxSize = 100)
        {
            _lock = new ReaderWriterLockSlim();

            _unityPool = new ObjectPool<T>(
                createFunc: createFunc,
                actionOnGet: onGet,
                actionOnRelease: onRelease,
                actionOnDestroy: onDestroy,
                collectionCheck: collectionCheck,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        /// <summary>
        /// 从对象池获取对象
        /// </summary>
        /// <returns>获取的对象</returns>
        public T Get()
        {
            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                return _unityPool.Get();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 释放对象到对象池
        /// </summary>
        /// <param name="obj">要释放的对象</param>
        public void Release(T obj)
        {
            ThrowIfDisposed();

            if (obj == null)
            {
                return;
            }

            _lock.EnterReadLock();
            try
            {
                _unityPool.Release(obj);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 预热对象池
        /// </summary>
        /// <param name="count">预热数量</param>
        public void Prewarm(int count)
        {
            ThrowIfDisposed();

            if (count <= 0)
            {
                return;
            }

            _lock.EnterReadLock();
            try
            {
                var objects = new T[count];
                for (int i = 0; i < count; i++)
                {
                    objects[i] = _unityPool.Get();
                }

                for (int i = 0; i < count; i++)
                {
                    _unityPool.Release(objects[i]);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();

            _lock.EnterWriteLock();
            try
            {
                _unityPool.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 检查对象池是否已被释放
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ThreadSafePool<T>), "对象池已被释放");
            }
        }

        /// <summary>
        /// 释放对象池资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _lock.EnterWriteLock();
            try
            {
                if (_disposed)
                {
                    return;
                }

                _unityPool?.Clear();
                _disposed = true;
            }
            finally
            {
                _lock.ExitWriteLock();
                _lock?.Dispose();
            }
        }
    }
}
