namespace SimpleToolkits.Editor
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// 纵向滚动布局编辑器
    /// </summary>
    [CustomEditor(typeof(VerticalScrollLayout))]
    public class VerticalScrollLayoutEditor : Editor
    {
        private bool _showAdvanced = false;

        public override void OnInspectorGUI()
        {
            var layout = (VerticalScrollLayout)target;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("纵向布局设置", EditorStyles.boldLabel);
            
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
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("工作模式", EditorStyles.boldLabel);
            
            // 简化：只显示两种模式
            if (layout.IsManagedByScrollView)
            {
                EditorGUILayout.HelpBox($"被 {layout.ManagedBy?.name} 管理中…", MessageType.Info);
                if (GUILayout.Button("脱离管理"))
                {
                    layout.ForceIndependentMode = true;
                }
            }
            else
            {
                layout.AutoApplyLayout = EditorGUILayout.Toggle("自动应用布局", layout.AutoApplyLayout);
                layout.UpdateChildrenLayout = EditorGUILayout.Toggle("更新子对象布局", layout.UpdateChildrenLayout);
                layout.ForceIndependentMode = EditorGUILayout.Toggle("强制独立模式", layout.ForceIndependentMode);
                
                if (layout.AutoApplyLayout)
                {
                    EditorGUILayout.HelpBox("独立模式：直接修改RectTransform", MessageType.Info);
                }
            }

            EditorGUILayout.Space();
            
            // 布局信息显示
            EditorGUILayout.LabelField("组件状态", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("布局类型", layout.Type.ToString());
            EditorGUILayout.LabelField("是否竖直", layout.IsVertical ? "是" : "否");
            
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("应用布局效果"))
            {
                layout.ForceUpdate();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}