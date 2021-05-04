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
        public static readonly Dictionary<string, string> tokenStore = new Dictionary<string, string>();

        public static ManualLogSource Log = new ManualLogSource("TranslationUtils");

        private static readonly string translationsPath = "Translations";
        private static readonly string defaultLanguage = "English";
        private static readonly string currentLanguage = Localization.instance.GetSelectedLanguage();

        public static void LoadTranslations(string modPath, ManualLogSource Logger = null)
        {            
            if (Logger == null) Logger = Log;

            var filePath = Path.Combine(modPath, translationsPath, $"{currentLanguage.ToLowerInvariant()}.json");
            if (!File.Exists(filePath))
            {
                Logger.LogWarning($"No translations found for language [{currentLanguage}]. Defaulting to {defaultLanguage}...");

                filePath = Path.Combine(modPath, translationsPath, $"{defaultLanguage.ToLowerInvariant()}.json");
                if (!File.Exists(filePath))
                {
                    Logger.LogWarning($"No translations found for default language [{defaultLanguage}]. Skipping translations...");

                    return;
                }
            }

            using (StreamReader reader = new StreamReader(filePath))
            {
                Traverse tAddWord = Traverse.Create(Localization.instance).Method("AddWord", new Type[] { typeof(string), typeof(string) });
                var localizationPairs = (Dictionary<string, object>)JSON.Parse(reader.ReadToEnd());
                foreach (KeyValuePair<string, object> pairs in localizationPairs)
                {
                    tAddWord.GetValue(new object[] { pairs.Key, pairs.Value.ToString() });
                    tokenStore.Add(pairs.Key, pairs.Value.ToString());
                }
            }
        }
    }
}
