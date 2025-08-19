using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SimpleToolkits
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocaleText : MonoBehaviour
    {
        private TextMeshProUGUI _textMeshProUGUI;
        private TextMeshPro _textMeshPro;

        [TextArea(0, int.MaxValue)] public string langKey;

        private void Start()
        {
            UpdateText(langKey);
        }

        /// <summary>
        /// 根据当前语言的语言键更新文本
        /// </summary>
        /// <param name="key">语言键</param>
        public void UpdateText(string key)
        {
            SetText(GKMgr.Instance.GetObject<LocaleManager>()[key]);
        }

        private void SetText(string text)
        {
            if (!_textMeshProUGUI && !_textMeshPro)
            {
                _textMeshProUGUI = GetComponent<TextMeshProUGUI>();
                _textMeshPro = GetComponent<TextMeshPro>();
            }

            if (_textMeshProUGUI)
            {
                _textMeshProUGUI.text = text;
            }

            if (_textMeshPro)
            {
                _textMeshPro.text = text;
            }
        }
    }
}
