using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ExcelExporterSettingsWindow : EditorWindow
{
    [MenuItem("Tools/Excel Exporter Settings")]
    private static void Open()
    {
        GetWindow<ExcelExporterSettingsWindow>("Excel Exporter");
    }

    private void OnGUI()
    {
        var settings = ExcelExporterSettings.Instance;

        EditorGUILayout.HelpBox("相对路径：基于 Assets", MessageType.Info);

        settings.csRelativePath =
            EditorGUILayout.TextField("CS Output Path", settings.csRelativePath);

        settings.jsonRelativePath =
            EditorGUILayout.TextField("JSON Output Path", settings.jsonRelativePath);

        if (GUILayout.Button("Save"))
            settings.Save();
    }
}
