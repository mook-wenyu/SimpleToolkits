using System;
using System.Collections.Generic;
using UnityEngine;

public class LocaleMgr
{
    /// <summary>
    /// 语言改变事件
    /// </summary>
    public static event Action<SystemLanguage> OnLanguageChanged;

    private static SystemLanguage _currentLanguage = SystemLanguage.ChineseSimplified;
    /// <summary>
    /// 当前语言
    /// </summary>
    public static SystemLanguage CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            _currentLanguage = value;
            PlayerPrefs.SetInt("CURRENT_LANGUAGE_INDEX", ResMgr.Settings.SupportedLanguages.FindIndex(l => l.language == value));
            OnLanguageChanged?.Invoke(value);
        }
    }

    /// <summary>
    /// 初始化语言管理器
    /// </summary>
    public static void Init()
    {
        int languageIndex = PlayerPrefs.GetInt("CURRENT_LANGUAGE_INDEX", 0);

        if (languageIndex >= ResMgr.Settings.SupportedLanguages.Count)
        {
            languageIndex = 0;
        }

        CurrentLanguage = ResMgr.Settings.SupportedLanguages[languageIndex].language;
    }

    /// <summary>
    /// 切换语言
    /// </summary>
    /// <param name="language">目标语言</param>
    public static void ChangeLanguage(SystemLanguage language)
    {
        CurrentLanguage = language;
    }
}
