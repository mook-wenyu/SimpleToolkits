namespace SimpleToolkits
{
    /// <summary>
    /// 定义路径节点的通用接口，支持不同类型的地图系统
    /// </summary>
    public interface IPathNode
    {
        /// <summary>
        /// 节点的唯一标识符
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 节点是否可通行
        /// </summary>
        bool IsWalkable { get; }

        /// <summary>
        /// 节点的地形信息
        /// </summary>
        ITerrain Terrain { get; }

        /// <summary>
        /// 从起始节点到此节点的移动成本 (G-Cost)
        /// </summary>
        float GCost { get; set; }

        /// <summary>
        /// 从此节点到终点的预估移动成本 (H-Cost)
        /// </summary>
        float HCost { get; set; }

        /// <summary>
        /// 节点的总成本 (F-Cost = G-Cost + H-Cost)
        /// </summary>
        float FCost { get; }

        /// <summary>
        /// 在路径中位于此节点之前的节点
        /// </summary>
        IPathNode Parent { get; set; }

        /// <summary>
        /// 重置节点状态，用于新的寻路计算
        /// </summary>
        void Reset();

        /// <summary>
        /// 计算到另一个节点的距离成本
        /// </summary>
        /// <param name="other">目标节点</param>
        /// <returns>距离成本</returns>
        float CalculateDistanceTo(IPathNode other);

        /// <summary>
        /// 计算移动到另一个节点的地形代价
        /// </summary>
        /// <param name="other">目标节点</param>
        /// <returns>包含地形影响的移动代价</returns>
        float CalculateMovementCostTo(IPathNode other);

        /// <summary>
        /// 获取节点的世界坐标位置
        /// </summary>
        /// <returns>世界坐标</returns>
        UnityEngine.Vector3 GetWorldPosition();
    }
}
