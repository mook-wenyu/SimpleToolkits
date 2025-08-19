using System;

namespace SimpleToolkits
{
    /// <summary>
    /// 为 A* 搜索提供版本化状态，避免每次全图重置。
    /// </summary>
    public interface IVersionedPathNode
    {
        /// <summary>
        /// 当前节点被哪个搜索轮次访问过（懒初始化用）。
        /// </summary>
        int Version { get; set; }

        /// <summary>
        /// 当前节点在某个搜索轮次中已被关闭。
        /// </summary>
        int ClosedVersion { get; set; }
    }
}
