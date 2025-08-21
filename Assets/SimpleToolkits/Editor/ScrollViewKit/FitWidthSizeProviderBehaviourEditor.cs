namespace SimpleToolkits.Editor
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// 自适应宽度尺寸提供器编辑器
    /// </summary>
    [CustomEditor(typeof(FitWidthSizeProviderBehaviour))]
    public class FitWidthSizeProviderBehaviourEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var provider = (FitWidthSizeProviderBehaviour)target;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("自适应宽度设置", EditorStyles.boldLabel);
            provider.FixedHeight = EditorGUILayout.FloatField("固定高度", provider.FixedHeight);
            provider.WidthPadding = EditorGUILayout.FloatField("宽度内边距", provider.WidthPadding);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(provider);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("宽度自适应视口，高度固定。适用于纵向列表。", MessageType.Info);
        }
    }
}