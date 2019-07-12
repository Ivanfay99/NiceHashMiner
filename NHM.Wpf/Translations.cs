﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NHM.Wpf
{
    public static class Translations
    {
        [Serializable]
        private class TranslationFile
        {
            public Dictionary<string, string> Languages { get; set; }
            public Dictionary<string, Dictionary<string, string>> Translations { get; set; }
        }

        [Serializable]
        public class Language
        {
            public string Code { get; set; }
            public string Name { get; set; }
        }

        public static event EventHandler LanguageChanged;

        static Translations()
        {
            // always have english
            var enMetaData = new Language
            {
                Code = "en",
                Name = "English",
            };
            AvailableLanguages = new List<Language>();

            // try init
            TryInitTranslations();
            bool flag = false;
            foreach(var lang in AvailableLanguages)
            {
                if (lang.Code == "en")
                {
                    flag = true;
                }
            }
            if (!flag)
            {
                AvailableLanguages.Add(enMetaData);
            }
        }

        private static string _selectedLanguage = "en";
        private static Dictionary<string, Dictionary<string, string>> _entries = new Dictionary<string, Dictionary<string, string>>();
        
        // transform so it is possible to switch from any language
        private static Dictionary<string, Dictionary<string, string>> _transformedEntries = new Dictionary<string, Dictionary<string, string>>();
        public static List<Language> AvailableLanguages = new List<Language>();

        private static void TryInitTranslations()
        {
            // file in binary root path
            const string transFilePath = "translations.json";
            try
            {
                var translations = JsonConvert.DeserializeObject<TranslationFile>(File.ReadAllText(transFilePath, Encoding.UTF8));
                if (translations == null) return;

                if (translations.Languages != null)
                {
                    AvailableLanguages = translations.Languages.Select(pair => new Language { Code = pair.Key, Name = pair.Value }).ToList();
                    foreach (var kvp in AvailableLanguages)
                    {
                        //Logger.Info("Translations", $"Found language: code: {kvp.Code}, name: {kvp.Name}");
                    }
                } 
                if (translations.Translations != null)
                {
                    _entries = translations.Translations;
                    // init transformed entries so we can switch back and forth
                    foreach (var lang in AvailableLanguages)
                    {
                        if (lang.Code == "en") continue;
                        foreach (var enTrans in _entries)
                        {
                            var enKey = enTrans.Key;
                            var enToOther = enTrans.Value;
                            if (enToOther.ContainsKey(lang.Code) == false) continue; 
                            var trKey = enToOther[lang.Code];                            
                            var trToOther = new Dictionary<string, string>();
                            trToOther["en"] = enKey;
                            foreach (var kvp in enToOther)
                            {
                                var langCode = kvp.Key;
                                if (lang.Code == langCode) continue;

                                var translatedText = kvp.Value;
                                trToOther[langCode] = translatedText;
                            }
                            _transformedEntries[trKey] = trToOther;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //Logger.Error("NICEHASH", $"Lang error: {e.Message}");
            }
        }

        public static void SetLanguage(string langCode)
        {
            foreach(var lang in AvailableLanguages)
            {
                if (lang.Code == langCode)
                {
                    _selectedLanguage = lang.Code;
                    LanguageChanged?.Invoke(null, EventArgs.Empty);
                    break;
                }
            }
        }

        public static List<string> GetAvailableLanguagesNames()
        {
            var langNames = new List<string>();
            foreach (var kvp in AvailableLanguages) 
            {
                langNames.Add(kvp.Name);
            }
            return langNames;
        }

        public static string GetLanguageCodeFromName(string name)
        {
            foreach (var kvp in AvailableLanguages)
            {
                if (kvp.Name == name) return kvp.Code;
            }
            return "";
        }

        public static string GetLanguageCodeFromIndex(int index)
        {
            if (index < AvailableLanguages.Count)
            {
                return AvailableLanguages[index].Code;
            }
            return "";
        }

        public static int GetLanguageIndexFromCode(string code)
        {
            for (var i = 0; i < AvailableLanguages.Count; i++)
            {
                var kvp = AvailableLanguages[i];
                if (kvp.Code == code) return i;
            }
            return 0;
        }

        public static int GetCurrentIndex()
        {
            return GetLanguageIndexFromCode(_selectedLanguage);
        }

        // Tr Short for translate
        public static string Tr(string text)
        {
            if (string.IsNullOrEmpty(_selectedLanguage)) return text;

            // if other language search for it
            if (_entries.ContainsKey(text) && _entries[text].ContainsKey(_selectedLanguage))
            {
                return _entries[text][_selectedLanguage];
            }
            var containsTransformed = _transformedEntries.ContainsKey(text);
            // check transformed entry
            if (containsTransformed && _transformedEntries[text].ContainsKey(_selectedLanguage))
            {
                return _transformedEntries[text][_selectedLanguage];
            }
            // check transformed entry en
            if (containsTransformed && _transformedEntries[text].ContainsKey("en"))
            {
                return _transformedEntries[text]["en"];
            }
            // didn't find text with language key so just return the text 
            return text;
        }

        public static string Tr(string text, params object[] args)
        {
            return string.Format(Tr(text), args);
        }
    }
}
