using System.Collections.Generic;

namespace SimpleToolkits
{
    /// <summary>
    /// 定义路径网格的通用接口，支持不同类型的地图系统
    /// </summary>
    /// <typeparam name="TNode">节点类型，必须实现 IPathNode</typeparam>
    public interface IPathGrid<TNode> where TNode : IPathNode
    {
        /// <summary>
        /// 获取指定位置的节点
        /// </summary>
        /// <param name="position">节点位置标识</param>
        /// <returns>节点实例，如果不存在则返回 null</returns>
        TNode GetNode(object position);

        /// <summary>
        /// 获取指定节点的所有邻居节点
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <returns>邻居节点集合</returns>
        IEnumerable<TNode> GetNeighbors(TNode node);

        /// <summary>
        /// 检查指定位置是否在网格范围内
        /// </summary>
        /// <param name="position">位置标识</param>
        /// <returns>是否在范围内</returns>
        bool IsValidPosition(object position);

        /// <summary>
        /// 重置网格中所有节点的状态
        /// </summary>
        void ResetAllNodes();

        /// <summary>
        /// 获取网格中的所有节点
        /// </summary>
        /// <returns>所有节点的集合</returns>
        IEnumerable<TNode> GetAllNodes();
    }
}
