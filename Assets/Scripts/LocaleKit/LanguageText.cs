using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[Serializable]
public class LanguageText
{
    [SerializeField] public int languageIndex = 0;
    [HideInInspector] public SystemLanguage language;

    [TextArea(0, int.MaxValue)] public string text;
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(LanguageText))]
public class LanguageTextDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.serializedObject.isEditingMultipleObjects)
            return EditorGUIUtility.singleLineHeight;

        var textProperty = property.FindPropertyRelative("text");
        float height = EditorGUI.GetPropertyHeight(textProperty);
        return height + EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.serializedObject.isEditingMultipleObjects)
            return;

        var languageIndex = property.FindPropertyRelative("languageIndex");
        var language = property.FindPropertyRelative("language");
        var text = property.FindPropertyRelative("text");

        EditorGUI.BeginProperty(position, label, property);

        var languageRect = new Rect(position.x, position.y, position.width,
            EditorGUIUtility.singleLineHeight);
        var languages = SimpleToolkitSettings.Instance.SupportedLanguages;

        languageIndex.intValue = EditorGUI.Popup(languageRect, languageIndex.intValue,
            languages.Select(l => l.ToString()).ToArray());

        if (language.intValue != (int)languages[languageIndex.intValue].language)
        {
            language.intValue = (int)languages[languageIndex.intValue].language;
        }

        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        float textHeight = EditorGUI.GetPropertyHeight(text);

        var textRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 5, position.width,
            textHeight - EditorGUIUtility.singleLineHeight - 12);
        text.stringValue = EditorGUI.TextArea(textRect, text.stringValue);


        var previewButtonRect = new Rect(position.x, position.y + 10
                                                                + textHeight - 12,
            position.width, EditorGUIUtility.singleLineHeight);
        if (GUI.Button(previewButtonRect, "预览 "))
        {
            var localeText = property.serializedObject.targetObject as LocaleText;
            if (localeText) localeText.UpdateText((SystemLanguage)language.intValue);
            EditorUtility.SetDirty(property.serializedObject.targetObject);
        }
        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }
}
#endif
