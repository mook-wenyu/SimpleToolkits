using System.Collections.Generic;

namespace SimpleToolkits
{
    /// <summary>
    /// 地形提供者接口，允许用户自定义地形管理逻辑
    /// </summary>
    public interface ITerrainProvider
    {
        /// <summary>
        /// 根据地形ID获取地形信息
        /// </summary>
        /// <param name="terrainId">地形ID</param>
        /// <returns>地形信息，如果不存在返回null</returns>
        ITerrain GetTerrain(string terrainId);
        
        /// <summary>
        /// 获取所有可用的地形类型
        /// </summary>
        /// <returns>地形集合</returns>
        IEnumerable<ITerrain> GetAllTerrains();
        
        /// <summary>
        /// 检查是否存在指定ID的地形
        /// </summary>
        /// <param name="terrainId">地形ID</param>
        /// <returns>是否存在</returns>
        bool HasTerrain(string terrainId);
    }

    /// <summary>
    /// 基础地形提供者实现，用户可以继承或直接使用
    /// </summary>
    public class BaseTerrainProvider : ITerrainProvider
    {
        private readonly Dictionary<string, ITerrain> _terrains;

        /// <summary>
        /// 创建基础地形提供者
        /// </summary>
        /// <param name="terrains">初始地形集合</param>
        public BaseTerrainProvider(IEnumerable<ITerrain> terrains = null)
        {
            _terrains = new Dictionary<string, ITerrain>();
            
            if (terrains != null)
            {
                foreach (var terrain in terrains)
                {
                    _terrains[terrain.Id] = terrain;
                }
            }
        }

        /// <summary>
        /// 创建包含常用地形的提供者
        /// </summary>
        /// <returns>包含常用地形的提供者</returns>
        public static BaseTerrainProvider CreateWithCommonTerrains()
        {
            return new BaseTerrainProvider(new[]
            {
                CommonTerrains.Plains,
                CommonTerrains.Water,
                CommonTerrains.Mountain,
                CommonTerrains.Forest,
                CommonTerrains.Desert,
                CommonTerrains.Road,
                CommonTerrains.Swamp,
                CommonTerrains.Ice,
                CommonTerrains.Impassable
            });
        }

        public ITerrain GetTerrain(string terrainId)
        {
            return _terrains.TryGetValue(terrainId, out var terrain) ? terrain : null;
        }

        public IEnumerable<ITerrain> GetAllTerrains()
        {
            return _terrains.Values;
        }

        public bool HasTerrain(string terrainId)
        {
            return _terrains.ContainsKey(terrainId);
        }

        /// <summary>
        /// 添加或更新地形
        /// </summary>
        /// <param name="terrain">地形信息</param>
        public void AddOrUpdateTerrain(ITerrain terrain)
        {
            if (terrain != null)
            {
                _terrains[terrain.Id] = terrain;
            }
        }

        /// <summary>
        /// 移除地形
        /// </summary>
        /// <param name="terrainId">地形ID</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveTerrain(string terrainId)
        {
            return _terrains.Remove(terrainId);
        }

        /// <summary>
        /// 清空所有地形
        /// </summary>
        public void ClearTerrains()
        {
            _terrains.Clear();
        }

        /// <summary>
        /// 获取地形数量
        /// </summary>
        public int TerrainCount => _terrains.Count;
    }
}
