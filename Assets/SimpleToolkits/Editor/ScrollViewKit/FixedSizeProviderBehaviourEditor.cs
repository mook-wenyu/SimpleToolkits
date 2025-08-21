namespace SimpleToolkits.Editor
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// 固定尺寸提供器编辑器
    /// </summary>
    [CustomEditor(typeof(FixedSizeProviderBehaviour))]
    public class FixedSizeProviderBehaviourEditor : Editor
    {
        private bool _showAdvanced = false;

        public override void OnInspectorGUI()
        {
            var provider = (FixedSizeProviderBehaviour)target;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("固定尺寸设置", EditorStyles.boldLabel);
            provider.FixedSize = EditorGUILayout.Vector2Field("固定尺寸", provider.FixedSize);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("工作模式", EditorStyles.boldLabel);
            
            // 简化：只显示两种模式
            if (provider.IsManagedByScrollView)
            {
                EditorGUILayout.HelpBox($"被 {provider.ManagedBy?.name} 管理中…", MessageType.Info);
                if (GUILayout.Button("脱离管理"))
                {
                    provider.ForceIndependentMode = true;
                }
            }
            else
            {
                provider.AutoResizeSelf = EditorGUILayout.Toggle("自动调整尺寸", provider.AutoResizeSelf);
                provider.ForceIndependentMode = EditorGUILayout.Toggle("强制独立模式", provider.ForceIndependentMode);
                
                if (provider.AutoResizeSelf)
                {
                    EditorGUILayout.HelpBox("独立模式：自动调整自身尺寸", MessageType.Info);
                }
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(provider);
            }

            EditorGUILayout.Space();
            
            // 组件信息显示

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("尺寸提供器状态", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("变尺寸支持", provider.SupportsVariableSize ? "否" : "是");
            var avgSize = provider.GetAverageSize(new Vector2(300, 100));
            EditorGUILayout.LabelField("平均尺寸", $"{avgSize.x:F1} x {avgSize.y:F1}");
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("应用尺寸效果"))
            {
                provider.ForceUpdate();
            }
        }
    }
}