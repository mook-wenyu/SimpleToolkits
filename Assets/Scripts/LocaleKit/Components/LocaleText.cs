using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class LocaleText : MonoBehaviour
{
    private TextMeshProUGUI _textMeshProUGUI;
    private TextMeshPro _textMeshPro;

    public List<LanguageText> languageTexts;

    public void SetText(string text)
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

    public void UpdateText(SystemLanguage language)
    {
        SetText(languageTexts.First(lt => lt.language == language).text);
    }
}
