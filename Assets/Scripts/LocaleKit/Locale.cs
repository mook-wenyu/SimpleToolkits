using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

public class Locale : IDisposable
{
    /// <summary>
    /// 语言改变事件
    /// </summary>
    public event Action<SystemLanguage> OnLanguageChanged;

    private readonly Dictionary<SystemLanguage, Dictionary<string, string>> _localeDataDict = new();

    private SystemLanguage _currentLanguage = SystemLanguage.ChineseSimplified;
    /// <summary>
    /// 当前语言
    /// </summary>
    public SystemLanguage CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            _currentLanguage = value;
            PlayerPrefs.SetInt("CURRENT_LANGUAGE_INDEX", Mgr.Instance.Settings.SupportedLanguages.FindIndex(l => l.language == value));
            OnLanguageChanged?.Invoke(value);
        }
    }

    public Locale()
    {
        int languageIndex = PlayerPrefs.GetInt("CURRENT_LANGUAGE_INDEX", 0);

        if (languageIndex >= Mgr.Instance.Settings.SupportedLanguages.Count)
        {
            languageIndex = 0;
        }
        ChangeLanguage(Mgr.Instance.Settings.SupportedLanguages[languageIndex].language);
    }

    /// <summary>
    /// 初始化语言数据
    /// </summary>
    public void InitLanguage()
    {
        var languages = Mgr.Instance.Data.GetAll<LanguagesConfig>();
        foreach (var lang in languages)
        {
            var l = Mgr.Instance.Settings.SupportedLanguages.Find(l => l.langKey == lang.langKey);
            if (!_localeDataDict.ContainsKey(l.language))
            {
                _localeDataDict.Add(l.language, new Dictionary<string, string>());
            }
            _localeDataDict[l.language][lang.id] = lang.text;
        }
        Mgr.Instance.Data.Remove<LanguagesConfig>();
    }

    /// <summary>
    /// 获取本地化文本
    /// </summary>
    /// <param name="key">文本键</param>
    [Preserve]
    public string this[string key] => GetText(key);

    /// <summary>
    /// 获取指定语言的本地化文本
    /// </summary>
    /// <param name="language">语言键</param>
    /// <param name="key">文本键</param>
    [Preserve]
    public string this[SystemLanguage language, string key] => GetText(language, key);

    /// <summary>
    /// 切换语言
    /// </summary>
    /// <param name="language">目标语言</param>
    public void ChangeLanguage(SystemLanguage language)
    {
        CurrentLanguage = language;
    }

    /// <summary>
    /// 获取本地化文本
    /// </summary>
    /// <param name="key">文本键</param>
    /// <returns></returns>
    public string GetText(string key)
    {
        return GetText(CurrentLanguage, key);
    }

    /// <summary>
    /// 获取指定语言的本地化文本
    /// </summary>
    /// <param name="language">语言键</param>
    /// <param name="key">文本键</param>
    /// <returns></returns>
    public string GetText(SystemLanguage language, string key)
    {
        if (_localeDataDict.TryGetValue(language, out var dict) && dict.TryGetValue(key, out string text))
        {
            return text;
        }

        return key;
    }

    public void Dispose()
    {
        _localeDataDict.Clear();
        OnLanguageChanged = null;
    }
}
