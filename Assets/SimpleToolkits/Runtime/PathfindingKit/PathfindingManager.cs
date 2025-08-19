using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 基于接口的寻路管理器，支持任意类型的路径网格系统
    /// 提供高性能、可扩展的寻路解决方案
    /// </summary>
    public class PathfindingManager : IDisposable
    {
        private GridPathSystem _gridSystem;

        /// <summary>
        /// 当前使用的网格系统
        /// </summary>
        public GridPathSystem GridSystem => _gridSystem;

        /// <summary>
        /// 网格是否已初始化
        /// </summary>
        public bool IsInitialized => _gridSystem != null;

        /// <summary>
        /// 网格宽度
        /// </summary>
        public int Width => _gridSystem?.Width ?? 0;

        /// <summary>
        /// 网格高度
        /// </summary>
        public int Height => _gridSystem?.Height ?? 0;

        /// <summary>
        /// 单元格大小
        /// </summary>
        public float CellSize => _gridSystem?.CellSize ?? 0f;

        /// <summary>
        /// 网格原点位置
        /// </summary>
        public Vector3 OriginPosition => _gridSystem?.OriginPosition ?? Vector3.zero;

        /// <summary>
        /// 初始化网格寻路系统
        /// </summary>
        /// <param name="width">网格宽度</param>
        /// <param name="height">网格高度</param>
        /// <param name="cellSize">单元格大小</param>
        /// <param name="originPosition">网格原点位置</param>
        public void Initialize(int width, int height, float cellSize, Vector3 originPosition)
        {
            _gridSystem = new GridPathSystem();
            _gridSystem.Initialize(width, height, cellSize, originPosition);
        }

        /// <summary>
        /// 使用自定义网格系统初始化
        /// </summary>
        /// <param name="gridSystem">自定义网格系统</param>
        public void Initialize(GridPathSystem gridSystem)
        {
            _gridSystem = gridSystem ?? throw new ArgumentNullException(nameof(gridSystem));
        }

        /// <summary>
        /// 查找从起点世界坐标到终点世界坐标的路径
        /// </summary>
        /// <param name="startWorldPos">起点世界坐标</param>
        /// <param name="endWorldPos">终点世界坐标</param>
        /// <returns>世界坐标路径列表，如果找不到路径则返回 null</returns>
        public List<Vector3> FindPath(Vector3 startWorldPos, Vector3 endWorldPos)
        {
            if (!IsInitialized) return null;

            var startNode = _gridSystem.GetNodeAtWorldPosition(startWorldPos);
            var endNode = _gridSystem.GetNodeAtWorldPosition(endWorldPos);

            if (startNode == null || endNode == null) return null;

            var pathNodes = GenericAStarAlgorithm.FindPath(_gridSystem, startNode, endNode);
            if (pathNodes == null) return null;

            var worldPath = new List<Vector3>(pathNodes.Count);
            foreach (var node in pathNodes)
            {
                worldPath.Add(node.GetWorldPosition());
            }

            return worldPath;
        }

        /// <summary>
        /// 使用网格坐标查找路径
        /// </summary>
        /// <param name="startX">起点 X 坐标</param>
        /// <param name="startY">起点 Y 坐标</param>
        /// <param name="endX">终点 X 坐标</param>
        /// <param name="endY">终点 Y 坐标</param>
        /// <returns>节点路径列表，如果找不到路径则返回 null</returns>
        public List<GridPathNode> FindPathByGridCoords(int startX, int startY, int endX, int endY)
        {
            if (!IsInitialized) return null;

            var startNode = _gridSystem.GetNodeAt(startX, startY);
            var endNode = _gridSystem.GetNodeAt(endX, endY);

            return GenericAStarAlgorithm.FindPath(_gridSystem, startNode, endNode);
        }

        /// <summary>
        /// 绑定地形提供者（可选）。用于通过地形ID设置地形。
        /// </summary>
        public void SetTerrainProvider(ITerrainProvider provider)
        {
            _gridSystem?.SetTerrainProvider(provider);
        }

        /// <summary>
        /// 设置指定网格坐标的地形
        /// </summary>
        public void SetTerrain(int x, int y, ITerrain terrain)
        {
            _gridSystem?.SetTerrain(x, y, terrain);
        }

        /// <summary>
        /// 通过地形ID设置指定网格坐标的地形（需要已设置地形提供者）
        /// </summary>
        public void SetTerrainById(int x, int y, string terrainId)
        {
            _gridSystem?.SetTerrainById(x, y, terrainId);
        }

        /// <summary>
        /// 在世界坐标处设置地形
        /// </summary>
        public void SetTerrainAtWorldPosition(Vector3 worldPosition, ITerrain terrain)
        {
            if (!IsInitialized) return;
            _gridSystem.SetTerrainAtWorldPosition(worldPosition, terrain);
        }

        /// <summary>
        /// 批量设置矩形区域地形
        /// </summary>
        public void SetTerrainArea(int startX, int startY, int endX, int endY, ITerrain terrain)
        {
            if (!IsInitialized) return;

            var minX = Mathf.Min(startX, endX);
            var maxX = Mathf.Max(startX, endX);
            var minY = Mathf.Min(startY, endY);
            var maxY = Mathf.Max(startY, endY);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    _gridSystem.SetTerrain(x, y, terrain);
                }
            }
        }

        /// <summary>
        /// 获取指定位置的节点
        /// </summary>
        /// <param name="x">X 坐标</param>
        /// <param name="y">Y 坐标</param>
        /// <returns>节点实例</returns>
        public GridPathNode GetNode(int x, int y)
        {
            return _gridSystem?.GetNodeAt(x, y);
        }

        /// <summary>
        /// 获取指定世界坐标的节点
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>节点实例</returns>
        public GridPathNode GetNodeAtWorldPosition(Vector3 worldPosition)
        {
            return _gridSystem?.GetNodeAtWorldPosition(worldPosition);
        }

        /// <summary>
        /// 检查指定坐标是否可通行
        /// </summary>
        /// <param name="x">X 坐标</param>
        /// <param name="y">Y 坐标</param>
        /// <returns>是否可通行</returns>
        public bool IsWalkable(int x, int y)
        {
            var node = GetNode(x, y);
            return node?.IsWalkable ?? false;
        }

        /// <summary>
        /// 检查指定世界坐标是否可通行
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>是否可通行</returns>
        public bool IsWalkableAtWorldPosition(Vector3 worldPosition)
        {
            var node = GetNodeAtWorldPosition(worldPosition);
            return node?.IsWalkable ?? false;
        }

        /// <summary>
        /// 获取网格系统的底层数据，用于调试或可视化
        /// </summary>
        /// <returns>网格数组</returns>
        public GridPathNode[,] GetGrid()
        {
            return _gridSystem?.GetGrid();
        }

        /// <summary>
        /// 重置所有节点状态
        /// </summary>
        public void ResetAllNodes()
        {
            _gridSystem?.ResetAllNodes();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _gridSystem = null;
        }
    }
}
