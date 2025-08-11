using System;
using UnityEngine;

/// <summary>
/// UI对象池配置类
/// </summary>
[Serializable]
public class UIPoolConfig
{
    [Header("对象池基本配置")]
    [Tooltip("是否启用对象池")]
    public bool enablePool = true;
    
    [Tooltip("默认容量")]
    [Range(1, 50)]
    public int defaultCapacity = 5;
    
    [Tooltip("最大容量")]
    [Range(5, 100)]
    public int maxSize = 20;
    
    [Tooltip("是否启用集合检查（调试用）")]
    public bool collectionCheck = true;
    
    [Header("预热配置")]
    [Tooltip("是否在初始化时预热对象池")]
    public bool preWarm = false;
    
    [Tooltip("预热数量")]
    [Range(0, 20)]
    public int preWarmCount = 3;
    
    [Header("清理配置")]
    [Tooltip("自动清理间隔（秒）")]
    [Range(30f, 300f)]
    public float autoCleanInterval = 60f;
    
    [Tooltip("空闲对象存活时间（秒）")]
    [Range(10f, 120f)]
    public float idleLifetime = 30f;
    
    [Tooltip("是否启用自动清理")]
    public bool enableAutoClean = true;

    /// <summary>
    /// 获取默认配置
    /// </summary>
    public static UIPoolConfig Default => new UIPoolConfig
    {
        enablePool = true,
        defaultCapacity = 5,
        maxSize = 20,
        collectionCheck = false,
        preWarm = false,
        preWarmCount = 3,
        autoCleanInterval = 60f,
        idleLifetime = 30f,
        enableAutoClean = true
    };

    /// <summary>
    /// 获取高性能配置（适用于频繁创建销毁的UI）
    /// </summary>
    public static UIPoolConfig HighPerformance => new UIPoolConfig
    {
        enablePool = true,
        defaultCapacity = 10,
        maxSize = 50,
        collectionCheck = false,
        preWarm = true,
        preWarmCount = 5,
        autoCleanInterval = 120f,
        idleLifetime = 60f,
        enableAutoClean = true
    };

    /// <summary>
    /// 获取内存优化配置（适用于内存敏感的场景）
    /// </summary>
    public static UIPoolConfig MemoryOptimized => new UIPoolConfig
    {
        enablePool = true,
        defaultCapacity = 2,
        maxSize = 10,
        collectionCheck = false,
        preWarm = false,
        preWarmCount = 0,
        autoCleanInterval = 30f,
        idleLifetime = 15f,
        enableAutoClean = true
    };

    /// <summary>
    /// 获取调试配置
    /// </summary>
    public static UIPoolConfig Debug => new UIPoolConfig
    {
        enablePool = true,
        defaultCapacity = 3,
        maxSize = 15,
        collectionCheck = true,
        preWarm = false,
        preWarmCount = 1,
        autoCleanInterval = 60f,
        idleLifetime = 30f,
        enableAutoClean = false
    };
}

/// <summary>
/// UI对象池管理器配置
/// </summary>
[CreateAssetMenu(fileName = "UIPoolManagerConfig", menuName = "UI/Pool Manager Config")]
public class UIPoolManagerConfig : ScriptableObject
{
    [Header("全局配置")]
    [SerializeField] private UIPoolConfig globalConfig = UIPoolConfig.Default;
    
    [Header("特定面板配置")]
    [SerializeField] private UIPoolPanelConfig[] panelConfigs;
    
    /// <summary>
    /// 获取全局配置
    /// </summary>
    public UIPoolConfig GlobalConfig => globalConfig;
    
    /// <summary>
    /// 获取指定面板的配置
    /// </summary>
    public UIPoolConfig GetPanelConfig(string panelName)
    {
        if (panelConfigs != null)
        {
            foreach (var config in panelConfigs)
            {
                if (config.panelName == panelName)
                {
                    return config.poolConfig;
                }
            }
        }
        
        return globalConfig;
    }
    
    /// <summary>
    /// 获取指定预制体路径的配置
    /// </summary>
    public UIPoolConfig GetPrefabConfig(string prefabPath)
    {
        if (panelConfigs != null)
        {
            foreach (var config in panelConfigs)
            {
                if (config.prefabPath == prefabPath)
                {
                    return config.poolConfig;
                }
            }
        }
        
        return globalConfig;
    }
}

/// <summary>
/// 特定面板的对象池配置
/// </summary>
[Serializable]
public class UIPoolPanelConfig
{
    [Header("面板标识")]
    [Tooltip("面板名称")]
    public string panelName;
    
    [Tooltip("预制体路径")]
    public string prefabPath;
    
    [Header("对象池配置")]
    public UIPoolConfig poolConfig = UIPoolConfig.Default;
}
