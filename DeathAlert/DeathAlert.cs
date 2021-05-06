using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using ModUtils;
using System.IO;

namespace DeathAlert
{
    [BepInPlugin(DeathAlert.UMID, DeathAlert.ModName, DeathAlert.Version)]
    public class DeathAlert : BaseUnityPlugin
    {
        public const string UMID = "fracticality.valheim.deathalert";
        public const string Version = "0.2.0";
        public const string ModName = "Death Alert";
        public static readonly string ModPath = Path.GetDirectoryName(typeof(DeathAlert).Assembly.Location);
        Harmony _Harmony;
        public static ManualLogSource Log;

        public static HitData lastHitData;
        private static List<string> deathShouts;
        private static Dictionary<string, List<string>> deathAlerts;

        public struct Settings
        {
            public static ConfigEntry<bool> Enabled;
            public static ConfigEntry<bool> EnableShoutOnDeath;
            public static ConfigEntry<bool> EnableAlertOnDeath;
        }

        private void Awake()
        {
			Log = Logger;                        

            Settings.Enabled = Config.Bind("General", "Enabled", true, $"Enable/disable {ModName}'s functionality.");
            Settings.EnableShoutOnDeath = Config.Bind("General", "EnableShoutOnDeath", true, "Enable/disable players shouting random phrases on death.");
            Settings.EnableAlertOnDeath = Config.Bind("General", "EnableAlertOnDeath", true, "Enable/disable server alerts describing a player's death.");

            if (Settings.Enabled.Value)
            {
                _Harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

                TranslationUtils.InsertTranslations(ModName, ModPath);
            }
        }

        private void OnDestroy()
        {
            if (_Harmony != null) _Harmony.UnpatchAll(null);
        }


        public static void ShowDeathAlert(Player player)
        {
            if (Settings.EnableShoutOnDeath.Value)
            {
                string text = Localization.instance.Localize(GetRandomDeathShout());
                Chat.instance.SendText(Talker.Type.Shout, text);
            }

            if (Settings.EnableAlertOnDeath.Value)
            {
                HitData hit = lastHitData;
                string lastAttackerName = "themself";
                Character attacker = hit.GetAttacker();
                if (attacker)
                {
                    lastAttackerName = attacker.GetHoverName();
                }

                Traverse tDamages = Traverse.Create(hit.m_damage);
                List<string> damageFieldNames = tDamages.Fields();

                float max = 0.0f;
                string highestDamageType = "m_damage";
                foreach (string fieldName in damageFieldNames)
                {
                    float value = tDamages.Field<float>(fieldName).Value;
                    if (value > max)
                    {
                        max = value;
                        highestDamageType = fieldName;
                    }
                }

                if (highestDamageType == "m_damage" && player.IsSwiming())
                {
                    highestDamageType = "m_drowning";
                }

                string damageTypeString = Localization.instance.Localize(GetRandomDeathAlert(highestDamageType));

                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ShowMessage", new object[]
                {
                    (int)MessageHud.MessageType.Center,
                    Localization.instance.Localize("$deathalert_killed_by_msg_peers", player.GetPlayerName(), lastAttackerName, damageTypeString)
                });
            }
        }
        public static string GetRandomDeathAlert(string damageType)
        {
            if (deathAlerts == null)
            {
                PopulateDeathAlerts();
            }

            if (deathAlerts.TryGetValue(damageType, out List<string> alerts))
            {
                if (alerts.Count > 0)
                {
                    int random = UnityEngine.Random.Range(0, alerts.Count);
                    return "$" + alerts[random];
                }
            }

            return string.Empty;
        }

        private static void PopulateDeathAlerts()
        {
            deathAlerts = new Dictionary<string, List<string>>();

            Traverse tDamageTypes = Traverse.Create(typeof(HitData.DamageTypes));
            List<string> damageTypes = tDamageTypes.Fields();

            foreach (string damageType in damageTypes)
            {
                PopulateDeathAlerts(damageType);
            }
        }

        private static void PopulateDeathAlerts(string damageType)
        {
            Dictionary<string, string> tokens = TranslationUtils.GetTokens(ModName);
            if (tokens != null)
            {
                List<string> damageTypeTokens = new List<string>();
                foreach (string token in tokens.Keys)
                {
                    if (token.StartsWith($"deathalert_deathby_{damageType.Substring(2)}"))
                    {
                        damageTypeTokens.Add(token);
                    }
                }

                deathAlerts.Add(damageType, damageTypeTokens);
            }            
        }

        public static string GetRandomDeathShout()
        {
            if (deathShouts == null)
            {
                PopulateDeathShouts();
            }

            if (deathShouts.Count > 0)
            {
                int random = UnityEngine.Random.Range(0, deathShouts.Count);
                return "$" + deathShouts[random];
            }

            return string.Empty;
        }

        private static void PopulateDeathShouts()
        {
            Dictionary<string, string> tokens = TranslationUtils.GetTokens(ModName);
            if (tokens != null)
            {
                deathShouts = new List<string>();
                foreach (string token in tokens.Keys)
                {
                    if (token.StartsWith("deathalert_death_msg"))
                    {
                        deathShouts.Add(token);
                    }
                }
            }            
        }
    }    
}
