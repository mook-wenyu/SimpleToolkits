using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "ExcelExporterSettings", menuName = "SimpleToolkits/ExcelExporterSettings")]
public class ExcelExporterSettings : ScriptableObject
{
    [Tooltip("生成 .cs 文件的相对路径（基于 Assets）")]
    public string csRelativePath = "Scripts/Configs";

    [Tooltip("生成 .json 文件的相对路径（基于 Assets）")]
    public string jsonRelativePath = "Resources/JsonConfigs";
    
    [Tooltip("使用 YooAsset 需要将该分组 Asset Tags 改为 JsonConfigs")]
    public bool useYooAsset = false;

    public string ExcelFullPath => Path.Combine(Application.dataPath, "ExcelConfigs");
    /// <summary>
    /// C# 脚本路径
    /// </summary>
    public string CsFullPath => Path.Combine(Application.dataPath, csRelativePath);
    /// <summary>
    /// Json 路径
    /// </summary>
    public string JsonFullPath => Path.Combine(Application.dataPath, jsonRelativePath);
    /// <summary>
    /// 使用 YooAsset
    /// </summary>
    public bool UseYooAsset => useYooAsset;

    #if UNITY_EDITOR
    /* ---------- 单例访问 ---------- */
    private const string AssetName = "ExcelExporterSettings.asset";

    private static ExcelExporterSettings _instance;
    public static ExcelExporterSettings Instance
    {
        get
        {
            if (!_instance)
            {
                string dir = Path.Combine("Assets", "Resources");
                string path = Path.Combine(dir, AssetName);

                _instance = AssetDatabase.LoadAssetAtPath<ExcelExporterSettings>(path.Replace("\\", "/"));
                if (!_instance)
                {
                    if (Directory.Exists(dir) is false)
                    {
                        Directory.CreateDirectory(dir);
                    }
                    _instance = CreateInstance<ExcelExporterSettings>();
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
