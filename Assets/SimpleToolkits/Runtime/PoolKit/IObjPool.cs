using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 对象池接口
    /// </summary>
    public interface IObjPool
    {
        /// <summary>
        /// 从对象池获取对象
        /// </summary>
        Object Get();

        /// <summary>
        /// 释放对象到对象池
        /// </summary>
        /// <param name="obj">要释放的对象</param>
        void Release(Object obj);

        /// <summary>
        /// 清空对象池
        /// </summary>
        void Clear();

        /// <summary>
        /// 获取对象池中未使用的对象数量
        /// </summary>
        int CountInactive { get; }
    }
}
