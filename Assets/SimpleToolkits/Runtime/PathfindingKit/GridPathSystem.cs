using System.Collections.Generic;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 基于网格的路径系统实现
    /// </summary>
    public class GridPathSystem : IPathGrid<GridPathNode>
    {
        private GridPathNode[,] _grid;
        private int _width;
        private int _height;
        private float _cellSize;
        private Vector3 _originPosition;
        private ITerrainProvider _terrainProvider; // 可选地形提供者

        /// <summary>
        /// 网格宽度
        /// </summary>
        public int Width => _width;

        /// <summary>
        /// 网格高度
        /// </summary>
        public int Height => _height;

        /// <summary>
        /// 单元格大小
        /// </summary>
        public float CellSize => _cellSize;

        /// <summary>
        /// 网格原点位置
        /// </summary>
        public Vector3 OriginPosition => _originPosition;

        /// <summary>
        /// 初始化网格路径系统
        /// </summary>
        /// <param name="width">网格宽度</param>
        /// <param name="height">网格高度</param>
        /// <param name="cellSize">单元格大小</param>
        /// <param name="originPosition">网格原点位置</param>
        public void Initialize(int width, int height, float cellSize, Vector3 originPosition)
        {
            _width = width;
            _height = height;
            _cellSize = cellSize;
            _originPosition = originPosition;
            _grid = new GridPathNode[width, height];

            // 创建所有节点
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // 默认使用平原地形；如设置了地形提供者，外部可再批量设置
                    _grid[x, y] = new GridPathNode(x, y, cellSize, originPosition, CommonTerrains.Plains);
                }
            }
        }

        public GridPathNode GetNode(object position)
        {
            if (position is Vector2Int gridPos)
            {
                return GetNodeAt(gridPos.x, gridPos.y);
            }
            
            if (position is (int x, int y))
            {
                return GetNodeAt(x, y);
            }

            return null;
        }

        /// <summary>
        /// 根据网格坐标获取节点
        /// </summary>
        /// <param name="x">X 坐标</param>
        /// <param name="y">Y 坐标</param>
        /// <returns>节点实例</returns>
        public GridPathNode GetNodeAt(int x, int y)
        {
            if (IsValidGridPosition(x, y))
            {
                return _grid[x, y];
            }
            return null;
        }

        /// <summary>
        /// 根据世界坐标获取节点
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>节点实例</returns>
        public GridPathNode GetNodeAtWorldPosition(Vector3 worldPosition)
        {
            GetGridCoordinates(worldPosition, out int x, out int y);
            return GetNodeAt(x, y);
        }

        public IEnumerable<GridPathNode> GetNeighbors(GridPathNode node)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue; // 跳过自身

                    int checkX = node.X + x;
                    int checkY = node.Y + y;

                    var neighbor = GetNodeAt(checkX, checkY);
                    if (neighbor == null) continue;

                    // 禁止对角穿缝：当为对角方向时，若相邻的两条边任一不可通行，则跳过该对角邻居
                    if (x != 0 && y != 0)
                    {
                        var sideA = GetNodeAt(node.X + x, node.Y);   // 水平相邻
                        var sideB = GetNodeAt(node.X, node.Y + y);   // 垂直相邻

                        if ((sideA != null && !sideA.IsWalkable) || (sideB != null && !sideB.IsWalkable))
                        {
                            continue; // 存在阻挡，禁止对角穿越
                        }
                    }

                    yield return neighbor;
                }
            }
        }

        public bool IsValidPosition(object position)
        {
            if (position is Vector2Int gridPos)
            {
                return IsValidGridPosition(gridPos.x, gridPos.y);
            }
            
            if (position is (int x, int y))
            {
                return IsValidGridPosition(x, y);
            }

            return false;
        }

        /// <summary>
        /// 检查网格坐标是否有效
        /// </summary>
        /// <param name="x">X 坐标</param>
        /// <param name="y">Y 坐标</param>
        /// <returns>是否有效</returns>
        public bool IsValidGridPosition(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        public void ResetAllNodes()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _grid[x, y]?.Reset();
                }
            }
        }

        public IEnumerable<GridPathNode> GetAllNodes()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    yield return _grid[x, y];
                }
            }
        }

        /// <summary>
        /// 绑定地形提供者（可选）。提供者仅用于外部查找地形，不强制依赖。
        /// </summary>
        /// <param name="provider">地形提供者</param>
        public void SetTerrainProvider(ITerrainProvider provider)
        {
            _terrainProvider = provider;
        }

        /// <summary>
        /// 设置指定网格坐标的地形
        /// </summary>
        /// <param name="x">X 坐标</param>
        /// <param name="y">Y 坐标</param>
        /// <param name="terrain">地形信息</param>
        public void SetTerrain(int x, int y, ITerrain terrain)
        {
            var node = GetNodeAt(x, y);
            if (node != null)
            {
                node.SetTerrain(terrain);
            }
        }

        /// <summary>
        /// 通过地形ID设置指定网格坐标的地形（需要已设置地形提供者）
        /// </summary>
        /// <param name="x">X 坐标</param>
        /// <param name="y">Y 坐标</param>
        /// <param name="terrainId">地形ID</param>
        public void SetTerrainById(int x, int y, string terrainId)
        {
            if (_terrainProvider == null) return;
            var terrain = _terrainProvider.GetTerrain(terrainId);
            SetTerrain(x, y, terrain ?? CommonTerrains.Plains);
        }

        /// <summary>
        /// 在世界坐标处设置地形
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="terrain">地形信息</param>
        public void SetTerrainAtWorldPosition(Vector3 worldPosition, ITerrain terrain)
        {
            GetGridCoordinates(worldPosition, out int x, out int y);
            SetTerrain(x, y, terrain);
        }

        /// <summary>
        /// 批量设置矩形区域地形
        /// </summary>
        /// <param name="startX">起始 X</param>
        /// <param name="startY">起始 Y</param>
        /// <param name="endX">结束 X</param>
        /// <param name="endY">结束 Y</param>
        /// <param name="terrain">地形信息</param>
        public void SetTerrainArea(int startX, int startY, int endX, int endY, ITerrain terrain)
        {
            var minX = Mathf.Min(startX, endX);
            var maxX = Mathf.Max(startX, endX);
            var minY = Mathf.Min(startY, endY);
            var maxY = Mathf.Max(startY, endY);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    SetTerrain(x, y, terrain);
                }
            }
        }

        /// <summary>
        /// 将世界坐标转换为网格坐标
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="x">输出的 X 坐标</param>
        /// <param name="y">输出的 Y 坐标</param>
        public void GetGridCoordinates(Vector3 worldPosition, out int x, out int y)
        {
            x = Mathf.FloorToInt((worldPosition - _originPosition).x / _cellSize);
            y = Mathf.FloorToInt((worldPosition - _originPosition).z / _cellSize); // 假设在 XZ 平面
        }

        /// <summary>
        /// 获取底层网格数据，用于调试或可视化
        /// </summary>
        /// <returns>网格数组</returns>
        public GridPathNode[,] GetGrid()
        {
            return _grid;
        }
    }
}
