namespace SimpleToolkits.Editor
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// 网格滚动布局编辑器
    /// </summary>
    [CustomEditor(typeof(GridScrollLayout))]
    public class GridScrollLayoutEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var layout = (GridScrollLayout)target;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("网格布局设置", EditorStyles.boldLabel);
            
            layout.CellSize = EditorGUILayout.Vector2Field("Cell尺寸", layout.CellSize);
            layout.Spacing = EditorGUILayout.FloatField("间距", layout.Spacing);
            layout.ConstraintCount = EditorGUILayout.IntField("约束数量", layout.ConstraintCount);
            layout.Axis = (GridAxis)EditorGUILayout.EnumPopup("轴向", layout.Axis);
            
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
            EditorGUILayout.HelpBox("网格布局使用固定尺寸，确保CellSize设置正确。", MessageType.Info);
            
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