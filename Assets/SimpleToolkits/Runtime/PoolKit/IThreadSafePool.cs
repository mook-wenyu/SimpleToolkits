using System;

namespace SimpleToolkits
{
    /// <summary>
    /// 线程安全对象池接口
    /// </summary>
    public interface IThreadSafePool : IDisposable
    {
        /// <summary>
        /// 清空对象池
        /// </summary>
        void Clear();
    }
}
