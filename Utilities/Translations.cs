using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fastJSON;
using HarmonyLib;

namespace Hardcore.Utilities
{
    class Translations
    {
        public static readonly Dictionary<string, string> tokenStore = new Dictionary<string, string>();

        private static readonly string translationsPath = Path.Combine(Hardcore.ModPath, "Translations");
        private static readonly string defaultLanguage = "English";
        private static readonly string currentLanguage = Localization.instance.GetSelectedLanguage();        

        public static void LoadTranslations()
        {
            var filePath = Path.Combine(translationsPath, $"{currentLanguage.ToLowerInvariant()}.json");
            if (!File.Exists(filePath))
            {
                Hardcore.Log.LogWarning($"No Hardcore translations found for language [{currentLanguage}]. Defaulting to {defaultLanguage}...");

                filePath = Path.Combine(translationsPath, $"{defaultLanguage.ToLowerInvariant()}.json");
                if (!File.Exists(filePath))
                {
                    Hardcore.Log.LogWarning($"No Hardcore translations found for default language [{defaultLanguage}]. Skipping translations...");

                    return;
                }
            }

            using (StreamReader reader = new StreamReader(filePath))
            {
                Traverse tAddWord = Traverse.Create(Localization.instance).Method("AddWord", new Type[] { typeof(string), typeof(string) });
                var localizationPairs = (Dictionary<string, object>)JSON.Parse(reader.ReadToEnd());
                foreach(KeyValuePair<string, object> pairs in localizationPairs)
                {
                    tAddWord.GetValue(new object[] { pairs.Key, pairs.Value.ToString() });
                    tokenStore.Add(pairs.Key, pairs.Value.ToString());
                }
            }
        }
    }
}
