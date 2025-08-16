using UnityEngine;
using UnityEngine.Pool;

namespace SimpleToolkits
{
    /// <summary>
    /// 对象池
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    internal sealed class ObjPool<T> : IObjPool where T : Object
    {
        public readonly ObjectPool<T> objPool;

        public ObjPool(ObjectPool<T> objPool)
        {
            this.objPool = objPool;
        }

        Object IObjPool.Get()
        {
            return objPool.Get();
        }

        void IObjPool.Release(Object obj)
        {
            objPool.Release(obj as T);
        }

        void IObjPool.Clear()
        {
            objPool.Clear();
        }

        int IObjPool.CountInactive => objPool.CountInactive;
    }
}
