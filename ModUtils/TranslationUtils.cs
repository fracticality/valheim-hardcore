using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BepInEx.Logging;
using fastJSON;
using HarmonyLib;

namespace ModUtils
{
    public static class TranslationUtils
    {
        private static readonly Dictionary<string, Dictionary<string, string>> tokenStore = new Dictionary<string, Dictionary<string, string>>();

        public static ManualLogSource Log = new ManualLogSource("TranslationUtils");

        private static readonly string translationsPath = "Translations";
        private static readonly string defaultLanguage = "English";
        private static readonly string currentLanguage = Localization.instance.GetSelectedLanguage();

        public static Dictionary<string, string> GetTokens(string modName)
        {
            if (!tokenStore.ContainsKey(modName))
            {
                Log.LogWarning($"No translation tokens found for mod [{modName}]. Have you loaded them?");

                return null;
            }

            tokenStore.TryGetValue(modName, out var tokens);

            return tokens;
        }

        public static Dictionary<string, object> ParseTranslationFile(string modPath)
        {            
            var filePath = Path.Combine(modPath, translationsPath, $"{currentLanguage.ToLowerInvariant()}.json");
            if (!File.Exists(filePath))
            {
                Log.LogWarning($"No translations found for language [{currentLanguage}]. Defaulting to {defaultLanguage}...");

                filePath = Path.Combine(modPath, translationsPath, $"{defaultLanguage.ToLowerInvariant()}.json");
                if (!File.Exists(filePath))
                {
                    Log.LogWarning($"No translations found for default language [{defaultLanguage}]. Skipping translations...");

                    return null;
                }
            }

            using (StreamReader reader = new StreamReader(filePath))
            {                
                var localizationPairs = (Dictionary<string, object>)JSON.Parse(reader.ReadToEnd());

                return localizationPairs;
            }
        }

        public static void LoadTranslations(string modName, string modPath)
        {            
            Dictionary<string, object> localizationPairs = ParseTranslationFile(modPath);
            if (localizationPairs == null)
            {
                return;
            }

            Dictionary<string, string> tokens = new Dictionary<string, string>();
            foreach (KeyValuePair<string, object> pairs in localizationPairs)
            {                
                tokens.Add(pairs.Key, pairs.Value.ToString());
            }

            tokenStore.Add(modName, tokens);
        }

        public static void InsertTranslations(string modName, string modPath, bool overwrite = false)
        {            
            if (!tokenStore.ContainsKey(modName))
            {
                LoadTranslations(modName, modPath);
            }

            if (tokenStore.TryGetValue(modName, out Dictionary<string, string> tokens)) 
            {
                Traverse AddWordFunc = Traverse.Create(Localization.instance).Method("AddWord", new Type[] { typeof(string), typeof(string) });
                Dictionary<string, string> m_translations = Traverse.Create(Localization.instance).Field<Dictionary<string, string>>("m_translations").Value;
                foreach (KeyValuePair<string, string> pair in tokens)
                {
                    if (overwrite || !m_translations.ContainsKey(pair.Key))
                    {
                        AddWordFunc.GetValue(new object[] { pair.Key, pair.Value });
                    }
                }
            }            
        }

        public static void ReloadTranslations(string modName, string modPath, bool overwrite = false)
        {
            tokenStore.Remove(modName);
            InsertTranslations(modName, modPath, overwrite);
        }
    }
}
