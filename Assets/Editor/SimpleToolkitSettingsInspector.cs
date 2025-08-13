using System.Collections;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(SimpleToolkitSettings))]
public class SimpleToolkitSettingsInspector : Editor
{
    private SerializedProperty _loaderTypeProp;
    private SerializedProperty _gamePlayModeProp;
    private SerializedProperty _yooPackageInfosProp;
    private SerializedProperty _csRelativePathProp;
    private SerializedProperty _jsonRelativePathProp;
    private SerializedProperty _uiPanelPathProp;

    private void OnEnable()
    {
        // 获取序列化属性
        _loaderTypeProp = serializedObject.FindProperty("loaderType");
        _gamePlayModeProp = serializedObject.FindProperty("gamePlayMode");
        _yooPackageInfosProp = serializedObject.FindProperty("yooPackageInfos");
        _csRelativePathProp = serializedObject.FindProperty("csRelativePath");
        _jsonRelativePathProp = serializedObject.FindProperty("jsonRelativePath");
        _uiPanelPathProp = serializedObject.FindProperty("uiPanelPath");
    }

    public override void OnInspectorGUI()
    {
        var settings = (SimpleToolkitSettings)target;

        // 更新序列化对象
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Simple Toolkit Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 资源加载器类型
        EditorGUILayout.PropertyField(_loaderTypeProp, new GUIContent("Loader Type", "资源加载器类型"));

        // 条件显示：仅当 loaderType 为 YooAsset 时显示相关UI元素
        if ((LoaderType)_loaderTypeProp.enumValueIndex == LoaderType.YooAsset)
        {
            EditorGUILayout.Space();
            
            // YooAsset 设置
            EditorGUILayout.LabelField("YooAsset 设置", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox("使用 YooAsset 需要将该分组 Asset Tags 改为 JsonConfigs", MessageType.Info);

            // YooAsset 运行模式
            EditorGUILayout.PropertyField(_gamePlayModeProp, new GUIContent("Game Play Mode", "YooAsset 运行模式"));

            // YooAsset 资源包信息
            EditorGUILayout.PropertyField(_yooPackageInfosProp, new GUIContent("YooAsset Package Infos", "YooAsset 资源包信息"), true);

            EditorGUILayout.Space();

            // 刷新包信息按钮
            if (GUILayout.Button("刷新包信息", GUILayout.Height(30)))
            {
                RefreshPackageInfos(settings);
            }
            
        }

        EditorGUILayout.Space();

        // 配置数据路径设置
        EditorGUILayout.LabelField("配置数据路径设置", EditorStyles.boldLabel);

        // CS 输出路径
        EditorGUILayout.PropertyField(_csRelativePathProp, new GUIContent("CS Output Path", "生成 .cs 文件的路径"));

        // JSON 输出路径
        EditorGUILayout.PropertyField(_jsonRelativePathProp, new GUIContent("JSON Output Path", "生成 .json 文件的路径"));

        EditorGUILayout.Space();
        
        // UI 面板路径设置
        EditorGUILayout.LabelField("UI 面板路径设置", EditorStyles.boldLabel);
        
        // UI 面板路径
        EditorGUILayout.PropertyField(_uiPanelPathProp, new GUIContent("UI Panel Path", "UI 面板预制体路径"));
        EditorGUILayout.HelpBox("UI 面板预制体路径仅在使用 Resources 加载器时有效," +
                                "YooAsset 加载器请使用 Addressable 加载", MessageType.Info);
        
        EditorGUILayout.Space();

        // 只读信息显示
        EditorGUILayout.LabelField("只读信息", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Excel File Path", settings.ExcelRelativePath);
            EditorGUILayout.TextField("CS File Path", settings.CsRelativePath);
            EditorGUILayout.TextField("JSON File Path", settings.JsonRelativePath);
            EditorGUILayout.TextField("UI Panel Path", settings.UIPanelPath);
        }

        EditorGUILayout.Space();
        
        // 应用修改的属性
        if (serializedObject.hasModifiedProperties)
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
        }
    }

    /// <summary>
    /// 刷新包信息，同步 AssetBundleCollectorSetting 和 yooPackageInfos 之间的数据
    /// </summary>
    private void RefreshPackageInfos(SimpleToolkitSettings settings)
    {
        try
        {
            // 加载 AssetBundleCollectorSetting
            var assetBundleCollectorSetting = AssetDatabase.LoadAssetAtPath<AssetBundleCollectorSetting>("Assets/AssetBundleCollectorSetting.asset");

            if (!assetBundleCollectorSetting)
            {
                EditorUtility.DisplayDialog("错误", "未找到 AssetBundleCollectorSetting.asset 文件，请确保文件存在于 Assets/ 目录下。", "确定");
                Debug.LogError("AssetBundleCollectorSetting.asset 文件不存在");
                return;
            }

            // 获取 AssetBundleCollectorSetting 中的包信息
            var collectorPackages = assetBundleCollectorSetting.Packages.Select(p => p.PackageName).ToList();

            if (collectorPackages.Count == 0)
            {
                collectorPackages.Add(Constants.DefaultPackageName);
                Debug.LogWarning("未能从 AssetBundleCollectorSetting 中获取包信息，添加默认包：" + Constants.DefaultPackageName);
            }

            // 执行同步逻辑
            SyncPackageInfos(settings, collectorPackages);

            // 标记为已修改并保存
            EditorUtility.SetDirty(settings);
            serializedObject.Update();

            Debug.Log($"包信息同步完成，当前包数量：{settings.yooPackageInfos.Count}");
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog("错误", $"刷新包信息时发生错误：{ex.Message}", "确定");
            Debug.LogError($"刷新包信息失败：{ex}");
        }
    }

    /// <summary>
    /// 同步包信息数据
    /// </summary>
    /// <param name="settings">SimpleToolkitSettings 实例</param>
    /// <param name="collectorPackageNames">从 AssetBundleCollectorSetting 获取的包名列表</param>
    private void SyncPackageInfos(SimpleToolkitSettings settings, List<string> collectorPackageNames)
    {
        settings.yooPackageInfos ??= new List<YooPackageInfo>();

        var currentPackageNames = settings.yooPackageInfos.Select(p => p.packageName).ToList();

        // 删除在 yooPackageInfos 中存在但在 AssetBundleCollectorSetting 中不存在的包
        for (int i = settings.yooPackageInfos.Count - 1; i >= 0; i--)
        {
            var packageInfo = settings.yooPackageInfos[i];
            if (!collectorPackageNames.Contains(packageInfo.packageName))
            {
                settings.yooPackageInfos.RemoveAt(i);
                Debug.Log($"删除包信息：{packageInfo.packageName}");
            }
        }

        // 添加在 AssetBundleCollectorSetting 中存在但在 yooPackageInfos 中不存在的包
        foreach (string packageName in collectorPackageNames)
        {
            if (!currentPackageNames.Contains(packageName))
            {
                var newPackageInfo = new YooPackageInfo(
                    packageName: packageName,
                    hostServerURL: "",
                    fallbackHostServerURL: "",
                    isDefaultPackage: packageName == Constants.DefaultPackageName
                );

                settings.yooPackageInfos.Add(newPackageInfo);
                Debug.Log($"添加包信息：{packageName}");
            }
        }

    }
}
