namespace SimpleToolkits.Editor
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// 自适应高度尺寸提供器编辑器
    /// </summary>
    [CustomEditor(typeof(FitHeightSizeProviderBehaviour))]
    public class FitHeightSizeProviderBehaviourEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var provider = (FitHeightSizeProviderBehaviour)target;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("自适应高度设置", EditorStyles.boldLabel);
            provider.FixedWidth = EditorGUILayout.FloatField("固定宽度", provider.FixedWidth);
            provider.HeightPadding = EditorGUILayout.FloatField("高度内边距", provider.HeightPadding);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(provider);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("高度自适应视口，宽度固定。适用于横向列表。", MessageType.Info);
        }
    }
}