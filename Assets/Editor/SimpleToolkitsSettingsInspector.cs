using System.Collections;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(SimpleToolkitsSettings))]
public class SimpleToolkitsSettingsInspector : Editor
{
    private SerializedProperty _gamePlayModeProp;
    private SerializedProperty _yooPackageInfosProp;
    private SerializedProperty _supportedLanguagesProp;
    private SerializedProperty _languageExcelFileNameProp;
    private SerializedProperty _csOutputPathProp;
    private SerializedProperty _jsonOutputPathProp;

    private void OnEnable()
    {
        // 获取序列化属性
        _gamePlayModeProp = serializedObject.FindProperty("gamePlayMode");
        _yooPackageInfosProp = serializedObject.FindProperty("yooPackageInfos");
        _supportedLanguagesProp = serializedObject.FindProperty("supportedLanguages");
        _languageExcelFileNameProp = serializedObject.FindProperty("languageExcelFileName");
        _csOutputPathProp = serializedObject.FindProperty("csOutputPath");
        _jsonOutputPathProp = serializedObject.FindProperty("jsonOutputPath");
    }

    public override void OnInspectorGUI()
    {
        var settings = (SimpleToolkitsSettings)target;

        // 更新序列化对象
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("简单工具包设置", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("YooAsset 加载器请启用 Addressable 加载", MessageType.Info);
        EditorGUILayout.Space();

        // YooAsset 设置
        EditorGUILayout.LabelField("YooAsset 设置", EditorStyles.boldLabel);

        // YooAsset 运行模式
        EditorGUILayout.PropertyField(_gamePlayModeProp, new GUIContent("YooAsset 运行模式"));

        // YooAsset 资源包信息
        EditorGUILayout.PropertyField(_yooPackageInfosProp, new GUIContent("YooAsset 资源包信息"), true);

        EditorGUILayout.Space();

        // 刷新包信息按钮
        if (GUILayout.Button("刷新包信息", GUILayout.Height(30)))
        {
            RefreshPackageInfos(settings);
        }

        EditorGUILayout.Space();
        // 本地化设置
        EditorGUILayout.LabelField("本地化设置", EditorStyles.boldLabel);

        // 支持的语言列表
        EditorGUILayout.PropertyField(_supportedLanguagesProp, new GUIContent("支持的语言"), true);
        EditorGUILayout.HelpBox("请确保语言列表中包含一种语言，否则可能无法正常显示文本！", MessageType.Warning);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.Space();
            // 语言配置表文件名
            EditorGUILayout.PropertyField(_languageExcelFileNameProp, new GUIContent("语言配置表文件名"));
            EditorGUILayout.Space();
        }
        EditorGUILayout.HelpBox("请确保语言配置表文件名正确，否则可能无法正常显示文本！", MessageType.Warning);

        EditorGUILayout.Space();

        // 配置数据路径设置
        EditorGUILayout.LabelField("配置数据路径设置", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Excel 文件路径", settings.ExcelFilePath);
        }
        // CS 输出路径
        EditorGUILayout.PropertyField(_csOutputPathProp, new GUIContent("CS 文件路径", "生成 .cs 文件的路径"));
        // JSON 输出路径
        EditorGUILayout.PropertyField(_jsonOutputPathProp, new GUIContent("JSON 文件路径", "生成 .json 文件的路径"));
        EditorGUILayout.HelpBox("需要将该分组 Asset Tags 改为 JsonConfigs", MessageType.Info);

        EditorGUILayout.Space();


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
    private void RefreshPackageInfos(SimpleToolkitsSettings settings)
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
    /// <param name="settings">SimpleToolkitsSettings 实例</param>
    /// <param name="collectorPackageNames">从 AssetBundleCollectorSetting 获取的包名列表</param>
    private void SyncPackageInfos(SimpleToolkitsSettings settings, List<string> collectorPackageNames)
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
