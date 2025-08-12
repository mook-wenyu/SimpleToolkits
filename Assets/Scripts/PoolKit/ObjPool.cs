using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 对象池
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
internal sealed class ObjPool<T> : IPool where T : Object
{
    public readonly ObjectPool<T> objPool;

    public ObjPool(ObjectPool<T> objPool)
    {
        this.objPool = objPool;
    }

    Object IPool.Get()
    {
        return objPool.Get();
    }

    void IPool.Release(Object obj)
    {
        objPool.Release(obj as T);
    }

    void IPool.Clear()
    {
        objPool.Clear();
    }

    int IPool.CountInactive => objPool.CountInactive;
}
