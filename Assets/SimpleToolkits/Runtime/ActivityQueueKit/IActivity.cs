using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace SimpleToolkits
{
    /// <summary>
    /// 活动接口，所有活动都需要实现此接口
    /// </summary>
    public interface IActivity
    {
        /// <summary>
        /// 执行活动
        /// </summary>
        UniTask Execute(CancellationToken cancellationToken = default);

        /// <summary>
        /// 中断活动
        /// </summary>
        void Interrupt();
    }

    /// <summary>
    /// 基础活动抽象类
    /// </summary>
    public abstract class ActivityBase : IActivity
    {
        protected bool mIsInterrupted;
        protected readonly string mName;

        protected ActivityBase(string name = null)
        {
            mName = name ?? GetType().Name;
        }

        public abstract UniTask Execute(CancellationToken cancellationToken = default);

        public virtual void Interrupt()
        {
            mIsInterrupted = true;
        }
    }
}
