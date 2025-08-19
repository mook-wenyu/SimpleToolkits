using System;

namespace SimpleToolkits
{
    /// <summary>
    /// 状态迁移接口（无 GC 分配，优先级用于同源多条迁移的决策）。
    /// </summary>
    /// <typeparam name="TOwner">状态机持有者类型</typeparam>
    public interface IFSMTransition<in TOwner>
    {
        /// <summary>
        /// 迁移起点状态 Id
        /// </summary>
        int FromId { get; }

        /// <summary>
        /// 迁移目标状态 Id
        /// </summary>
        int ToId { get; }

        /// <summary>
        /// 优先级（数值越大优先级越高）。
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 是否允许从 FromId 迁移到 ToId。
        /// </summary>
        /// <param name="owner">持有者</param>
        bool CanTransition(TOwner owner);
    }
}
