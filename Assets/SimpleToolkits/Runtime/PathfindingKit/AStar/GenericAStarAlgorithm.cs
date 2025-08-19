using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine; // 仅用于编辑器断言
#endif

namespace SimpleToolkits
{
    /// <summary>
    /// 提供泛型 A* 寻路算法的实现，支持任何实现 IPathNode 的节点类型
    /// </summary>
    public static class GenericAStarAlgorithm
    {
        // 全局搜索计数，用于“版本号”懒初始化与关闭标记
        private static int _searchCounter = 0;

        /// <summary>
        /// 比较器：优先按 FCost 升序，其次按 HCost 升序
        /// </summary>
        private sealed class NodeComparer<TNode> : IComparer<TNode> where TNode : IPathNode
        {
            // 静态复用，避免每次搜索分配新比较器
            public static readonly NodeComparer<TNode> Instance = new NodeComparer<TNode>();

            public int Compare(TNode a, TNode b)
            {
                if (ReferenceEquals(a, b)) return 0;
                var f = a.FCost.CompareTo(b.FCost);
                if (f != 0) return f;
                return a.HCost.CompareTo(b.HCost);
            }
        }

        /// <summary>
        /// 简化的最小二叉堆：仅支持 Push/Pop/Count，不提供优先级更新。
        /// 采用“重复入堆 + 关闭版本丢弃”的策略，显著简化代码。
        /// </summary>
        private sealed class BinaryHeap<TNode> where TNode : class, IPathNode
        {
            private readonly List<TNode> _data = new List<TNode>(256);
            private readonly IComparer<TNode> _comparer;

            public BinaryHeap(IComparer<TNode> comparer)
            {
                _comparer = comparer;
            }

            public int Count => _data.Count;

            public void Push(TNode item)
            {
                _data.Add(item);
                int i = _data.Count - 1;
                while (i > 0)
                {
                    int p = (i - 1) >> 1;
                    if (_comparer.Compare(_data[i], _data[p]) < 0)
                    {
                        (_data[i], _data[p]) = (_data[p], _data[i]);
                        i = p;
                    }
                    else break;
                }
            }

            public TNode Pop()
            {
#if UNITY_EDITOR
                Debug.Assert(_data.Count > 0, "BinaryHeap.Pop() 在空堆上被调用，这通常意味着上层逻辑错误（open.Count <= 0）");
#endif
                var min = _data[0];
                int lastIndex = _data.Count - 1;
                var last = _data[lastIndex];
                _data.RemoveAt(lastIndex);
                if (_data.Count > 0)
                {
                    _data[0] = last;
                    int i = 0;
                    while (true)
                    {
                        int left = (i << 1) + 1;
                        if (left >= _data.Count) break;
                        int right = left + 1;
                        int smallest = (right < _data.Count && _comparer.Compare(_data[right], _data[left]) < 0) ? right : left;
                        if (_comparer.Compare(_data[smallest], _data[i]) < 0)
                        {
                            (_data[i], _data[smallest]) = (_data[smallest], _data[i]);
                            i = smallest;
                        }
                        else break;
                    }
                }
                return min;
            }
        }

        /// <summary>
        /// 使用 A* 算法查找两个节点之间的路径
        /// </summary>
        /// <typeparam name="TNode">节点类型，必须实现 IPathNode</typeparam>
        /// <param name="grid">路径网格系统</param>
        /// <param name="startNode">起始节点</param>
        /// <param name="endNode">目标节点</param>
        /// <returns>代表路径的节点列表，如果找不到路径则返回 null</returns>
        public static List<TNode> FindPath<TNode>(IPathGrid<TNode> grid, TNode startNode, TNode endNode)
        where TNode : class, IPathNode, IVersionedPathNode
        {
            if (grid == null || startNode == null || endNode == null)
                return null;

            if (!startNode.IsWalkable || !endNode.IsWalkable)
                return null;

            // 采用“版本号懒初始化”避免全图重置
            int searchId = ++_searchCounter;

            // Open 集合使用简化最小堆；Closed 使用节点上的 ClosedVersion 标记
            var open = new BinaryHeap<TNode>(NodeComparer<TNode>.Instance);

            // 懒初始化起点
            if (startNode.Version != searchId)
            {
                startNode.Version = searchId;
                startNode.GCost = 0f;
                startNode.HCost = startNode.CalculateDistanceTo(endNode);
                startNode.Parent = null;
            }

            // 早停：起点即终点
            if (ReferenceEquals(startNode, endNode))
            {
                return CalculatePath(endNode);
            }

            open.Push(startNode);

            while (open.Count > 0)
            {
                var currentNode = open.Pop();

                // 若该条目已关闭（或陈旧），跳过
                if (currentNode.ClosedVersion == searchId)
                    continue;

                // 引用相等判断更高效，且网格体系内节点对象唯一
                if (ReferenceEquals(currentNode, endNode))
                {
                    return CalculatePath(endNode); // 找到路径
                }

                currentNode.ClosedVersion = searchId;

                foreach (var neighbor in grid.GetNeighbors(currentNode))
                {
                    if (!neighbor.IsWalkable) continue;
                    if (neighbor.ClosedVersion == searchId) continue;

                    // 懒初始化该邻居的搜索状态
                    if (neighbor.Version != searchId)
                    {
                        neighbor.Version = searchId;
                        neighbor.GCost = float.MaxValue;
                        neighbor.HCost = neighbor.CalculateDistanceTo(endNode);
                        neighbor.Parent = null;
                    }

                    // 使用包含地形影响的移动代价
                    var tentativeGCost = currentNode.GCost + currentNode.CalculateMovementCostTo(neighbor);
                    if (tentativeGCost < neighbor.GCost)
                    {
                        neighbor.GCost = tentativeGCost;
                        neighbor.Parent = currentNode;
                        // 不进行堆内更新，直接重复入堆
                        open.Push(neighbor);
                    }
                }
            }

            // 未找到路径
            return null;
        }

        /// <summary>
        /// 从终点回溯以构建路径
        /// </summary>
        /// <typeparam name="TNode">节点类型</typeparam>
        /// <param name="endNode">终点节点</param>
        /// <returns>从起点到终点的路径</returns>
        private static List<TNode> CalculatePath<TNode>(TNode endNode) where TNode : class, IPathNode
        {
            var path = new List<TNode> {endNode};
            var currentNode = endNode;

            while (currentNode.Parent != null)
            {
                path.Add((TNode)currentNode.Parent);
                currentNode = (TNode)currentNode.Parent;
            }

            path.Reverse(); // 翻转为从起点到终点的顺序
            return path;
        }
    }
}
