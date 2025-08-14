using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset;

[CreateAssetMenu(fileName = "Simple Toolkit Settings", menuName = "Simple Toolkits/Simple Toolkit Settings")]
[Serializable]
public class SimpleToolkitSettings : ScriptableObject
{
    [Tooltip("资源加载器类型，使用 YooAsset 需要将该分组 Asset Tags 改为 JsonConfigs")]
    [SerializeField] private LoaderType loaderType = LoaderType.YooAsset;

    [Tooltip("YooAsset 运行模式")]
    [SerializeField] private EPlayMode gamePlayMode = EPlayMode.OfflinePlayMode;

    [Tooltip("YooAsset 资源包信息")]
    public List<YooPackageInfo> yooPackageInfos = new();

    [Tooltip("支持的语言列表")]
    [SerializeField] private List<Language> supportedLanguages = new()
    {
        new Language
        {
            key = "cn",
            language = SystemLanguage.ChineseSimplified
        },
        new Language
        {
            key = "en",
            language = SystemLanguage.English
        }
    };

    [Tooltip("生成 .cs 文件的路径")]
    [SerializeField] private string csOutputPath = "Assets/Scripts/Configs";

    [Tooltip("生成 .json 文件的路径")]
    [SerializeField] private string jsonOutputPath = "Assets/GameRes/JsonConfigs";

    [Tooltip("UI 面板预制体路径（基于 Resources 路径）")]
    [SerializeField] private string uiPanelPath = "Prefabs/UIPanel";
    
    [Tooltip("音频路径（基于 Resources 路径）")]
    [SerializeField] private string audioPath = "Audio";

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
    /// 支持的语言列表
    /// </summary>
    public List<Language> SupportedLanguages => supportedLanguages;

    /// <summary>
    /// Excel 文件路径
    /// </summary>
    public string ExcelFilePath => "Assets/ExcelConfigs";

    /// <summary>
    /// C# 脚本路径
    /// </summary>
    public string CsOutputPath => csOutputPath;

    /// <summary>
    /// Json 路径
    /// </summary>
    public string JsonOutputPath => jsonOutputPath;

    /// <summary>
    /// UI 面板预制体路径
    /// </summary>
    public string UIPanelPath => uiPanelPath;
    
    /// <summary>
    /// 音频路径
    /// </summary>
    public string AudioPath => audioPath;
    

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
