using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SimpleToolkits.Editor
{
    /// <summary>
    /// 自定义工具栏扩展
    /// </summary>
    public static class CustomToolbarExtension
    {
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            EditorApplication.delayCall += OnEditorApplicationDelayCall;
        }

        private static void OnEditorApplicationDelayCall()
        {
            Type barType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
            var toolbars = Resources.FindObjectsOfTypeAll(barType);
            var toolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
            if (toolbar != null)
            {
                var root = toolbar.GetType().GetField("m_Root", BindingFlags.NonPublic
                                                                | BindingFlags.Instance);
                var mRoot = root.GetValue(toolbar) as VisualElement;
                var toolbarZone = mRoot.Q("ToolbarZoneRightAlign");
                var container = new IMGUIContainer();
                container.style.flexGrow = 0;
                container.onGUIHandler += OnGUI;
                toolbarZone.Add(container);
            }
        }

        private static void OnGUI()
        {
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            // 创建按钮样式
            var buttonStyle = new GUIStyle("Command")
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageLeft,
                fontStyle = FontStyle.Normal,
                fixedWidth = 104,
                fixedHeight = 20
            };

            // 创建按钮内容
            var openProjectContent = new GUIContent("Open C# Project", "在配置的外部 IDE 中打开当前 Unity 项目");

            var refreshContent = new GUIContent("Refresh", "刷新资源");

            // 绘制按钮
            if (GUILayout.Button(openProjectContent, buttonStyle))
            {
                OpenProjectInExternalIDE();
            }

            if (GUILayout.Button(refreshContent, buttonStyle))
            {
                RefreshChange();
            }

            GUILayout.EndHorizontal();
        }

        private static void RefreshChange()
        {
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 在外部 IDE 中打开当前项目
        /// </summary>
        private static void OpenProjectInExternalIDE()
        {
            try
            {
                // 方法1：使用 Unity 的菜单命令（最可靠的方法）
                EditorApplication.ExecuteMenuItem("Assets/Open C# Project");
                Debug.Log("CustomToolbarExtension: 已通过菜单命令打开外部 IDE");
            }
            catch (Exception ex)
            {
                Debug.LogError($"CustomToolbarExtension: 打开外部 IDE 失败 - {ex.Message}");

                // 显示错误对话框
                EditorUtility.DisplayDialog(
                    "打开 IDE 失败",
                    "无法打开外部 IDE。请确保在 Edit > Preferences > External Tools 中正确配置了外部脚本编辑器。",
                    "确定"
                );
            }
        }

        /// <summary>
        /// 获取当前配置的外部脚本编辑器路径
        /// </summary>
        /// <returns>外部脚本编辑器路径，如果未配置则返回 null</returns>
        private static string GetExternalScriptEditorPath()
        {
            try
            {
                // 使用 EditorPrefs 获取外部脚本编辑器路径
                var editorPath = EditorPrefs.GetString("kScriptsDefaultApp");
                return string.IsNullOrEmpty(editorPath) ? null : editorPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"CustomToolbarExtension: 获取外部脚本编辑器路径失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 检查是否配置了外部脚本编辑器
        /// </summary>
        /// <returns>如果配置了外部脚本编辑器返回 true，否则返回 false</returns>
        public static bool IsExternalScriptEditorConfigured()
        {
            var editorPath = GetExternalScriptEditorPath();
            return !string.IsNullOrEmpty(editorPath) && System.IO.File.Exists(editorPath);
        }
    }

    /// <summary>
    /// 为自定义工具栏扩展提供菜单项
    /// </summary>
/*public static class CustomToolbarMenu
{
    [MenuItem("Tools/Custom Toolbar/Open IDE %&o")]
    public static void OpenIDEMenuItem()
    {
        if (CustomToolbarExtension.IsExternalScriptEditorConfigured())
        {
            EditorApplication.ExecuteMenuItem("Assets/Open C# Project");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "未配置外部编辑器",
                "请在 Edit > Preferences > External Tools 中配置外部脚本编辑器。",
                "打开设置",
                "取消"
            );

            if (EditorUtility.DisplayDialog("", "", "打开设置", "取消"))
            {
                SettingsService.OpenUserPreferences("Preferences/External Tools");
            }
        }
    }

    [MenuItem("Tools/Custom Toolbar/Open IDE %&o", true)]
    public static bool ValidateOpenIDEMenuItem()
    {
        return !EditorApplication.isPlaying;
    }
}
*/
}
