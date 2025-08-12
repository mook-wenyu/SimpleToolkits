using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset;

[CreateAssetMenu(fileName = "Simple Toolkit Settings", menuName = "Simple Toolkits/Simple Toolkit Settings")]
public class SimpleToolkitSettings : ScriptableObject
{
    [Tooltip("资源加载器类型，使用 YooAsset 需要将该分组 Asset Tags 改为 JsonConfigs")]
    public LoaderType loaderType = LoaderType.Resources;
    
    [Tooltip("YooAsset 运行模式")]
    public EPlayMode gamePlayMode = EPlayMode.OfflinePlayMode;
    
    [Tooltip("YooAsset 资源包信息")]
    public List<YooPackageInfo> yooPackageInfos = new();

    [Tooltip("生成 .cs 文件的路径")]
    public string csRelativePath = "Assets/Scripts/Configs";

    [Tooltip("生成 .json 文件的路径")]
    public string jsonRelativePath = "Assets/Resources/JsonConfigs";
    
    [Tooltip("UI 面板预制体路径（基于 Resources 路径）")]
    public string uiPanelPath = "Prefabs/UIPanel";

    /// <summary>
    /// 资源加载器类型
    /// </summary>
    public LoaderType LoaderType => loaderType;
    
    /// <summary>
    /// YooAsset 运行模式
    /// </summary>
    public EPlayMode GamePlayMode => gamePlayMode;
    
    /// <summary>
    /// YooAsset 资源包信息列表
    /// </summary>
    public List<YooPackageInfo> YooPackageInfos => yooPackageInfos;
    
    /// <summary>
    /// Excel 文件路径
    /// </summary>
    public string ExcelRelativePath => "Assets/ExcelConfigs";

    /// <summary>
    /// C# 脚本路径
    /// </summary>
    public string CsRelativePath => csRelativePath;

    /// <summary>
    /// Json 路径
    /// </summary>
    public string JsonRelativePath => loaderType == LoaderType.YooAsset ? "JsonConfigs" : jsonRelativePath;
    
    /// <summary>
    /// UI 面板预制体路径
    /// </summary>
    public string UIPanelPath => loaderType == LoaderType.YooAsset ? "" : uiPanelPath;
    
    
    
#if UNITY_EDITOR
    /* ---------- 单例访问 ---------- */
    private const string AssetName = "SimpleToolkitSettings.asset";

    private static SimpleToolkitSettings _instance;
    public static SimpleToolkitSettings Instance
    {
        get
        {
            if (!_instance)
            {
                const string dir = "Assets/Resources";
                string path = Path.Combine(dir, AssetName);

                _instance = AssetDatabase.LoadAssetAtPath<SimpleToolkitSettings>(path.Replace("\\", "/"));
                if (!_instance)
                {
                    if (Directory.Exists(dir) is false)
                    {
                        Directory.CreateDirectory(dir);
                    }
                    _instance = CreateInstance<SimpleToolkitSettings>();
                    AssetDatabase.CreateAsset(_instance, path.Replace("\\", "/"));
                    AssetDatabase.SaveAssets();
                }
            }
            return _instance;
        }
    }

    public void Save()
    {
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssetIfDirty(this);
    }

#endif
}
