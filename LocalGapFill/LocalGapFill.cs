using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using ModUtils;
using UnityEngine;
using JotunnLib.Managers;
using System.Collections.Generic;

namespace LocalGapFill
{
    [BepInPlugin(LocalGapFill.UMID, LocalGapFill.ModName, LocalGapFill.Version)]    
    public class LocalGapFill : BaseUnityPlugin
    {
        public const string UMID = "fracticality.valheim.localgapfill";
        public const string Version = "0.1.0";
        public const string ModName = "Localization Gap Fill";
        public static readonly string ModPath = Path.GetDirectoryName(typeof(LocalGapFill).Assembly.Location);

        Harmony _Harmony;
        public static ManualLogSource Log;

        public struct Settings
        {
            public static ConfigEntry<bool> Enabled;
            public static ConfigEntry<bool> OverwriteExisting;            
        }

        private void Awake()
        {
			Log = Logger;            

            Settings.Enabled = Config.Bind("General", "Enabled", true, $"Enable/disable {ModName}'s functionality.");
            Settings.OverwriteExisting = Config.Bind("General", "Overwrite", false, "Overwrite existing localization keys.");            

            if (Settings.Enabled.Value)
            {
                _Harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);                
            }
        }

        private void Start()
        {
            if (Settings.Enabled.Value)
            {
                TranslationUtils.InsertTranslations(ModName, ModPath, Settings.OverwriteExisting.Value);
            }
        }

        private void Update()
        {
            if (Settings.Enabled.Value && Input.GetKeyDown(KeyCode.F4))
            {
                Log.LogInfo("Reloading translations...");
                TranslationUtils.ReloadTranslations(ModName, ModPath, Settings.OverwriteExisting.Value);
            }
        }

        private void OnDestroy()
        {
            if (_Harmony != null) _Harmony.UnpatchAll(null);
        }
    }    
}
