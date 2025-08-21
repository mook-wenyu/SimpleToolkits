namespace SimpleToolkits.Editor
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// 文本内容动态尺寸提供器编辑器
    /// </summary>
    [CustomEditor(typeof(TextContentSizeProviderBehaviour))]
    public class TextContentSizeProviderBehaviourEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var provider = (TextContentSizeProviderBehaviour)target;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("文本动态尺寸设置", EditorStyles.boldLabel);
            provider.BaseSize = EditorGUILayout.Vector2Field("基础尺寸", provider.BaseSize);
            provider.MaxHeight = EditorGUILayout.FloatField("最大高度", provider.MaxHeight);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(provider);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("根据文本内容长度动态计算高度。需要在代码中设置GetTextContent函数。", MessageType.Info);
        }
    }
}