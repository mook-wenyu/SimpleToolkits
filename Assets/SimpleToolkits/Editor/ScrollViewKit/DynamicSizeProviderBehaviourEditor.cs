namespace SimpleToolkits.Editor
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// 动态尺寸提供器编辑器
    /// </summary>
    [CustomEditor(typeof(DynamicSizeProviderBehaviour))]
    public class DynamicSizeProviderBehaviourEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var provider = (DynamicSizeProviderBehaviour)target;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("动态尺寸设置", EditorStyles.boldLabel);
            provider.DefaultSize = EditorGUILayout.Vector2Field("默认尺寸", provider.DefaultSize);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("缓存设置", EditorStyles.boldLabel);
            provider.EnableCache = EditorGUILayout.Toggle("启用缓存", provider.EnableCache);
            
            if (provider.EnableCache)
            {
                EditorGUI.indentLevel++;
                provider.MaxCacheSize = EditorGUILayout.IntField("最大缓存数", provider.MaxCacheSize);
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(provider);
            }

            EditorGUILayout.Space();
            
            // 缓存信息显示
            if (Application.isPlaying && provider.EnableCache)
            {
                EditorGUILayout.LabelField("缓存信息", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                
                // 使用反射获取缓存计数（因为_sizeCache是私有的）
                var cacheField = typeof(DynamicSizeProviderBehaviour).GetField("_sizeCache", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (cacheField != null)
                {
                    var cache = cacheField.GetValue(provider) as System.Collections.IDictionary;
                    EditorGUILayout.LabelField("当前缓存项数", cache?.Count.ToString() ?? "0");
                }
                
                EditorGUI.indentLevel--;
                
                EditorGUILayout.Space();
                if (GUILayout.Button("清理缓存"))
                {
                    provider.ClearCache();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("需要通过代码设置SetSizeCalculator函数来计算动态尺寸。", MessageType.Info);
            
            if (GUILayout.Button("刷新布局"))
            {
                var scrollView = provider.GetComponentInParent<ScrollView>();
                if (scrollView != null && scrollView.IsInitialized)
                {
                    scrollView.Refresh();
                }
            }
        }
    }
}