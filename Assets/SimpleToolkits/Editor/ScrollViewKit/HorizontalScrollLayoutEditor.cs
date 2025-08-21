namespace SimpleToolkits.Editor
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// 横向滚动布局编辑器
    /// </summary>
    [CustomEditor(typeof(HorizontalScrollLayout))]
    public class HorizontalScrollLayoutEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var layout = (HorizontalScrollLayout)target;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("横向布局设置", EditorStyles.boldLabel);
            
            layout.Spacing = EditorGUILayout.FloatField("间距", layout.Spacing);
            
            EditorGUILayout.LabelField("内边距");
            EditorGUI.indentLevel++;
            var padding = layout.Padding;
            padding.left = EditorGUILayout.IntField("左", padding.left);
            padding.right = EditorGUILayout.IntField("右", padding.right);
            padding.top = EditorGUILayout.IntField("上", padding.top);
            padding.bottom = EditorGUILayout.IntField("下", padding.bottom);
            layout.Padding = padding;
            EditorGUI.indentLevel--;

            layout.ChildAlignment = (TextAnchor)EditorGUILayout.EnumPopup("子元素对齐", layout.ChildAlignment);
            layout.ReverseArrangement = EditorGUILayout.Toggle("反向排列", layout.ReverseArrangement);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(layout);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("刷新布局"))
            {
                var scrollView = layout.GetComponentInParent<ScrollView>();
                if (scrollView != null && scrollView.IsInitialized)
                {
                    scrollView.Refresh();
                }
            }
        }
    }
}