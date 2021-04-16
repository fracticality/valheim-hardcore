using System;
using System.Collections.Generic;
using HarmonyLib;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hardcore.DeathAlerts
{
    public static class DeathAlerts
    {
        
        private static List<string> deathShouts;
        private static Dictionary<string, List<string>> deathAlerts;

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
            Dictionary<string, string> tokens = Traverse.Create(typeof(ValheimLib.Language))
                                                        .Method("GetLanguageDict", new Type[] { typeof(string) })
                                                        .GetValue<Dictionary<string, string>>(new object[]
                                                        {
                                                            "English"
                                                        });

            List<string> damageTypeTokens = new List<string>();
            foreach (string token in tokens.Keys)
            {
                if (token.StartsWith($"hardcore_deathby_{damageType.Substring(2)}"))
                {
                    damageTypeTokens.Add(token);
                }
            }

            deathAlerts.Add(damageType, damageTypeTokens);
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
            Dictionary<string, string> tokens = Traverse.Create(typeof(ValheimLib.Language))
                                                        .Method("GetLanguageDict", new Type[] { typeof(string) })
                                                        .GetValue<Dictionary<string, string>>(new object[]
                                                        {
                                                            "English"
                                                        });
            deathShouts = new List<string>();
            foreach (string token in tokens.Keys)
            {
                if (token.StartsWith("hardcore_death_msg"))
                {
                    deathShouts.Add(token);
                }
            }
        }
        
    }
}
