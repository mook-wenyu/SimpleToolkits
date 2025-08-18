using System;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 对象池管理器
    /// </summary>
    public class PoolManager : IDisposable
    {
        /// <summary>
        /// 线程安全的对象池字典，存储所有已创建的对象池
        /// </summary>
        private readonly ConcurrentDictionary<string, IThreadSafePool> _pools;

        /// <summary>
        /// 对象池管理器是否已被释放
        /// </summary>
        private volatile bool _disposed;

        /// <summary>
        /// 用于同步释放操作的锁对象
        /// </summary>
        private readonly object _disposeLock = new object();

        /// <summary>
        /// 初始化对象池管理器
        /// </summary>
        public PoolManager()
        {
            _pools = new ConcurrentDictionary<string, IThreadSafePool>();
        }

        /// <summary>
        /// 创建或获取对象池
        /// </summary>
        /// <typeparam name="T">对象类型，必须是引用类型</typeparam>
        /// <param name="poolName">池名称，不能为空</param>
        /// <param name="createFunc">对象创建函数</param>
        /// <param name="onGet">获取对象时的回调函数（可选）</param>
        /// <param name="onRelease">释放对象时的回调函数（可选）</param>
        /// <param name="onDestroy">销毁对象时的回调函数（可选）</param>
        /// <param name="collectionCheck">是否启用集合检查，防止重复释放（默认启用）</param>
        /// <param name="defaultCapacity">默认容量（默认10）</param>
        /// <param name="maxSize">最大容量（默认100）</param>
        /// <returns>是否成功创建或获取对象池</returns>
        public bool CreateOrGetPool<T>(
            string poolName,
            Func<T> createFunc,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            bool collectionCheck = true,
            int defaultCapacity = 5,
            int maxSize = 10000) where T : class
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(poolName))
            {
                throw new ArgumentException("池名称不能为空", nameof(poolName));
            }

            if (createFunc == null)
            {
                throw new ArgumentNullException(nameof(createFunc), "对象创建函数不能为空");
            }

            try
            {
                // 使用 GetOrAdd 确保线程安全地创建池
                var pool = _pools.GetOrAdd(poolName, _ =>
                    new ThreadSafePool<T>(createFunc, onGet, onRelease, onDestroy, collectionCheck, defaultCapacity, maxSize));

                return pool != null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"创建对象池 '{poolName}' 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从对象池获取对象
        /// </summary>
        /// <typeparam name="T">对象类型，必须是引用类型</typeparam>
        /// <param name="poolName">池名称</param>
        /// <returns>获取的对象，如果池不存在则返回 null</returns>
        public T Get<T>(string poolName) where T : class
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(poolName))
            {
                Debug.LogWarning("池名称不能为空");
                return null;
            }

            try
            {
                if (_pools.TryGetValue(poolName, out var poolWrapper))
                {
                    if (poolWrapper is ThreadSafePool<T> typedPool)
                    {
                        return typedPool.Get();
                    }
                    else
                    {
                        Debug.LogWarning($"对象池 '{poolName}' 的类型不匹配。期望类型: {typeof(T).Name}");
                        return null;
                    }
                }
                else
                {
                    Debug.LogWarning($"未找到对象池 '{poolName}'");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"从对象池 '{poolName}' 获取对象失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 回收对象到对象池
        /// </summary>
        /// <typeparam name="T">对象类型，必须是引用类型</typeparam>
        /// <param name="obj">要回收的对象</param>
        /// <param name="poolName">池名称</param>
        /// <returns>是否成功回收</returns>
        public bool Release<T>(T obj, string poolName) where T : class
        {
            ThrowIfDisposed();

            if (obj == null)
            {
                Debug.LogWarning("要回收的对象不能为空");
                return false;
            }

            if (string.IsNullOrEmpty(poolName))
            {
                Debug.LogWarning("池名称不能为空");
                return false;
            }

            try
            {
                if (_pools.TryGetValue(poolName, out var poolWrapper))
                {
                    if (poolWrapper is ThreadSafePool<T> typedPool)
                    {
                        typedPool.Release(obj);
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning($"对象池 '{poolName}' 的类型不匹配。期望类型: {typeof(T).Name}");
                        return false;
                    }
                }
                else
                {
                    Debug.LogWarning($"未找到对象池 '{poolName}'");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"回收对象到对象池 '{poolName}' 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 预热对象池，预先创建指定数量的对象
        /// </summary>
        /// <typeparam name="T">对象类型，必须是引用类型</typeparam>
        /// <param name="poolName">池名称</param>
        /// <param name="count">预热数量</param>
        public void Prewarm<T>(string poolName, int count) where T : class
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(poolName))
            {
                Debug.LogWarning("池名称不能为空");
                return;
            }

            if (count <= 0)
            {
                Debug.LogWarning("预热数量必须大于0");
                return;
            }

            try
            {
                if (_pools.TryGetValue(poolName, out var poolWrapper))
                {
                    if (poolWrapper is ThreadSafePool<T> typedPool)
                    {
                        typedPool.Prewarm(count);
                    }
                    else
                    {
                        Debug.LogWarning($"对象池 '{poolName}' 的类型不匹配。期望类型: {typeof(T).Name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"未找到对象池 '{poolName}'");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"预热对象池 '{poolName}' 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清空指定对象池中的所有对象
        /// </summary>
        /// <param name="poolName">池名称</param>
        public void Clear(string poolName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(poolName))
            {
                Debug.LogWarning("池名称不能为空");
                return;
            }

            try
            {
                if (_pools.TryGetValue(poolName, out var poolWrapper))
                {
                    poolWrapper.Clear();
                    Debug.Log($"对象池 '{poolName}' 已清空");
                }
                else
                {
                    Debug.LogWarning($"未找到对象池 '{poolName}'");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"清空对象池 '{poolName}' 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        public void ClearAll()
        {
            ThrowIfDisposed();

            try
            {
                foreach (var kvp in _pools)
                {
                    try
                    {
                        kvp.Value.Clear();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"清空对象池 '{kvp.Key}' 失败: {ex.Message}");
                    }
                }
                Debug.Log("所有对象池已清空");
            }
            catch (Exception ex)
            {
                Debug.LogError($"清空所有对象池失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查指定名称的对象池是否存在
        /// </summary>
        /// <param name="poolName">池名称</param>
        /// <returns>是否存在</returns>
        public bool HasPool(string poolName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(poolName))
            {
                return false;
            }

            return _pools.ContainsKey(poolName);
        }

        /// <summary>
        /// 获取所有对象池的名称
        /// </summary>
        /// <returns>池名称数组</returns>
        public string[] GetAllPoolNames()
        {
            ThrowIfDisposed();
            return _pools.Keys.ToArray();
        }

        /// <summary>
        /// 获取对象池的数量
        /// </summary>
        /// <returns>池数量</returns>
        public int GetPoolCount()
        {
            ThrowIfDisposed();
            return _pools.Count;
        }

        /// <summary>
        /// 移除指定的对象池
        /// </summary>
        /// <param name="poolName">池名称</param>
        /// <returns>是否成功移除</returns>
        public bool RemovePool(string poolName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(poolName))
            {
                Debug.LogWarning("池名称不能为空");
                return false;
            }

            try
            {
                if (_pools.TryRemove(poolName, out var poolWrapper))
                {
                    poolWrapper.Dispose();
                    Debug.Log($"对象池 '{poolName}' 已移除");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"未找到对象池 '{poolName}'");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"移除对象池 '{poolName}' 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查管理器是否已被释放，如果已释放则抛出异常
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PoolManager), "对象池管理器已被释放");
            }
        }

        /// <summary>
        /// 释放对象池管理器及其管理的所有对象池
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            lock (_disposeLock)
            {
                if (_disposed)
                {
                    return;
                }

                try
                {
                    // 释放所有对象池
                    foreach (var kvp in _pools)
                    {
                        try
                        {
                            kvp.Value?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"释放对象池 '{kvp.Key}' 失败: {ex.Message}");
                        }
                    }

                    _pools.Clear();
                    _disposed = true;

                    Debug.Log("对象池管理器已释放");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"释放对象池管理器失败: {ex.Message}");
                }
            }
        }
    }
}
