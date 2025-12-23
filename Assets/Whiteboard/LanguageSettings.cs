using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;

public class LanguageSettings : MonoBehaviour
{
    public enum Language { EN, ES }
    public Language currentLanguage = Language.ES;

    [Header("JSON")]
    public TextAsset languageJson;

    List<LangItem> items;

    void Awake()
    {
        LoadJson();
        RefreshAll();
    }

    void LoadJson()
    {
        items = JsonUtility.FromJson<LangFile>(languageJson.text).items;
    }

    public void ToggleLanguage(string lang)
    {
        currentLanguage = (Language) Enum.Parse(typeof(Language), lang);
        RefreshAll();
    }

    public string Translate(string text)
    {
        string result = text;

        foreach (var item in items)
        {
            string from = currentLanguage == Language.EN ? item.ES : item.EN;
            string to = currentLanguage == Language.EN ? item.EN : item.ES;

            if (result.Contains(from))
            {
                result = result.Replace(from, to);
            }
        }

        return result;
    }

    void RefreshAll()
    {
        TMP_Text[] texts = FindObjectsOfType<TMP_Text>(true);

        foreach (var t in texts)
        {
            t.text = Translate(t.text);
        }
    }

    [System.Serializable]
    class LangFile
    {
        public List<LangItem> items;
    }

    [System.Serializable]
    class LangItem
    {
        public string EN;
        public string ES;
    }
}
