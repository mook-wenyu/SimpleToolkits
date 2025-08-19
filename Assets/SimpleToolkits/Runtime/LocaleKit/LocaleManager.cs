using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace SimpleToolkits
{
    public class LocaleManager : IDisposable
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
                var languageIndex = GKMgr.Instance.Settings.SupportedLanguages.FindIndex(l => l.language == value);
                PlayerPrefs.SetInt("CURRENT_LANGUAGE_INDEX", languageIndex);
                OnLanguageChanged?.Invoke(value);
            }
        }

        public LocaleManager()
        {
            var languageIndex = PlayerPrefs.GetInt("CURRENT_LANGUAGE_INDEX", 0);

            if (languageIndex >= GKMgr.Instance.Settings.SupportedLanguages.Count)
            {
                languageIndex = 0;
            }
            ChangeLanguage(GKMgr.Instance.Settings.SupportedLanguages[languageIndex].language);
        }

        /// <summary>
        /// 初始化语言数据
        /// </summary>
        public void InitLanguage()
        {
            // 获取配置数据
            var configData = GKMgr.Instance.GetObject<ConfigManager>();
            var type = TypeReflectionUtility.FindType("LanguagesConfig", "Assembly-CSharp");
            if (type == null)
            {
                Debug.LogError("LanguagesConfig type not found");
                return;
            }
            var langKeyField = type.GetField("langKey");
            var textField = type.GetField("text");
            var languages = configData.GetAll(type);
            if (languages == null || languages.Count == 0)
            {
                Debug.LogError("LanguagesConfig is empty");
                return;
            }
            foreach (var lang in languages)
            {
                var langKey = langKeyField?.GetValue(lang) as string;
                var text = textField?.GetValue(lang) as string;
                if (string.IsNullOrEmpty(langKey) || string.IsNullOrEmpty(text))
                {
                    Debug.LogWarning("LanguagesConfig has null or empty langKey or text, skipping.");
                    continue;
                }

                // 查找匹配的语言配置
                var supportedLanguage = GKMgr.Instance.Settings.SupportedLanguages.Find(l => l.langKey == langKey);

                if (!_localeDataDict.ContainsKey(supportedLanguage.language))
                {
                    _localeDataDict[supportedLanguage.language] = new Dictionary<string, string>();
                }
                if (!string.IsNullOrEmpty(lang.id))
                {
                    _localeDataDict[supportedLanguage.language][lang.id] = text;
                }
                else
                {
                    Debug.LogWarning($"LanguagesConfig has null or empty id for langKey '{langKey}', skipping.");
                }
            }

            // 清理配置数据
            configData.Remove(type);
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
            if (_localeDataDict.TryGetValue(language, out var dict) && dict.TryGetValue(key, out var text))
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
}
