using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 基于网格的路径节点实现
    /// </summary>
    public class GridPathNode : IPathNode, IVersionedPathNode
    {
        private const float MoveStraightCost = 10f;
        private const float MoveDiagonalCost = 14f;

        /// <summary>
        /// 节点在网格中的 X 坐标
        /// </summary>
        public int X { get; private set; }

        /// <summary>
        /// 节点在网格中的 Y 坐标
        /// </summary>
        public int Y { get; private set; }

        /// <summary>
        /// 网格单元格大小
        /// </summary>
        public float CellSize { get; private set; }

        /// <summary>
        /// 网格原点位置
        /// </summary>
        public Vector3 OriginPosition { get; private set; }

        public string Id => $"{X},{Y}";

        public bool IsWalkable => Terrain?.IsWalkable ?? true;

        public ITerrain Terrain { get; private set; }

        public float GCost { get; set; }

        public float HCost { get; set; }

        public float FCost => GCost + HCost;

        public IPathNode Parent { get; set; }

        /// <summary>
        /// 该节点被哪个搜索轮次访问（用于懒初始化）
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 该节点在某次搜索中已被关闭（用于跳过陈旧入堆）
        /// </summary>
        public int ClosedVersion { get; set; }

        /// <summary>
        /// 初始化网格路径节点
        /// </summary>
        /// <param name="x">X 坐标</param>
        /// <param name="y">Y 坐标</param>
        /// <param name="cellSize">单元格大小</param>
        /// <param name="originPosition">网格原点位置</param>
        /// <param name="terrain">地形信息</param>
        public GridPathNode(int x, int y, float cellSize, Vector3 originPosition, ITerrain terrain = null)
        {
            X = x;
            Y = y;
            CellSize = cellSize;
            OriginPosition = originPosition;
            Terrain = terrain ?? CommonTerrains.Plains;
            Reset();
        }

        public void Reset()
        {
            GCost = float.MaxValue;
            HCost = 0f;
            Parent = null;
        }

        public float CalculateDistanceTo(IPathNode other)
        {
            if (other is not GridPathNode gridNode)
                return float.MaxValue;

            var xDistance = Mathf.Abs(X - gridNode.X);
            var yDistance = Mathf.Abs(Y - gridNode.Y);
            var remaining = Mathf.Abs(xDistance - yDistance);
            return MoveDiagonalCost * Mathf.Min(xDistance, yDistance) + MoveStraightCost * remaining;
        }

        public float CalculateMovementCostTo(IPathNode other)
        {
            if (other is not GridPathNode gridNode)
                return float.MaxValue;

            // 计算基础距离代价
            var baseCost = CalculateDistanceTo(other);
            
            // 应用目标节点的地形代价倍数
            var terrainMultiplier = gridNode.Terrain?.MovementCostMultiplier ?? 1.0f;
            
            return baseCost * terrainMultiplier;
        }

        public Vector3 GetWorldPosition()
        {
            return new Vector3(X, 0, Y) * CellSize + OriginPosition + new Vector3(CellSize, 0, CellSize) * 0.5f;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPathNode node && X == node.X && Y == node.Y;
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(X, Y);
        }

        /// <summary>
        /// 设置节点的地形类型
        /// </summary>
        /// <param name="terrain">地形信息</param>
        public void SetTerrain(ITerrain terrain)
        {
            Terrain = terrain ?? CommonTerrains.Plains;
        }

        public override string ToString()
        {
            return $"GridNode({X},{Y}) - Terrain: {Terrain?.Name}, Walkable: {IsWalkable}, FCost: {FCost:F1}";
        }
    }
}
