namespace SimpleToolkits
{
    /// <summary>
    /// 代表寻路网格中的一个节点
    /// </summary>
    public class PathNode
    {
        /// <summary>
        /// 节点在网格中的 X 坐标
        /// </summary>
        public int X { get; private set; }

        /// <summary>
        /// 节点在网格中的 Y 坐标
        /// </summary>
        public int Y { get; private set; }

        /// <summary>
        /// 指示此节点是否可通行
        /// </summary>
        public bool IsWalkable { get; set; }

        /// <summary>
        /// 从起始节点到此节点的移动成本 (G-Cost)
        /// </summary>
        public int GCost { get; set; }

        /// <summary>
        /// 从此节点到终点的预估移动成本 (H-Cost)
        /// </summary>
        public int HCost { get; set; }

        /// <summary>
        /// 节点的总成本 (F-Cost = G-Cost + H-Cost)
        /// </summary>
        public int FCost => GCost + HCost;

        /// <summary>
        /// 在路径中位于此节点之前的节点
        /// </summary>
        public PathNode Parent { get; set; }

        /// <summary>
        /// 初始化 PathNode 类的新实例
        /// </summary>
        /// <param name="x">X 坐标</param>
        /// <param name="y">Y 坐标</param>
        /// <param name="isWalkable">节点是否可通行</param>
        public PathNode(int x, int y, bool isWalkable = true)
        {
            X = x;
            Y = y;
            IsWalkable = isWalkable;
            Reset();
        }

        /// <summary>
        /// 为新的寻路计算重置节点的成本和父节点
        /// </summary>
        public void Reset()
        {
            GCost = int.MaxValue;
            HCost = 0;
            Parent = null;
        }
    }
}
