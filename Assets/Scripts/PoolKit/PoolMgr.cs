using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

/// <summary>
/// 通用对象池管理器
/// </summary>
public class PoolMgr
{
    // 存储所有对象池的字典，key为池名称，value为对象池实例
    private readonly Dictionary<string, IPool> _pools = new();

    /// <summary>
    /// 获取或创建对象池
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="poolName">对象池名称</param>
    /// <param name="createFunc">创建对象的函数</param>
    /// <param name="actionOnGet">从池中获取对象时的回调</param>
    /// <param name="actionOnRelease">释放对象到池时的回调</param>
    /// <param name="actionOnDestroy">销毁对象时的回调</param>
    /// <param name="defaultCapacity">默认容量</param>
    /// <param name="maxSize">最大容量</param>
    public bool GetOrCreatePool<T>(
        string poolName,
        Func<T> createFunc,
        Action<T> actionOnGet = null,
        Action<T> actionOnRelease = null,
        Action<T> actionOnDestroy = null,
        int defaultCapacity = 1,
        int maxSize = 20) where T : Object
    {
        if (_pools.TryGetValue(poolName, out var existingPool) && existingPool is ObjPool<T>)
        {
            return true;
        }

        // 创建新的对象池
        var pool = new ObjectPool<T>(
            createFunc: createFunc,
            actionOnGet: actionOnGet,
            actionOnRelease: actionOnRelease,
            actionOnDestroy: actionOnDestroy,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );

        _pools[poolName] = new ObjPool<T>(pool);

        if (_pools.ContainsKey(poolName)) return true;

        Debug.LogError($"对象池 {poolName} 创建失败");
        return false;
    }

    /// <summary>
    /// 从对象池获取对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="poolName">对象池名称</param>
    /// <returns>对象实例，如果池不存在则返回null</returns>
    public T GetFromPool<T>(string poolName) where T : Object
    {
        if (_pools.TryGetValue(poolName, out var existingPool) && existingPool is ObjPool<T> typedPool)
        {
            return typedPool.objPool.Get();
        }

        Debug.LogWarning($"对象池 {poolName} 不存在或类型不匹配，期望类型: {typeof(T).Name}");
        return null;
    }

    /// <summary>
    /// 回收对象到对象池
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="obj">要回收的对象</param>
    /// <param name="poolName">对象池名称</param>
    public void RecycleToPool<T>(T obj, string poolName) where T : Object
    {
        if (!obj) return;

        if (_pools.TryGetValue(poolName, out var existingPool) && existingPool is ObjPool<T> typedPool)
        {
            typedPool.objPool.Release(obj);
        }
        else
        {
            Debug.LogWarning($"对象池 {poolName} 不存在或类型不匹配，期望类型: {typeof(T).Name}");
        }
    }

    /// <summary>
    /// 清空指定对象池
    /// </summary>
    /// <param name="poolName">对象池名称，为空则清空所有对象池</param>
    public void Clear(string poolName = null)
    {
        if (string.IsNullOrEmpty(poolName))
        {
            // 清空所有对象池
            foreach (var kvp in _pools)
            {
                kvp.Value.Clear();
            }
            _pools.Clear();
            Debug.Log("已清空所有对象池");
        }
        else
        {
            // 清空指定对象池
            if (_pools.TryGetValue(poolName, out var pool))
            {
                pool.Clear();
                _pools.Remove(poolName);
                Debug.Log($"已清空对象池: {poolName}");
            }
        }
    }

    /// <summary>
    /// 检查对象池是否存在
    /// </summary>
    /// <param name="poolName">对象池名称</param>
    /// <returns>是否存在</returns>
    public bool Has(string poolName)
    {
        return _pools.ContainsKey(poolName);
    }

    /// <summary>
    /// 获取对象池数量
    /// </summary>
    /// <returns>对象池数量</returns>
    public int GetPoolCount()
    {
        return _pools.Count;
    }

    /// <summary>
    /// 获取所有对象池名称
    /// </summary>
    /// <returns>对象池名称数组</returns>
    public string[] GetAllPoolNames()
    {
        var names = new string[_pools.Count];
        var index = 0;
        foreach (string key in _pools.Keys)
        {
            names[index++] = key;
        }
        return names;
    }

}
