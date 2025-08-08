using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SimpleToolkits
{
    [CreateAssetMenu(fileName = "ExcelExporterSettings", menuName = "SimpleToolkits/ExcelExporterSettings")]
    public class ExcelExporterSettings : ScriptableObject
    {
        [Tooltip("生成 .cs 文件的相对路径（基于 Assets）")]
        public string csRelativePath = "Scripts/Configs";

        [Tooltip("生成 .json 文件的相对路径（基于 Assets）")]
        public string jsonRelativePath = "Resources/JsonConfigs";

        public string ExcelFullPath => Path.Combine(Application.dataPath, "ExcelConfigs");
        public string CsFullPath => Path.Combine(Application.dataPath, csRelativePath);
        public string JsonFullPath => Path.Combine(Application.dataPath, jsonRelativePath);

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
    }
}
