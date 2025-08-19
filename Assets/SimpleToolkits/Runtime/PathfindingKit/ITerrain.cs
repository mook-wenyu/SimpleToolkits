using System;

namespace SimpleToolkits
{
    /// <summary>
    /// 地形信息接口，允许用户自定义地形类型和属性
    /// </summary>
    public interface ITerrain : IEquatable<ITerrain>
    {
        /// <summary>
        /// 地形的唯一标识符
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// 地形名称
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// 移动代价倍数（相对于基础移动代价）
        /// </summary>
        float MovementCostMultiplier { get; }
        
        /// <summary>
        /// 是否可通行
        /// </summary>
        bool IsWalkable { get; }
        
        /// <summary>
        /// 地形描述
        /// </summary>
        string Description { get; }
    }

    /// <summary>
    /// 基础地形实现类，用户可以继承或直接使用
    /// </summary>
    [Serializable]
    public class BaseTerrain : ITerrain
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public float MovementCostMultiplier { get; private set; }
        public bool IsWalkable { get; private set; }
        public string Description { get; private set; }

        /// <summary>
        /// 创建基础地形
        /// </summary>
        /// <param name="id">地形ID</param>
        /// <param name="name">地形名称</param>
        /// <param name="costMultiplier">移动代价倍数</param>
        /// <param name="isWalkable">是否可通行</param>
        /// <param name="description">描述</param>
        public BaseTerrain(string id, string name, float costMultiplier, bool isWalkable = true, string description = "")
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            // 强制代价倍数不少于 1，保证 A* 启发式采用几何距离时的可采纳性与高性能
            MovementCostMultiplier = costMultiplier < 1f ? 1f : costMultiplier;
            IsWalkable = isWalkable;
            Description = description ?? "";
        }

        public bool Equals(ITerrain other)
        {
            return other != null && Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is ITerrain terrain && Equals(terrain);
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return $"{Name} (ID: {Id}, Cost: {MovementCostMultiplier:F1}, Walkable: {IsWalkable})";
        }
    }

    /// <summary>
    /// 预定义的常用地形类型，用户可以使用或自定义
    /// </summary>
    public static class CommonTerrains
    {
        public static readonly ITerrain Plains = new BaseTerrain("plains", "平原", 1.0f, true, "标准移动地形");
        public static readonly ITerrain Water = new BaseTerrain("water", "水域", 3.0f, true, "需要游泳的水域");
        public static readonly ITerrain Mountain = new BaseTerrain("mountain", "山地", 4.0f, true, "需要攀爬的山地");
        public static readonly ITerrain Forest = new BaseTerrain("forest", "森林", 1.5f, true, "茂密的森林");
        public static readonly ITerrain Desert = new BaseTerrain("desert", "沙漠", 2.0f, true, "炎热的沙漠");
        public static readonly ITerrain Road = new BaseTerrain("road", "道路", 0.8f, true, "平整的道路");
        public static readonly ITerrain Swamp = new BaseTerrain("swamp", "沼泽", 5.0f, true, "泥泞的沼泽");
        public static readonly ITerrain Ice = new BaseTerrain("ice", "冰面", 1.2f, true, "滑滑的冰面");
        public static readonly ITerrain Impassable = new BaseTerrain("impassable", "不可通行", float.MaxValue, false, "无法通过的区域");
    }
}
