using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset;

namespace SimpleToolkits
{
    [CreateAssetMenu(fileName = "Simple Toolkits Settings", menuName = "Simple Toolkits/Simple Toolkits Settings")]
    [Serializable]
    public class SimpleToolkitsSettings : ScriptableObject
    {
        [Tooltip("YooAsset 运行模式")]
        [SerializeField] private EPlayMode gamePlayMode = EPlayMode.OfflinePlayMode;

        [Tooltip("YooAsset 资源包信息")]
        public List<YooPackageInfo> yooPackageInfos = new();

        [Tooltip("支持的语言列表")]
        [SerializeField] private List<Language> supportedLanguages = new()
        {
            new Language
            {
                langKey = "cn",
                language = SystemLanguage.ChineseSimplified
            },
            new Language
            {
                langKey = "en",
                language = SystemLanguage.English
            }
        };
        [Tooltip("语言配置表文件名")]
        [SerializeField] private string languageExcelFileName = "Languages";

        [Tooltip("生成 .cs 文件的路径")]
        [SerializeField] private string csOutputPath = "Assets/Scripts/Configs";
        [Tooltip("生成 .json 文件的路径，需要将该分组 Asset Tags 改为 JsonConfigs")]
        [SerializeField] private string jsonOutputPath = "Assets/GameRes/JsonConfigs";

        [Header("数据存储设置")]
        [Tooltip("存储方式类型")]
        [SerializeField] private StorageType storageType = StorageType.Auto;
        [Tooltip("是否启用数据加密")]
        [SerializeField] private bool enableEncryption = false;
        [Tooltip("加密密钥（留空则使用默认密钥）")]
        [SerializeField] private string encryptionKey = "";
        [Tooltip("自动保存间隔（秒，0表示禁用自动保存）")]
        [SerializeField] private float autoSaveInterval = 0f;

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
        /// 存储方式类型
        /// </summary>
        public StorageType StorageType => storageType;

        /// <summary>
        /// 配置文件名（固定为 Config.json）
        /// </summary>
        public string ConfigFileName => "Config";

        /// <summary>
        /// 是否启用数据加密
        /// </summary>
        public bool EnableEncryption => enableEncryption;

        /// <summary>
        /// 获取加密密钥
        /// </summary>
        public string EncryptionKey => string.IsNullOrEmpty(encryptionKey) ? "SimpleToolkits_DefaultKey_2025" : encryptionKey;

        /// <summary>
        /// 自动保存间隔（秒）
        /// </summary>
        public float AutoSaveInterval => autoSaveInterval;


#if UNITY_EDITOR
        /* ---------- 单例访问 ---------- */
        private const string AssetName = "SimpleToolkitsSettings.asset";

        private static SimpleToolkitsSettings _instance;
        public static SimpleToolkitsSettings Instance
        {
            get
            {
                if (!_instance)
                {
                    const string dir = "Assets/Resources";
                    string path = Path.Combine(dir, AssetName);

                    _instance = AssetDatabase.LoadAssetAtPath<SimpleToolkitsSettings>(path.Replace("\\", "/"));
                    if (!_instance)
                    {
                        if (Directory.Exists(dir) is false)
                        {
                            Directory.CreateDirectory(dir);
                        }
                        _instance = CreateInstance<SimpleToolkitsSettings>();
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
}
